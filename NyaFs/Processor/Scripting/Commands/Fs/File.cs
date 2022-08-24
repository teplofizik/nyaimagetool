using NyaFs.Processor.Scripting.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs
{
    public class File : ScriptStepGenerator
    {
        public File() : base("file")
        {
            // Config to update 
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                    new Params.FsPathScriptArgsParam(),
                    new Params.LocalPathScriptArgsParam(),
                }));
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] {
                    new Params.FsPathScriptArgsParam(),
                    new Params.LocalPathScriptArgsParam(),
                    new Params.ModeScriptArgsParam(),
                    new Params.NumberScriptArgsParam("user"),
                    new Params.NumberScriptArgsParam("group")
                }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;

            if (Args.ArgConfig == 0)
                return new FileScriptStep(A[0], A[1]);
            else
                return new FileScriptStep(A[0], A[1], Utils.ConvertMode(A[2]), Convert.ToUInt32(A[3]), Convert.ToUInt32(A[4]));

        }

        public class FileScriptStep : ScriptStep
        {
            UpdateMode AddMode;

            string Path = null;
            uint User = uint.MaxValue;
            uint Group = uint.MaxValue;
            uint Mode = uint.MaxValue;
            string Filename = null;

            public FileScriptStep(string Path, string Filename) : base("file")
            {
                AddMode = UpdateMode.Update;

                this.Path = Path;
                this.Filename = Filename;
            }

            public FileScriptStep(string Path, string Filename, uint Mode, uint User, uint Group) : base("file")
            {
                AddMode = UpdateMode.AddOrUpdate;

                this.Path  = Path;
                this.User  = User;
                this.Group = Group;
                this.Mode  = Mode;
                this.Filename = Filename;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                // Проверим наличие загруженной файловой системы
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                var DetectedFilename = this.DetectFilePath(Filename);
                // Проверим наличие локального файла, содержимое которого надо загрузить в ФС
                if (DetectedFilename == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Filename} not found!");

                if (Fs.Exists(Path))
                {
                    var Item = Fs.GetElement(Path);
                    if (Item.ItemType == ImageFormat.Types.FilesystemItemType.File)
                    {
                        var File = Item as ImageFormat.Elements.Fs.Items.File;

                        if (AddMode == UpdateMode.AddOrUpdate)
                        {
                            File.Mode = Mode;
                            File.User = User;
                            File.Group = Group;
                        }

                        File.Modified = DateTime.Now;
                        File.Content = System.IO.File.ReadAllBytes(DetectedFilename);

                        return new ScriptStepResult(ScriptStepStatus.Ok, $"{Path} updated!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} is not file!");
                }
                else
                {
                    var Parent = Fs.GetParentDirectory(Path);
                    if (Parent != null)
                    {
                        if (AddMode == UpdateMode.AddOrUpdate)
                        {
                            var Content = System.IO.File.ReadAllBytes(DetectedFilename);
                            var File = new ImageFormat.Elements.Fs.Items.File(Path, User, Group, Mode, Content);

                            Parent.Items.Add(File);
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"{Path} added!");
                        }
                        else
                            return new ScriptStepResult(ScriptStepStatus.Error, $"File {Path} not exists! Cannot update. Specify user, group and mode to add file.");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Parent dir for {Path} is not found!");
                }
            }

            enum UpdateMode
            {
                Update,
                AddOrUpdate
            }
        }
    }
}
