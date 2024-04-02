using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Templates;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsIncmEncrypt.Commands
{
    class Encrypt : ScriptStepGenerator
    {
        public Encrypt() : base("incmencrypt")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new EncryptScriptStep();
        }

        public class EncryptScriptStep : IncmFileProcessScriptStep
        {
            public EncryptScriptStep() : base("incmencrypt")
            {

            }

            /// <summary>
            /// Encrypt data in file
            /// </summary>
            /// <param name="Path"></param>
            /// <param name="Data"></param>
            /// <returns></returns>
            protected override byte[] ProcessData(string Path, byte[] Data)
            {
                byte[] Res = new byte[Data.Length];

                // Simple EXAMPLE ADD+XOR encryption
                for (int i = 0; i < Res.Length; i++)
                    Res[i] = Convert.ToByte(((Data[i] + 0x12) ^ 0x55) & 0xFF);

                NyaFs.Log.Ok(1, $"Encrypted {Path}");

                return Res;
            }
        }
    }
}
