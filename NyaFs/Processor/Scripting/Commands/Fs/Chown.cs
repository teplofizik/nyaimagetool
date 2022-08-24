using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Commands.Fs
{
    public class Chown : ScriptStepGenerator
    {
        public Chown() : base("chown")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new Params.FsPathScriptArgsParam(),
                new Params.NumberScriptArgsParam("user")
            }));
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[]
            {
                new Params.FsPathScriptArgsParam(),
                new Params.NumberScriptArgsParam("user"),
                new Params.NumberScriptArgsParam("group")
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            if(Args.ArgConfig == 0)
                return new ChownScriptStepUser(Args.RawArgs[0], Convert.ToUInt32(Args.RawArgs[1]));
            else
                return new ChownScriptStepUserGroup(Args.RawArgs[0], Convert.ToUInt32(Args.RawArgs[1]), Convert.ToUInt32(Args.RawArgs[1]));
        }

        public class ChownScriptStepUser : ScriptStep
        {
            string Path;
            uint User;

            public ChownScriptStepUser(string Path, uint User) : base("chown")
            {
                this.Path = Path;
                this.User = User;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                // Проверим наличие загруженной файловой системы

                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                if (Fs.Exists(Path))
                {
                    // Есть старый файл в файловой системе. Удалим.
                    var Item = Fs.GetElement(Path);
                    Item.User = User;

                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Owner of {Path} is updated to {User}!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Warning, $"{Path} not found!");
            }
        }

        public class ChownScriptStepUserGroup : ScriptStep
        {
            string Path;
            uint User;
            uint Group;

            public ChownScriptStepUserGroup(string Path, uint User, uint Group) : base("chown")
            {
                this.Path = Path;
                this.User = User;
                this.Group = Group;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var Fs = Processor.GetFs();
                // Проверим наличие загруженной файловой системы

                if (Fs == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

                if (Fs.Exists(Path))
                {
                    // Есть старый файл в файловой системе. Удалим.
                    var Item = Fs.GetElement(Path);
                    Item.User = User;
                    Item.Group = Group;

                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Owner of {Path} is updated to {User}, group is to {Group}!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Warning, $"{Path} not found!");
            }
        }
    }
}

