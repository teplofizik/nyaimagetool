using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace NyaImageTool.Commands
{
    internal sealed class Version : ScriptStepGenerator
    {
        public Version() : base("version")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { }));
        }

        public override ScriptStep Get(ScriptArgs Args) => new VersionStep();

        private sealed class VersionStep : ScriptStep
        {
            public VersionStep() : base("version")
            {
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                NyaFs.Log.Write(0, $"NyaImageTool {VersionOf(Assembly.GetExecutingAssembly())}");
                NyaFs.Log.Write(0, $"NyaFs        {VersionOf(typeof(ImageProcessor).Assembly)}");

                var NyaLZO = TryLoad("NyaLZO");
                var NyaXZ = TryLoad("NyaXZ");
                NyaFs.Log.Write(0, $"NyaLZO       {(NyaLZO == null ? "not loaded" : VersionOf(NyaLZO))}");
                NyaFs.Log.Write(0, $"NyaXZ        {(NyaXZ == null ? "not loaded" : VersionOf(NyaXZ))}");

                var Plugins = GetPlugins();
                NyaFs.Log.Write(0, $"Plugins ({Plugins.Count}):");

                foreach (var Plugin in Plugins)
                    NyaFs.Log.Write(0, $"  {Plugin.Name,-28} {Plugin.Version}");

                return new ScriptStepResult(ScriptStepStatus.Ok, null);
            }

            private static Assembly TryLoad(string Name)
            {
                try
                {
                    return Assembly.Load(new AssemblyName(Name));
                }
                catch
                {
                    return null;
                }
            }

            private static string VersionOf(Assembly Assembly)
            {
                var Info = Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!String.IsNullOrEmpty(Info))
                    return Info.Split('+')[0];

                return Assembly.GetName().Version?.ToString() ?? "unknown";
            }

            private static List<PluginVersion> GetPlugins()
            {
                var Result = new List<PluginVersion>();
                var Seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var Directories = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "plugins"),
                    "plugins"
                };

                foreach (var Directory in Directories)
                {
                    if (String.IsNullOrEmpty(Directory) || !System.IO.Directory.Exists(Directory))
                        continue;

                    foreach (var File in System.IO.Directory.GetFiles(Directory, "NyaFs*.dll"))
                    {
                        var Name = Path.GetFileNameWithoutExtension(File);
                        if (String.Equals(Name, "NyaFs", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!Seen.Add(Name))
                            continue;

                        try
                        {
                            var AsmName = AssemblyName.GetAssemblyName(File);
                            var Info = FileVersionInfo.GetVersionInfo(File);
                            var Version = Info.ProductVersion ?? Info.FileVersion ?? AsmName.Version?.ToString() ?? "unknown";
                            Result.Add(new PluginVersion(Name, Version.Split('+')[0]));
                        }
                        catch
                        {
                            Result.Add(new PluginVersion(Name, "unknown"));
                        }
                    }
                }

                Result.Sort((A, B) => StringComparer.OrdinalIgnoreCase.Compare(A.Name, B.Name));
                return Result;
            }

            private sealed class PluginVersion
            {
                public readonly string Name;
                public readonly string Version;

                public PluginVersion(string Name, string Version)
                {
                    this.Name = Name;
                    this.Version = Version;
                }
            }
        }
    }
}
