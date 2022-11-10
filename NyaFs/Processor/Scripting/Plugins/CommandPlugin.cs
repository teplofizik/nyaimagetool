using NyaFs.ImageFormat.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Plugins
{
    public class CommandPlugin : NyaPlugin
    {
        public CommandPlugin(string Name) : base (Name, "command")
        {

        }

        public virtual ScriptStepGenerator[] GetGenerators() => null;
    }
}
