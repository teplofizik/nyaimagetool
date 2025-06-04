using NyaFs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAllwinner.Types;
using NyaFsAllwinner.Loader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace NyaFsAllwinner.Commands
{
    class LoadWty : ScriptStepGenerator
    {
        public LoadWty() : base("loadwty")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new LoadWtyScriptStep(Args.RawArgs[0]);
        }

        public class LoadWtyScriptStep : ScriptStep
        {
            private string Path;

            public LoadWtyScriptStep(string Path) : base("loadwty")
            {
                this.Path = Path;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                // Проверим наличие локального файла, содержимое которого надо загрузить в ФС
                if (!System.IO.File.Exists(Path))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found!");

                var Data = File.ReadAllBytes(Path);
                var Image = new WtyImage(Data);

                if (Image.Correct)
                {
                    var Files = Image.FileHeaders;

                    // File list
                    for (int i = 0; i < Files.Length; i++)
                    {
                        var Header = Files[i];

                        Log.Write(0, $"{Header.Maintype} ({Header.Subtype}): {Header.Filename}  offset: {Header.Offset:X08} img: {Header.StoredLength:X08} orig: {Header.OriginalLength:X08}");
                    }

                    var FsRes = ReadFs(Processor, Image);
                    if (FsRes.Status == ScriptStepStatus.Error)
                        return FsRes;

                    /*if (FW.Correct)
                    {

                        /*var KernelRes = ReadKernel(Processor, Image);
                        if (FsRes.Status == ScriptStepStatus.Error)
                            return KernelRes;

                        var DevtreeRes = ReadDtb(Processor, Image);
                        if (FsRes.Status == ScriptStepStatus.Error)
                            return DevtreeRes;

                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Rockchip image is loaded!");
                    }*/
                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Allwinner wty image is loaded!");
                }
                
                return new ScriptStepResult(ScriptStepStatus.Error, $"Allwinner wty image is invalid image!");
            }

            private ScriptStepResult ReadFs(ImageProcessor Processor, WtyImage Image)
            {
                var OldLoaded = Processor.GetFs()?.Loaded ?? false;
                var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();

                var Raw = Image.GetPartition(33);
                if (Fs != null)
                {
                    var FsReader = new WtyFsReader(Raw);

                    FsReader.ReadToFs(Fs);
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
                        NyaFs.ImageFormat.Helper.LogHelper.RamfsInfo(Fs);

                        if (OldLoaded)
                            return new ScriptStepResult(ScriptStepStatus.Warning, $"BMU image is loaded as filesystem! Old filesystem is replaced by this.");
                        else
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"BMU is loaded as filesystem!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown fs format!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"No rootfs partition in firmware package!");
            }

            private NyaFs.ImageFormat.Types.CPU FindExistArch(NyaFs.ImageFormat.BaseImageBlob Blob)
            {
                NyaFs.ImageFormat.Types.CPU arch = NyaFs.ImageFormat.Types.CPU.IH_ARCH_INVALID;
                if (Blob.IsProvidedKernel)
                {
                    var K = Blob.GetKernel();
                    if (K.Info.Architecture != NyaFs.ImageFormat.Types.CPU.IH_ARCH_INVALID)
                        arch = K.Info.Architecture;
                }
                if (Blob.IsProvidedFs)
                {
                    var Fs = Blob.GetFilesystem();
                    if (Fs.Info.Architecture != NyaFs.ImageFormat.Types.CPU.IH_ARCH_INVALID)
                    {
                        if (arch != NyaFs.ImageFormat.Types.CPU.IH_ARCH_INVALID)
                        {
                            // Is detected os info differs to kernel
                            if (arch != Fs.Info.Architecture)
                                return NyaFs.ImageFormat.Types.CPU.IH_ARCH_INVALID;
                        }
                        else
                            arch = Fs.Info.Architecture;
                    }
                }
                if (Blob.IsProvidedDTB)
                {
                    var Dtb = Blob.GetDevTree();
                    if (Dtb.Info.Architecture != NyaFs.ImageFormat.Types.CPU.IH_ARCH_INVALID)
                    {
                        if (arch != NyaFs.ImageFormat.Types.CPU.IH_ARCH_INVALID)
                        {
                            // Is detected os info differs to dtb
                            if (arch != Dtb.Info.Architecture)
                                return NyaFs.ImageFormat.Types.CPU.IH_ARCH_INVALID;
                        }
                        else
                            arch = Dtb.Info.Architecture;
                    }
                }

                return arch;
            }

            private NyaFs.ImageFormat.Types.OS FindExistOS(NyaFs.ImageFormat.BaseImageBlob Blob)
            {
                NyaFs.ImageFormat.Types.OS os = NyaFs.ImageFormat.Types.OS.IH_OS_INVALID;
                if (Blob.IsProvidedKernel)
                {
                    var K = Blob.GetKernel();
                    if (K.Info.OperatingSystem != NyaFs.ImageFormat.Types.OS.IH_OS_INVALID)
                        os = K.Info.OperatingSystem;
                }
                if (Blob.IsProvidedFs)
                {
                    var Fs = Blob.GetFilesystem();
                    if (Fs.Info.OperatingSystem != NyaFs.ImageFormat.Types.OS.IH_OS_INVALID)
                    {
                        if (os != NyaFs.ImageFormat.Types.OS.IH_OS_INVALID)
                        {
                            // Is FS os info differs to kernel
                            if (os != Fs.Info.OperatingSystem)
                                return NyaFs.ImageFormat.Types.OS.IH_OS_INVALID;
                        }
                        else
                            os = Fs.Info.OperatingSystem;
                    }
                }

                return os;
            }
            private void AssumeAutoImageParams(ImageProcessor Processor)
            {
                var Blob = Processor.GetBlob();
                var Arch = FindExistArch(Blob);
                if (Arch != NyaFs.ImageFormat.Types.CPU.IH_ARCH_INVALID)
                {
                    var K = Processor.GetKernel();
                    if (K != null)
                    {
                        if (K.Info.Architecture == NyaFs.ImageFormat.Types.CPU.IH_ARCH_INVALID)
                            K.Info.Architecture = Arch;
                    }
                    var Fs = Processor.GetFs();
                    if (Fs != null)
                    {
                        if (Fs.Info.Architecture == NyaFs.ImageFormat.Types.CPU.IH_ARCH_INVALID)
                            Fs.Info.Architecture = Arch;
                    }
                    var Dtb = Processor.GetDevTree();
                    if (Dtb != null)
                    {
                        if (Dtb.Info.Architecture == NyaFs.ImageFormat.Types.CPU.IH_ARCH_INVALID)
                            Dtb.Info.Architecture = Arch;
                    }
                }
                var Os = FindExistOS(Blob);
                if (Os != NyaFs.ImageFormat.Types.OS.IH_OS_INVALID)
                {
                    var K = Processor.GetKernel();
                    if (K != null)
                    {
                        if (K.Info.OperatingSystem == NyaFs.ImageFormat.Types.OS.IH_OS_INVALID)
                            K.Info.OperatingSystem = Os;
                    }
                    var Fs = Processor.GetFs();
                    if (Fs != null)
                    {
                        if (Fs.Info.OperatingSystem == NyaFs.ImageFormat.Types.OS.IH_OS_INVALID)
                            Fs.Info.OperatingSystem = Os;
                    }
                }
            }
        }
    }
}
