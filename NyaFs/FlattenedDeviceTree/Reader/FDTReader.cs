using System;
using System.Collections.Generic;
using System.Text;
using Extension.Packet;
using Extension.Array;
using NyaFs.FlattenedDeviceTree.Types;

namespace NyaFs.FlattenedDeviceTree.Reader
{
    public class FDTReader : RawPacket
    {
        // https://devicetree-specification.readthedocs.io/en/v0.2/flattened-format.html
        // https://wiki.freebsd.org/FlattenedDeviceTree
        // 
        public FDTReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename))
        {

        }

        public FDTReader(byte[] Data) : base(Data)
        {

        }

        /// <summary>
        /// Read string from file to first zero byte
        /// </summary>
        /// <param name="Address"></param>
        /// <returns></returns>
        private string GetStringByAddress(uint Address)
        {
            List<byte> RawText = new List<byte>();

            byte C = ReadByte(Address);
            while (C != 0)
            {
                RawText.Add(C);

                Address++;
                C = ReadByte(Address);
            }

            return UTF8Encoding.UTF8.GetString(RawText.ToArray());
        }

        /// <summary>
        /// Read device tree from root node...
        /// </summary>
        /// <returns></returns>
        public FlattenedDeviceTree Read()
        {
            FlattenedDeviceTree fdt = new FlattenedDeviceTree();
            fdt.ReserveMemory = ReserveMemory;
            fdt.CpuId = BootCpuIdPhys;
            ProcessNode(fdt.Root, OffsetDtStruct);

            return fdt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="Address">Address of FDT_BEGIN_NODE node</param>
        /// <returns></returns>
        private uint ProcessNode(Node Node, uint Address)
        {
            Token TokenType = (Token)ReadUInt32BE(Address);
            if (TokenType == Token.FDT_BEGIN_NODE)
            {
                Address += 4;
                // Read name...
                Node.Name = GetStringByAddress(Address);
                uint NameLength = Convert.ToUInt32(Node.Name.Length) + 1;
                Address += NameLength.GetAligned(4);

                TokenType = (Token)ReadUInt32BE(Address);

                //Log.Write(0, $"Token {TokenType} at {Address:x08}");
                while (TokenType != Token.FDT_END_NODE)
                {
                    switch (TokenType)
                    {
                        case Token.FDT_PROP:
                            {
                                // Property
                                uint Length = ReadUInt32BE(Address + 4);
                                uint StringOffset = ReadUInt32BE(Address + 8);

                                string Name = GetStringByAddress(OffsetDtStrings + StringOffset);
                                byte[] Value = ReadArray(Address + 12, Length);

                                //Log.Write(0, $"Found node parameter at address {Address:x08} with name {Name}...");
                                Node.Properties.Add(new Property(Name, Value));

                                uint ParamSize = (12 + Length).GetAligned(4u);
                                Address += ParamSize;
                                break;
                            }
                        case Token.FDT_NOP:
                            Address += 4;
                            break;
                        case Token.FDT_BEGIN_NODE:
                            {
                                Node Nested = new Node();
                                // Nested node...

                                Node.Nodes.Add(Nested);
                                //Log.Write(0, $"Found nested node at address {Address:x08}...");
                                Address = ProcessNode(Nested, Address);
                                break;
                            }
                        default:
                            {
                                throw new InvalidOperationException($"Invalid dev tree state at address 0x{Address:x08}. Unexpected token {TokenType}.");
                            }
                    }

                    TokenType = (Token)ReadUInt32BE(Address);
                    //Log.Write(0, $"Token {TokenType} at {Address:x08}");
                }

                Address += 4;
            }
            else
                throw new InvalidOperationException($"Invalid dev tree state at address 0x{Address:x08}. Expected FDT_BEGIN_NODE token.");

            return Address;
        }

        /// <summary>
        /// This field shall contain the value 0xd00dfeed (big-endian).
        /// </summary>
        private uint Magic => ReadUInt32BE(0x0);

        /// <summary>
        /// This field shall contain the total size in bytes of the devicetree data structure. 
        /// This size shall encompass all sections of the structure: the header, the memory reservation block, 
        /// structure block and strings block, as well as any free space gaps between the blocks or after the final block.
        /// </summary>
        private uint TotalSize => ReadUInt32BE(0x4);

        /// <summary>
        /// This field shall contain the offset in bytes of the structure block (see section 5.4) from the beginning of the header.
        /// </summary>
        private uint OffsetDtStruct => ReadUInt32BE(0x8);

        /// <summary>
        /// This field shall contain the offset in bytes of the strings block (see section 5.5) from the beginning of the header.
        /// </summary>
        private uint OffsetDtStrings => ReadUInt32BE(0xC);

        /// <summary>
        /// This field shall contain the offset in bytes of the memory reservation block (see section 5.3) from the beginning of the header.
        /// </summary>
        private uint MemReserveMap => ReadUInt32BE(0x10);

        /// <summary>
        /// This field shall contain the version of the devicetree data structure. 
        /// The version is 17 if using the structure as defined in this document. 
        /// An DTSpec boot program may provide the devicetree of a later version, in which case this field shall contain 
        /// the version number defined in whichever later document gives the details of that version.
        /// </summary>
        private uint Version => ReadUInt32BE(0x14);

        /// <summary>
        /// This field shall contain the lowest version of the devicetree data structure with which the version used is backwards compatible. 
        /// So, for the structure as defined in this document (version 17), this field shall contain 16 because version 17 is 
        /// backwards compatible with version 16, but not earlier versions. 
        /// As per section 5.1, a DTSpec boot program should provide a devicetree in a format which is backwards compatible with version 16, 
        /// and thus this field shall always contain 16.
        /// </summary>
        private uint LastCompatibleVersion => ReadUInt32BE(0x18);

        /// <summary>
        /// This field shall contain the physical ID of the system’s boot CPU. 
        /// It shall be identical to the physical ID given in the reg property of that CPU node within the devicetree.
        /// </summary>
        private uint BootCpuIdPhys => ReadUInt32BE(0x1C);

        /// <summary>
        /// This field shall contain the length in bytes of the strings block section of the devicetree blob.

        /// </summary>
        private uint SizeDtString => ReadUInt32BE(0x20);

        /// <summary>
        /// This field shall contain the length in bytes of the structure block section of the devicetree blob.
        /// </summary>
        private uint SizeDtStruct => ReadUInt32BE(0x24);

        /// <summary>
        /// The memory reservation block consists of a list of pairs of 64-bit big-endian integers, 
        /// each pair being represented by the following C structure.
        /// </summary>
        private Types.ReservedMemory[] ReserveMemory
        {
            get
            {
                List<Types.ReservedMemory> fdtReserveEntries = new List<Types.ReservedMemory>();

                uint Address = MemReserveMap;
                ulong RegAddress = ReadUInt64BE(Address);
                ulong RegSize = ReadUInt64BE(Address + 8);

                while ((RegAddress != 0) && (RegSize != 0))
                {
                    fdtReserveEntries.Add(new Types.ReservedMemory(RegAddress, RegSize));

                    Address += 16;
                    RegAddress = ReadUInt64BE(Address);
                    RegSize = ReadUInt64BE(Address + 8);
                }

                return fdtReserveEntries.ToArray();
            }
        }

        private bool Correct
        {
            get
            {
                if (Magic != 0xd00dfeedu)
                    return false;

                return true;
            }
        }
    }
}
