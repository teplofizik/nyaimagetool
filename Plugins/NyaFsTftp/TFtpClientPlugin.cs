using NyaFs.Processor.Scripting;
using System;

namespace NyaFsTftp
{
    public class TFtpClientPlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public TFtpClientPlugin() : base("tftpclient")
        {

        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[] {
                new Commands.TFtpGet(),
                new Commands.TFtpPut()
            };
        }
    }
}
