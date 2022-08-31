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
                new string[] { "gz", "gzip", "lzma", "lz4", "legacy", "fit", "raw" }));

            AddConfig(new Configs.ImageScriptArgsConfig(1, "ramfs", 
                new string[] { "cpio", "gz", "gzip", "lzma", "lz4", "bz2", "bzip2", "legacy", "fit", "ext2" }));

            AddConfig(new Configs.ImageScriptArgsConfig(2, "devtree", 
                new string[] { "dtb", "fit"  }));

            AddConfig(new ScriptArgsConfig(3, new ScriptArgsParam[] { new Params.FsPathScriptArgsParam() }));

            AddConfig(new Configs.ErrorConfig("Invalid image type: %1%. Must be one of: kernel, ramfs, devtree"));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;

            if (Args.ArgConfig == 3)
                return new LoadScriptStep(A[0], "all", "fit");
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
                ScriptStepResult Res;
                switch (Format)
                {
                    case "fit":
                        Res = ReadKernel(Processor);
                        if (Res.Status != ScriptStepStatus.Ok)
                            return Res;

                        Res = ReadFs(Processor);
                        if (Res.Status != ScriptStepStatus.Ok)
                            return Res;

                        Res = ReadDtb(Processor);
                        if (Res.Status != ScriptStepStatus.Ok)
                            return Res;

                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Kernel, ramfs and devtree are loaded FIT image!");
                    default:
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown image format!");

                }
            }

            private ScriptStepResult ReadKernel(ImageProcessor Processor)
            {
                var OldLoaded = Processor.GetKernel()?.Loaded ?? false;
                var Kernel = new ImageFormat.Elements.Kernel.LinuxKernel();
                switch (Format)
                {
                    case "raw":
                        {
                            var Importer = new ImageFormat.Elements.Kernel.Reader.ArchiveReader(Path, ImageFormat.Types.CompressionType.IH_COMP_NONE);
                            Importer.ReadToKernel(Kernel);
                            if (Kernel.Loaded)
                            {
                                Processor.SetKernel(Kernel);
                                if (OldLoaded)
                                    return new ScriptStepResult(ScriptStepStatus.Warning, $"Raw kernel image is loaded as kernel! Old kernel is replaced by this.");
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Raw kernel image is loaded as kernel!");
                            }
                            else
                                return new ScriptStepResult(ScriptStepStatus.Error, $"kernel file is not loaded!");
                        }
                    case "legacy":
                        {
                            var Importer = new ImageFormat.Elements.Kernel.Reader.LegacyReader(Path);
                            Importer.ReadToKernel(Kernel);
                            if (Kernel.Loaded)
                            {
                                Processor.SetKernel(Kernel);
                                if (OldLoaded)
                                    return new ScriptStepResult(ScriptStepStatus.Warning, $"Legacy image is loaded as kernel! Old kernel is replaced by this.");
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Legacy image is loaded as kernel!");
                            }
                            else
                                return new ScriptStepResult(ScriptStepStatus.Error, $"Legacy file is not loaded!");
                        }
                    case "lzma":
                    case "lz4":
                    case "gz":
                    case "gzip":
                    case "bzip2":
                    case "gz2":
                        {
                            var CompressionType = Helper.ArchiveHelper.GetCompressionFormat(Format);
                            var Importer = new ImageFormat.Elements.Kernel.Reader.ArchiveReader(Path, CompressionType);
                            Importer.ReadToKernel(Kernel);
                            if (Kernel.Loaded)
                            {
                                Processor.SetKernel(Kernel);
                                if (OldLoaded)
                                    return new ScriptStepResult(ScriptStepStatus.Warning, $"{Format} compressed image is loaded as kernel! Old kernel is replaced by this.");
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"{Format} compressed image is loaded as kernel!");
                            }
                            else
                                return new ScriptStepResult(ScriptStepStatus.Error, $"{Format} compressed file is not loaded!");
                        }
                    case "fit":
                        {
                            var Importer = new ImageFormat.Elements.Kernel.Reader.FitReader(Path);
                            Importer.ReadToKernel(Kernel);
                            if (Kernel.Loaded)
                            {
                                Processor.SetKernel(Kernel);
                                if (OldLoaded)
                                    return new ScriptStepResult(ScriptStepStatus.Warning, $"Kernel is loaded from FIT image! Old kernel is replaced by this.");
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Kernel is loaded from FIT image!");
                            }
                            else
                                return new ScriptStepResult(ScriptStepStatus.Error, $"gz file is not loaded!");
                        }
                    default:
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown kernel format!");
                }
            }

            private ScriptStepResult ReadDtb(ImageProcessor Processor)
            {
                var OldLoaded = Processor.GetDevTree()?.Loaded ?? false;
                var Dtb = new DeviceTree();
                switch (Format)
                {
                    case "dtb":
                        {
                            var Importer = new ImageFormat.Elements.Dtb.Reader.DtbReader(Path);
                            Importer.ReadToDevTree(Dtb);
                            Processor.SetDeviceTree(Dtb);
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"Dtb is loaded!");
                        }
                    case "fit":
                        {
                            var Importer = new ImageFormat.Elements.Dtb.Reader.FitReader(Path);
                            Importer.ReadToDevTree(Dtb);
                            Processor.SetDeviceTree(Dtb);
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"Devtree is loaded from Fit image!");
                        }
                    case "dts":
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Dts is not supported now!");
                    default:
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown devtree format!");
                }
            }

            private ScriptStepResult ReadFs(ImageProcessor Processor)
            {
                var OldLoaded = Processor.GetFs()?.Loaded ?? false;
                var Fs = new NyaFs.ImageFormat.Elements.Fs.Filesystem();
                switch (Format)
                {
                    case "cpio":
                        {
                            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.CpioReader(Path);
                            Importer.ReadToFs(Fs);
                            if (Fs.Loaded)
                            {
                                Processor.SetFs(Fs);
                                if(OldLoaded)
                                    return new ScriptStepResult(ScriptStepStatus.Warning, $"cpio image is loaded as filesystem! Old filesystem is replaced by this.");
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"cpio is loaded as filesystem!");
                            }
                            else
                                return new ScriptStepResult(ScriptStepStatus.Error, $"Cpio is not loaded!");
                        }
                    case "lz4":
                    case "lzma":
                    case "gz":
                    case "gzip":
                    case "bz2":
                    case "bzip2":
                        {
                            var CompressionType = Helper.ArchiveHelper.GetCompressionFormat(Format);
                            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.ArchiveReader(Path, CompressionType);
                            Importer.ReadToFs(Fs);
                            if (Fs.Loaded)
                            {
                                Processor.SetFs(Fs);
                                if (OldLoaded)
                                    return new ScriptStepResult(ScriptStepStatus.Warning, $"{Format} compressed image is loaded as filesystem! Old filesystem is replaced by this.");
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"{Format} compressed image is loaded as filesystem!");
                            }
                            else
                                return new ScriptStepResult(ScriptStepStatus.Error, $"{Format} file is not loaded!");
                        }
                    case "legacy":
                        {
                            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.LegacyReader(Path);
                            Importer.ReadToFs(Fs);
                            if (Fs.Loaded)
                            {
                                Processor.SetFs(Fs);
                                if (OldLoaded)
                                    return new ScriptStepResult(ScriptStepStatus.Warning, $"Cpio is loaded as filesystem! Old filesystem is replaced by this.");
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"legacy file is loaded as filesystem!");
                            }
                            else
                                return new ScriptStepResult(ScriptStepStatus.Error, $"legacy file is not loaded!");
                        }
                    case "fit":
                        {
                            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.FitReader(Path);
                            Importer.ReadToFs(Fs);
                            if (Fs.Loaded)
                            {
                                Processor.SetFs(Fs);
                                if (OldLoaded)
                                    return new ScriptStepResult(ScriptStepStatus.Warning, $"Fit is loaded as filesystem! Old filesystem is replaced by this.");
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Filesystem is loaded from FIT image!");
                            }
                            else
                                return new ScriptStepResult(ScriptStepStatus.Error, $"legacy file is not loaded!");
                        }
                    case "ext2":
                        {
                            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.ExtReader(Path);
                            Importer.ReadToFs(Fs);
                            if (Fs.Loaded)
                            {
                                Processor.SetFs(Fs);
                                if (OldLoaded)
                                    return new ScriptStepResult(ScriptStepStatus.Warning, $"Ext2 image is loaded as filesystem! Old filesystem is replaced by this.");
                                else
                                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Filesystem is loaded from ext2 image!");
                            }
                            else
                                return new ScriptStepResult(ScriptStepStatus.Error, $"legacy file is not loaded!");
                        }
                    default:
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown fs format!");
                }
            }
        }
    }
}
