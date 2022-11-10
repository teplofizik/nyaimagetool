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
                new Params.StringScriptArgsParam("name")
            }));
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] { 
                new Params.StringScriptArgsParam("name"),
                new Params.StringScriptArgsParam("command")
            }));
            AddConfig(new ScriptArgsConfig(2, new ScriptArgsParam[] {
                new Params.StringScriptArgsParam("name"),
                new Params.EnumScriptArgsParam("command", new string[] { "set" }),
                new Params.StringScriptArgsParam("option"),
                new Params.StringScriptArgsParam("value")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            switch (Args.ArgConfig)
            {
                case 1: return new ServiceScriptStep(Args.RawArgs[0], Args.RawArgs[1]);
                case 2: return new ServiceParamScriptStep(Args.RawArgs[0], Args.RawArgs[2], Args.RawArgs[3]);
                default:
                    return new ServiceUsageScriptStep(Args.RawArgs[0]);
            } 
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
                        case "info":
                            Plugin.Info();
                            break;
                        default:
                            return new ScriptStepResult(ScriptStepStatus.Error, "Unknown command. Use 'start', 'stop', 'status', 'info'");

                    }
                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, "Invalid service name");
            }
        }

        public class ServiceParamScriptStep : ScriptStep
        {
            private string ServiceName;
            private string Param;
            private string Value;

            public ServiceParamScriptStep(string Name, string Param, string Value) : base("service")
            {
                ServiceName = Name;

                this.Param = Param;
                this.Value = Value;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Plugin = Processor.Plugins.GetService(ServiceName);
                if (Plugin != null)
                {
                    Plugin.SetOption(Param, Value);
                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, "Invalid service name");
            }
        }

        public class ServiceUsageScriptStep : ScriptStep
        {
            private string ServiceName;

            public ServiceUsageScriptStep(string Name) : base("service")
            {
                ServiceName = Name;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                if (ServiceName == "")
                    return new ScriptStepResult(ScriptStepStatus.Ok, "Specify service name and command. Use commands 'start', 'stop', 'status', 'info', 'set <param> <option>'");
                else
                {
                    var Plugin = Processor.Plugins.GetService(ServiceName);
                    if (Plugin != null)
                    {
                        Plugin.Usage();

                        return new ScriptStepResult(ScriptStepStatus.Ok, null);
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, "Invalid service name");
                }
            }
        }
    }
}
