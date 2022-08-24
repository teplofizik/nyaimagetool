using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting
{
    public class Script
    {
        public List<ScriptStep> Steps = new List<ScriptStep>();

        public bool HasErrors = false;
    }
}
