using NyaFs.Processor.Scripting;
using System;

namespace NyaFsRockchip
{
    public class NyaFsRockchipPlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public NyaFsRockchipPlugin() : base("rockchip")
        {

        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[] {
                new Commands.LoadRkfw(),
                new Commands.LsRkfw(),
                new Commands.ReadRkfw()
            };
        }
    }
}
