using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace NyaFs.Processor.Scripting.Plugins
{
    class PluginLoader
    {
        public static Assembly LoadPlugin(string relativePath)
        {
            string pluginLocation = Path.GetFullPath(relativePath);
           // Console.WriteLine($"Loading commands from: {pluginLocation}");

            PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }
    }
}
