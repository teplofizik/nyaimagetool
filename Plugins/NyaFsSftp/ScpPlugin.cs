using NyaFsSftp.Ssh;
using System;

namespace NyaFsSftp
{
    
    public class SftpPlugin : NyaFs.ImageFormat.Plugins.Base.ServicePlugin
    {
        NyaSshService ssh;

        public SftpPlugin() : base("sftp", "SFTP service")
        {
        }

        protected override void OnStart()
        {
            ssh = new NyaSshService(getProcessor());
            ssh.Start();
        }

        protected override void OnStop()
        {
            ssh.Stop();
        }

        protected override void Setup()
        {
            base.Setup();
        }


        protected override void Loop()
        {
            base.Loop();
        }
    }
}
