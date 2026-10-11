using NyaFs.Processor.Scripting;
using System;

namespace NyaFsBmu
{
    public class NyaFsBmuCommandPlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public NyaFsBmuCommandPlugin() : base("bmu")
        {

        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[] {
                new Commands.LoadBmu(),
                new Commands.ReadBmu()
            };
        }
    }
}
