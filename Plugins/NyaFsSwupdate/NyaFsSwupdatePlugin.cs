using NyaFs.Processor.Scripting;
using System;

namespace NyaFsSwupdate
{
    public class NyaFsSwupdatePlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        // https://github.com/sbabic/swupdate
        public NyaFsSwupdatePlugin() : base("swupdate")
        {

        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[] {
                new Commands.LoadSwu(),
                new Commands.LsSwu(),
                new Commands.ReadSwu()
            };
        }
    }
}
