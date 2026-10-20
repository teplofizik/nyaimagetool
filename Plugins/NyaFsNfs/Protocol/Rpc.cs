using System;

namespace NyaFsNfs.Protocol
{
    internal sealed class RpcCall
    {
        public uint Xid;
        public uint Program;
        public uint Version;
        public uint Procedure;
        public XdrReader Args;
    }

    internal static class Rpc
    {
        public const uint ProgramNfs = 100003;
        public const uint ProgramMount = 100005;
        public const uint ProgramPortmap = 100000;

        public const uint AcceptSuccess = 0;
        public const uint AcceptProgramUnavailable = 1;
        public const uint AcceptProgramMismatch = 2;
        public const uint AcceptProcedureUnavailable = 3;
        public const uint AcceptGarbageArgs = 4;
        public const uint AcceptSystemError = 5;

        public static bool TryParse(byte[] Data, out RpcCall Call)
        {
            Call = null;

            try
            {
                var Reader = new XdrReader(Data);
                uint Xid = Reader.ReadUInt32();
                uint MessageType = Reader.ReadUInt32();
                uint RpcVersion = Reader.ReadUInt32();
                uint Program = Reader.ReadUInt32();
                uint Version = Reader.ReadUInt32();
                uint Procedure = Reader.ReadUInt32();

                if ((MessageType != 0) || (RpcVersion != 2))
                    return false;

                Reader.ReadUInt32();
                Reader.ReadOpaque();
                Reader.ReadUInt32();
                Reader.ReadOpaque();

                Call = new RpcCall
                {
                    Xid = Xid,
                    Program = Program,
                    Version = Version,
                    Procedure = Procedure,
                    Args = Reader
                };

                return true;
            }
            catch (XdrException)
            {
                return false;
            }
        }

        public static byte[] Accepted(RpcCall Call, XdrWriter Body) => Accepted(Call.Xid, Body);

        public static byte[] Accepted(uint Xid, XdrWriter Body)
        {
            var Writer = new XdrWriter();
            Writer.WriteUInt32(Xid);
            Writer.WriteUInt32(1);
            Writer.WriteUInt32(0);
            Writer.WriteUInt32(0);
            Writer.WriteUInt32(0);
            Writer.WriteUInt32(AcceptSuccess);
            Writer.WriteRaw(Body?.ToArray());
            return Writer.ToArray();
        }

        public static byte[] Accepted(uint Xid, Action<XdrWriter> Body)
        {
            var Writer = new XdrWriter();
            Body(Writer);
            return Accepted(Xid, Writer);
        }

        public static byte[] Error(uint Xid, uint Status)
        {
            var Writer = new XdrWriter();
            Writer.WriteUInt32(Xid);
            Writer.WriteUInt32(1);
            Writer.WriteUInt32(0);
            Writer.WriteUInt32(0);
            Writer.WriteUInt32(0);
            Writer.WriteUInt32(Status);
            return Writer.ToArray();
        }

        public static byte[] ProgramMismatch(uint Xid, uint Low, uint High)
        {
            var Writer = new XdrWriter();
            Writer.WriteUInt32(Xid);
            Writer.WriteUInt32(1);
            Writer.WriteUInt32(0);
            Writer.WriteUInt32(0);
            Writer.WriteUInt32(0);
            Writer.WriteUInt32(AcceptProgramMismatch);
            Writer.WriteUInt32(Low);
            Writer.WriteUInt32(High);
            return Writer.ToArray();
        }
    }
}
