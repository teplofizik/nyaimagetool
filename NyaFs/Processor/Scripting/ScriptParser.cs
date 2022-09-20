using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Processor.Scripting
{
    public class ScriptParser
    {
        public Script Script = new Script();
        private ScriptBase Base;

        public ScriptParser(ScriptBase Base, string Filename)
        {
            this.Base = Base;

            Parse(Filename, System.IO.Path.GetFileName(Filename), System.IO.File.ReadAllLines(Filename));
        }

        public ScriptParser(ScriptBase Base, string Name, string[] Lines)
        {
            this.Base = Base;

            Parse("", Name, Lines);
        }

        /// <summary>
        /// Extract command args (all elements after first)
        /// </summary>
        /// <param name="Parts">Splitted command line</param>
        /// <returns></returns>
        private string[] ExtractArgs(string[] Parts)
        {
            List<string> Args = new List<string>();

            string Temp = null;
            for(int i = 1; i < Parts.Length; i++)
            {
                var P = Parts[i];
                if(P.Length > 0)
                {
                    if (Temp == null)
                    {
                        if (P.First() == '"')
                            Temp = P;
                        else
                            Args.Add(P);
                    }
                    else
                    {
                        Temp += $" {P}";
                        if(Temp.Last() == '"')
                            Args.Add(Temp.Substring(1, Temp.Length - 2));
                    }
                }
                else
                {
                    if (Temp != null) Temp += " ";
                }

            }

            if (Temp != null)
                return null;
            else
                return Args.ToArray();
        }

        /// <summary>
        /// Process a set of lines
        /// </summary>
        /// <param name="Filename"></param>
        /// <param name="Lines"></param>
        private void Parse(string Filename, string Name, string[] Lines)
        {
            var Sep = new char[] { ' ' };
            var SepTab = new char[] { '\t' };
            for (int i = 0; i < Lines.Length; i++)
            {
                var TCmd = Lines[i].Trim();
                if (TCmd.StartsWith('#')) continue;
                if (TCmd.Length == 0) continue;

                var Parts = (TCmd.IndexOf('\t') > 0) ? TCmd.Split(SepTab) :  TCmd.Split(Sep);

                var Command = Parts[0];
                var Args = ExtractArgs(Parts);

                if (Args == null)
                {
                    Script.HasErrors = true;
                    Log.Error(0, $"Error at {Name}:{i + 1}. Invalid quotes.");
                }
                else
                    ParseLine(Filename, Name, i + 1, Command, Args);
            }
        }

        private string DetectPath(string Caller, string Path)
        {
            string[] Variants = new string[]
            {
                "",
                System.IO.Path.GetDirectoryName(Caller)
            };
            foreach(var V in Variants)
            {
                var Target = System.IO.Path.Combine(V, Path);

                if (System.IO.File.Exists(Target))
                    return Target;
            }

            return null;
        }

        private void ParseLine(string Filename, string Name, int Line, string Cmd, string[] Args)
        {
            switch (Cmd)
            {
                case "include":
                    {
                        if(Args.Length != 1)
                        {
                            Script.HasErrors = true;
                            Log.Error(0, $"Error at {Name}:{Line}. Invalid arguments for command {Cmd}.");
                            return;
                        }

                        var Path = DetectPath(Filename, Args[0]);
                        if (Path == null)
                        {
                            Script.HasErrors = true;
                            Log.Error(0, $"Error at {Name}:{Line}.  not found.");
                            return;
                        }
                        else
                        {
                            Parse(Path, System.IO.Path.GetFileName(Path), System.IO.File.ReadAllLines(Path));
                        }
                    }
                    break;
                default:
                    {
                        var Gen = Base.GetGenerator(Cmd);

                        if (Gen == null)
                        {
                            Script.HasErrors = true;
                            Log.Error(0, $"Error at {Name}:{Line}. Command {Cmd} is not supported.");
                        }
                        else
                        {
                            // Check and select args configuration
                            var SArgs = Gen.GetArgs(Args);
                            if (SArgs != null)
                            {
                                var Step = Gen.Get(SArgs);
                                Step.SetScriptInfo(Filename, Name, Line);

                                Script.Steps.Add(Step);
                            }
                            else
                            {
                                Script.HasErrors = true;
                                Log.Error(0, $"Error at {Name}:{Line}. Invalid arguments for command '{Cmd}'.");
                            }
                        }
                        break;
                    }
            }
        }
    }
}
