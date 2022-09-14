using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs
{
    public class Block : ScriptStepGenerator
    {
        public Block() : base("block")
        {
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] {
                    new Params.FsPathScriptArgsParam(),
                    new Params.NumberScriptArgsParam("major"),
                    new Params.NumberScriptArgsParam("minor"),
                    new Params.ModeScriptArgsParam(),
                    new Params.NumberScriptArgsParam("user"),
                    new Params.NumberScriptArgsParam("group")
                }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;

            return new BlockScriptStep(A[0], Convert.ToUInt32(A[1]), Convert.ToUInt32(A[2]), Utils.ConvertMode(A[3]), Convert.ToUInt32(A[4]), Convert.ToUInt32(A[5]));
        }

        public class BlockScriptStep : ScriptStep
        {
            string Path;
            uint User;
            uint Group;
            uint Mode;
            uint Major;
            uint Minor;

            public BlockScriptStep(string Path, uint Major, uint Minor, uint Mode, uint User, uint Group) : base("block")
            {
                this.Path  = Path;
                this.User  = User;
                this.Group = Group;
                this.Mode  = Mode;
                this.Major = Major;
                this.Minor = Minor;
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
                    if (Item.ItemType == Filesystem.Universal.Types.FilesystemItemType.Block)
                    {
                        var File = Item as Filesystem.Universal.Items.Block;

                        File.Mode = Mode;
                        File.User = User;
                        File.Group = Group;

                        File.Major = Major;
                        File.Minor = Minor;

                        File.Modified = DateTime.Now;

                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Block device {Path} updated!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} is not Block device!");
                }
                else
                {
                    var Parent = Fs.GetParentDirectory(Path);
                    if (Parent != null)
                    {
                        var File = new Filesystem.Universal.Items.Block(Path, User, Group, Mode);

                        File.Major = Major;
                        File.Minor = Minor;

                        Parent.Items.Add(File);
                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Block device {Path} added!");                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Parent dir for {Path} is not found!");
                }
            }
        }
    }
}
