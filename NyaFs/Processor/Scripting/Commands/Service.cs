using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands
{
    public class Service : ScriptStepGenerator
    {
        public Service() : base("service")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { 
                new Params.StringScriptArgsParam("name"),
                new Params.StringScriptArgsParam("command")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            if(Args.ArgConfig == 0)
                return new ServiceScriptStep(Args.RawArgs[0], Args.RawArgs[1]);
            else
                return new ServiceScriptStep(Args.RawArgs[0], "");
        }

        public class ServiceScriptStep : ScriptStep
        {
            private string ServiceName;
            private string Command;

            public ServiceScriptStep(string Name, string Command) : base("service")
            {
                ServiceName = Name;
                this.Command = Command;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Plugin = Processor.Plugins.GetService(ServiceName);

                if (Plugin != null)
                {
                    switch(Command)
                    {
                        case "start": 
                            if(Plugin.IsRunning())
                                return new ScriptStepResult(ScriptStepStatus.Warning, "Service is already running");
                            else
                            {
                                Plugin.Run(Processor);
                                break;
                            }
                        case "stop":
                            if (!Plugin.IsRunning())
                                return new ScriptStepResult(ScriptStepStatus.Warning, "Service is not running");
                            else
                            {
                                Plugin.Stop();
                                break;
                            }
                        case "status":
                            {
                                if(Plugin.IsRunning())
                                    Log.Write(0, $"{Plugin.Name} is running");
                                else
                                    Log.Write(0, $"{Plugin.Name} is stopped");
                            }
                            break;
                        default:
                            return new ScriptStepResult(ScriptStepStatus.Error, "Unknown command. Use 'start', 'stop', 'status'");

                    }
                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, "Invalid service name");
            }
        }
    }
}
