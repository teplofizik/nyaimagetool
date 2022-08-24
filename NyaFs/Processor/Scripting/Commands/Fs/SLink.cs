using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs
{
    public class SLink : ScriptStepGenerator
    {
        public SLink() : base("slink")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                    new Params.FsPathScriptArgsParam(),
                    new Params.FsPathScriptArgsParam(),
                    new Params.ModeScriptArgsParam(),
                    new Params.NumberScriptArgsParam("user"),
                    new Params.NumberScriptArgsParam("group")
                }));
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] {
                    new Params.FsPathScriptArgsParam(),
                    new Params.FsPathScriptArgsParam()
                }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;

            return new SLinkScriptStep(A[0], A[1], Utils.ConvertMode(A[2]), Convert.ToUInt32(A[3]), Convert.ToUInt32(A[4]));

        }

        public class SLinkScriptStep : ScriptStep
        {
            UpdateMode UMode;

            string Path = null;
            string Target = null;
            uint User = uint.MaxValue;
            uint Group = uint.MaxValue;
            uint Mode = uint.MaxValue;

            public SLinkScriptStep(string Path, string Target, uint Mode, uint User, uint Group) : base("slink")
            {
                UMode = UpdateMode.AddOrUpdate;
                this.Path = Path;
                this.Target = Target;
                this.User = User;
                this.Group = Group;
                this.Mode = Mode;
            }

            public SLinkScriptStep(string Path, string Target) : base("slink")
            {
                UMode = UpdateMode.Update;
                this.Path = Path;
                this.Target = Target;
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
                    if (Item.ItemType == ImageFormat.Types.FilesystemItemType.SymLink)
                    {
                        var SymLink = Item as ImageFormat.Elements.Fs.Items.SymLink;

                        if (UMode == UpdateMode.AddOrUpdate)
                        {
                            SymLink.Mode = Mode;
                            SymLink.User = User;
                            SymLink.Group = Group;
                        }
                        SymLink.Target = Target;
                        SymLink.Modified = DateTime.Now;

                        return new ScriptStepResult(ScriptStepStatus.Ok, $"{Path} updated!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} is not symlink!");
                }
                else
                {
                    var Parent = Fs.GetParentDirectory(Path);
                    if (Parent != null)
                    {
                        if (UMode == UpdateMode.AddOrUpdate)
                        {
                            var SymLink = new ImageFormat.Elements.Fs.Items.SymLink(Path, User, Group, Mode, Target);

                            Parent.Items.Add(SymLink);
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"{Path} added!");
                        }
                        else
                            return new ScriptStepResult(ScriptStepStatus.Error, $"SLink {Path} not exists! Cannot update. Specify user, group and mode to add symlink.");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Parent dir for {Path} is not found!");
                }
            }
        }

        enum UpdateMode
        {
            Update,
            AddOrUpdate
        }
    }
}
