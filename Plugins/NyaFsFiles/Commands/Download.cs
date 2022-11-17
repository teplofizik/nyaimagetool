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
    class Download : ScriptStepGenerator
    {

        public Download() : base("download")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.LocalPathScriptArgsParam(),
                new NyaFs.Processor.Scripting.Params.UrlScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new DownloadScriptStep(Args.RawArgs[0], Args.RawArgs[1]);
        }


        public class DownloadScriptStep : ScriptStep
        {
            private string Filename;
            private string Url;

            public DownloadScriptStep(string Filename, string Url) : base("download")
            {
                this.Filename = Filename;
                this.Url = Url;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                try
                {
                    WebClient wc_ = new WebClient();
                    wc_.Headers.Add(HttpRequestHeader.UserAgent, "Other");
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateServerCertificate);
                    wc_.DownloadFile(Url, Filename);

                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }
                catch (Exception E)
                {
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Cannot download '{Filename} from {Url}: {E.Message}'");
                }
            }

            private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            }
        }
    }
}
