using System.Text;

namespace FxSsh.Messages.Connection
{
    [Message("SSH_MSG_GLOBAL_REQUEST", MessageNumber)]
    public class GlobalRequestMessage : ConnectionServiceMessage
    {
        private const byte MessageNumber = 80;

        public string RequestName { get; set; }

        public bool WantReply { get; set; }

        public override byte MessageType { get { return MessageNumber; } }

        protected override void OnLoad(SshDataWorker reader)
        {
            RequestName = reader.ReadString(Encoding.ASCII);
            WantReply = reader.ReadBoolean();
        }

        protected override void OnGetPacket(SshDataWorker writer)
        {
            writer.Write(RequestName, Encoding.ASCII);
            writer.Write(WantReply);
        }
    }
}
