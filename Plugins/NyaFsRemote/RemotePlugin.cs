using NyaFs.Processor.Scripting;

namespace NyaFsRemote
{
    /// <summary>Legacy "copysshfrom" command. Prefer the core "remoteget ssh ...".</summary>
    public class RemotePlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public RemotePlugin() : base("remote")
        {
        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[]
            {
                new Commands.CopySSHFrom()
            };
        }
    }
}
