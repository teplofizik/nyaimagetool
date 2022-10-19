using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Conditions
{
    public class Condition
    {
        public virtual bool IsCorrect(ImageProcessor Proc) => true;
    }
}
