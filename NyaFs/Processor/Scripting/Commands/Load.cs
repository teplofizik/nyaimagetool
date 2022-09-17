using NyaFs.ImageFormat.Elements.Dtb;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands
{
    public class Load : ScriptStepGenerator
    {
        public Load() : base("load")
        {
            AddConfig(new Configs.ImageScriptArgsConfig(0, "kernel",
                new string[] { "gz", "gzip", "lzma", "lz4", "bz2", "lzo", "zstd", "bzip2", "bz2", "raw", "legacy", "fit", "android", "zimage" }));

            AddConfig(new Configs.ImageScriptArgsConfig(1, "ramfs", 
                new string[] { "gz", "gzip", "lzma", "lz4", "bz2", "lzo", "zstd", "bzip2", "bz2", "fit", "android", "legacy", "cpio", "ext2", "squashfs", "cramfs" }));


            AddConfig(new Configs.ImageScriptArgsConfig(2, "devtree", 
                new string[] { "gz", "gzip", "lzma", "lz4", "bz2", "lzo", "zstd", "bzip2", "bz2", "dtb", "fit"  }));

            AddConfig(new ScriptArgsConfig(3, new ScriptArgsParam[] { new Params.FsPathScriptArgsParam() }));

            AddConfig(new Configs.ErrorConfig("Invalid image type: %1%. Must be one of: kernel, ramfs, devtree"));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;

            if (Args.ArgConfig == 3)
                return new LoadScriptStep(A[0], "detect", "fit");
            else
                return new LoadScriptStep(A[0], A[1], A[2]);
        }

        public class LoadScriptStep : ScriptStep
        {
            string Path;
            string Type;
            string Format;

            public LoadScriptStep(string Path, string Type, string Format) : base("load")
            {
                this.Path = Path;
                this.Type = Type;
                this.Format = Format;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                // Проверим наличие локального файла, содержимое которого надо загрузить в ФС
                if (!System.IO.File.Exists(Path))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found!");

                if(Type == "detect")
                {
                    var Detected = Helper.ArchiveHelper.DetectImageFormat(Path);
                    if(Detected != null)
                    {
                        Type = Detected.Item1;
                        Format = Detected.Item2;
                    }
                }

                switch (Type)
                {
                    case "ramfs":   return ReadFs(Processor);
                    case "devtree": return ReadDtb(Processor);
                    case "kernel":  return ReadKernel(Processor);
                    case "all": return ReadAll(Processor);
                    default:
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown image type!");
                }
            }

            private ScriptStepResult ReadAll(ImageProcessor Processor)
            {
                uint Mask = 0;
                ScriptStepResult Res;
                switch (Format)
                {
                    case "android":
                        Res = ReadKernel(Processor);
                        if (Res.Status == ScriptStepStatus.Ok)
                            Mask |= 1;

                        Res = ReadFs(Processor);
                        if (Res.Status == ScriptStepStatus.Ok)
                            Mask |= 2;

                        AssumeAutoImageParams(Processor);
                        //Res = ReadDtb(Processor);
                        //if (Res.Status != ScriptStepStatus.Ok)
                        //    return Res;
                        if (Mask == 0)
                            return new ScriptStepResult(ScriptStepStatus.Error, $"Images are not loaded from Android image!");
                        else if (Mask == 3)
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"Images are loaded from Android image!");
                        else
                            return new ScriptStepResult(ScriptStepStatus.Warning, $"Several images are loaded from Android image!");

                    case "fit":
                        Res = ReadKernel(Processor);
                        if (Res.Status == ScriptStepStatus.Ok)
                            Mask |= 1;

                        Res = ReadFs(Processor);
                        if (Res.Status == ScriptStepStatus.Ok)
                            Mask |= 2;

                        Res = ReadDtb(Processor);
                        if (Res.Status == ScriptStepStatus.Ok)
                            Mask |= 4;

                        if (Mask == 0)
                            return new ScriptStepResult(ScriptStepStatus.Error, $"Images are not loaded from FIT image!");
                        else if (Mask == 7)
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"All images are loaded from FIT image!");
                        else
                            return new ScriptStepResult(ScriptStepStatus.Warning, $"Several images are loaded from FIT image!");

                    default:
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown image format!");

                }
            }

            private ImageFormat.Elements.Kernel.Reader.Reader GetKernelReader()
            {
                switch (Format)
                {
                    case "raw": return new ImageFormat.Elements.Kernel.Reader.ArchiveReader(Path, ImageFormat.Types.CompressionType.IH_COMP_NONE);
                    case "legacy": return new ImageFormat.Elements.Kernel.Reader.LegacyReader(Path);
                    case "fit": return new ImageFormat.Elements.Kernel.Reader.FitReader(Path);
                    case "android": return new ImageFormat.Elements.Kernel.Reader.AndroidReader(Path);
                    case "zimage": return new ImageFormat.Elements.Kernel.Reader.zImageReader(Path);
                    case "lzma":
                    case "lz4":
                    case "gz":
                    case "gzip":
                    case "bzip2":
                    case "gz2":
                    case "zstd":
                    case "lzo":
                        var CompressionType = Helper.ArchiveHelper.GetCompressionFormat(Format);
                        return new ImageFormat.Elements.Kernel.Reader.ArchiveReader(Path, CompressionType);
                    default:
                        return null;
                }
            }

            private ScriptStepResult ReadKernel(ImageProcessor Processor)
            {
                var OldLoaded = Processor.GetKernel()?.Loaded ?? false;
                var Kernel = new ImageFormat.Elements.Kernel.LinuxKernel();
                var Reader = GetKernelReader();
                if(Reader != null)
                {
                    Reader.ReadToKernel(Kernel);
                    if (Kernel.Loaded)
                    {
                        Processor.SetKernel(Kernel);
                        AssumeAutoKernelParams(Kernel);
                        AssumeAutoImageParams(Processor);
                        ImageFormat.Helper.LogHelper.KernelInfo(Kernel);

                        if (OldLoaded)
                            return new ScriptStepResult(ScriptStepStatus.Warning, $"{Format} kernel image is loaded as kernel! Old kernel is replaced by this.");
                        else
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"{Format} kernel image is loaded as kernel!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Kernel image file is not loaded!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown kernel format!");
            }

            private ImageFormat.Elements.Dtb.Reader.Reader GetDtbReader()
            {
                switch (Format)
                {
                    case "dtb": return new ImageFormat.Elements.Dtb.Reader.DtbReader(Path);
                    case "fit": return new ImageFormat.Elements.Dtb.Reader.FitReader(Path);
                    case "lz4":
                    case "lzma":
                    case "gz":
                    case "gzip":
                    case "bz2":
                    case "bzip2":
                    case "zstd":
                    case "lzo":
                        var CompressionType = Helper.ArchiveHelper.GetCompressionFormat(Format);
                        return new ImageFormat.Elements.Dtb.Reader.ArchiveReader(Path, CompressionType);
                    case "dts":
                    default:
                        return null;
                }
            }

            private ScriptStepResult ReadDtb(ImageProcessor Processor)
            {
                var OldLoaded = Processor.GetDevTree()?.Loaded ?? false;
                var Dtb = new DeviceTree();
                var Reader = GetDtbReader();
                if(Reader != null)
                {
                    Reader.ReadToDevTree(Dtb);
                    if (Dtb.Loaded)
                    {
                        Processor.SetDeviceTree(Dtb);
                        AssumeAutoImageParams(Processor);
                        ImageFormat.Helper.LogHelper.DevtreeInfo(Dtb);
                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Device tree is loaded from {Format} file!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Device tree is not loaded!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown devtree format!");
            }

            private ImageFormat.Elements.Fs.Reader.Reader GetFsReader()
            {
                switch (Format)
                {
                    case "cramfs": return new ImageFormat.Elements.Fs.Reader.CramFsReader(Path);
                    case "squashfs": return new ImageFormat.Elements.Fs.Reader.SquashFsReader(Path);
                    case "legacy": return new ImageFormat.Elements.Fs.Reader.LegacyReader(Path);
                    case "cpio": return new ImageFormat.Elements.Fs.Reader.CpioFsReader(Path);
                    case "romfs": return new ImageFormat.Elements.Fs.Reader.RomFsReader(Path);
                    case "fit": return new ImageFormat.Elements.Fs.Reader.FitReader(Path);
                    case "android": return new ImageFormat.Elements.Fs.Reader.AndroidReader(Path);
                    case "ext2": return new ImageFormat.Elements.Fs.Reader.ExtReader(Path);
                    case "lz4":
                    case "lzma":
                    case "gz":
                    case "gzip":
                    case "bz2":
                    case "bzip2":
                    case "zstd":
                    case "lzo":
                        var CompressionType = Helper.ArchiveHelper.GetCompressionFormat(Format);
                        return new ImageFormat.Elements.Fs.Reader.ArchiveReader(Path, CompressionType);
                    default:
                        return null;
                }
            }

            private ScriptStepResult ReadFs(ImageProcessor Processor)
            {
                var OldLoaded = Processor.GetFs()?.Loaded ?? false;
                var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
                var Reader = GetFsReader();
                if(Reader != null)
                {
                    Reader.ReadToFs(Fs);
                    if (Fs.Loaded)
                    {
                        Processor.SetFs(Fs);
                        AssumeAutoImageParams(Processor);
                        //if (Fs.FilesystemType != ImageFormat.Types.FsType.Cpio)
                        //{
                            // Fs.FilesystemType = ImageFormat.Types.FsType.Cpio;
                            // Log.Warning(0, "Filesystem is switched to cpio as only supported for packing.");
                            // if (Fs.Info.Compression == ImageFormat.Types.CompressionType.IH_COMP_NONE)
                            // {
                            //     Fs.Info.Compression = ImageFormat.Types.CompressionType.IH_COMP_GZIP;
                            //     Log.Warning(0, "Compression is switched to gzip.");
                            // }
                        //}
                        ImageFormat.Helper.LogHelper.RamfsInfo(Fs);

                        if (OldLoaded)
                            return new ScriptStepResult(ScriptStepStatus.Warning, $"{Format} image is loaded as filesystem! Old filesystem is replaced by this.");
                        else
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"{Format} is loaded as filesystem!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"{Format} image is not loaded!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown fs format!");
            }


            private void AssumeAutoKernelParams(ImageFormat.Elements.Kernel.LinuxKernel Kernel)
            {
                if(Kernel.Info.Architecture == ImageFormat.Types.CPU.IH_ARCH_INVALID)
                {
                    var Type = Helper.KernelHelper.DetectImageArch(Kernel.Image);

                    if(Type != ImageFormat.Types.CPU.IH_ARCH_INVALID)
                    {
                        Log.Write(0, $"Assumed kernel architecture: {ImageFormat.Helper.FitHelper.GetCPUArchitecture(Type)}");
                        Kernel.Info.Architecture = Type;
                    }
                }

                if(Kernel.Info.OperatingSystem == ImageFormat.Types.OS.IH_OS_INVALID)
                {
                    var Os = Helper.KernelHelper.DetectImageOs(Kernel.Image);

                    if (Os != ImageFormat.Types.OS.IH_OS_INVALID)
                    {
                        Log.Write(0, $"Assumed kernel OS: {ImageFormat.Helper.FitHelper.GetOperatingSystem(Os)}");
                        Kernel.Info.OperatingSystem = Os;
                    }
                }
            }

            private void AssumeAutoImageParams(ImageProcessor Processor)
            {
                var Blob = Processor.GetBlob();
                var Arch = FindExistArch(Blob);
                if(Arch != ImageFormat.Types.CPU.IH_ARCH_INVALID)
                {
                    var K = Processor.GetKernel();
                    if(K != null)
                    {
                        if (K.Info.Architecture == ImageFormat.Types.CPU.IH_ARCH_INVALID)
                            K.Info.Architecture = Arch;
                    }
                    var Fs = Processor.GetFs();
                    if (Fs != null)
                    {
                        if (Fs.Info.Architecture == ImageFormat.Types.CPU.IH_ARCH_INVALID)
                            Fs.Info.Architecture = Arch;
                    }
                    var Dtb = Processor.GetDevTree();
                    if (Dtb != null)
                    {
                        if (Dtb.Info.Architecture == ImageFormat.Types.CPU.IH_ARCH_INVALID)
                            Dtb.Info.Architecture = Arch;
                    }
                }
                var Os = FindExistOS(Blob);
                if(Os != ImageFormat.Types.OS.IH_OS_INVALID)
                {
                    var K = Processor.GetKernel();
                    if (K != null)
                    {
                        if (K.Info.OperatingSystem == ImageFormat.Types.OS.IH_OS_INVALID)
                            K.Info.OperatingSystem = Os;
                    }
                    var Fs = Processor.GetFs();
                    if (Fs != null)
                    {
                        if (Fs.Info.OperatingSystem == ImageFormat.Types.OS.IH_OS_INVALID)
                            Fs.Info.OperatingSystem = Os;
                    }
                }
            }

            ImageFormat.Types.CPU FindExistArch(ImageFormat.BaseImageBlob Blob)
            {
                ImageFormat.Types.CPU arch = ImageFormat.Types.CPU.IH_ARCH_INVALID;
                if (Blob.IsProvidedKernel)
                {
                    var K = Blob.GetKernel();
                    if (K.Info.Architecture != ImageFormat.Types.CPU.IH_ARCH_INVALID)
                        arch = K.Info.Architecture;
                }
                if (Blob.IsProvidedFs)
                {
                    var Fs = Blob.GetFilesystem();
                    if (Fs.Info.Architecture != ImageFormat.Types.CPU.IH_ARCH_INVALID)
                    {
                        if (arch != ImageFormat.Types.CPU.IH_ARCH_INVALID)
                        {
                            // Is detected os info differs to kernel
                            if (arch != Fs.Info.Architecture)
                                return ImageFormat.Types.CPU.IH_ARCH_INVALID;
                        }
                        else
                            arch = Fs.Info.Architecture;
                    }
                }
                if (Blob.IsProvidedDTB)
                {
                    var Dtb = Blob.GetDevTree();
                    if (Dtb.Info.Architecture != ImageFormat.Types.CPU.IH_ARCH_INVALID)
                    {
                        if (arch != ImageFormat.Types.CPU.IH_ARCH_INVALID)
                        {
                            // Is detected os info differs to dtb
                            if (arch != Dtb.Info.Architecture)
                                return ImageFormat.Types.CPU.IH_ARCH_INVALID;
                        }
                        else
                            arch = Dtb.Info.Architecture;
                    }
                }

                return arch;
            }

            ImageFormat.Types.OS FindExistOS(ImageFormat.BaseImageBlob Blob)
            {
                ImageFormat.Types.OS os = ImageFormat.Types.OS.IH_OS_INVALID;
                if (Blob.IsProvidedKernel)
                {
                    var K = Blob.GetKernel();
                    if (K.Info.OperatingSystem != ImageFormat.Types.OS.IH_OS_INVALID)
                        os = K.Info.OperatingSystem;
                }
                if(Blob.IsProvidedFs)
                {
                    var Fs = Blob.GetFilesystem();
                    if (Fs.Info.OperatingSystem != ImageFormat.Types.OS.IH_OS_INVALID)
                    {
                        if (os != ImageFormat.Types.OS.IH_OS_INVALID)
                        {
                            // Is FS os info differs to kernel
                            if (os != Fs.Info.OperatingSystem)
                                return ImageFormat.Types.OS.IH_OS_INVALID;
                        }
                        else
                            os = Fs.Info.OperatingSystem;
                    }
                }

                return os;
            }
        }
    }
}
