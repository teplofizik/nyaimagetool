using NyaFsNfs.Protocol;
using System;

namespace NyaFsNfs
{
    internal sealed class PortmapService
    {
        private readonly int NfsPort;
        private readonly int MountPort;
        private readonly int PortmapPort;

        public PortmapService(int NfsPort, int MountPort, int PortmapPort)
        {
            this.NfsPort = NfsPort;
            this.MountPort = MountPort;
            this.PortmapPort = PortmapPort;
        }

        public byte[] Dispatch(RpcCall Call)
        {
            if (Call.Version != 2)
                return Rpc.ProgramMismatch(Call.Xid, 2, 2);

            try
            {
                var Body = new XdrWriter();

                switch (Call.Procedure)
                {
                    case 0:
                        break;

                    case 3:
                        {
                            uint Program = Call.Args.ReadUInt32();
                            uint Version = Call.Args.ReadUInt32();
                            uint Protocol = Call.Args.ReadUInt32();
                            Call.Args.ReadUInt32();

                            Body.WriteUInt32(GetPort(Program, Version, Protocol));
                        }
                        break;

                    case 4:
                        Body.WriteBool(true);
                        WriteMapping(Body, Rpc.ProgramNfs, 3, 6, NfsPort);
                        WriteMapping(Body, Rpc.ProgramNfs, 3, 17, NfsPort);
                        WriteMapping(Body, Rpc.ProgramMount, 3, 6, MountPort);
                        WriteMapping(Body, Rpc.ProgramMount, 3, 17, MountPort);
                        WriteMapping(Body, Rpc.ProgramPortmap, 2, 6, PortmapPort);
                        WriteMapping(Body, Rpc.ProgramPortmap, 2, 17, PortmapPort);
                        Body.WriteBool(false);
                        break;

                    default:
                        return Rpc.Error(Call.Xid, Rpc.AcceptProcedureUnavailable);
                }

                return Rpc.Accepted(Call, Body);
            }
            catch (XdrException)
            {
                return Rpc.Error(Call.Xid, Rpc.AcceptGarbageArgs);
            }
            catch
            {
                return Rpc.Error(Call.Xid, Rpc.AcceptSystemError);
            }
        }

        private uint GetPort(uint Program, uint Version, uint Protocol)
        {
            bool Supported = (Protocol == 6) || (Protocol == 17);
            if (!Supported)
                return 0;

            if ((Program == Rpc.ProgramNfs) && (Version == 3))
                return (uint)NfsPort;

            if ((Program == Rpc.ProgramMount) && (Version >= 1) && (Version <= 3))
                return (uint)MountPort;

            if ((Program == Rpc.ProgramPortmap) && (Version == 2))
                return (uint)PortmapPort;

            return 0;
        }

        private static void WriteMapping(XdrWriter Body, uint Program, uint Version, uint Protocol, int Port)
        {
            Body.WriteBool(true);
            Body.WriteUInt32(Program);
            Body.WriteUInt32(Version);
            Body.WriteUInt32(Protocol);
            Body.WriteUInt32((uint)Math.Max(0, Port));
        }
    }
}
