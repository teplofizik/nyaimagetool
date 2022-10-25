using NyaFs.ImageFormat.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting
{
    public class ScriptPlugins
    {
        private List<NyaPlugin> LoadedPlugins = new List<NyaPlugin>();

        private List<CompressorPlugin> Compressors = new List<CompressorPlugin>();
        private List<FilesystemPlugin> Filesystems = new List<FilesystemPlugin>();
        private List<ServicePlugin> Services = new List<ServicePlugin>();

        /// <summary>
        /// Get compressor plugin by name
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public CompressorPlugin GetCompressors(string Name)
        {
            foreach (var S in Compressors)
            {
                if (S.Name == Name)
                    return S;
            }

            return null;
        }

        /// <summary>
        /// Get filesystem plugin by name
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public FilesystemPlugin GetFilesystem(string Name)
        {
            foreach (var S in Filesystems)
            {
                if (S.Name == Name)
                    return S;
            }

            return null;
        }

        /// <summary>
        /// Get service plugin by name
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public ServicePlugin GetService(string Name)
        {
            foreach (var S in Services)
            {
                if (S.Name == Name)
                    return S;
            }

            return null;
        }

        private bool IsLoaded(string Name)
        {
            foreach(var P in LoadedPlugins)
            {
                if (P.Name == Name)
                    return true;
            }

            return false;
        }

        private bool AddPlugin(NyaPlugin Plugin)
        {
            var Compressor = Plugin as CompressorPlugin;
            if (Compressor != null)
            {
                Compressors.Add(Compressor);
                return true;
            }

            var Filesystem = Plugin as FilesystemPlugin;
            if (Filesystem != null)
            {
                Filesystems.Add(Filesystem);
                return true;
            }

            var Service = Plugin as ServicePlugin;
            if (Service != null)
            {
                Services.Add(Service);
                return true;
            }

            return false;
        }

        public void Load(NyaPlugin Plugin)
        {
            if(IsLoaded(Plugin.Name))
                Log.Error(0, $"Plugin with name {Plugin.Name} already loaded");
            else
            {
                if(CheckPlugin(Plugin))
                {
                    if (AddPlugin(Plugin))
                    {
                        LoadedPlugins.Add(Plugin);
                        Log.Ok(3, $"Plugin {Plugin.Name} was loaded.");
                    }
                    else
                        Log.Error(0, $"Plugin {Plugin.Name} has unsupported type.");
                }
                else
                    Log.Error(0, $"Plugin {Plugin.Name} cannot be loaded.");
            }
        }

        private bool CheckPlugin(NyaPlugin Plugin)
        {
            return true;
        }
    }
}
