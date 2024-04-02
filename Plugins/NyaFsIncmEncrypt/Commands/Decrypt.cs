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
        public Decrypt() : base("incmdecrypt")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new DecryptScriptStep();
        }

        public class DecryptScriptStep : IncmFileProcessScriptStep
        {
            public DecryptScriptStep() : base("incmdecrypt")
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

                NyaFs.Log.Ok(1, $"Decrypted {Path}");

                return Res;
            }

            /// <summary>
            /// Check is need to process file
            /// </summary>
            /// <param name="Path"></param>
            /// <returns></returns>
            protected override bool IsNeedProcessFile(string Path)
            {
                return Path.StartsWith("/etc/init.d/") && !Path.StartsWith("/etc/init.d/rc");
            }
        }
    }
}