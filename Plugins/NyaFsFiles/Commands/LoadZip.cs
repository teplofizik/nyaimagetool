using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NyaFs;
using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Items;
using NyaFs.Filesystem.Universal.Types;
using NyaFs.FlattenedDeviceTree.Types;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Helper;
using ZstdSharp.Unsafe;

namespace NyaFsFiles.Commands
{
    public class LoadZip : ScriptStepGenerator
    {
        public LoadZip() : base("loadzip")
        {
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] {
                    new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam(),
                    new NyaFs.Processor.Scripting.Params.LocalPathScriptArgsParam(),
                    new NyaFs.Processor.Scripting.Params.ModeScriptArgsParam(),
                    new NyaFs.Processor.Scripting.Params.ModeScriptArgsParam(),
                    new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("user"),
                    new NyaFs.Processor.Scripting.Params.NumberScriptArgsParam("group")
                }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;

            return new ZipScriptStep(A[0], A[1], Utils.ConvertMode(A[2]), Utils.ConvertMode(A[3]), Convert.ToUInt32(A[4]), Convert.ToUInt32(A[5]));
        }

        public class ZipScriptStep : ScriptStep
        {
            string Path = null;
            string LocalPath = null;
            uint User = uint.MaxValue;
            uint Group = uint.MaxValue;
            uint DirMode = uint.MaxValue;
            uint FileMode = uint.MaxValue;

