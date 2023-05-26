using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Processor.Scripting
{
    public class ScriptParser
    {
        private ScriptBase Base;

        public ScriptParser(ScriptBase Base)
        {
            this.Base = Base;
        }

        public Script ParseScript(string Filename)
        {
            return Parse(Filename, System.IO.Path.GetFileName(Filename), System.IO.File.ReadAllLines(Filename));
        }

        public Script ParseScript(string Name, string[] Lines)
        {
            return Parse("", Name, Lines);
        }

        /// <summary>
        /// Add commands
        /// </summary>
        /// <param name="Generators"></param>
        public void AddGenerators(ScriptStepGenerator[] Generators)
        {
            foreach(var G in Generators)
                Base.Add(G);
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
                        if (P.Last() == '"')
                        {
                            Args.Add(Temp.Substring(1, Temp.Length - 2));
                            Temp = null;
                        }
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

        private string ExtractCondition(string Line)
        {
            var Pos = Line.IndexOf('?');
            if (Pos > 0)
                return Line.Substring(0, Pos).Trim();
            else
                return null;
        }

        private string ExtraceCommand(string Line)
        {
            var Pos = Line.IndexOf('?');
            if (Pos > 0)
                return Line.Substring(Pos + 1).Trim();
            else
                return Line;
        }

        /// <summary>
        /// Process a set of lines
        /// </summary>
        /// <param name="Filename"></param>
        /// <param name="Lines"></param>
        public Script Parse(string Filename, string Name, string[] Lines)
        {
            var Result = new Script();

            var Sep = new char[] { ' ' };
            var SepTab = new char[] { '\t' };
            for (int i = 0; i < Lines.Length; i++)
            {
                var TCmd = Lines[i].Trim();
                if (TCmd.StartsWith('#')) continue;
                if (TCmd.Length == 0) continue;

                string Cond = ExtractCondition(TCmd);
                string Cmd = ExtraceCommand(TCmd);

                var Parts = (Cmd.IndexOf('\t') > 0) ? Cmd.Split(SepTab) : Cmd.Split(Sep);

                var Command = Parts[0];
                var Args = ExtractArgs(Parts);

                if (Args == null)
                {
                    Result.HasErrors = true;
                    Log.Error(0, $"Error at {Name}:{i + 1}. Invalid quotes.");
                }
                else
                    ParseLine(Result, Filename, Name, i + 1, Cond, Command, Args);
            }
            return Result;
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

        private void ParseLine(Script Result, string Filename, string Name, int Line, string Cond, string Cmd, string[] Args)
        {
            var Gen = Base.GetGenerator(Cmd);

            if (Gen == null)
            {
                Result.HasErrors = true;
                Log.Error(0, $"Error at {Name}:{Line}. Command {Cmd} is not supported.");
            }
            else
            {
                // Check and select args configuration
                var SArgs = Gen.GetArgs(Args);
                if ((SArgs != null) && (SArgs.ArgConfig >= 0))
                {
                    var Step = Gen.Get(SArgs);
                    Step.SetScriptInfo(Filename, Name, Line);
                    Step.SetGenerator(Gen, Args);

                    if (Cond != null) Step.SetCondition(Conditions.ConditionParser.Parse(Cond));

                    Result.Steps.Add(Step);
                }
                else
                {
                    Result.HasErrors = true;
                    Log.Error(0, $"Error at {Name}:{Line}. Invalid arguments for command '{Cmd}'.");
                }
            }
        }
    }
}
