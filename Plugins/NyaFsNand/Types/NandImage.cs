using System;
using System.Collections.Generic;

namespace NyaFsNand.Types
{
    /// <summary>
    /// A raw NAND chip dump: page data interleaved with the spare (OOB) area.
    ///
    /// There is no magic to rely on, so the geometry is recovered from the dump itself:
    /// candidate page/OOB sizes are scored by self-validating structures (UBI erase-counter
    /// headers with a valid CRC and a volume identifier header at the declared offset,
    /// UBIFS nodes with a valid CRC, well-known container magics) and by how "spare-like"
    /// the trailing bytes of every page look.
    ///
    /// Used areas are then exposed as block-aligned regions with the OOB stripped, so the
    /// regular plugin pipeline can detect and read the filesystems inside them.
    /// </summary>
    public class NandImage
    {
        public const uint UbiEcMagic = 0x55424923;     // "UBI#"
        public const uint UbiVidMagic = 0x55424921;    // "UBI!"
        public const uint UbifsNodeMagic = 0x06101831;
        public const uint UbiLayoutVolumeId = 0x7FFFEFFF;

        private const int MinDumpSize = 0x10000;
        private const int MinBlockSize = 0x4000;
        private const int MaxBlockSize = 0x200000;
        private const int MaxSamplePages = 4096;
        private const double MinSpareFraction = 0.45;

        private static readonly int[] PageSizes = { 512, 1024, 2048, 4096, 8192 };
        private static readonly int[] OobSizes = { 16, 32, 64, 128, 224, 256, 436, 512, 1024 };

        public readonly byte[] Raw;
        public readonly long Base;
        public readonly long Length;
        public readonly NandGeometry Geometry;
        public readonly NandRegion[] Regions;

        private NandImage(byte[] Raw, long Base, long Length, NandGeometry Geometry, NandRegion[] Regions)
        {
            this.Raw = Raw;
            this.Base = Base;
            this.Length = Length;
            this.Geometry = Geometry;
            this.Regions = Regions;
        }

        /// <summary>True when the image has no spare area (logical MTD dump, not a raw chip dump).</summary>
        public bool Logical => Geometry.OobSize == 0;

        /// <summary>
        /// Number of eraseblocks carrying a bad-block marker in the spare area, or -1 when
        /// the spare layout does not follow the usual marker convention (many controllers
        /// keep ECC/zeros in the first spare bytes of every written page) and when the image
        /// has no spare area at all.
        /// </summary>
        public long BadBlocks
        {
            get
            {
                if (Geometry.OobSize <= 0)
                    return -1;

                var BlockCount = Length / Geometry.RawBlockSize;
                if (BlockCount <= 0)
                    return -1;

                var Marked = 0L;
                for (long B = 0; B < BlockCount; B++)
                {
                    var Oob = Base + B * Geometry.RawBlockSize + Geometry.PageSize;
                    if ((Raw[Oob] != 0xFF) || (Raw[Oob + 1] != 0xFF))
                        Marked++;
                }

                return (Marked * 20 > BlockCount) ? -1 : Marked;
            }
        }

        /// <summary>
        /// Recover the geometry. Cheap enough to be used by content detection: it only looks
        /// at page/eraseblock starts and validates structures, it does not build regions.
        /// </summary>
        public static NandGeometry DetectGeometry(byte[] Raw, long Base, long Length, string SourceName) =>
            DetectGeometryInternal(Raw, Base, Length, SourceName, false);

        /// <summary>
        /// Parse a raw NAND dump or a logical (no OOB) MTD image: geometry plus the list of
        /// used block-aligned regions.
        /// <paramref name="AllowWithoutSpare"/> additionally accepts ambiguous data-only
        /// images; it is only used for explicit loading.
        /// </summary>
        public static NandImage TryParse(byte[] Raw, long Base, long Length, string SourceName = null, bool AllowWithoutSpare = false)
        {
            var Geometry = DetectGeometryInternal(Raw, Base, Length, SourceName, AllowWithoutSpare);
            if (Geometry == null)
                return null;

            var Regions = BuildRegions(Raw, Base, Length, Geometry);
            if (Regions.Length == 0)
                return null;

            return new NandImage(Raw, Base, Length, Geometry, Regions);
        }

