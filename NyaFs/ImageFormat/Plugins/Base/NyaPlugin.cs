using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.Base
{
    public class NyaPlugin
    {
        public readonly string Name;
        public readonly string PluginType;

        public NyaPlugin(string Name, string Type) 
        {
            this.Name = Name;
        }
    }
}
