using NyaFs;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.ImageFormat.Elements.Kernel;
using NyaFs.ImageFormat.Helper;
using NyaFs.ImageFormat.Plugins;
using NyaFsAllwinner.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFsAllwinner
{
    /// <summary>
    /// Writes Allwinner IMAGEWTY containers. Existing WTY metadata (main/sub type,
    /// header fields) is preserved when the source plan has the original raw image.
    /// </summary>
    public class WtyWriter
    {
        private readonly string Path;

        public WtyWriter(string Path)
        {
            this.Path = Path;
        }

        public byte[] Build(BaseImageBlob Blob, out string Error)
        {
            Error = null;
            try
            {
                var Source = (Blob.BuildPlan != null) ? Blob.BuildPlan.Root.Raw : null;
                WtyImage SourceImage = null;
                if (Source != null)
                {
                    var Candidate = new WtyImage(Source);
                    if (Candidate.Correct)
                        SourceImage = Candidate;
                }

                var Parts = CollectParts(Blob);
                if (Parts.Count == 0)
                {
                    Error = "No WTY partitions to write";
                    return null;
                }

                var HeaderVersion = SourceImage != null ? SourceImage.HeaderVersion : 0x300;
                var HeaderSize = SourceImage != null ? SourceImage.HeaderSize : 0x60;
                var ImageHeaderSize = SourceImage != null ? SourceImage.ImageHeaderSize : 0;
                var RamBase = SourceImage != null ? SourceImage.RamBase : 0x4D00000;
                var Version = SourceImage != null ? SourceImage.Version : 0x100234;
                var SourceHeaders = SourceImage != null ? SourceImage.FileHeaders : new WtyImage.FileHeader[0];

                var HeaderBytes = 1024 * (Parts.Count + 1);
                var Offsets = new int[Parts.Count];
                var Offset = HeaderBytes;
                for (var I = 0; I < Parts.Count; I++)
                {
                    Offset = Align(Offset, 256);
                    Offsets[I] = Offset;
                    Offset += Parts[I].Data.Length;
                }
                var Total = Align(Offset, 1024);
                var Result = new byte[Total];

                if ((Source != null) && (Source.Length >= 0x80))
                    Buffer.BlockCopy(Source, 0, Result, 0, Math.Min(0x80, Source.Length));

                SetString(Result, 0x00, 8, "IMAGEWTY");
                SetU32(Result, 0x08, HeaderVersion);
                SetU32(Result, 0x0C, HeaderSize);
                SetU32(Result, 0x10, RamBase);
                SetU32(Result, 0x14, Version);
                SetU32(Result, 0x18, (uint)Total);
                SetU32(Result, 0x1C, ImageHeaderSize);
                SetU32(Result, (HeaderVersion == 0x300) ? 0x3C : 0x38, (uint)Parts.Count);

                for (var I = 0; I < Parts.Count; I++)
                {
                    var Part = Parts[I];
                    var SourceHeader = (I < SourceHeaders.Length) ? SourceHeaders[I] : null;
                    var Name = Part.Name;
                    var MainType = Part.MainType;
                    var SubType = Part.SubType;
                    if (SourceHeader != null)
                    {
                        if (String.IsNullOrEmpty(MainType))
                            MainType = SourceHeader.Maintype;
                        if (String.IsNullOrEmpty(SubType))
                            SubType = SourceHeader.Subtype;
                        if (!String.IsNullOrEmpty(SourceHeader.Filename))
                            Name = SourceHeader.Filename;
                    }

                    var Header = 1024 * (I + 1);
                    SetU32(Result, Header + 0x00, 256);
                    SetU32(Result, Header + 0x04, 1024);
                    SetString(Result, Header + 0x08, 8, MainType ?? "COMMON");
                    SetString(Result, Header + 0x10, 16, SubType ?? "FILE");
                    SetU32(Result, Header + 0x20, 0);

                    if (HeaderVersion == 0x300)
                    {
                        SetString(Result, Header + 0x24, 256, Name ?? "file");
                        SetU32(Result, Header + 0x124, (uint)Part.Data.Length);
                        SetU32(Result, Header + 0x128, 0);
                        SetU32(Result, Header + 0x12C, (uint)Part.Data.Length);
                        SetU32(Result, Header + 0x130, 0);
                        SetU32(Result, Header + 0x134, (uint)Offsets[I]);
                    }
                    else
                    {
                        SetU32(Result, Header + 0x24, (uint)Part.Data.Length);
                        SetU32(Result, Header + 0x28, (uint)Part.Data.Length);
                        SetU32(Result, Header + 0x2C, (uint)Offsets[I]);
                        SetU32(Result, Header + 0x30, 0);
                        SetString(Result, Header + 0x34, 256, Name ?? "file");
                    }

                    Buffer.BlockCopy(Part.Data, 0, Result, Offsets[I], Part.Data.Length);
                }

                return Result;
            }
            catch (Exception E)
            {
                Error = E.Message;
                Log.Error(0, $"Cannot write IMAGEWTY image: {E.Message}");
                return null;
            }
        }

        public bool Write(BaseImageBlob Blob)
        {
            var Data = Build(Blob, out var Error);
            if (Data == null)
                return false;
            File.WriteAllBytes(Path, Data);
            return true;
        }

        private static List<Part> CollectParts(BaseImageBlob Blob)
        {
            var Result = new List<Part>();
            var Plan = Blob.BuildPlan;
            if ((Plan != null) && (Plan.Root != null) && (Plan.Root.Format == "wty"))
            {
                foreach (var Child in Plan.Root.Children)
                {
                    if ((Child.Raw == null) || (Child.Raw.Length == 0))
                        continue;
                    var ChildData = GetChildData(Child);
                    if ((ChildData != null) && (ChildData.Length > 0))
                        Result.Add(new Part(Child.Name, null, null, ChildData));
                }
                if (Result.Count > 0)
                    return Result;
            }

            var Kernel = Blob.GetKernel(0);
            if ((Kernel != null) && Kernel.Loaded)
                Result.Add(new Part("kernel", "BOOT", "BOOT_0000000000", GetKernelData(Kernel)));

            var Fs = Blob.GetFilesystem(0);
            if ((Fs != null) && Fs.Loaded)
                Result.Add(new Part("rootfs", "ROOTFS", "ROOTFS_00000000", GetFilesystemData(Fs)));

            var Dtb = Blob.GetDevTree(0);
            if ((Dtb != null) && Dtb.Loaded)
                Result.Add(new Part("sunxi.fex", "COMMON", "DTB_CONFIG000000", GetDtbData(Dtb)));

            return Result;
        }

        private static byte[] GetChildData(ImageBuildNode Child)
        {
            if (!Child.Dirty && (Child.Raw != null))
                return Child.Raw;

            if (Child.Format == "android")
            {
                var Temp = new BaseImageBlob();
                var Plan = new ImageBuildPlan(Child);
                Temp.BuildPlan = Plan;
                foreach (var Option in Child.Options)
                    Temp.SetContainerOption("android", Option.Key, Option.Value);

                var Kernel = FindKind(Child, ImageKind.Kernel);
                var Fs = FindKind(Child, ImageKind.Filesystem);
                var Dtb = FindKind(Child, ImageKind.Devtree);
                if ((Kernel != null) && (Kernel.Kernel != null)) Temp.SetKernel(0, Kernel.Kernel);
                if ((Fs != null) && (Fs.Fs != null))
                {
                    if (Fs.Compression != NyaFs.ImageFormat.Types.CompressionType.IH_COMP_NONE)
                        Fs.Fs.Info.Compression = Fs.Compression;
                    Temp.SetFilesystem(0, Fs.Fs);
                }
                if ((Dtb != null) && (Dtb.Dtb != null)) Temp.SetDevTree(0, Dtb.Dtb);

                var Data = new NyaFs.ImageFormat.Composite.AndroidImageWriter(null).Build(Temp, out var Error);
                if (Data != null)
                    return Data;
            }

            var Writer = new NyaFs.ImageFormat.Composite.ImageBuildPlanWriter(new ImageBuildPlan(Child));
            var Result = Writer.Build(out var BuildError);
            return (Result != null) ? Result : Child.Raw;
        }

        private static ImageBuildNode FindKind(ImageBuildNode Node, ImageKind Kind)
        {
            if (Node == null)
                return null;
            if (Node.Kind == Kind)
                return Node;
            foreach (var Child in Node.Children)
            {
                var Res = FindKind(Child, Kind);
                if (Res != null)
                    return Res;
            }
            return null;
        }

        private static byte[] GetKernelData(LinuxKernel Kernel)
        {
            if (!String.IsNullOrEmpty(Kernel.KernelFormat))
            {
                var Plugin = PluginHost.Current.ResolveKernel(Kernel.KernelFormat);
                if ((Plugin != null) && Plugin.SupportWrite)
                {
                    var Writer = Plugin.GetKernelWriter(Kernel);
                    if (Writer != null)
                    {
                        Writer.WriteKernel(Kernel);
                        if (Writer.HasRawStreamData)
                            return Writer.RawStream;
                    }
                }
            }
            return Kernel.Image ?? Array.Empty<byte>();
        }

        private static byte[] GetFilesystemData(LinuxFilesystem Fs)
        {
            NyaFs.ImageFormat.Elements.Fs.Writer.Writer Writer = null;
            var Plugin = PluginHost.Current.ResolveFilesystem(Fs.FilesystemFormat);
            if ((Plugin != null) && Plugin.SupportWrite)
                Writer = Plugin.GetFilesystemWriter(Fs);
            if (Writer == null)
                return Array.Empty<byte>();

            Writer.WriteFs(Fs);
            if (!Writer.HasRawStreamData)
                return Array.Empty<byte>();
            return FitHelper.GetCompressedData(Writer.RawStream, Fs.Info.Compression);
        }

        private static byte[] GetDtbData(NyaFs.ImageFormat.Elements.Dtb.DeviceTree Dtb)
        {
            var Plugin = PluginHost.Current.ResolveDtb(Dtb.DevtreeFormat);
            if ((Plugin != null) && Plugin.SupportWrite)
            {
                var Writer = Plugin.GetDevtreeWriter(Dtb);
                if (Writer != null)
                {
                    Writer.WriteDevtree(Dtb);
                    if (Writer.HasRawStreamData)
                        return Writer.RawStream;
                }
            }
            return new NyaFs.FlattenedDeviceTree.Writer.FDTWriter(Dtb.DevTree).GetBinary();
        }

        private static int Align(int Value, int Alignment) =>
            ((Value + Alignment - 1) / Alignment) * Alignment;

        private static void SetU32(byte[] Data, int Offset, uint Value)
        {
            Data[Offset] = (byte)Value;
            Data[Offset + 1] = (byte)(Value >> 8);
            Data[Offset + 2] = (byte)(Value >> 16);
            Data[Offset + 3] = (byte)(Value >> 24);
        }

        private static void SetString(byte[] Data, int Offset, int Size, string Value)
        {
            var Bytes = Encoding.ASCII.GetBytes(Value ?? "");
            Buffer.BlockCopy(Bytes, 0, Data, Offset, Math.Min(Size, Bytes.Length));
        }

        private sealed class Part
        {
            public readonly string Name;
            public readonly string MainType;
            public readonly string SubType;
            public readonly byte[] Data;

            public Part(string Name, string MainType, string SubType, byte[] Data)
            {
                this.Name = Name;
                this.MainType = MainType;
                this.SubType = SubType;
                this.Data = Data;
            }
        }
    }
}