        private static NandGeometry DetectGeometryInternal(byte[] Raw, long Base, long Length, string SourceName, bool AllowWithoutSpare)
        {
            // 1) Raw chip dump: page data interleaved with the spare area.
            var Candidate = FindGeometry(Raw, Base, Length, SourceName, false);
            if (Candidate != null)
            {
                var PagesPerBlock = RawBlockPages(Candidate, Length);
                if (PagesPerBlock > 0)
                    return Candidate.ToGeometry(PagesPerBlock);
            }

            // 2) Logical MTD image (no OOB): a full chip/partition set dumped contiguously.
            var Logical = DetectLogicalGeometry(Raw, Base, Length, SourceName, false);
            if (Logical != null)
                return Logical;

            if (!AllowWithoutSpare)
                return null;

            // 3) Explicitly forced loading: accept a leaky best-effort match.
            Candidate = FindGeometry(Raw, Base, Length, SourceName, true);
            if (Candidate != null)
            {
                var PagesPerBlock = RawBlockPages(Candidate, Length);
                if (PagesPerBlock > 0)
                    return Candidate.ToGeometry(PagesPerBlock);
            }

            return DetectLogicalGeometry(Raw, Base, Length, SourceName, true);
        }


        /// <summary>Copy a logical (OOB-stripped) range out of the dump.</summary>
        public byte[] ReadLogical(long Offset, long Size)
        {
            var FirstPage = Offset / Geometry.PageSize;
            var PageCount = Size / Geometry.PageSize;
            var Result = new byte[PageCount * Geometry.PageSize];
            var Stride = Geometry.RawStride;

            for (long i = 0; i < PageCount; i++)
                Array.Copy(Raw, Base + (FirstPage + i) * Stride, Result, i * Geometry.PageSize, Geometry.PageSize);

            return Result;
        }

        /// <summary>
        /// Scan every UBI region: PEB count, erase counters, image sequence number and the
        /// volume table (volume names/types/sizes) plus the number of mapped LEBs per volume.
        /// </summary>
        public List<NandUbiDevice> ReadUbiDevices()
        {
            var Result = new List<NandUbiDevice>();
            var BlockSize = Geometry.BlockSize;
            var RawBlock = Geometry.RawBlockSize;

            foreach (var Region in Regions)
            {
                if (Region.Kind != "ubi")
                    continue;

                var Device = new NandUbiDevice { Offset = Region.Offset };
                var FirstBlock = Region.Offset / BlockSize;
                var BlockCount = Region.Size / BlockSize;
                var EcSum = 0.0;
                var EcCount = 0;
                var Seq = -1L;
                var Volumes = new Dictionary<uint, NandUbiVolume>();
                var VidCounts = new Dictionary<uint, int>();
                var Lebs = new Dictionary<uint, List<NandLebMapping>>();

                for (long I = 0; I < BlockCount; I++)
                {
                    var RawOff = Base + (FirstBlock + I) * RawBlock;

                    if (!Match(Raw, RawOff, "UBI#"))
                    {
                        if (IsAllFF(Raw, RawOff, RawBlock))
                            Device.ErasedPebs++;
                        continue;
                    }

                    if (!EcCrcOk(Raw, RawOff))
                    {
                        Device.BadEcPebs++;
                        continue;
                    }

                    Device.Pebs++;

                    var Ec = (long)ReadBE64(Raw, RawOff + 0x08);
                    if ((EcCount == 0) || (Ec < Device.MinEc)) Device.MinEc = Ec;
                    if ((EcCount == 0) || (Ec > Device.MaxEc)) Device.MaxEc = Ec;
                    EcSum += Ec;
                    EcCount++;

                    var ImageSeq = ReadBE32(Raw, RawOff + 0x18);
                    if (Seq < 0)
                        Seq = ImageSeq;

                    var VidOff = ReadBE32(Raw, RawOff + 0x10);
                    var DataOff = ReadBE32(Raw, RawOff + 0x14);
                    var VidPos = RawOff + (VidOff / Geometry.PageSize) * Geometry.RawStride + (VidOff % Geometry.PageSize);

                    if (!Match(Raw, VidPos, "UBI!"))
                        continue;

                    var VolumeId = ReadBE32(Raw, VidPos + 0x08);
                    if (VolumeId == UbiLayoutVolumeId)
                    {
                        Device.LayoutPebs++;

                        var TablePos = RawOff + (DataOff / Geometry.PageSize) * Geometry.RawStride + (DataOff % Geometry.PageSize);
                        ReadVolumeTable(Raw, TablePos, Volumes);
                        continue;
                    }

                    VidCounts[VolumeId] = (VidCounts.TryGetValue(VolumeId, out var N) ? N : 0) + 1;

                    if (!Lebs.TryGetValue(VolumeId, out var List))
                        Lebs[VolumeId] = List = new List<NandLebMapping>();

                    List.Add(new NandLebMapping
                    {
                        LNum = ReadBE32(Raw, VidPos + 0x0C),
                        Block = (int)(FirstBlock + I),
                        DataSize = ReadBE32(Raw, VidPos + 0x14),
                        Sqnum = ReadBE64(Raw, VidPos + 0x28)
                    });
                }

                Device.ImageSeq = (Seq >= 0) ? Seq : 0;
                Device.MeanEc = (EcCount > 0) ? EcSum / EcCount : 0;

                foreach (var V in VidCounts)
                {
                    if (!Volumes.ContainsKey(V.Key))
                        Volumes[V.Key] = new NandUbiVolume { Id = V.Key };
                }

                foreach (var V in Volumes.Values)
                {
                    if (VidCounts.TryGetValue(V.Id, out var N))
                        V.MappedLebs = N;

                    if (Lebs.TryGetValue(V.Id, out var List))
                    {
                        List.Sort((A, B) => A.LNum.CompareTo(B.LNum));
                        V.Lebs.AddRange(List);
                    }
                }

                Device.Volumes.AddRange(Volumes.Values);
                Device.Volumes.Sort((A, B) => A.Id.CompareTo(B.Id));
                Result.Add(Device);
            }

            return Result;
        }

