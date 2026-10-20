using NyaFsNfs.Protocol;
using System;

namespace NyaFsNfs
{
    internal sealed class MountService
    {
        private readonly NfsFs Fs;

        public MountService(NfsFs Fs)
        {
            this.Fs = Fs;
        }

        public byte[] Dispatch(RpcCall Call)
        {
            if ((Call.Version < 1) || (Call.Version > 3))
                return Rpc.ProgramMismatch(Call.Xid, 3, 3);

            try
            {
                var Body = new XdrWriter();

                switch (Call.Procedure)
                {
                    case 0:
                        break;

                    case 1:
                        Call.Args.ReadString(4096);
                        Body.WriteUInt32(0);
                        Body.WriteOpaque(Fs.Handles.Get(""));
                        Body.WriteUInt32(2);
                        Body.WriteUInt32(0);
                        Body.WriteUInt32(1);
                        break;

                    case 2:
                        Body.WriteBool(false);
                        break;

                    case 3:
                        Call.Args.ReadString(4096);
                        break;

                    case 4:
                        break;

                    case 5:
                        Body.WriteBool(true);
                        Body.WriteUInt32(1);
                        Body.WriteString("/");
                        Body.WriteUInt32(0);
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
    }
}
