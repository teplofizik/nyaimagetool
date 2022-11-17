using NyaFs.Processor.Scripting;
using System;

namespace NyaFsTftp
{
    public class TFtpClient : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public TFtpClient() : base("tftp")
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
