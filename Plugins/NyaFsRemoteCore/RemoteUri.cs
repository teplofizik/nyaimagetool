using System;

namespace NyaFsRemoteCore
{
    public static class RemoteUri
    {
        public static (string Host, int Port) SplitHostPort(string HostPort, int DefaultPort)
        {
            if (string.IsNullOrWhiteSpace(HostPort))
                throw new ArgumentException("Remote host is empty");

            var Value = HostPort.Trim();

            if (Value.StartsWith("["))
            {
                int End = Value.IndexOf(']');
                if (End < 0)
                    throw new ArgumentException($"Invalid IPv6 host '{HostPort}'");

                var Host = Value.Substring(1, End - 1);
                if ((End + 1 < Value.Length) && (Value[End + 1] == ':') &&
                    Int32.TryParse(Value.Substring(End + 2), out int Port))
                    return (Host, Port);

                return (Host, DefaultPort);
            }

            int Colon = Value.LastIndexOf(':');
            if ((Colon > 0) && (Value.IndexOf(':') == Colon) &&
                Int32.TryParse(Value.Substring(Colon + 1), out int P))
                return (Value.Substring(0, Colon), P);

            return (Value, DefaultPort);
        }
    }
}
