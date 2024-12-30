using NyaFs.Processor.Scripting;
using System;

namespace NyaFsBmu
{
    public class NyaFsBmuPlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public NyaFsBmuPlugin() : base("bmu")
        {

        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[] {
                new Commands.LoadBmu()
            };
        }
    }
}
