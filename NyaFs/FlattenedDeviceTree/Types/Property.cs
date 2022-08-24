using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.FlattenedDeviceTree.Types
{
    public class Property
    {
        public string Name;
        public byte[] Value;

        public Property(string Name, byte[] Value)
        {
            this.Name = Name;
            this.Value = Value;
        }

        public override string ToString()
        {
            return $"PROPERTY {Name} V:{Value.Length}";
        }
    }
}
