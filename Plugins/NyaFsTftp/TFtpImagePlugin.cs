using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;

namespace NyaFsTftp
{
    public sealed class TFtpImagePlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public TFtpImagePlugin() : base("tftpimage")
        {
        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[]
            {
                new Commands.TFtpImage()
            };
        }
    }
}
