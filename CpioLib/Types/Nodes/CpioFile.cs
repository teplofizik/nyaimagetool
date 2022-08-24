using Extension.Array;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CpioLib.Types.Nodes
{
    public class CpioFile : CpioNode
    {
        public CpioFile(string Path, DateTime Modified, byte[] Data) : base(Path,
                                                                           Data,
                                                                           Modified,
                                                                           0x81a4U)
        {

        }

        public CpioFile(string Path, string LocalPath) : this(Path,
                                                              new FileInfo(LocalPath).LastWriteTime,
                                                              File.ReadAllBytes(LocalPath))
        {

        }
    }
}
