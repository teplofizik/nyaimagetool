using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs
{
    public class Dir : ScriptStepGenerator
    {
        public Dir() : base("dir")
        {
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] {
                    new Params.FsPathScriptArgsParam(),
                    new Params.ModeScriptArgsParam(),
                    new Params.NumberScriptArgsParam("user"),
                    new Params.NumberScriptArgsParam("group")
                }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;

            return new DirScriptStep(A[0], Utils.ConvertMode(A[1]), Convert.ToUInt32(A[2]), Convert.ToUInt32(A[3]));

        }

        public class DirScriptStep : ScriptStep
        {
            string Path = null;
            uint User = uint.MaxValue;
            uint Group = uint.MaxValue;
            uint Mode = uint.MaxValue;

            public DirScriptStep(string Path, uint Mode, uint User, uint Group) : base("dir")
            {
                this.Path  = Path;
                this.User  = User;
                this.Group = Group;
                this.Mode  = Mode;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                // Проверим наличие загруженной файловой системы
                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                if (Fs.Exists(Path))
                {
                    var Item = Fs.GetElement(Path);
                    if (Item.ItemType == ImageFormat.Types.FilesystemItemType.Dir)
                    {
                        var File = Item as ImageFormat.Elements.Fs.Items.Dir;

                        File.Mode = Mode;
                        File.User = User;
                        File.Group = Group;

                        File.Modified = DateTime.Now;

                        return new ScriptStepResult(ScriptStepStatus.Ok, $"{Path} updated!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} is not dir!");
                }
                else
                {
                    var Parent = Fs.GetParentDirectory(Path);
                    if (Parent != null)
                    {
                        var File = new ImageFormat.Elements.Fs.Items.Dir(Path, User, Group, Mode);

                        Parent.Items.Add(File);
                        return new ScriptStepResult(ScriptStepStatus.Ok, $"{Path} added!");                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Parent dir for {Path} is not found!");
                }
            }
        }
    }
}
