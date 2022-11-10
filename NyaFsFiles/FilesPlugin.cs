using NyaFs.Processor.Scripting;
using System;

namespace NyaFsFiles
{
    public class FilesPlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public FilesPlugin() : base("filescommands")
        {

        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[] {
                new Commands.Copy()
            };
        }
    }
}
