using NyaFs.Processor.Scripting.Configs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs
{
    public class Rm : ScriptStepGenerator
    {
        public Rm() : base("rm")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new Params.FsPathScriptArgsParam()
            }));
            AddConfig(new AnyConfig(1));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            if(Args.ArgConfig == 0)
                return new RmScriptStep(Args.RawArgs[0]);
            else
                return new RmScriptStep(Args.RawArgs);
        }

        public class RmScriptStep : ScriptStep
        {
            List<string> Pathes = new List<string>();
            List<string> Excluded = new List<string>();
            List<string> ExcludeMask = new List<string>();
            int Total = 0;

            public RmScriptStep(string path) : base("rm")
            {
                Pathes.Add(path);
            }
            public RmScriptStep(string[] args) : base("rm")
            {
                foreach(var A in args)
                {
                    if (A.StartsWith("-"))
                    {
                        // Добавляем в исключения
                        ExcludeMask.Add(A.Substring(1));
                    }
                    else
                        Pathes.Add(A);
                }
            }

            private bool IsExcluded(string path) => Excluded.Contains(path);

            private void GenerateExcludedFileList(ImageProcessor processor, ImageFormat.Elements.Fs.LinuxFilesystem fs)
            {
                foreach (var path in ExcludeMask)
                {
                    if (path.Contains('*'))
                    {
                        if (path.StartsWith("/"))
                        {
                            var Items = fs.Search(path.Substring(1));

                            Excluded.AddRange(Items);
                        }
                        else
                        {
                            var Dir = fs.GetDirectory(processor.ActivePath);
                            if (Dir != null)
                            {
                                var Items = fs.Search(Dir, path);

                                Excluded.AddRange(Items);
                            }
                        }
                    }
                    else
                    {
                        if (path.StartsWith("/"))
                        {
                            if (fs.Exists(path))
                                Excluded.Add(path.Substring(1));
                        }
                        else
                        {
                            var Dir = fs.GetDirectory(processor.ActivePath);
                            if (Dir != null)
                            {
                                var Items = fs.Search(Dir, "*" + path);

                                Excluded.AddRange(Items);
                            }
                        }
                    }
                }
            }

            private void Delete(ImageFormat.Elements.Fs.LinuxFilesystem fs, string path)
            {
                if (fs.Exists(path))
                {
                    if (!IsExcluded(path))
                    {
                        fs.Delete(path);
                        Log.Write(2, $"Deleted {path}");
                        Total++;
                    }
                    else
                        Log.Warning(2, $"Item is not removed: {path}");
                }
            }

            private void ProcessPath(ImageProcessor processor, ImageFormat.Elements.Fs.LinuxFilesystem fs, string path)
            {
                if (path.Contains('*'))
                {
                    if (path.StartsWith("/"))
                    {
                        // Это путь с маской
                        var Items = fs.Search(path.Substring(1));

                        if (Items.Length > 0)
                        {
                            foreach (var I in Items)
                                Delete(fs, I);
                        }
                        else
                            Log.Warning(0, $"{path} not found!");
                    }
                    else
                    {
                        var Dir = fs.GetDirectory(processor.ActivePath);
                        if (Dir != null)
                        {
                            var Items = fs.Search(Dir, path);
                            if (Items.Length > 0)
                            {
                                foreach (var I in Items) 
                                    Delete(fs, I);
                            }
                            else
                                Log.Warning(0, $"{path} not found!");
                        }
                        else
                            Log.Warning(0, $"{processor.ActivePath} not found!");
                    }
                }
                else
                {
                    if (fs.Exists(path))
                    {
                        // Есть старый файл в файловой системе. Удалим.
                        Delete(fs, path);
                    }
                    else
                        Log.Warning(0, $"{path} not found!");
                }
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                // Проверим наличие загруженной файловой системы
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                if(ExcludeMask.Count > 0)
                    GenerateExcludedFileList(Processor, Fs);

                foreach (var P in Pathes)
                    ProcessPath(Processor, Fs, P);

                if(Total > 0)
                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Deleted {Total} items!");
                else
                    return new ScriptStepResult(ScriptStepStatus.Warning, $"Items to delete are not found!");
            }
        }
    }
}
