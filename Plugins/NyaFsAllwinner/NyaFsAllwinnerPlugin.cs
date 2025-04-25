using NyaFs.Processor.Scripting;
using System;

namespace NyaFsAllwinner
{
    public class NyaFsAllwinnerPlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public NyaFsAllwinnerPlugin() : base("allwinner")
        {

        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[] {
                new Commands.LoadWty(), // loadwty xxx.bin
                new Commands.LsWty(),   // lswty xxx.bin
                new Commands.ReadWty()  // readwty xxx.bin rootfs.fex xxx.rootfs.fex
            };
        }
    }
}
