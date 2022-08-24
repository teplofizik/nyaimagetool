using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.FlattenedDeviceTree.Writer.Types
{
    class FDTCompilerState
    {
        public List<byte> Strings = new List<byte>();
        public List<byte> Struct = new List<byte>();
        public Dictionary<string, uint> StringCache = new Dictionary<string, uint>();

    }
}
