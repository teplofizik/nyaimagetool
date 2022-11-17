using NyaFsSftp.Ssh;
using System;

namespace NyaFsSftp
{
    public class SftpPlugin : NyaFs.ImageFormat.Plugins.Base.ServicePlugin
    {
        /// <summary>
        /// SSH port. Default is 22
        /// </summary>
        private int Port = 22;

        /// <summary>
        ///  SSH server class
        /// </summary>
        NyaSshService ssh;

        public SftpPlugin() : base("sftp", "SFTP service")
        {

        }

        protected override void OnStart()
        {
            ssh = new NyaSshService(Port, getProcessor());
            NyaFs.Log.Ok(3, $"SFTP server is running on 0.0.0.0:{Port}");

            ssh.Start();
        }

        protected override void OnStop()
        {
            ssh.Stop();
            ssh = null;
            NyaFs.Log.Ok(0, $"SFTP server is stopped");
        }

        /// <summary>
        /// Information about service
        /// </summary>
        public override void Info()
        {
            NyaFs.Log.Write(0, "SFTP server");
            NyaFs.Log.Write(0, $"    port: {Port}");
            NyaFs.Log.Write(0, $" running: {((ssh != null) ? "yes" : "no")}");
        }

        /// <summary>
        /// Print sservice usage
        /// </summary>
        public override void Usage()
        {
            base.Usage();

            NyaFs.Log.Write(0, $"  set port <portnumber> (at now portnumber is {Port})");
        }

        /// <summary>
        /// Set service options
        /// </summary>
        public override void SetOption(string Param, string Value)
        {
            switch(Param)
            {
                case "port":
                    {
                        int Res;
                        if(Int32.TryParse(Value, out Res))
                        {
                            if((Res > 0) && (Res < 64000))
                            {
                                Port = Res;
                                return;
                            }
                        }
                        NyaFs.Log.Error(0, $"Invalid value {Value} for param {Param}.");
                    }
                    break;
                default:
                    NyaFs.Log.Error(0, $"Unknown option {Param}");
                    break;
            }
        }
    }
}
