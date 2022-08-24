using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CpioLib.Types;
using Extension.Array;

namespace CpioLib.IO
{
    public static class CpioParser
    {

        public static CpioArchive Load(byte[] Data)
        {
            var Res = new CpioArchive();

            long Offset = 0;
            while (Offset < Data.Length)
            {
                var FI = new CpioFileInfo(Data, Offset);

                if (FI.IsCorrectMagic)
                {
                    var Raw = Data.ReadArray(Offset, FI.FullFileBlockSize);
                    var F = new CpioNode(Raw);

                    if(F.INode > CpioNode.MaxNodeId)
                    {
                        CpioNode.MaxNodeId = F.INode;
                    }

                    if (!FI.IsTrailer)
                    {
                        Res.Files.Add(F);

                        Offset += FI.FullFileBlockSize;
                    }
                    else
                    {
                        Res.Trailer = F;
                        break;
                    }
                }
                else
                    break;
            }

            return Res;
        }

        public static CpioArchive Load(string Filename) => Load(File.ReadAllBytes(Filename));

    }
}
