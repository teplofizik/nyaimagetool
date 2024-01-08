using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
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
            private readonly string Filename;
            private readonly string Url;

            public DownloadScriptStep(string Filename, string Url) : base("download")
            {
                this.Filename = Filename;
                this.Url = Url;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                try
                {
                    using HttpClient client = new();

                    client.BaseAddress = new Uri(Url);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("User-Agent", "Other");
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateServerCertificate);

                    var Req = client.GetAsync(Url); Req.Wait();
                    HttpResponseMessage response = Req.Result;

                    if (response.IsSuccessStatusCode)
                    {
                        HttpContent content = response.Content;
                        var contentStream = content.ReadAsStream();

                        using (FileStream outputFileStream = new(Filename, FileMode.Create))
                        {
                            contentStream.CopyTo(outputFileStream);
                        }

                        return new ScriptStepResult(ScriptStepStatus.Ok, null);
                    }
                    else
                    {
                        throw new FileNotFoundException();
                    }
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
