using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Plugins;

namespace NyaFsUboot
{
    public class NyaFsUbootPlugin : CommandPlugin
    {
        public NyaFsUbootPlugin() : base("uboot") { }

        public override ScriptStepGenerator[] GetGenerators() =>
            new ScriptStepGenerator[] { new Commands.Env() };
    }
}
