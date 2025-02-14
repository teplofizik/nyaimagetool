using NyaFs.Processor.Scripting;
using System;

namespace NyaFsRemote
{
    public class RemotePlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public RemotePlugin() : base("remote")
        {

        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[] {
                new Commands.CopySSHFrom()
            };
        }
    }
}
