namespace FxSsh.Messages.Connection
{
    [Message("SSH_MSG_REQUEST_FAILURE", MessageNumber)]
    public class RequestFailureMessage : ConnectionServiceMessage
    {
        private const byte MessageNumber = 82;

        public override byte MessageType { get { return MessageNumber; } }

        protected override void OnLoad(SshDataWorker reader)
        {
        }

        protected override void OnGetPacket(SshDataWorker writer)
        {
        }
    }
}
