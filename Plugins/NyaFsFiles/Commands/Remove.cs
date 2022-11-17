using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace NyaFsFiles.Commands
{
    class Remove : ScriptStepGenerator
    {

        public Remove() : base("remove")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.LocalPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new RemoveScriptStep(Args.RawArgs[0]);
        }


        public class RemoveScriptStep : ScriptStep
        {
            private string Filename;

            public RemoveScriptStep(string Filename) : base("remove")
            {
                this.Filename = Filename;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                try
                {
                    System.IO.File.Delete(Filename);

                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }
                catch (Exception E)
                {
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Cannot remove '{Filename} from host filesystem: {E.Message}'");
                }
            }

            private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            }
        }
    }
}
