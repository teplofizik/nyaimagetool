namespace NyaFsRemoteCore
{
    public static class ShellQuote
    {
        public static string Quote(string Value)
        {
            if (Value == null)
                return "''";

            return "'" + Value.Replace("'", "'\"'\"'") + "'";
        }
    }
}
