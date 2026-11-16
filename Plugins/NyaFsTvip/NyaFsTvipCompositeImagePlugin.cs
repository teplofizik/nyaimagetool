using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Composite;
using NyaFs.ImageFormat.Plugins.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace NyaFsTvip
{
    /// <summary>
    /// TVIP firmware container (magic "TVIP"). Sections (kernel/rootfs/splash/uboot)
    /// are opaque children; the normal pipeline detects the uImage kernel, CramFS
    /// rootfs and so on recursively.
    /// </summary>
    public class NyaFsTvipCompositeImagePlugin : CompositeImagePlugin
    {
        public NyaFsTvipCompositeImagePlugin() : base("image.tvip", "tvip") { }

        public override bool PreserveUnknownChildren => true;

        public override bool SupportWrite => true;

        public override bool DetectImage(ImageData Image)
        {
            if (Image.Length < TvipImage.GlobalHeaderSize)
                return false;

            var Tvip = new TvipImage(Image.ReadArray(0, Image.Length));
            if (!Tvip.Correct)
                return false;

            return (Tvip.Find("kernel") != null) || (Tvip.Find("rootfs") != null);
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            var Raw = Image.ReadArray(0, Image.Length);
            var Tvip = new TvipImage(Raw);
            if (!Tvip.Correct)
                return new ContainedImage[0];

            var Result = new List<ContainedImage>();
            foreach (var Section in Tvip.Sections)
            {
                var Content = Tvip.GetData(Section);
                Result.Add(new ContainedImage(Section.Name, ImageKind.Unknown, null,
                    Section.DataOffset, Section.Size, Content));
            }
            return Result.ToArray();
        }

        public override IEnumerable<KeyValuePair<string, string>> ReadOptions(ImageData Image)
        {
            var Tvip = new TvipImage(Image.ReadArray(0, Image.Length));
            if (!Tvip.Correct)
                yield break;

            if (!String.IsNullOrEmpty(Tvip.Model))
                yield return new KeyValuePair<string, string>("model", Tvip.Model);
            if (!String.IsNullOrEmpty(Tvip.Version))
                yield return new KeyValuePair<string, string>("version", Tvip.Version);
        }

        public override string ReadDescription(ImageData Image)
        {
            var Tvip = new TvipImage(Image.ReadArray(0, Image.Length));
            if (!Tvip.Correct)
                return null;

            var Description = (Tvip.Model + " " + Tvip.Version).Trim();
            return String.IsNullOrEmpty(Description) ? "TVIP firmware" : $"TVIP firmware {Description}";
        }

        public override byte[] Build(BaseImageBlob Blob, out string Error)
        {
            Error = null;
            try
            {
                var Root = Blob.BuildPlan?.Root;
                if (Root?.Raw == null)
                {
                    Error = "TVIP writer needs an existing source image";
                    return null;
                }

                if (!Root.Dirty)
                    return Root.Raw;

                var Source = new TvipImage(Root.Raw);
                if (!Source.Correct)
                {
                    Error = "Invalid TVIP source image";
                    return null;
                }

                using (var Stream = new MemoryStream())
                {
                    Stream.Write(Source.GlobalHeader, 0, Source.GlobalHeader.Length);

                    for (var I = 0; I < Source.Sections.Count; I++)
                    {
                        var Section = Source.Sections[I];
                        var Child = FindChild(Root, Section.Name);
                        var Dirty = (Child != null) && IsDirty(Child);

                        byte[] Data;
                        uint Size;
                        byte[] Md5;

                        if (Dirty)
                        {
                            Data = GetNodeData(Child);
                            if (Data == null)
                            {
                                Error = "Cannot serialize TVIP section '" + Section.Name + "'";
                                return null;
                            }

                            Size = (uint)Data.Length;
                            Md5 = MD5.HashData(Data);
                        }
                        else
                        {
                            Data = Source.GetData(Section);
                            Size = Section.Size;
                            Md5 = Section.Md5;
                        }

                        if (Section.Truncated && !Dirty && (I + 1 < Source.Sections.Count))
                        {
                            Error = "Cannot rebuild a truncated non-final TVIP section";
                            return null;
                        }

                        WriteSectionHeader(Stream, Section.Name, Section.Flags, Size, Md5);
                        Stream.Write(Data, 0, Data.Length);
                    }

                    return Stream.ToArray();
                }
            }
            catch (Exception E)
            {
                Error = E.Message;
                return null;
            }
        }

        private static void WriteSectionHeader(Stream Stream, string Name, uint Flags, uint Size, byte[] Md5)
        {
            var Header = new byte[TvipImage.SectionHeaderSize];
            var NameBytes = System.Text.Encoding.ASCII.GetBytes(Name ?? "");
            Buffer.BlockCopy(NameBytes, 0, Header, 0, Math.Min(6, NameBytes.Length));

            Header[7] = (byte)(Flags >> 24);
            Header[8] = (byte)(Flags >> 16);
            Header[9] = (byte)(Flags >> 8);
            Header[10] = (byte)Flags;

            Header[11] = (byte)(Size >> 24);
            Header[12] = (byte)(Size >> 16);
            Header[13] = (byte)(Size >> 8);
            Header[14] = (byte)Size;

            Buffer.BlockCopy(Md5, 0, Header, 15, 16);
            Stream.Write(Header, 0, Header.Length);
        }

        private static ImageBuildNode FindChild(ImageBuildNode Root, string Name)
        {
            foreach (var Child in Root.Children)
                if (String.Equals(Child.Name, Name, StringComparison.OrdinalIgnoreCase))
                    return Child;
            return null;
        }

        private static bool IsDirty(ImageBuildNode Node)
        {
            if (Node.Dirty)
                return true;
            foreach (var Child in Node.Children)
                if (IsDirty(Child))
                    return true;
            return false;
        }

        private static byte[] GetNodeData(ImageBuildNode Node)
        {
            if (!Node.Dirty && (Node.Raw != null))
                return Node.Raw;

            if (Node.Kind != ImageKind.Unknown)
            {
                var Writer = new ImageBuildPlanWriter(new ImageBuildPlan(Node));
                var Result = Writer.Build(out _);
                if (Result != null)
                    return Result;
            }

            return Node.Raw;
        }
    }
}
