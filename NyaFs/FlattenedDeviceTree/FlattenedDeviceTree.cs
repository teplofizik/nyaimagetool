using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.FlattenedDeviceTree
{
    public class FlattenedDeviceTree
    {
        public Types.Node Root = new Types.Node();


        public uint CpuId = 0;

        /// <summary>
        /// Зарезре
        /// </summary>
        public Types.ReservedMemory[] ReserveMemory = new Types.ReservedMemory[] { };

        public Types.Node Get(string Path)
        {
            if (Path == ".") return Root;

            if (Path[0] == '/') Path = Path.Substring(1);
            var Parts = Path.Split("/");

            Types.Node Base = Root;
            for(int i = 0; i < Parts.Length; i++)
            {
                var P = Parts[i];
                bool Found = false;
                foreach (var I in Base.Nodes)
                {
                    if (I.Name == P)
                    {
                        Base = I;
                        Found = true;
                        if (i == Parts.Length - 1)
                            return I;
                        else
                            break;
                    }
                }

                if(!Found)
                    throw new ArgumentException($"{Path} is not found in filesystem");
            }

            throw new ArgumentException($"{Path} is not found in filesystem");
        }
    }
}
