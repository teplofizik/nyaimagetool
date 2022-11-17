using System;
using System.Collections.Generic;
using System.Text;
using Tftp.Net;

namespace NyaFsTftp
{
    class TFtpServerPlugin : NyaFs.ImageFormat.Plugins.Base.ServicePlugin
    {
        /// <summary>
        /// TFTP port. Default is 69
        /// </summary>
        int Port = 69;
        
        /// <summary>
        /// TFTP server class
        /// </summary>
        TftpServer server;

        /// <summary>
        /// Default server directory
        /// </summary>
        string ServerDirectory = "./";

        public TFtpServerPlugin() : base("tftp", "TFTP service")
        {

        }

        protected override void OnStart()
        {
            server = new TftpServer(Port);
            server.OnReadRequest += Server_OnReadRequest;
            server.OnWriteRequest += Server_OnWriteRequest;
            NyaFs.Log.Ok(3, $"TFTP server is running on 0.0.0.0:{Port}");

            server.Start();
        }

        private void Server_OnWriteRequest(ITftpTransfer transfer, System.Net.EndPoint client)
        {
            String file = System.IO.Path.Combine(ServerDirectory, transfer.Filename);
            transfer.Start(new System.IO.FileStream(file, System.IO.FileMode.CreateNew));
        }

        private void Server_OnReadRequest(ITftpTransfer transfer, System.Net.EndPoint client)
        {
            String file = System.IO.Path.Combine(ServerDirectory, transfer.Filename);
            transfer.Start(new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read));
        }

        protected override void OnStop()
        {
            server.Dispose();
            server = null;
            NyaFs.Log.Ok(0, $"TFTP server is stopped");
        }

        /// <summary>
        /// Information about service
        /// </summary>
        public override void Info()
        {
            NyaFs.Log.Write(0, "TFTP server");
            NyaFs.Log.Write(0, $"    port: {Port}");
            NyaFs.Log.Write(0, $"     dir: {ServerDirectory}");
            NyaFs.Log.Write(0, $" running: {((server != null) ? "yes" : "no")}");
        }

        /// <summary>
        /// Print sservice usage
        /// </summary>
        public override void Usage()
        {
            base.Usage();

            NyaFs.Log.Write(0, $"  set port <portnumber> (at now portnumber is {Port})");
            NyaFs.Log.Write(0, $"  set dir  <directory>  (at now directory is '{ServerDirectory}')");
        }

        /// <summary>
        /// Set service options
        /// </summary>
        public override void SetOption(string Param, string Value)
        {
            switch (Param)
            {
                case "port":
                    {
                        int Res;
                        if (Int32.TryParse(Value, out Res))
                        {
                            if ((Res > 0) && (Res < 64000))
                            {
                                Port = Res;
                                return;
                            }
                        }
                        NyaFs.Log.Error(0, $"Invalid value {Value} for param {Param}.");
                    }
                    break;
                case "dir":
                    if(System.IO.Directory.Exists(Value))
                        ServerDirectory = Value;
                    else
                        NyaFs.Log.Error(0, $"Invalid path {Value} for param {Param}. Such directory is not found.");
                    break;
                default:
                    NyaFs.Log.Error(0, $"Unknown option {Param}");
                    break;
            }
        }
    }
}