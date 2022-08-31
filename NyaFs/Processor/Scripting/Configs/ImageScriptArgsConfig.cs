using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Processor.Scripting.Configs
{
    internal class ImageScriptArgsConfig : ScriptArgsConfig
    {
        private string ImgType;
        private string[] SupportedFormats;

        public ImageScriptArgsConfig(int Id, string ImgType, string[] SupportedFormats) : base(Id, new ScriptArgsParam[] {
                    new Params.FsPathScriptArgsParam(),
                    new Params.EnumScriptArgsParam("type", new string[] { ImgType }),
                    new Params.EnumScriptArgsParam("format", SupportedFormats)
                })
        {
            this.ImgType = ImgType;
            this.SupportedFormats = SupportedFormats;
        }

        public override bool IsMyConfig(string[] Args) => (Args.Length > 1) && (ImgType == Args[1]);

        public override bool CheckArgs(string[] Args)
        {
            if (!base.IsMyConfig(Args))
            {
                // Check path... TODO
                if(Args.Length != 3)
                {
                    if(Args.Length < 3)
                        Log.Error(0, $"Please specify format of image to load. Must be one of: {String.Join(", ", SupportedFormats)}");
                    if(Args.Length > 3)
                        Log.Error(0, $"There are many arguments. Must be <filename> <imagetype> <format>");

                    return false;
                }
                // Check format
                var Format = Args[2];
                if (!SupportedFormats.Contains(Format))
                    Log.Error(0, $"Invalid format: {Format}. Must be one of: {String.Join(", ", SupportedFormats)}");

                return false;
            }
            else
                return true;
        }
    }
}
