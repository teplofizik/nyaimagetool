using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs
{
    public class Fifo : ScriptStepGenerator
    {
        public Fifo() : base("fifo")
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

            return new FifoScriptStep(A[0], Utils.ConvertMode(A[1]), Convert.ToUInt32(A[2]), Convert.ToUInt32(A[3]));

        }

        public class FifoScriptStep : ScriptStep
        {
            string Path = null;
            uint User = uint.MaxValue;
            uint Group = uint.MaxValue;
            uint Mode = uint.MaxValue;

            public FifoScriptStep(string Path, uint Mode, uint User, uint Group) : base("fifo")
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
                    if (Item.ItemType == Filesystem.Universal.Types.FilesystemItemType.Fifo)
                    {
                        var File = Item as Filesystem.Universal.Items.Fifo;

                        File.Mode = Mode;
                        File.User = User;
                        File.Group = Group;

                        File.Modified = DateTime.Now;

                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Fifo {Path} updated!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} is not Fifo!");
                }
                else
                {
                    var Parent = Fs.GetParentDirectory(Path);
                    if (Parent != null)
                    {
                        var File = new Filesystem.Universal.Items.Fifo(Path, User, Group, Mode);

                        Parent.Items.Add(File);
                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Fifo {Path} added!");                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Parent dir for {Path} is not found!");
                }
            }
        }
    }
}
