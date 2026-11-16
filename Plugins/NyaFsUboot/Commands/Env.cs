using NyaFs;
using NyaFs.ImageFormat;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Helper;
using Params = NyaFs.Processor.Scripting.Params;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace NyaFsUboot.Commands
{
    /// <summary>
    /// U-Boot environment editor: env load/save/new/list/get/set/unset/import/export/size/layout/redundant.
    /// </summary>
    public class Env : ScriptStepGenerator
    {
        public Env() : base("env")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "load" }),
                new Params.LocalPathScriptArgsParam()
            }));
            AddConfig(new ScriptArgsConfig(1, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "save" }),
                new Params.LocalPathScriptArgsParam()
            }));
            AddConfig(new ScriptArgsConfig(2, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "new" })
            }));
            AddConfig(new ScriptArgsConfig(3, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "new" }),
                new Params.StringScriptArgsParam("size")
            }));
            AddConfig(new ScriptArgsConfig(4, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "list" })
            }));
            AddConfig(new ScriptArgsConfig(5, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "get" }),
                new Params.StringScriptArgsParam("key")
            }));
            AddConfig(new ScriptArgsConfig(6, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "set" }),
                new Params.StringScriptArgsParam("key"),
                new Params.StringScriptArgsParam("value")
            }));
            AddConfig(new ScriptArgsConfig(7, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "unset" }),
                new Params.StringScriptArgsParam("key")
            }));
            AddConfig(new ScriptArgsConfig(8, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "import" }),
                new Params.LocalPathScriptArgsParam()
            }));
            AddConfig(new ScriptArgsConfig(9, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "export" }),
                new Params.LocalPathScriptArgsParam()
            }));
            AddConfig(new ScriptArgsConfig(10, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "size" }),
                new Params.StringScriptArgsParam("size")
            }));
            AddConfig(new ScriptArgsConfig(11, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "layout" }),
                new Params.NumberScriptArgsParam("data-offset"),
                new Params.StringScriptArgsParam("flags")
            }));
            AddConfig(new ScriptArgsConfig(12, new ScriptArgsParam[]
            {
                new Params.EnumScriptArgsParam("action", new[] { "redundant" }),
                new Params.EnumScriptArgsParam("mode", new[] { "on", "off" })
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            var A = Args.RawArgs;
            var Action = A[0].ToLowerInvariant();
            var File = (Args.ArgConfig == 0 || Args.ArgConfig == 1 || Args.ArgConfig == 8 || Args.ArgConfig == 9) ? A[1] : null;
            var Key = (Args.ArgConfig == 5 || Args.ArgConfig == 6 || Args.ArgConfig == 7) ? A[1] : null;
            var Value = (Args.ArgConfig == 6) ? A[2] : null;
            var Size = (Args.ArgConfig == 3 || Args.ArgConfig == 10) ? A[1] : null;
            var DataOffset = (Args.ArgConfig == 11) ? (int?)Convert.ToInt32(A[1]) : null;
            var Flags = (Args.ArgConfig == 11) ? A[2] : null;
            var Mode = (Args.ArgConfig == 12) ? A[1] : null;

            return new EnvStep(Action, File, Key, Value, Size, DataOffset, Flags, Mode);
        }

        private sealed class Session
        {
            public UbootEnvironment Env;
            public string Source;
        }

        private static readonly ConditionalWeakTable<BaseImageBlob, Session> Sessions =
            new ConditionalWeakTable<BaseImageBlob, Session>();

        private static Session GetSession(ImageProcessor Processor)
        {
            var Blob = Processor.GetBlob();
            var S = Sessions.GetOrCreateValue(Blob);
            if (S.Env == null)
                S.Env = new UbootEnvironment();
            return S;
        }

        public class EnvStep : ScriptStep
        {
            private readonly string Action;
            private readonly string File;
            private readonly string Key;
            private readonly string Value;
            private readonly string Size;
            private readonly int? DataOffset;
            private readonly string Flags;
            private readonly string Mode;

            public EnvStep(string Action, string File, string Key, string Value, string Size,
                int? DataOffset, string Flags, string Mode) : base("env")
            {
                this.Action = Action;
                this.File = File;
                this.Key = Key;
                this.Value = Value;
                this.Size = Size;
                this.DataOffset = DataOffset;
                this.Flags = Flags;
                this.Mode = Mode;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                var S = GetSession(Processor);

                switch (Action)
                {
                    case "load": return Load(Processor, S);
                    case "save": return Save(S);
                    case "new": return New(S);
                    case "list": return List(S);
                    case "get": return Get(S);
                    case "set": return Set(S);
                    case "unset": return Unset(S);
                    case "import": return Import(Processor, S);
                    case "export": return Export(Processor, S);
                    case "size": return ChangeSize(S);
                    case "layout": return Layout(S);
                    case "redundant": return Redundant(S);
                    default:
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown env action: {Action}");
                }
            }

            private ScriptStepResult Load(ImageProcessor Processor, Session S)
            {
                var Path = this.DetectFilePath(File);
                if (Path == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{File} not found");

                byte[] Raw;
                try { Raw = System.IO.File.ReadAllBytes(Path); }
                catch (Exception E) { return new ScriptStepResult(ScriptStepStatus.Error, E.Message); }

                if (!UbootEnvironment.TryParse(Raw, out var Env, out var Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Not a U-Boot environment: {Error}");

                S.Env = Env;
                S.Source = Path;
                return new ScriptStepResult(ScriptStepStatus.Ok,
                    $"Loaded {Env.Count} entries ({Env.RegionSize} bytes x{Env.Copies}, data offset {Env.DataOffset}) from {File}");
            }

            private ScriptStepResult Save(Session S)
            {
                if (S.Env == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "No environment is loaded");

                var Data = S.Env.Build(out var Error);
                if (Data == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                try { System.IO.File.WriteAllBytes(File, Data); }
                catch (Exception E) { return new ScriptStepResult(ScriptStepStatus.Error, E.Message); }

                return new ScriptStepResult(ScriptStepStatus.Ok,
                    $"U-Boot environment ({Data.Length} bytes, crc32 recalculated) is written to {File}");
            }

            private ScriptStepResult New(Session S)
            {
                var Env = new UbootEnvironment();
                if (Size != null)
                {
                    if (!TryParseSize(Size, out var Region))
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Invalid size: {Size}");
                    Env.RegionSize = Region;
                }

                S.Env = Env;
                S.Source = null;
                return new ScriptStepResult(ScriptStepStatus.Ok,
                    $"New U-Boot environment created ({Env.RegionSize} bytes, data offset {Env.DataOffset})");
            }

            private ScriptStepResult List(Session S)
            {
                if (S.Env == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "No environment is loaded");

                foreach (var E in S.Env.Entries)
                    Log.Write(0, $"{E.Key}={E.Value}");

                return new ScriptStepResult(ScriptStepStatus.Ok, $"{S.Env.Count} entries");
            }

            private ScriptStepResult Get(Session S)
            {
                if (S.Env == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "No environment is loaded");

                var Result = S.Env.Get(Key);
                if (Result == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Key} is not set");

                Log.Write(0, Result);
                return new ScriptStepResult(ScriptStepStatus.Ok, $"{Key}={Result}");
            }

            private ScriptStepResult Set(Session S)
            {
                S.Env.Set(Key, Value ?? "");
                return new ScriptStepResult(ScriptStepStatus.Ok, $"{Key} is set");
            }

            private ScriptStepResult Unset(Session S)
            {
                return S.Env.Unset(Key)
                    ? new ScriptStepResult(ScriptStepStatus.Ok, $"{Key} is removed")
                    : new ScriptStepResult(ScriptStepStatus.Warning, $"{Key} is not set");
            }

            private ScriptStepResult Import(ImageProcessor Processor, Session S)
            {
                var Path = this.DetectFilePath(File);
                if (Path == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{File} not found");

                string Text;
                try { Text = System.IO.File.ReadAllText(Path, Encoding.UTF8); }
                catch (Exception E) { return new ScriptStepResult(ScriptStepStatus.Error, E.Message); }

                if (!S.Env.TryParseText(Text, out var Error))
                    return new ScriptStepResult(ScriptStepStatus.Error, Error);

                S.Source = Path;
                return new ScriptStepResult(ScriptStepStatus.Ok, $"Imported {S.Env.Count} entries from {File}");
            }

            private ScriptStepResult Export(ImageProcessor Processor, Session S)
            {
                if (S.Env == null)
                    return new ScriptStepResult(ScriptStepStatus.Error, "No environment is loaded");

                try { System.IO.File.WriteAllText(File, S.Env.ToText(), new UTF8Encoding(false)); }
                catch (Exception E) { return new ScriptStepResult(ScriptStepStatus.Error, E.Message); }

                return new ScriptStepResult(ScriptStepStatus.Ok, $"Exported {S.Env.Count} entries to {File}");
            }

            private ScriptStepResult ChangeSize(Session S)
            {
                if (!TryParseSize(Size, out var Region))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Invalid size: {Size}");

                S.Env.RegionSize = Region;
                return new ScriptStepResult(ScriptStepStatus.Ok, $"Region size is {Region} bytes");
            }

            private ScriptStepResult Layout(Session S)
            {
                if ((DataOffset == null) || ((DataOffset != 4) && (DataOffset != 5) && (DataOffset != 8)))
                    return new ScriptStepResult(ScriptStepStatus.Error, "Data offset must be 4, 5 or 8");

                if (!TryParseUInt(Flags, out var FlagsValue))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Invalid flags: {Flags}");

                S.Env.DataOffset = DataOffset.Value;
                S.Env.Flags = FlagsValue;
                S.Env.CopyFlags = null;
                return new ScriptStepResult(ScriptStepStatus.Ok,
                    $"Layout: data offset {S.Env.DataOffset}, flags {FlagsValue}");
            }

            private ScriptStepResult Redundant(Session S)
            {
                var On = String.Equals(Mode, "on", StringComparison.OrdinalIgnoreCase);
                S.Env.Copies = On ? 2 : 1;
                S.Env.CopyFlags = On ? new uint[] { 1, 0 } : new uint[] { 0 };
                return new ScriptStepResult(ScriptStepStatus.Ok,
                    $"Environment copies: {S.Env.Copies}");
            }

            private static bool TryParseUInt(string Text, out uint Value)
            {
                Value = 0;
                if (String.IsNullOrWhiteSpace(Text))
                    return false;
                if (Text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    return UInt32.TryParse(Text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out Value);
                return UInt32.TryParse(Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out Value);
            }

            private static bool TryParseSize(string Text, out int Value)
            {
                Value = 0;
                if (String.IsNullOrWhiteSpace(Text))
                    return false;

                var Source = Text.Trim();
                long Multiplier = 1;
                var Last = Char.ToUpperInvariant(Source[Source.Length - 1]);
                if ((Last == 'K') || (Last == 'M'))
                {
                    Multiplier = (Last == 'K') ? 1024L : 1024L * 1024L;
                    Source = Source.Substring(0, Source.Length - 1);
                }

                long Parsed;
                if (Source.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Int64.TryParse(Source.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out Parsed))
                        return false;
                }
                else if (!Int64.TryParse(Source, NumberStyles.Integer, CultureInfo.InvariantCulture, out Parsed))
                    return false;

                var Result = Parsed * Multiplier;
                if ((Result < 0x40) || (Result > Int32.MaxValue))
                    return false;

                Value = (int)Result;
                return true;
            }
        }
    }
}
