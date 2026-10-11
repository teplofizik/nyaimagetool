using NyaFs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Steps;
using NyaFsRockchip.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace NyaFsRockchip.Commands
{
    class ReadRkfw : ScriptStepGenerator
    {
        public ReadRkfw() : base("loadrkfw")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new LoadRkfwScriptStep(Args.RawArgs[0]);
        }

        public class LoadRkfwScriptStep : ImageImportScriptStep
        {
            private string Path;

            public LoadRkfwScriptStep(string Path) : base("loadrkfw")
            {
                this.Path = Path;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                // Проверим наличие локального файла, содержимое которого надо загрузить в ФС
                if (!System.IO.File.Exists(Path))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found!");

                var Data = File.ReadAllBytes(Path);
                var Image = new RkfwImage(Data);

                if (Image.Correct)
                {
                    var FW = Image.FirmwareData;
                    if (FW.Correct)
                    {
                        // File list
                        for(int i = 0; i < FW.FileCount; i++)
                        {
                            var Header = FW.GetHeader(i);

                            Log.Write(0, $"{Header.Name}: {Header.FileName}  offset: {Header.RawOffset:X08}  nand: {Header.NandAddr:X08} img: {Header.ImgFSize:X08} orig: {Header.OrigFSize:X08}");
                        }

                        var FsRes = ReadFs(Processor, FW);
                        if (FsRes.Status == ScriptStepStatus.Error)
                            return FsRes;

                        /*var KernelRes = ReadKernel(Processor, Image);
                        if (FsRes.Status == ScriptStepStatus.Error)
                            return KernelRes;

                        var DevtreeRes = ReadDtb(Processor, Image);
                        if (FsRes.Status == ScriptStepStatus.Error)
                            return DevtreeRes;*/

                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Rockchip image is loaded!");
                    }
                }
                
                return new ScriptStepResult(ScriptStepStatus.Error, $"Rockchip image is invalid image!");
            }


            private ScriptStepResult ReadFs(ImageProcessor Processor, RkfwData Data)
            {
                var OldLoaded = Processor.GetFs()?.Loaded ?? false;
                var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();

                var Raw = Data.GetPartition("rootfs");
                if (Fs != null)
                {
                    var FsReader = new NyaFsRockchip.Loader.RkfwFsReader(Raw);

                    //if(!File.Exists($"{Path}.fs.bin"))
                    //    System.IO.File.WriteAllBytes($"{Path}.fs.bin", Raw);

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
                            return new ScriptStepResult(ScriptStepStatus.Warning, $"Rockchip image is loaded as filesystem! Old filesystem is replaced by this.");
                        else
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"Rockchip is loaded as filesystem!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown fs format!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"No rootfs partition in firmware package!");
            }
        }
    }
}
