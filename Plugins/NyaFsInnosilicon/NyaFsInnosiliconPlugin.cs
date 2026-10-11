using NyaFs.Processor.Scripting;
using System;

namespace NyaFsInnosilicon
{
    public class NyaFsInnosiliconPlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        // https://github.com/Hitsxx/Innosilicon-LTC-Miner-Firmware/blob/master/home/inno_py/inno_upgrade.py
        public NyaFsInnosiliconPlugin() : base("innosilicon")
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
