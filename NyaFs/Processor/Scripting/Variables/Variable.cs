using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Variables
{
    class Variable
    {
        /// <summary>
        /// Имя переменной
        /// </summary>
        public string Name;

        /// <summary>
        /// Значение переменной
        /// </summary>
        public string Value;

        public Variable(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
    }
}
