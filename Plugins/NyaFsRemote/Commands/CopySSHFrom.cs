using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Items;
using NyaFs.Filesystem.Universal.Types;
using NyaFs.FlattenedDeviceTree.Types;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Helper;
using System.Text.RegularExpressions;
using NyaFs;
using System.Linq;
using NyaFsRemote.RemoteFs;
using System.Net.Http.Headers;

namespace NyaFsRemote.Commands
{
    class CopySSHFrom : ScriptStepGenerator
    {

        public CopySSHFrom() : base("copysshfrom")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("ipport"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("login"),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("pass"),
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam(),
                new NyaFs.Processor.Scripting.Params.StringScriptArgsParam("remotepath")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new CopyScriptStep(Args.RawArgs[0], Args.RawArgs[1], Args.RawArgs[2], Args.RawArgs[3], Args.RawArgs[4]);
        }


        public class CopyScriptStep : ScriptStep
        {
            private string IPPort;
            private string Login;
            private string Pass;
            private string Filename;
            private string Remote;

            public CopyScriptStep(string IPPort, string Login, string Pass, string Filename, string Remote) : base("copysshfrom")
            {
                this.IPPort = IPPort;
                this.Login = Login;
                this.Pass = Pass;

                this.Filename = Filename;
                this.Remote = Remote;
            }

            /// <summary>
            /// Is item exists in filesystem
            /// </summary>
            /// <param name="dir"></param>
            /// <param name="path"></param>
            /// <returns></returns>
            private FilesystemItem GetElement(Dir dir, string path)
            {
                foreach (var I in dir.Items)
                {
                    if (I.Filename == path) return I;
                }

                return null;
            }

            private ScriptStepResult EnsureParent(NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Fs, string Path)
            {
                var Parts = Path.Split('/').Skip(1).SkipLast(1).ToArray();
                var BuildedPath = "/";
                var CurrentDir = Fs.GetDirectory("/");

                foreach (var Part in Parts)
                {
                    BuildedPath = FsHelper.CombinePath(BuildedPath, Part);

                    if(Fs.Exists(BuildedPath))
                    {
                        var Item = Fs.GetElement(Filename);
                        if(Item.ItemType == FilesystemItemType.Directory)
                        {
                            CurrentDir = Item as Dir;
                        }
                        else
                        {
                            return new ScriptStepResult(ScriptStepStatus.Error, $"{BuildedPath} is not directory!");
                        }
                    }
                    else
                    {
                        uint Mode = Utils.ConvertMode("777");
                        var Dir = new Dir(BuildedPath, 0, 0, Mode);
                        CurrentDir.Items.Add(Dir);

                        CurrentDir = Dir;
                    }
                }

                return new ScriptStepResult(ScriptStepStatus.Ok, "");
            }

            private ScriptStepResult AddFileToFs(NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Fs, string Filename, NyaFs.Filesystem.Universal.Items.File FileToAdd)
            {
                if (Fs.Exists(Filename))
                {
                    var Item = Fs.GetElement(Filename);
                    if (Item.ItemType == FilesystemItemType.File)
                    {
                        var File = Item as NyaFs.Filesystem.Universal.Items.File;

                        File.Mode = FileToAdd.Mode;
                        File.User = FileToAdd.User;
                        File.Group = FileToAdd.Group;
                        File.Modified = DateTime.Now;
                        File.Content = FileToAdd.Content;

                        return new ScriptStepResult(ScriptStepStatus.Ok, $"{Filename} updated!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"{Filename} is not file!");
                }
                else
                {
                    var Res = EnsureParent(Fs, Filename);
                    if (Res.Status != ScriptStepStatus.Ok)
                        return Res;

                    var Parent = Fs.GetParentDirectory(Filename);
                    if (Parent != null)
                    {
                        FileToAdd.Filename = Filename;
                        Parent.Items.Add(FileToAdd);
                        return new ScriptStepResult(ScriptStepStatus.Ok, $"File {Filename} added!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Parent dir for {Filename} is not found!");
                }
            }

            private ScriptStepResult ImportDir(Dir Root, SshFs sshfs, string Subpath, string Remote)
            {
                var ActualDirPath = FsHelper.CombinePath(Remote, Subpath);
                var Items = sshfs.GetDirContent(ActualDirPath);
                foreach (var Item in Items)
                {
                    var ConcatenatedPath = FsHelper.CombinePath(ActualDirPath, Item).Replace("//", "/");
                    var ItemType = sshfs.GetFileType(ConcatenatedPath);
                    switch (ItemType)
                    {
                        case "d":
                            {
                                uint Mode  = sshfs.GetAccessRights(ConcatenatedPath);
                                uint User  = sshfs.GetUserId(ConcatenatedPath);
                                uint Group = sshfs.GetGroupId(ConcatenatedPath);

                                var LocalPath = FsHelper.CombinePath(Root.Filename, Item).Replace("//", "/");
                                var LocalSubPath = FsHelper.CombinePath(Subpath, Item);

                                var D = new Dir(LocalPath, User, Group, Mode);
                                Root.Items.Add(D);

                                Log.Ok(1, $"Added dir {LocalPath}");
                                ImportDir(D, sshfs, LocalSubPath, Remote);
                            }
                            break;
                        case "-":
                            {
                                var File = sshfs.ReadFile(ConcatenatedPath);
                                if (File == null)
                                    Log.Error(0, $"Cannot read {ConcatenatedPath} from remote fs");
                                else
                                {
                                    var LocalPath = FsHelper.CombinePath(Root.Filename, Item).Replace("//", "/");

                                    File.Filename = LocalPath;
                                    Root.Items.Add(File);
                                    Log.Ok(1, $"Added file {LocalPath} <= {ConcatenatedPath}");
                                }
                            }
                            break;
                        default:
                            Log.Error(0, $"Cannot read {ConcatenatedPath} from remote fs: unsupported type {ItemType}");
                            break;
                    }
                }

                return new ScriptStepResult(ScriptStepStatus.Ok, "OK");
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();

                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                SshFs sshfs = new SshFs(IPPort, Login, Pass);
                if (sshfs.Connect())
                {
                    ScriptStepResult Res = null;
                    if (sshfs.IsFile(Remote))
                    {
                        var Content = sshfs.ReadFile(Remote);
                        if (Content == null)
                            Res = new ScriptStepResult(ScriptStepStatus.Error, $"Cannot download file {Remote}");
                        else
                            Res = AddFileToFs(Fs, Filename, Content);
                    }
                    else if (sshfs.IsDir(Remote))
                    {
                        var Dir = Fs.GetDirectory(Filename);
                        if (Dir == null)
                            Res = new ScriptStepResult(ScriptStepStatus.Error, $"Path {Filename} is not dir in fs");
                        else
                            Res = ImportDir(Dir, sshfs, "", Remote);
                    }
                    else
                        Res = new ScriptStepResult(ScriptStepStatus.Error, $"Path {Remote} is not dir or file");

                    sshfs.Close();
                    return Res;
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Cannot connect to remote {IPPort}");
            }
        }
    }
}
