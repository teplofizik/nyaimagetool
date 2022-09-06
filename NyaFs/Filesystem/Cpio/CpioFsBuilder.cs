using Extension.Array;
using NyaFs.Filesystem.Universal;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Cpio
{
    class CpioFsBuilder : IFilesystemBuilder
    {
        private List<Types.CpioNode> Files = new List<Types.CpioNode>();

        /// <summary>
        /// Get builded filesystem image
        /// </summary>
        /// <returns></returns>
        public byte[] GetFilesystemImage()
        {
            var Res = new List<byte>();

            foreach (var F in Files)
                Res.AddRange(F.getPacket());

            Res.AddRange(new Types.Nodes.CpioTrailer().getPacket());

            var Padding = Convert.ToInt64(Res.Count).MakeSizeAligned(0x100);
            for (long i = 0; i < Padding; i++) Res.Add(0);
            return Res.ToArray();
        }

        /// <summary>
        /// Create block device
        /// </summary>
        /// <param name="Path">Path to block device</param>
        /// <param name="Major">Major number</param>
        /// <param name="Minor">Minor number</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Block(string Path, uint Major, uint Minor, uint User, uint Group, uint Mode)
        {
            Files.Add(new Types.Nodes.CpioBlock(Path, Mode, User, Group, Major, Minor));
        }

        /// <summary>
        /// Create char device
        /// </summary>
        /// <param name="Path">Path to char device</param>
        /// <param name="Major">Major number</param>
        /// <param name="Minor">Minor number</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Char(string Path, uint Major, uint Minor, uint User, uint Group, uint Mode)
        {
            Files.Add(new Types.Nodes.CpioChar(Path, Mode, User, Group, Major, Minor));
        }

        /// <summary>
        /// Create directory
        /// </summary>
        /// <param name="Path">Path to directory (parent dir must exists)</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Directory(string Path, uint User, uint Group, uint Mode)
        {
            Files.Add(new Types.Nodes.CpioDir(Path, Mode, User, Group));
        }

        /// <summary>
        /// Create fifo
        /// </summary>
        /// <param name="Path">Path to fifo</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Fifo(string Path, uint User, uint Group, uint Mode)
        {
            Files.Add(new Types.Nodes.CpioFifo(Path, Mode, User, Group));
        }

        /// <summary>
        /// Create file
        /// </summary>
        /// <param name="Path">Path to file</param>
        /// <param name="Content">File content</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void File(string Path, byte[] Content, uint User, uint Group, uint Mode)
        {
            Files.Add(new Types.Nodes.CpioFile(Path, Mode, User, Group, Content));
        }

        /// <summary>
        /// Create socket
        /// </summary>
        /// <param name="Path">Path to socket</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Socket(string Path, uint User, uint Group, uint Mode)
        {
            Files.Add(new Types.Nodes.CpioSocket(Path, Mode, User, Group));
        }

        /// <summary>
        /// Create symlink
        /// </summary>
        /// <param name="Path">Path to symlink</param>
        /// <param name="Target">Target path</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void SymLink(string Path, string Target, uint User, uint Group, uint Mode)
        {
            Files.Add(new Types.Nodes.CpioSLink(Path, Mode, User, Group, Target));
        }
    }
}
