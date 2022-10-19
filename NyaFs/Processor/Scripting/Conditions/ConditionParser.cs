using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Conditions
{
    class ConditionParser
    {
        public static Condition Parse(string ConditionText)
        {
            if ((ConditionText == null) || (ConditionText == ""))
                return null;

            if(Variables.VariableChecker.IsCorrectName(ConditionText))
                return new Basic.IsDefined(ConditionText);

            throw new ArgumentException("Invalid condition expression");
        }
    }
}
