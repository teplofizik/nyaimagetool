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

    public class ScriptBaseAll : ScriptBase
    {
        public ScriptBaseAll()
        {
            Add(new Commands.Load());
            Add(new Commands.Store());
            Add(new Commands.Set());
            Add(new Commands.Export());
            Add(new Commands.Fs.Dir());
            Add(new Commands.Fs.File());
            Add(new Commands.Fs.SLink());
            Add(new Commands.Fs.Rm());
            Add(new Commands.Fs.Chmod());
            Add(new Commands.Fs.Chown());
        }
    }
}
