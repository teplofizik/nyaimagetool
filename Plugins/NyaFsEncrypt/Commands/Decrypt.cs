using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Templates;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsIncmEncrypt.Commands
{
    class Decrypt : ScriptStepGenerator
    {
        public Decrypt() : base("decrypt")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new DecryptScriptStep(Args.RawArgs[0]);
        }

        public class DecryptScriptStep : FileProcessScriptStep
        {
            public DecryptScriptStep(string Path) : base("decrypt", Path)
            {

            }

            /// <summary>
            /// Decrypt data in file
            /// </summary>
            /// <param name="Path"></param>
            /// <param name="Data"></param>
            /// <returns></returns>
            protected override byte[] ProcessData(string Path, byte[] Data)
            {
                byte[] Res = new byte[Data.Length];

                // Simple EXAMPLE ADD+XOR decryption
                for (int i = 0; i < Res.Length; i++)
                    Res[i] = Convert.ToByte(((Data[i] ^ 0x55) - 0x12) & 0xFF);

                return Res;
            }
        }
    }
}