        /// <summary>Per-eraseblock state of the whole image (used by `nandinfo --blocks`).</summary>
        public List<NandBlock> ReadBlocks()
        {
            var Result = new List<NandBlock>();
            var Count = Length / Geometry.RawBlockSize;

            for (int I = 0; I < Count; I++)
            {
                var RawOff = Base + (long)I * Geometry.RawBlockSize;
                var Info = new NandBlock
                {
                    Index = I,
                    RawOffset = RawOff,
                    LogicalOffset = (long)I * Geometry.BlockSize
                };

                for (int P = 0; P < Geometry.PagesPerBlock; P++)
                {
                    if (!IsAllFF(Raw, RawOff + (long)P * Geometry.RawStride, Geometry.PageSize))
                    {
                        Info.Used = true;
                        break;
                    }
                }

                if (Match(Raw, RawOff, "UBI#"))
                {
                    Info.Ubi = true;

                    if (EcCrcOk(Raw, RawOff))
                    {
                        Info.EcOk = true;
                        Info.Ec = (long)ReadBE64(Raw, RawOff + 0x08);

                        var VidOff = ReadBE32(Raw, RawOff + 0x10);
                        var VidPos = RawOff + (VidOff / Geometry.PageSize) * Geometry.RawStride + (VidOff % Geometry.PageSize);

                        if (Match(Raw, VidPos, "UBI!"))
                        {
                            Info.HasVid = true;
                            Info.VolumeId = ReadBE32(Raw, VidPos + 0x08);
                            Info.LNum = ReadBE32(Raw, VidPos + 0x0C);
                            Info.Layout = Info.VolumeId == UbiLayoutVolumeId;
                        }
                    }
                }
                else if (Info.Used)
                {
                    Info.Container = DetectContainer(Raw, RawOff, Geometry);
                }

                Result.Add(Info);
            }

            return Result;
        }

        private static void ReadVolumeTable(byte[] Raw, long TablePos, Dictionary<uint, NandUbiVolume> Volumes)
        {
            for (uint I = 0; I < 128; I++)
            {
                var Off = TablePos + I * 0xAC;
                if (Off + 0xAC > Raw.Length)
                    break;

                var Stored = ReadBE32(Raw, Off + 0xA8);
                if (Stored == 0xFFFFFFFF)
                    break;
                if (Stored != Crc32(Raw, Off, 0xA8))
                    continue;

                var Reserved = ReadBE32(Raw, Off + 0x00);

                // Empty slots are valid records with reserved_pebs == 0 (not 0xFF).
                if (Reserved == 0)
                    continue;

                var NameLen = ReadBE16(Raw, Off + 0x0E);
                if (NameLen > 127)
                    continue;

                var Name = System.Text.Encoding.UTF8.GetString(Raw, (int)(Off + 0x10), (int)NameLen);

                Volumes[I] = new NandUbiVolume
                {
                    Id = I,
                    Name = Name,
                    Type = Raw[Off + 0x0C],
                    ReservedPebs = Reserved
                };
            }
        }

        // ---- geometry detection ----

        private class Candidate
        {
            public int Page;
            public int Oob;
            public long Pages;
            public int EcValid;
            public int EcGeometry;
            public int Magic;
            public int UbifsCrc;
            public long SpareZero;
            public long SpareTotal;
            public long EcStep;

            public double SpareFraction => (SpareTotal > 0) ? (double)SpareZero / SpareTotal : 0;

            public NandGeometry ToGeometry(int PagesPerBlock) => new NandGeometry(Page, Oob, PagesPerBlock);
        }

