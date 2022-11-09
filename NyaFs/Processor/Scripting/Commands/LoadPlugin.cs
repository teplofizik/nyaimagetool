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
                new Params.LocalPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new LoadPluginScriptStep(Args.RawArgs[0], null);
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
                // https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
                // https://habr.com/ru/post/242209/
                try
                {
                    var Loaded = Processor.Plugins.LoadFromFile(Filename);

                    if (Loaded.Length > 0)
                        return new ScriptStepResult(ScriptStepStatus.Ok, null);
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"File '{Filename}' has no any successors of NyaPlugin.");
                }
                catch (Exception E)
                {
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Cannot load assembly '{Filename}: {E.Message}'");
                }
            }
        }
    }
}
