using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NyaFs.ImageFormat.Plugins.Base
{
    public class ServicePlugin : NyaPlugin
    {
        private Thread loopthread;
        private Processor.ImageProcessor processor = null;
        private bool Running = false;

        public readonly string ServiceName;

        public ServicePlugin(string Name, string ServiceName) : base(Name, "service")
        {
            this.ServiceName = ServiceName;
        }

        public bool IsRunning() => Running;

        protected virtual void OnStart()
        {

        }

        protected virtual void OnStop()
        {

        }

        public void Run(Processor.ImageProcessor Proc)
        {
            Running = true;
            processor = Proc;
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
            OnStart();
            Setup();
            while(Running)
            {
                Loop();
                Thread.Sleep(1);
            }
            OnStop();
        }

        protected virtual void Setup()
        {

        }

        protected virtual void Loop()
        {

        }

        /// <summary>
        /// Print sservice usage
        /// </summary>
        public virtual void Usage()
        {
            Log.Write(0, "  start: run service");
            Log.Write(0, "   stop: stop service");
            Log.Write(0, " status: stop service status (running/stopped)");
            Log.Write(0, "   info: show service information");
            Log.Write(0, "    set: set options <option> <param>");
        }

        /// <summary>
        /// Information about service
        /// </summary>
        public virtual void Info()
        {

        }

        /// <summary>
        /// Set service options
        /// </summary>
        public virtual void SetOption(string Param, string Value)
        {

        }
    }
}
