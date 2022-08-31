using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs.Interactive
{
    class Ls : ScriptStepGenerator
    {
        public Ls() : base("ls")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { }));
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[] { new Params.FsPathScriptArgsParam() }));
        }
        public override ScriptStep Get(ScriptArgs Args)
        {
            if(Args.ArgConfig == 0)
                return new LsScriptStep();
            else
                return new LsScriptStep(Args.RawArgs[0]);
        }

        public class LsScriptStep : ScriptStep
        {
            string Path = null;

            public LsScriptStep() : base("ls")
            {

            }

            public LsScriptStep(string Path) : base("ls")
            {
                this.Path = Path;
            }

            private ImageFormat.Elements.Fs.Items.Dir GetTarget(ImageFormat.Elements.Fs.Filesystem Fs, string Active)
            {
                if (Path == null)
                {
                    try
                    {
                        return Fs.GetDirectory(Active);
                    }
                    catch(Exception E)
                    {
                        return null;
                    }
                }
                else
                {
                    return Helper.FsHelper.GetItem(Fs, Active, Path) as ImageFormat.Elements.Fs.Items.Dir;
                }
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                if ((Fs == null) || !Fs.Loaded)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem not loaded");
                else
                {
                    var Target = GetTarget(Fs, Processor.ActivePath);

                    if (Target == null)
                        return new ScriptStepResult(ScriptStepStatus.Error, "Target directory is not found!");

                    foreach (var I in Target.Items)
                    {
                        Log.Write(0, FormatItem(I));
                    }

                    return new ScriptStepResult(ScriptStepStatus.Ok, null);
                }
            }

            private string FormatItem(ImageFormat.Elements.Fs.FilesystemItem Item)
            {
                var Mode = $"{GetItemType(Item)}{ConvertModeToString(Item.Mode)}";
                var User = $"{Item.User}".PadLeft(5);
                var Group = $"{Item.Group}".PadLeft(5);

                var Size = $"{Item.Size}".PadLeft(12);

                var Name = (Item.ItemType == ImageFormat.Types.FilesystemItemType.SymLink) ? $"{Item.ShortFilename} -> {(Item as ImageFormat.Elements.Fs.Items.SymLink).Target}" : Item.ShortFilename;

                return $"{Mode} {User} {Group} {Size} {Name}";
            }

            private string GetItemType(ImageFormat.Elements.Fs.FilesystemItem Item)
            {
                switch(Item.ItemType)
                {
                    case ImageFormat.Types.FilesystemItemType.File: return "-";
                    case ImageFormat.Types.FilesystemItemType.Dir: return "d";
                    case ImageFormat.Types.FilesystemItemType.SymLink: return "l";
                    case ImageFormat.Types.FilesystemItemType.Node: return "c";
                    case ImageFormat.Types.FilesystemItemType.Block: return "b";
                    case ImageFormat.Types.FilesystemItemType.Fifo: return "f";
                    default: return "?";
                }
            }
            public static string ConvertModeToString(UInt32 Mode)
            {
                var Res = "";
                for (int i = 0; i < 3; i++)
                {
                    UInt32 Part = (Mode >> (2 - i) * 4) & 0x7;

                    Res += ((Part & 0x04) != 0) ? "r" : "-";
                    Res += ((Part & 0x02) != 0) ? "w" : "-";
                    Res += ((Part & 0x01) != 0) ? ((((Mode >> 12 >> (2 - i)) & 0x1) != 1) ? "x" : "s") : "-";
                }
                return Res;
            }
        }
    }
}
