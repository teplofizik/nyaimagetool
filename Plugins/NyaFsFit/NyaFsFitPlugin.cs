using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Plugins;
using System;

namespace NyaFsFit
{
    public class NyaFsFitPlugin : CommandPlugin
    {
        public NyaFsFitPlugin() : base("fitsource") { }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[]
            {
                new Commands.ExportFit()
            };
        }
    }
}