        private static Candidate FindGeometry(byte[] Raw, long Base, long Length, string SourceName, bool AllowWithoutSpare)
        {
            if ((Raw == null) || (Base < 0) || (Length < MinDumpSize) || (Base + Length > Raw.Length))
                return null;

            var Candidates = new List<Candidate>();

            foreach (var Page in PageSizes)
            {
                foreach (var Oob in OobSizes)
                    AddCandidate(Candidates, Raw, Base, Length, Page, Oob);

                if (AllowWithoutSpare)
                    AddCandidate(Candidates, Raw, Base, Length, Page, 0);
            }

            // UBI is self-describing: the erase-counter header at a page start declares the
            // VID/data offsets, so a real UBI dump can be recognised with high confidence.
            Candidate BestUbi = null;
            foreach (var C in Candidates)
            {
                if (C.EcGeometry < 2)
                    continue;

                // A plain (OOB-stripped) UBI image also passes the probe; only a dump that
                // actually carries a spare area is auto-detected as raw NAND.
                if (!AllowWithoutSpare && (C.Oob <= 0))
                    continue;

                var PagesPerBlock = RawBlockPages(C, Length);
                if (PagesPerBlock <= 0)
                    continue;

                if (IsBetterUbi(C, BestUbi))
                    BestUbi = C;
            }

            if (BestUbi != null)
                return BestUbi;

            // UBI erase-counter headers are present but no candidate geometry can explain
            // them: this is a (possibly OOB-stripped) UBI image, not a raw NAND dump.
            foreach (var C in Candidates)
            {
                if (C.EcValid > 0)
                    return null;
            }


            // No UBI: the dump must have a spare area that really looks like one, otherwise
            // any .bin whose size happens to divide by a page+oob stride could be mistaken
            // for a NAND dump. Specialised file extensions relax the "recognisable content"
            // requirement (e.g. YAFFS2 has no magic at all).
            var TrustedName = HasTrustedName(SourceName);
            var MinMagic = TrustedName ? 0 : 2;

            Candidate BestData = null;
            foreach (var C in Candidates)
            {
                if (C.Oob <= 0)
                    continue;
                if (C.SpareFraction < MinSpareFraction)
                    continue;
                if ((C.Magic + C.UbifsCrc) < MinMagic)
                    continue;
                if (RawBlockPages(C, Length) <= 0)
                    continue;

                if (IsBetterData(C, BestData))
                    BestData = C;
            }

            if ((BestData != null) || !AllowWithoutSpare)
                return BestData;

            // Explicitly forced loading also accepts page-data-only dumps (no spare area).
            foreach (var C in Candidates)
            {
                if ((C.Magic + C.UbifsCrc) < 2)
                    continue;
                if (RawBlockPages(C, Length) <= 0)
                    continue;

                if (IsBetterData(C, BestData))
                    BestData = C;
            }

            return BestData;
        }

