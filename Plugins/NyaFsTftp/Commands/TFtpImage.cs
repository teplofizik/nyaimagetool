using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Params;
using System;

namespace NyaFsTftp.Commands
{
    internal sealed class TFtpImage : ScriptStepGenerator
    {
        public TFtpImage() : base("tftpimage")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] { }));

            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[]
            {
                new EnumScriptArgsParam("action", new[] { "add" }),
                new StringScriptArgsParam("name"),
                new StringScriptArgsParam("source")
            }));

            AddConfig(new ScriptArgsConfig(2, new ScriptArgsParam[]
            {
                new EnumScriptArgsParam("action", new[] { "remove" }),
                new StringScriptArgsParam("name")
            }));

            AddConfig(new ScriptArgsConfig(3, new ScriptArgsParam[]
            {
                new EnumScriptArgsParam("action", new[] { "list" })
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            switch (Args.ArgConfig)
            {
                case 1:
                    return new Step("add", Args.RawArgs[1], Args.RawArgs[2]);
                case 2:
                    return new Step("remove", Args.RawArgs[1], null);
                default:
                    return new Step("list", null, null);
            }
        }

        private sealed class Step : ScriptStep
        {
            private readonly string Action;
            private readonly string ImageName;
            private readonly string Source;

            public Step(string Action, string Name, string Source) : base("tftpimage")
            {
                this.Action = Action;
                this.ImageName = Name;
                this.Source = Source;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                try
                {
                    switch (Action)
                    {
                        case "add":
                            if (!TftpImageMap.Add(ImageName, Source, out string Error))
                                return new ScriptStepResult(ScriptStepStatus.Error, Error);

                            NyaFs.Log.Write(0, $"TFTP image '{ImageName}' -> {Source}");
                            return new ScriptStepResult(ScriptStepStatus.Ok, null);

                        case "remove":
                            if (!TftpImageMap.Remove(ImageName))
                                return new ScriptStepResult(ScriptStepStatus.Error, $"TFTP image '{ImageName}' is not defined");

                            NyaFs.Log.Write(0, $"TFTP image '{ImageName}' removed");
                            return new ScriptStepResult(ScriptStepStatus.Ok, null);

                        default:
                            var Images = TftpImageMap.List();
                            if (Images.Length == 0)
                                NyaFs.Log.Write(0, "No TFTP image mappings");
                            else
                                foreach (var Image in Images)
                                    NyaFs.Log.Write(0, $"{Image.Name} -> {Image.Description}");

                            return new ScriptStepResult(ScriptStepStatus.Ok, null);
                    }
                }
                catch (Exception E)
                {
                    return new ScriptStepResult(ScriptStepStatus.Error, $"tftpimage {Action}: {E.Message}");
                }
            }
        }
    }
}
