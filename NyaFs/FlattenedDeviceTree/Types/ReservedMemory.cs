using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.FlattenedDeviceTree.Types
{

    public class ReservedMemory
    {
        public ulong Address;
        public ulong Size;

        public ReservedMemory(ulong Address, ulong Size)
        {
            this.Address = Address;
            this.Size = Size;
        }
    }
}
