using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Variables
{
    public class VariableScope
    {
        private List<Variable> Variables = new List<Variable>();

        public bool IsDefined(string Name)
        {
            foreach(var V in Variables)
            {
                if (V.Name == Name)
                    return true;
            }

            return false;
        }

        public string GetValue(string Name)
        {
            foreach (var V in Variables)
            {
                if (V.Name == Name)
                    return V.Value;
            }

            return "";
        }

        public void SetValue(string Name, string Value)
        {
            foreach (var V in Variables)
            {
                if (V.Name == Name)
                {
                    V.Value = Value;
                    return;
                }
            }

            Variables.Add(new Variable(Name, Value));
        }
    }
}
