using NyaFs.Processor.Scripting;
using System;

namespace NyaFsLinux
{
    public class LinuxPlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public LinuxPlugin() : base("linux")
        {

        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[] {
                new Commands.LsUsers(),
                new Commands.LsHashes(),
                new Commands.Passwd()
            };
        }
    }
}
