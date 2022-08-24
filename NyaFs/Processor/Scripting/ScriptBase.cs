using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting
{
    public class ScriptBase
    {
        private List<ScriptStepGenerator> Commands = new List<ScriptStepGenerator>();

        public void Add(ScriptStepGenerator G)
        {
            Commands.Add(G);
        }

        public ScriptStepGenerator GetGenerator(string Name)
        {
            foreach (var G in Commands)
            {
                if (G.Name == Name)
                    return G;
            }

            return null;
        }
    }
}
