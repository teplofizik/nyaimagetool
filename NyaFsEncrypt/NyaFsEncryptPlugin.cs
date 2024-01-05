using NyaFs.Processor.Scripting;
using System;

namespace NyaFsIncmEncrypt
{
    public class NyaFsEncryptPlugin : NyaFs.Processor.Scripting.Plugins.CommandPlugin
    {
        public NyaFsEncryptPlugin() : base("encrypt")
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
