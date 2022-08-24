using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.FlattenedDeviceTree.Writer.Types
{
    class FDTHeader : RawPacket
    {
        public FDTHeader() : base(0x28)
        {
            WriteUInt32BE(0, 0xd00dfeed); // Magic

            WriteUInt32BE(0x14, 0x11); // Version
            WriteUInt32BE(0x18, 0x10); // Compat. Version
        }

        /// <summary>
        /// This field shall contain the total size in bytes of the devicetree data structure. 
        /// This size shall encompass all sections of the structure: the header, the memory reservation block, 
        /// structure block and strings block, as well as any free space gaps between the blocks or after the final block.
        /// </summary>
        public uint TotalSize
        {
            get { return ReadUInt32BE(0x4); }
            set { WriteUInt32BE(0x4, value); }
        }

        /// <summary>
        /// This field shall contain the offset in bytes of the structure block (see section 5.4) from the beginning of the header.
        /// </summary>
        public uint OffsetDtStruct
        {
            get { return ReadUInt32BE(0x8); }
            set { WriteUInt32BE(0x8, value); }
        }

        /// <summary>
        /// This field shall contain the offset in bytes of the strings block (see section 5.5) from the beginning of the header.
        /// </summary>
        public uint OffsetDtStrings
        {
            get { return ReadUInt32BE(0xC); }
            set { WriteUInt32BE(0xC, value); }
        }

        /// <summary>
        /// This field shall contain the offset in bytes of the memory reservation block (see section 5.3) from the beginning of the header.
        /// </summary>
        public uint MemReserveMap
        {
            get { return ReadUInt32BE(0x10); }
            set { WriteUInt32BE(0x10, value); }
        }

        /// <summary>
        /// This field shall contain the physical ID of the system’s boot CPU. 
        /// It shall be identical to the physical ID given in the reg property of that CPU node within the devicetree.
        /// </summary>
        public uint BootCpuIdPhys
        {
            get { return ReadUInt32BE(0x1C); }
            set { WriteUInt32BE(0x1C, value); }
        }

        /// <summary>
        /// This field shall contain the length in bytes of the strings block section of the devicetree blob.

        /// </summary>
        public uint SizeDtString
        {
            get { return ReadUInt32BE(0x20); }
            set { WriteUInt32BE(0x20, value); }
        }

        /// <summary>
        /// This field shall contain the length in bytes of the structure block section of the devicetree blob.
        /// </summary>
        public uint SizeDtStruct
        {
            get { return ReadUInt32BE(0x24); }
            set { WriteUInt32BE(0x24, value); }
        }

    }
}
