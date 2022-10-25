using NyaFs.ImageFormat.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands
{
    public class LoadPlugin : ScriptStepGenerator
    {
        public LoadPlugin() : base("loadplugin")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { 
                new Params.LocalPathScriptArgsParam(),
                new Params.StringScriptArgsParam("name")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new LoadPluginScriptStep(Args.RawArgs[0], Args.RawArgs[1]);
        }

        public class LoadPluginScriptStep : ScriptStep
        {
            private string Filename;
            private string ClassName;

            public LoadPluginScriptStep(string Filename, string ClassName) : base("loadplugin")
            {
                this.Filename = Filename;
                this.ClassName = ClassName;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var PluginAssembly = Assembly.Load(Filename);

                if (PluginAssembly != null)
                {
                    object Class = PluginAssembly.CreateInstance(ClassName);
                    if(Class != null)
                    {
                        var Plugin = Class as NyaPlugin;
                        if (Plugin != null)
                        {
                            Processor.Plugins.Load(Plugin);

                            return new ScriptStepResult(ScriptStepStatus.Ok, null);
                        }
                        else
                            return new ScriptStepResult(ScriptStepStatus.Error, $"Class '{ClassName}' in file '{Filename}' do not successor of NyaPlugin.");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Cannot find class '{ClassName}' in file '{Filename}'");

                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Cannot load assembly '{Filename}'");
            }
        }
    }
}
