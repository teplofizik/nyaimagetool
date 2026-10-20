using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaFsNfs
{
    internal sealed class NfsHandles
    {
        private const uint Magic = 0x4E594653;
        private readonly Dictionary<string, uint> PathToId = new Dictionary<string, uint>(StringComparer.Ordinal);
        private readonly Dictionary<uint, string> IdToPath = new Dictionary<uint, string>();
        private uint NextId = 1;

        public NfsHandles()
        {
            Allocate("");
        }

        public byte[] Get(string Path)
        {
            uint Id = Allocate(Path);
            var Result = new byte[8];
            Result[0] = (byte)((Magic >> 24) & 0xFF);
            Result[1] = (byte)((Magic >> 16) & 0xFF);
            Result[2] = (byte)((Magic >> 8) & 0xFF);
            Result[3] = (byte)(Magic & 0xFF);
            Result[4] = (byte)(Id >> 24);
            Result[5] = (byte)(Id >> 16);
            Result[6] = (byte)(Id >> 8);
            Result[7] = (byte)Id;
            return Result;
        }

        public uint GetId(string Path) => Allocate(Path);

        public bool TryResolve(byte[] Handle, out string Path)
        {
            Path = null;

            if ((Handle == null) || (Handle.Length != 8))
                return false;

            uint MagicValue = ((uint)Handle[0] << 24) | ((uint)Handle[1] << 16) |
                              ((uint)Handle[2] << 8) | Handle[3];
            if (MagicValue != Magic)
                return false;

            uint Id = ((uint)Handle[4] << 24) | ((uint)Handle[5] << 16) |
                      ((uint)Handle[6] << 8) | Handle[7];

            return IdToPath.TryGetValue(Id, out Path);
        }

        public void Rename(string OldPath, string NewPath)
        {
            if (OldPath == NewPath)
                return;

            var Affected = IdToPath
                .Where(Pair => Pair.Value == OldPath || Pair.Value.StartsWith(OldPath + "/", StringComparison.Ordinal))
                .Select(Pair => Pair.Key)
                .ToArray();

            foreach (var Id in Affected)
            {
                var Path = IdToPath[Id];
                var Updated = NewPath + Path.Substring(OldPath.Length);
                PathToId.Remove(Path);
                PathToId[Updated] = Id;
                IdToPath[Id] = Updated;
            }
        }

        public void Remove(string Path)
        {
            var Affected = IdToPath
                .Where(Pair => Pair.Value == Path || Pair.Value.StartsWith(Path + "/", StringComparison.Ordinal))
                .Select(Pair => Pair.Key)
                .ToArray();

            foreach (var Id in Affected)
            {
                PathToId.Remove(IdToPath[Id]);
                IdToPath.Remove(Id);
            }
        }

        private uint Allocate(string Path)
        {
            if (PathToId.TryGetValue(Path, out uint Id))
                return Id;

            Id = NextId++;
            PathToId[Path] = Id;
            IdToPath[Id] = Path;
            return Id;
        }
    }
}
