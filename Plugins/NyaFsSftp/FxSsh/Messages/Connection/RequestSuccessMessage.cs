namespace FxSsh.Messages.Connection
{
    [Message("SSH_MSG_REQUEST_SUCCESS", MessageNumber)]
    public class RequestSuccessMessage : ConnectionServiceMessage
    {
        private const byte MessageNumber = 81;

        public override byte MessageType { get { return MessageNumber; } }

        protected override void OnLoad(SshDataWorker reader)
        {
        }

        protected override void OnGetPacket(SshDataWorker writer)
        {
        }
    }
}
