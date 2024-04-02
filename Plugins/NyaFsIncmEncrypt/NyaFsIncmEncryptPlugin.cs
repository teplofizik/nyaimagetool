using NyaFs.Processor.Scripting;
using System;

namespace NyaFsIncmEncrypt
{
    public class NyaFsIncmEncryptPlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public NyaFsIncmEncryptPlugin() : base("incmencrypt")
        {

        }

        public override ScriptStepGenerator[] GetGenerators()
        {
            return new ScriptStepGenerator[] {
                new Commands.Encrypt(),
                new Commands.Decrypt()
            };
        }
    }
}
