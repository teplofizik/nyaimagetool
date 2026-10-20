using NyaFs.Processor.Scripting;

namespace NyaFsFtp
{
    public class FtpPlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public FtpPlugin() : base("ftp")
        {
        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[]
            {
                new Commands.FtpGet(),
                new Commands.FtpPut()
            };
        }
    }
}
