using NyaFs.ImageFormat.Elements.Dtb;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.ImageFormat.Elements.Kernel;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor
{
    public class ImageProcessor
    {
        public string ActivePath = "/";

        /// <summary>
        /// Session images blob
        /// </summary>
        ImageFormat.BaseImageBlob Blob = new ImageFormat.BaseImageBlob();

        /// <summary>
        /// Session variable scope
        /// </summary>
        public Scripting.Variables.VariableScope Scope = new Scripting.Variables.VariableScope();

        /// <summary>
        /// Session plugins
        /// </summary>
        public Scripting.ScriptPlugins Plugins = new Scripting.ScriptPlugins();

        /// <summary>
        /// Session parser
        /// </summary>
        public Scripting.ScriptParser Parser;

        public ImageProcessor(Scripting.ScriptParser Parser)
        {
            this.Parser = Parser;
        }

        public void SetFs(LinuxFilesystem Fs) => Blob.SetFilesystem(0, Fs);

        public void SetKernel(LinuxKernel Kernel) => Blob.SetKernel(0, Kernel);

        public void SetDeviceTree(DeviceTree Dtb) => Blob.SetDevTree(0, Dtb);

        public void Reset()
        {
            Blob = new ImageFormat.BaseImageBlob();
        }

        public LinuxKernel GetKernel() => Blob.GetKernel(0);

        public LinuxFilesystem GetFs() => Blob.GetFilesystem(0);

        public DeviceTree GetDevTree() => Blob.GetDevTree(0);

        public ImageFormat.BaseImageBlob GetBlob() => Blob;

        public bool IsFsLoaded => Blob.IsProvidedFs;

        public void Process(Scripting.Script Script)
        {
            foreach(var S in Script.Steps)
            {
                if (S.CheckCondition(this))
                {
                    var Res = S.Exec(this);

                    WriteLogLine(S, Res);
                }
            }
        }

        private void WriteLogLine(Scripting.ScriptStep Step, Scripting.ScriptStepResult Res)
        {
            if (Res.Text != null)
            {
                switch (Res.Status)
                {
                    case ScriptStepStatus.Error: Log.Error(0, $"{Step.Name} [{Step.ScriptName}:{Step.ScriptLine}]: {Res.Text}"); break;
                    case ScriptStepStatus.Ok: Log.Ok(0, $"{Step.Name}: {Res.Text}"); break;
                    case ScriptStepStatus.Warning: Log.Warning(0, $"{Step.Name} [{Step.ScriptName}:{Step.ScriptLine}]: {Res.Text}"); break;
                }
            }
        }
    }
}
