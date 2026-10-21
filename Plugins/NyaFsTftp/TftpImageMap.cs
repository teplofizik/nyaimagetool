using System;
using System.Collections.Generic;
using System.IO;

namespace NyaFsTftp
{
    internal sealed class TftpImageSource
    {
        public string Name;
        public string Source;
        public bool Loaded;
        public string Component;
        public string Format;

        public string Description =>
            Loaded ? $"loaded:{Component}" + (String.IsNullOrEmpty(Format) ? "" : ":" + Format) : Source;
    }

    internal static class TftpImageMap
    {
        private static readonly object Lock = new object();
        private static readonly Dictionary<string, TftpImageSource> Images =
            new Dictionary<string, TftpImageSource>(StringComparer.OrdinalIgnoreCase);

        public static bool Add(string Name, string Source, out string Error)
        {
            Error = null;

            if (!ValidateName(Name, out Error))
                return false;

            var Parsed = ParseSource(Source, out Error);
            if (Parsed == null)
                return false;

            Parsed.Name = Name;

            lock (Lock)
                Images[Name] = Parsed;

            return true;
        }

        public static bool Remove(string Name)
        {
            lock (Lock)
                return Images.Remove(Name);
        }

        public static TftpImageSource Find(string Name)
        {
            lock (Lock)
                return Images.TryGetValue(Name, out TftpImageSource Result) ? Result : null;
        }

        public static TftpImageSource[] List()
        {
            lock (Lock)
            {
                var Result = new TftpImageSource[Images.Count];
                Images.Values.CopyTo(Result, 0);
                Array.Sort(Result, (A, B) => StringComparer.OrdinalIgnoreCase.Compare(A.Name, B.Name));
                return Result;
            }
        }

        public static void Clear()
        {
            lock (Lock)
                Images.Clear();
        }

        private static bool ValidateName(string Name, out string Error)
        {
            Error = null;

            if (String.IsNullOrWhiteSpace(Name))
            {
                Error = "Image name must not be empty";
                return false;
            }

            if ((Name.IndexOf('/') >= 0) || (Name.IndexOf('\\') >= 0) || (Name == ".") || (Name == ".."))
            {
                Error = $"Invalid image name '{Name}'";
                return false;
            }

            return true;
        }

        private static TftpImageSource ParseSource(string Source, out string Error)
        {
            Error = null;

            if (String.IsNullOrWhiteSpace(Source))
            {
                Error = "Image source must not be empty";
                return null;
            }

            if (!Source.StartsWith("loaded:", StringComparison.OrdinalIgnoreCase))
            {
                return new TftpImageSource
                {
                    Source = Source,
                    Loaded = false
                };
            }

            var Value = Source.Substring("loaded:".Length);
            var Parts = Value.Split(new[] { ':' }, 2);
            var Component = Parts[0].ToLowerInvariant();

            if ((Component != "fs") && (Component != "kernel") && (Component != "dtb"))
            {
                Error = $"Unknown loaded image component '{Component}' (use fs, kernel or dtb)";
                return null;
            }

            return new TftpImageSource
            {
                Source = Source,
                Loaded = true,
                Component = Component,
                Format = (Parts.Length > 1) ? Parts[1].ToLowerInvariant() : null
            };
        }
    }
}
