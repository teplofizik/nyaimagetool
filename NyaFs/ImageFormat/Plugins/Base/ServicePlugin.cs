using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NyaFs.ImageFormat.Plugins.Base
{
    public class ServicePlugin : NyaPlugin
    {
        private Thread loopthread;
        private Processor.ImageProcessor processor;
        private bool Running = false;

        public readonly string ServiceName;

        public ServicePlugin(string Name, string ServiceName) : base(Name, "service")
        {
            this.ServiceName = ServiceName;
        }

        public bool IsRunning() => Running;

        public void Run(Processor.ImageProcessor Proc)
        {
            Running = true;
            loopthread = new Thread(ServiceLoop);
            loopthread.Start();
        }

        public void Stop()
        {
            Running = false;
            loopthread.Join();
        }

        protected Processor.ImageProcessor getProcessor() => processor;

        private void ServiceLoop()
        {
            Setup();
            while(Running)
            {
                Loop();
            }
        }

        protected virtual void Setup()
        {

        }

        protected virtual void Loop()
        {

        }
    }
}
