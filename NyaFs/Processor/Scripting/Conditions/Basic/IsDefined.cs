using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Conditions.Basic
{
    class IsDefined : Condition
    {
        private string VarName;

        public IsDefined(string Name)
        {
            VarName = Name;
        }

        public override bool IsCorrect(ImageProcessor Proc) => Proc.Scope.IsDefined(VarName);
    }
}