        private static bool HasTrustedName(string SourceName)
        {
            if (String.IsNullOrEmpty(SourceName))
                return false;

            var Extension = System.IO.Path.GetExtension(SourceName);
            if (String.IsNullOrEmpty(Extension))
                return false;

            return String.Equals(Extension, ".nand", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(Extension, ".dump", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(Extension, ".mtd", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBetterUbi(Candidate C, Candidate Best)
        {
            if (Best == null)
                return true;

            if (C.EcGeometry != Best.EcGeometry)
                return C.EcGeometry > Best.EcGeometry;
            if (C.SpareFraction != Best.SpareFraction)
                return C.SpareFraction > Best.SpareFraction;

            return C.Page > Best.Page;
        }

        private static bool IsBetterData(Candidate C, Candidate Best)
        {
            if (Best == null)
                return true;

            var Rate = C.Magic + C.UbifsCrc;
            var BestRate = Best.Magic + Best.UbifsCrc;

            if (Rate != BestRate)
                return Rate > BestRate;
            if (C.SpareFraction != Best.SpareFraction)
                return C.SpareFraction > Best.SpareFraction;
            if (C.Page != Best.Page)
                return C.Page > Best.Page;

            return false;
        }

        // ---- logical MTD detection (full chip dump without OOB/ECC) ----
        //
        // A logical image is a contiguous dump of the whole flash (or of a partition set):
        // eraseblocks follow each other with no spare area. There is no magic either, so the
        // eraseblock size is the candidate at which the most self-validating structures sit
        // exactly on eraseblock boundaries (UBI erase-counter headers are the strongest
        // signal, as they repeat once per PEB).

        private static readonly long[] LogicalBlockSizes =
        {
            0x8000, 0x10000, 0x20000, 0x40000, 0x80000, 0x100000, 0x200000, 0x400000
        };

        private class LogicalStats
        {
            public long Blocks;
            public int Ubi;
            public int LayoutZero;
            public int Android;
            public int SquashFs;
            public int UImage;
            public int Fit;
            public int Ext;
            public long FirstUbiBlock = -1;

            public int Structures => Ubi + Android + SquashFs + UImage + Fit + Ext;

            public int Kinds =>
                ((Ubi > 0) ? 1 : 0) + ((Android > 0) ? 1 : 0) + ((SquashFs > 0) ? 1 : 0) +
                ((UImage > 0) ? 1 : 0) + ((Fit > 0) ? 1 : 0) + ((Ext > 0) ? 1 : 0);
        }

        private class LogicalCandidate
        {
            public long BlockSize;
            public LogicalStats Stats;
        }

        private static NandGeometry DetectLogicalGeometry(byte[] Raw, long Base, long Length, string SourceName, bool Force)
        {
            if ((Raw == null) || (Base < 0) || (Length < MinDumpSize) || (Base + Length > Raw.Length))
                return null;

            LogicalCandidate Best = null;
            foreach (var BlockSize in LogicalBlockSizes)
            {
                var Stats = ScanBlockStarts(Raw, Base, Length, BlockSize);
                if ((Stats == null) || (Stats.Structures == 0))
                    continue;

                if ((Best == null) || IsBetterLogical(BlockSize, Stats, Best))
                    Best = new LogicalCandidate { BlockSize = BlockSize, Stats = Stats };
            }

            if (Best == null)
                return null;

            var S = Best.Stats;
            var Trusted = HasTrustedName(SourceName);

            bool Accepted;
            if (Force)
                Accepted = true;
            else if (Trusted)
                Accepted = (S.Ubi > 0) || (S.Structures >= 2);
            else
                Accepted = (S.Blocks >= 8) && (S.Structures >= 3) && (S.Ubi >= 2) &&
                    ((S.FirstUbiBlock > 0) || (S.LayoutZero >= 2) || (S.Kinds >= 2));

            if (!Accepted)
                return null;

            var Page = (Best.BlockSize % 2048 == 0) ? 2048 :
                ((Best.BlockSize % 4096 == 0) ? 4096 : 512);

            if ((Page <= 0) || ((Best.BlockSize % Page) != 0))
                return null;

            return new NandGeometry(Page, 0, (int)(Best.BlockSize / Page));
        }

        private static bool IsBetterLogical(long BlockSize, LogicalStats Stats, LogicalCandidate Best)
        {
            if (Stats.Ubi != Best.Stats.Ubi)
                return Stats.Ubi > Best.Stats.Ubi;
            if (Stats.Structures != Best.Stats.Structures)
                return Stats.Structures > Best.Stats.Structures;

            // Smaller divisors of the real eraseblock size see the same structures; the
            // largest one with the best score is the real eraseblock size.
            return BlockSize > Best.BlockSize;
        }

        private static LogicalStats ScanBlockStarts(byte[] Raw, long Base, long Length, long BlockSize)
        {
            if ((BlockSize <= 0) || ((Length % BlockSize) != 0))
                return null;

            var Blocks = Length / BlockSize;
            var Stats = new LogicalStats { Blocks = Blocks };

            for (long B = 0; B < Blocks; B++)
            {
                var Off = Base + B * BlockSize;

                if (Match(Raw, Off, "UBI#") && EcCrcOk(Raw, Off))
                {
                    Stats.Ubi++;
                    if (Stats.FirstUbiBlock < 0)
                        Stats.FirstUbiBlock = B;

                    var VidOff = ReadBE32(Raw, Off + 0x10);
                    if ((VidOff > 0) && Match(Raw, Off + VidOff, "UBI!"))
                    {
                        var VolumeId = ReadBE32(Raw, Off + VidOff + 0x08);
                        var LNum = ReadBE32(Raw, Off + VidOff + 0x0C);
                        if ((VolumeId == UbiLayoutVolumeId) && (LNum == 0))
                            Stats.LayoutZero++;
                    }
                }
                else if (Match(Raw, Off, "ANDROID!"))
                    Stats.Android++;
                else if (Match(Raw, Off, "hsqs") || Match(Raw, Off, "sqsh"))
                    Stats.SquashFs++;
                else if (MatchBytes(Raw, Off, 0x27, 0x05, 0x19, 0x56))
                    Stats.UImage++;
                else if (MatchBytes(Raw, Off, 0xd0, 0x0d, 0xfe, 0xed))
                    Stats.Fit++;
                else if (MatchBytes(Raw, Off + 0x438, 0x53, 0xef))
                    Stats.Ext++;
            }

            return Stats;
        }


        private static void AddCandidate(List<Candidate> Candidates, byte[] Raw, long Base, long Length, int Page, int Oob)
        {
            var Stride = Page + Oob;

            if ((Stride <= 0) || ((Length % Stride) != 0))
                return;

            var Pages = Length / Stride;
            if (Pages <= 0)
                return;

            var C = new Candidate { Page = Page, Oob = Oob, Pages = Pages };
            var SampleStep = Math.Max(1L, Pages / MaxSamplePages);
            var LastEcPage = -1L;
            var Steps = new Dictionary<long, int>();

            for (long P = 0; P < Pages; P++)
            {
                var Off = Base + P * Stride;

                if (IsUbiEc(Raw, Off, Page, Oob, out var GeometryOk, out var LayoutPage, out var Step))
                {
                    C.EcValid++;
                    if (GeometryOk)
                        C.EcGeometry++;

                    if (LastEcPage >= 0)
                    {
                        var Delta = P - LastEcPage;
                        if ((Delta > 0) && (Delta <= 1024))
                            Steps[Delta] = Steps.TryGetValue(Delta, out var N) ? N + 1 : 1;
                    }
                    LastEcPage = P;
                }
                else
                {
                    C.Magic += PageMagic(Raw, Off);
                }

                if ((Off + 24 <= Base + Length) &&
                    (ReadLE32(Raw, Off) == UbifsNodeMagic) && UbifsNodeCrcOk(Raw, Off, Base + Length))
                    C.UbifsCrc++;

                if ((SampleStep > 0) && ((P % SampleStep) == 0) && (Oob > 0))
                {
                    if (!IsAllFF(Raw, Off, Page))
                    {
                        C.SpareZero += CountZeroFF(Raw, Off + Page, Oob);
                        C.SpareTotal += Oob;
                    }
                }
            }

            if (Steps.Count > 0)
            {
                var BestDelta = 0L;
                var BestCount = 0;
                foreach (var S in Steps)
                {
                    if (S.Value > BestCount)
                    {
                        BestCount = S.Value;
                        BestDelta = S.Key;
                    }
                }

                C.EcStep = BestDelta;
            }

            Candidates.Add(C);
        }

        private static int RawBlockPages(Candidate C, long Length)
        {
            long PagesPerBlock;

            if (C.EcStep > 0)
                PagesPerBlock = C.EcStep;
            else
            {
                PagesPerBlock = 0;
                foreach (var BlockSize in new[] { 0x20000, 0x40000, 0x10000, 0x80000, 0x100000 })
                {
                    if ((BlockSize % C.Page) != 0)
                        continue;

                    var Pages = BlockSize / C.Page;
                    if ((Length % ((long)Pages * (C.Page + C.Oob))) == 0)
                    {
                        PagesPerBlock = Pages;
                        break;
                    }
                }
            }

            if (PagesPerBlock <= 0)
                return 0;

            var Size = PagesPerBlock * C.Page;
            if ((Size < MinBlockSize) || (Size > MaxBlockSize))
                return 0;

            if ((Length % (PagesPerBlock * (C.Page + C.Oob))) != 0)
                return 0;

            return (int)PagesPerBlock;
        }

        private static bool IsUbiEc(byte[] D, long Off, int Page, int Oob, out bool GeometryOk, out bool LayoutPage, out long ImageSeq)
        {
            GeometryOk = false;
            LayoutPage = false;
            ImageSeq = 0;

            if (!Match(D, Off, "UBI#") || !EcCrcOk(D, Off))
                return false;

            var VidOff = ReadBE32(D, Off + 0x10);
            var DataOff = ReadBE32(D, Off + 0x14);
            ImageSeq = ReadBE32(D, Off + 0x18);

            var BlockSize = (long)Page * 512;

            if ((VidOff > 0) && (DataOff > VidOff) && (DataOff < BlockSize) &&
                ((VidOff % Page) == 0) && ((DataOff % Page) == 0))
            {
                var VidPos = Off + (VidOff / Page) * (Page + Oob) + (VidOff % Page);
                if (Match(D, VidPos, "UBI!"))
                {
                    GeometryOk = true;

                    var VolumeId = ReadBE32(D, VidPos + 0x08);
                    var LNum = ReadBE32(D, VidPos + 0x0C);
                    LayoutPage = (VolumeId == UbiLayoutVolumeId) && (LNum == 0);
                }
            }

            return true;
        }

        private static bool EcCrcOk(byte[] D, long Off)
        {
            if (Off + 0x40 > D.Length)
                return false;

            var Stored = ReadBE32(D, Off + 0x3C);
            return Stored == Crc32(D, Off, 0x3C);
        }

        // ---- region discovery ----

        private class BlockInfo
        {
            public bool Used;
            public bool Ubi;
            public bool EcOk;
            public bool LayoutStart;
            public long ImageSeq;
        }

        private static NandRegion[] BuildRegions(byte[] Raw, long Base, long Length, NandGeometry G)
        {
            var Stride = G.RawStride;
            var RawBlock = G.RawBlockSize;
            var TotalBlocks = Length / RawBlock;
            var Blocks = new BlockInfo[TotalBlocks];

            for (long B = 0; B < TotalBlocks; B++)
            {
                var RawOff = Base + B * RawBlock;
                var Info = new BlockInfo
                {
                    Ubi = Match(Raw, RawOff, "UBI#")
                };

                for (int P = 0; P < G.PagesPerBlock; P++)
                {
                    if (!IsAllFF(Raw, RawOff + P * Stride, G.PageSize))
                    {
                        Info.Used = true;
                        break;
                    }
                }

                if (IsUbiEc(Raw, RawOff, G.PageSize, G.OobSize, out var GeometryOk, out var LayoutStart, out var ImageSeq))
                {
                    Info.EcOk = true;
                    Info.LayoutStart = GeometryOk && LayoutStart;
                    Info.ImageSeq = ImageSeq;
                }

                Blocks[B] = Info;
            }

            var Result = new List<NandRegion>();
            BuildUbiRegions(Raw, Base, G, Blocks, Result);
            BuildDataRegions(Raw, Base, G, Blocks, Result);

            Result.Sort((A, B) => A.Offset.CompareTo(B.Offset));
            return Result.ToArray();
        }

        /// <summary>A single erased block inside a used area is tolerated as a gap.</summary>
        private const int GapTolerance = 1;

        private static void BuildUbiRegions(byte[] Raw, long Base, NandGeometry G, BlockInfo[] Blocks, List<NandRegion> Result)
        {
            var RawBlock = G.RawBlockSize;
            long Start = -1;
            long Last = -1;
            long Seq = -1;
            long Gap = 0;
            var SeenLayout = false;

            for (long B = 0; B <= Blocks.Length; B++)
            {
                var Info = (B < Blocks.Length) ? Blocks[B] : null;
                var IsUbi = (Info != null) && Info.Ubi;

                if (!IsUbi)
                {
                    if (Start >= 0)
                    {
                        Gap = (Info == null) ? GapTolerance + 1L : Gap + 1;
                        if (Gap > GapTolerance)
                        {
                            Result.Add(MakeRegion("ubi", null, Start, Last + 1, G));
                            Start = -1;
                            Gap = 0;
                        }
                    }
                    continue;
                }

                // Back-to-back UBI devices share one eraseblock run (e.g. config + nvdata):
                // a new image sequence number or a second layout volume starts a new device.
                var NewDevice = (Start >= 0) &&
                    ((Info.EcOk && (Seq >= 0) && (Info.ImageSeq != Seq)) ||
                     (Info.LayoutStart && SeenLayout));

                if (NewDevice || (Gap > GapTolerance))
                {
                    Result.Add(MakeRegion("ubi", null, Start, Last + 1, G));
                    Start = -1;
                }

                if (Start < 0)
                {
                    Start = B;
                    Seq = Info.EcOk ? Info.ImageSeq : -1;
                    SeenLayout = false;
                }

                if (Info.LayoutStart)
                    SeenLayout = true;

                Last = B;
                Gap = 0;
            }
        }

        private static void BuildDataRegions(byte[] Raw, long Base, NandGeometry G, BlockInfo[] Blocks, List<NandRegion> Result)
        {
            var RawBlock = G.RawBlockSize;
            long Start = -1;
            long Last = -1;
            long Gap = 0;
            string Container = null;

            for (long B = 0; B <= Blocks.Length; B++)
            {
                var Info = (B < Blocks.Length) ? Blocks[B] : null;
                var IsData = (Info != null) && Info.Used && !Info.Ubi;

                if (!IsData)
                {
                    if (Start >= 0)
                    {
                        Gap = (Info == null) ? GapTolerance + 1L : Gap + 1;
                        if (Gap > GapTolerance)
                        {
                            Result.Add(MakeRegion("data", Container, Start, Last + 1, G));
                            Start = -1;
                            Gap = 0;
                        }
                    }
                    continue;
                }

                var Detected = DetectContainer(Raw, Base + B * RawBlock, G);
                var Split = (Start >= 0) && ((Gap > GapTolerance) || ((B > Start) && (Detected != null)));

                if (Split)
                {
                    Result.Add(MakeRegion("data", Container, Start, Last + 1, G));
                    Start = -1;
                }

                if (Start < 0)
                {
                    Start = B;
                    Container = Detected;
                }

                Last = B;
                Gap = 0;
            }
        }

        private static string DetectContainer(byte[] Raw, long Off, NandGeometry G)
        {
            if (Match(Raw, Off, "ANDROID!"))
                return "android";
            if (Match(Raw, Off, "hsqs") || Match(Raw, Off, "sqsh"))
                return "squashfs";
            if (MatchBytes(Raw, Off, 0x27, 0x05, 0x19, 0x56))
                return "uimage";
            if (MatchBytes(Raw, Off, 0xd0, 0x0d, 0xfe, 0xed))
                return "fit";
            if (Match(Raw, Off, "-rom1fs-"))
                return "romfs";
            if (MatchBytes(Raw, Off, 0x45, 0x3d, 0xcd, 0x28))
                return "cramfs";
            if ((G.PageSize >= 2048) && (Raw.Length >= Off + 0x440) &&
                MatchBytes(Raw, Off + 0x438, 0x53, 0xef))
                return "ext";

            return null;
        }

        private static NandRegion MakeRegion(string Kind, string Container, long StartBlock, long EndBlock, NandGeometry G)
        {
            var Blocks = EndBlock - StartBlock;
            var Offset = StartBlock * G.BlockSize;
            var Size = Blocks * G.BlockSize;

            var Name = (Container ?? Kind) + "@" + Offset.ToString("x8");
            return new NandRegion(Name, Kind, Offset, Size);
        }

        // ---- primitive helpers ----

        private static int PageMagic(byte[] D, long Off)
        {
            if (Match(D, Off, "ANDROID!"))
                return 8;
            if (Match(D, Off, "hsqs") || Match(D, Off, "sqsh"))
                return 8;
            if (MatchBytes(D, Off, 0x27, 0x05, 0x19, 0x56))
                return 6;
            if (MatchBytes(D, Off, 0xd0, 0x0d, 0xfe, 0xed))
                return 5;
            if (Match(D, Off, "-rom1fs-"))
                return 5;

            return 0;
        }

        private static bool UbifsNodeCrcOk(byte[] D, long Off, long Limit)
        {
            var Len = ReadLE32(D, Off + 0x10);
            if ((Len < 24) || (Len > 0x10000) || (Off + Len > Limit))
                return false;

            return ReadLE32(D, Off + 4) == Crc32(D, Off + 8, Len - 8);
        }

        private static bool Match(byte[] D, long Off, string Text)
        {
            if ((Off < 0) || (Off + Text.Length > D.Length))
                return false;

            for (int i = 0; i < Text.Length; i++)
                if (D[Off + i] != Text[i])
                    return false;

            return true;
        }

        private static bool MatchBytes(byte[] D, long Off, params byte[] Bytes) => Match(D, Off, Bytes);

        private static bool Match(byte[] D, long Off, byte[] Bytes)
        {
            if ((Off < 0) || (Off + Bytes.Length > D.Length))
                return false;

            for (int i = 0; i < Bytes.Length; i++)
                if (D[Off + i] != Bytes[i])
                    return false;

            return true;
        }

        private static bool IsAllFF(byte[] D, long Off, int Len)
        {
            var Pos = 0;

            if (((Off & 7) == 0) && (Off + Len <= D.Length))
            {
                for (; Pos + 8 <= Len; Pos += 8)
                    if (BitConverter.ToUInt64(D, (int)(Off + Pos)) != 0xFFFFFFFFFFFFFFFFUL)
                        return false;
            }

            for (; Pos < Len; Pos++)
                if (D[Off + Pos] != 0xFF)
                    return false;

            return true;
        }

        private static int CountZeroFF(byte[] D, long Off, int Len)
        {
            var Count = 0;
            for (int i = 0; i < Len; i++)
            {
                var B = D[Off + i];
                if ((B == 0x00) || (B == 0xFF))
                    Count++;
            }

            return Count;
        }

        private static uint ReadBE16(byte[] D, long Off)
        {
            if (Off + 2 > D.Length)
                return 0;

            return ((uint)D[Off] << 8) | D[Off + 1];
        }

        private static ulong ReadBE64(byte[] D, long Off)
        {
            if (Off + 8 > D.Length)
                return 0;

            return ((ulong)ReadBE32(D, Off) << 32) | ReadBE32(D, Off + 4);
        }

        private static uint ReadBE32(byte[] D, long Off)
        {
            if (Off + 4 > D.Length)
                return 0;

            return ((uint)D[Off] << 24) | ((uint)D[Off + 1] << 16) | ((uint)D[Off + 2] << 8) | D[Off + 3];
        }

        private static uint ReadLE32(byte[] D, long Off)
        {
            if (Off + 4 > D.Length)
                return 0;

            return ((uint)D[Off + 3] << 24) | ((uint)D[Off + 2] << 16) | ((uint)D[Off + 1] << 8) | D[Off];
        }

        // ---- CRC32 (UBI/UBIFS flavour: init 0xFFFFFFFF, no final xor) ----

        private static readonly uint[] CrcTable = CreateCrcTable();

        private static uint[] CreateCrcTable()
        {
            var Table = new uint[256];

            for (uint i = 0; i < 256; i++)
            {
                var C = i;
                for (int k = 0; k < 8; k++)
                    C = ((C & 1) != 0) ? (0xEDB88320u ^ (C >> 1)) : (C >> 1);

                Table[i] = C;
            }

            return Table;
        }

        private static uint Crc32(byte[] D, long Off, long Length)
        {
            if ((Off < 0) || (Length < 0) || (Off + Length > D.Length))
                return 0xFFFFFFFF;

            var Crc = 0xFFFFFFFFu;
            for (long i = 0; i < Length; i++)
                Crc = (Crc >> 8) ^ CrcTable[(Crc ^ D[Off + i]) & 0xFF];

            return Crc;
        }
    }
}