            public ZipScriptStep(string Path, string LocalPath, uint DirMode, uint FileMode, uint User, uint Group) : base("loadzip")
            {
                this.Path  = Path;
                this.LocalPath = LocalPath;
                this.User  = User;
                this.Group = Group;
                this.DirMode = DirMode;
                this.FileMode = FileMode;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                // Проверим наличие загруженной файловой системы
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                var DetectedFile = this.DetectFilePath(LocalPath);
                if (DetectedFile == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Local file {LocalPath} is not found");

                ZipArchive Zip = null;
                try
                {
                    Zip = new ZipArchive(new System.IO.FileStream(DetectedFile, System.IO.FileMode.Open), ZipArchiveMode.Read);
                }
                catch (System.IO.IOException e)
                {
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Cannot open zip archive {LocalPath}");
                }
                if (Fs.Exists(Path))
                {
                    var Item = Fs.GetElement(Path);
                    if (Item.ItemType == FilesystemItemType.Directory)
                    {
                        var Dir = Item as Dir;

                        Dir.Mode = DirMode;
                        Dir.User = User;
                        Dir.Group = Group;

                        Dir.Modified = DateTime.Now;

                        if(LoadDirectory(Dir, "", Zip.Entries.ToArray()))
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"Dir {Path} updated!");
                        else
                            return new ScriptStepResult(ScriptStepStatus.Error, $"Cannot load items to {Path}!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} is not dir!");
                }
                else
                {
                    var Parent = Fs.GetParentDirectory(Path);
                    if (Parent != null)
                    {
                        var Dir = new Dir(Path, User, Group, DirMode);

                        Parent.Items.Add(Dir);

                        if (LoadDirectory(Dir, "", Zip.Entries.ToArray()))
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"Dir {Path} added!");
                        else
                            return new ScriptStepResult(ScriptStepStatus.Error, $"Cannot load items to {Path}!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Parent dir for {Path} is not found!");
                }
            }

            /// <summary>
            /// Is item exists in filesystem
            /// </summary>
            /// <param name="dir"></param>
            /// <param name="path"></param>
            /// <returns></returns>
            private FilesystemItem GetElement(Dir dir, string path)
            {
                foreach(var I in dir.Items)
                {
                    if(I.Filename == path) return I;
                }

                return null;
            }

            /// <summary>
            /// Read file content from zip archive entry
            /// </summary>
            /// <param name="entry"></param>
            /// <returns></returns>
            private byte[] ReadEntryContent(ZipArchiveEntry entry)
            {
                var Stream = entry.Open();
                var MemStream = new System.IO.MemoryStream();

                Stream.CopyTo(MemStream);
                return MemStream.ToArray();
            }

            /// <summary>
            /// Fix directory name
            /// </summary>
            /// <returns></returns>
            private string GetDirName(string path)
            {
                if (path.EndsWith('/'))
                    return path.Substring(0, path.Length - 1);
                else
                    return path;
            }

            /// <summary>
            /// Get list of nested directories for this path
            /// </summary>
            /// <param name="entries"></param>
            /// <returns></returns>
            private string[] GetDirectories(string relpath, ZipArchiveEntry[] entries)
            {
                var Res = new List<string>();

                foreach(var e in entries)
                {
                    if (e.FullName.Last() == '/')
                    {
                        if (!Res.Contains(e.FullName))
                            Res.Add(e.FullName);
                    }
                }

                return Res.ToArray();
            }

            /// <summary>
            /// Get list of nested directories for this path
            /// </summary>
            /// <param name="entries"></param>
            /// <returns></returns>
            private ZipArchiveEntry[] GetDirectoryEntries(string relpath, ZipArchiveEntry[] entries)
            {
                var Res = new List<ZipArchiveEntry>();

                foreach (var e in entries)
                {
                    if (e.FullName.StartsWith(relpath) && (e.FullName != relpath))
                    {
                        var Name = e.FullName.Substring(relpath.Length);

                        if (Name.Count(c => c == '/') == 0)
                            Res.Add(e);
                    }
                }

                return Res.ToArray();
            }

            /// <summary>
            /// Load file to directory
            /// </summary>
            /// <param name="Dir"></param>
            /// <param name="Path"></param>
            /// <returns></returns>
            private void LoadFile(Dir dir, ZipArchiveEntry entry)
            {
                var FsPath = FsHelper.CombinePath(dir.Filename, entry.Name);
                var El = GetElement(dir, FsPath);

                if (El != null)
                {
                    if (El.ItemType == FilesystemItemType.File)
                    {
                        var File = El as File;

                        File.Mode = FileMode;
                        File.User = User;
                        File.Group = Group;

                        File.Modified = DateTime.Now;
                        File.Content = ReadEntryContent(entry);
                        Log.Write(0, $"Updated {FsPath}!");
                    }
                    else
                        Log.Warning(0, $"Cannot update {FsPath}: not file!");
                }
                else
                {
                    var Content = ReadEntryContent(entry);
                    var File = new File(FsPath, User, Group, FileMode, Content);

                    dir.Items.Add(File);
                    Log.Write(0, $"Added {FsPath}!");
                }
            }

            /// <summary>
            /// Load local directory to filesystem
            /// </summary>
            /// <param name="dir">Directory in filesystem</param>
            /// <param name="relpath">Local path</param>
            /// <param name="entries">Zip entries</param>
            private bool LoadDirectory(Dir dir, string relpath, ZipArchiveEntry[] entries)
            {
                var Dirs = GetDirectories(relpath, entries);

                foreach (var Dir in Dirs)
                {
                    var FsPath = GetDirName(FsHelper.CombinePath(dir.Filename, Dir.Substring(relpath.Length)));
                    var El = GetElement(dir, FsPath);
                    if (El != null)
                    {
                        if (El.ItemType == FilesystemItemType.Directory)
                        {
                            var NDir = El as Dir;

                            NDir.Mode = DirMode;
                            NDir.User = User;
                            NDir.Group = Group;

                            NDir.Modified = DateTime.Now;
                            LoadDirectory(NDir, Dir, GetDirectoryEntries(Dir, entries));
                            Log.Write(2, $"Updated {FsPath}!");
                        }
                        else
                            Log.Warning(0, $"Cannot update {FsPath}: not directory!");
                    }
                    else
                    {
                        var NDir = new Dir(FsPath, User, Group, DirMode);

                        dir.Items.Add(NDir);
                        LoadDirectory(NDir, Dir, GetDirectoryEntries(Dir, entries));
                        Log.Write(2, $"Added dir {FsPath}!");
                    }
                }

                foreach (var entry in entries)
                {
                    var Fn = (relpath.Length > 0) ? entry.FullName.Replace(relpath, "") : entry.FullName;

                    if(Fn.Count(c => c == '/') == 0)
                        LoadFile(dir, entry);
                }

                return true;
            }
        }
    }
}
