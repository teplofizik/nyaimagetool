using NyaFs.Processor.Scripting;
using System;

namespace NyaFsFiles
{
    public class FilesPlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public FilesPlugin() : base("files")
        {

        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[] {
                new Commands.Copy(),
                new Commands.Download(),
                new Commands.Remove()
            };
        }
    }
}
