using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace NyaFsTftp.Commands
{
    class TFtpPut : ScriptStepGenerator
    {

        public TFtpPut() : base("tftpput")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.LocalPathScriptArgsParam(),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("server"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("serverfilename"),
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new TFtpPutScriptStep(Args.RawArgs[0], Args.RawArgs[1], Args.RawArgs[2]);
        }

        public class TFtpPutScriptStep : ScriptStep
        {
            private AutoResetEvent TransferFinishedEvent = new AutoResetEvent(false);

            private string Filename;
            private string Server;
            private string ServerFN;

            /// <summary>
            /// Result of transfer
            /// </summary>
            private bool Result = false;
            /// <summary>
            /// Text of tftp download error (if Result = false)
            /// </summary>
            private string ErrorText = "";

            public TFtpPutScriptStep(string Filename, string Server, string ServerFN) : base("tftp")
            {
                this.Filename = Filename;
                this.Server = Server;
                this.ServerFN = ServerFN;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var client = new Tftp.Net.TftpClient(Server);
                var transfer = client.Upload(ServerFN);

                //Capture the events that may happen during the transfer
                transfer.OnProgress += Transfer_OnProgress;
                transfer.OnFinished += Transfer_OnFinished;
                transfer.OnError += Transfer_OnError;

                Stream stream = new MemoryStream(File.ReadAllBytes(Filename));
                transfer.Start(stream);

                TransferFinishedEvent.WaitOne();
                if(Result)
                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, ErrorText);
            }

            private void Transfer_OnError(Tftp.Net.ITftpTransfer transfer, Tftp.Net.TftpTransferError error)
            {
                ErrorText = error.ToString();
                Result = false;

                TransferFinishedEvent.Set();
            }

            private void Transfer_OnFinished(Tftp.Net.ITftpTransfer transfer)
            {
                Result = true;
                TransferFinishedEvent.Set();
            }

            private void Transfer_OnProgress(Tftp.Net.ITftpTransfer transfer, Tftp.Net.TftpTransferProgress progress)
            {
            }
        }
    }
}
