using System.Net;

namespace FxSsh
{
    public class SshServerSettings
    {
        public const int DefaultPort = 22;

        public IPAddress LocalAddress { get; set; } = IPAddress.IPv6Any;
        public int Port { get; set; } = DefaultPort;

        public string ServerBanner { get; set; } = "FxSSH";

        public int IdleTimeout { get; set; }

        public SshServerSettings() { }

        public SshServerSettings(IPAddress Address, int Port, string Banner)
        {
            LocalAddress = Address;
            this.Port = Port;
            ServerBanner = Banner;
        }
    }
}