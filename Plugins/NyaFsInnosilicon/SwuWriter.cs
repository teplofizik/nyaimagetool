using NyaFs;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Composite;
using NyaFs.ImageFormat.Plugins;
using NyaFs.Keys;
using NyaFs.Filesystem.Cpio.Types;
using NyaFs.Filesystem.Cpio.Types.Nodes;
using NyaFsInnosilicon.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NyaFsInnosilicon
{
    /// <summary>
    /// Rebuilds an Innosilicon SWU CPIO archive. Untouched archives are returned
    /// byte-for-byte; for a dirty plan, every source entry is preserved and only
    /// changed payloads are replaced. The CPIO 070702 checksum is refreshed and
    /// the matching filename/sha256 pairs in sw-description are updated.
    /// </summary>
    public class SwuWriter
    {
        private readonly IKeyStore KeyStore;

        public SwuWriter(IKeyStore KeyStore = null)
        {
            this.KeyStore = KeyStore;
        }

        public byte[] Build(BaseImageBlob Blob, out string Error)
        {
            Error = null;
            try
            {
                var Source = Blob.BuildPlan?.Root?.Raw;
                if (Source == null)
                {
                    Error = "SWU writer needs an existing source image";
                    return null;
                }

                var Fw = new InnoFirmwareUpdate(Source);
                if (!Fw.Correct)
                {
                    Error = "SWU source is not a valid CPIO archive";
                    return null;
                }

                var Root = Blob.BuildPlan.Root;
                if (!IsDirty(Root))
                    return Source;

                var Contents = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                foreach (var Node in Fw.Items)
                {
                    var Name = (Node.Path ?? "").TrimStart('/');
                    var Child = FindChild(Root, Name);
                    Contents[Name] = (Child != null) ? (GetChildData(Child) ?? Node.Content) : Node.Content;
                }

                var Description = Fw.Read("sw-description");
                var DescriptionChanged = false;
                if (Description != null)
                {
                    var UpdatedDescription = Encoding.UTF8.GetBytes(
                        UpdateHashes(Encoding.UTF8.GetString(Description), Contents));
                    DescriptionChanged = !UpdatedDescription.SequenceEqual(Description);
                    Contents["sw-description"] = UpdatedDescription;
                }

                var ExistingSignature = Fw.Read("sw-description.sig");
                var SignProfile = Blob.GetContainerOption("swu", "sign-key", null);
                var HasNewSignature = false;

                if (DescriptionChanged || ((ExistingSignature == null) && !String.IsNullOrEmpty(SignProfile)))
                {
                    if (String.IsNullOrEmpty(SignProfile))
                    {
                        if (ExistingSignature != null)
                            Log.Warning(0, "SWU: sw-description changed, but no signing key is configured; " +
                                $"existing {DescribeSignature(ExistingSignature)} is stale. " +
                                "Set container swu.sign-key <profile> to re-sign.");
                    }
                    else
                    {
                        var Store = KeyStore ?? PluginHost.Current.KeyStore;
                        var Record = Store.Find(SignProfile);
                        if (Record == null)
                            Log.Warning(0, "SWU: signing key profile not found: " + SignProfile +
                                ((ExistingSignature != null)
                                    ? $"; existing {DescribeSignature(ExistingSignature)} is stale."
                                    : "; sw-description will remain unsigned."));
                        else
                        {
                            try
                            {
                                using var Key = FitSignature.LoadPrivateKey(
                                    Store.GetPrivateMaterial(Record));
                                var Signature = Key.SignHash(
                                    SHA256.HashData(Contents["sw-description"]),
                                    HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                                Contents["sw-description.sig"] = Signature;
                                HasNewSignature = ExistingSignature == null;
                            }
                            catch (Exception E)
                            {
                                Log.Warning(0, "SWU: cannot re-sign sw-description: " + E.Message);
                            }
                        }
                    }
                }

                var Builder = new NyaFs.Filesystem.Cpio.CpioFsBuilder("070702");
                foreach (var Node in Fw.Items)
                {
                    var Name = (Node.Path ?? "").TrimStart('/');
                    if (Contents.TryGetValue(Name, out var Data) && (Data != null) &&
                        !Data.SequenceEqual(Node.Content))
                        Builder.Raw(Node.UpdateContent(Data));
                    else
                        Builder.Raw(Node);
                }

                if (HasNewSignature && Contents.TryGetValue("sw-description.sig", out var NewSignature))
                {
                    var SignatureNode = new CpioFile("sw-description.sig", 0x1A4, 0, 0, NewSignature);
                    SignatureNode.SetMagic("070702");
                    SignatureNode.UpdateCheck();
                    Builder.Raw(SignatureNode);
                }

                return Builder.GetFilesystemImage();
            }
            catch (Exception E)
            {
                Error = E.Message;
                Log.Error(0, $"Cannot write SWU image: {E.Message}");
                return null;
            }
        }

        private static string DescribeSignature(byte[] Signature)
        {
            if (Signature == null)
                return "sw-description.sig";
            return Signature.Length switch
            {
                256 => "256-byte signature (looks like RSA-2048/SHA-256/PKCS#1 v1.5)",
                384 => "384-byte signature (looks like RSA-3072/SHA-256/PKCS#1 v1.5)",
                512 => "512-byte signature (looks like RSA-4096/SHA-256/PKCS#1 v1.5)",
                _ => $"{Signature.Length}-byte signature (format unknown)"
            };
        }

        private static string UpdateHashes(string Description, Dictionary<string, byte[]> Contents)
        {
            var Lines = Description.Split('\n');
            string Filename = null;

            for (var I = 0; I < Lines.Length; I++)
            {
                var File = Regex.Match(Lines[I], @"filename\s*=\s*""([^""]+)""");
                if (File.Success)
                    Filename = File.Groups[1].Value;

                if ((Filename != null) && Contents.TryGetValue(Filename, out var Data) && (Data != null))
                {
                    Lines[I] = Regex.Replace(Lines[I],
                        @"(sha256\s*=\s*"")[0-9a-fA-F]{64}("")",
                        M => M.Groups[1].Value + Hash(Data) + M.Groups[2].Value);
                }
            }

            return String.Join("\n", Lines);
        }

        private static string Hash(byte[] Data)
        {
            using (var Sha = SHA256.Create())
                return Convert.ToHexString(Sha.ComputeHash(Data)).ToLowerInvariant();
        }

        private static bool IsDirty(ImageBuildNode Node)
        {
            if (Node == null)
                return false;
            if (Node.Dirty)
                return true;
            foreach (var Child in Node.Children)
                if (IsDirty(Child))
                    return true;
            return false;
        }

        private static ImageBuildNode FindChild(ImageBuildNode Node, string Name)
        {
            if (Node == null)
                return null;
            foreach (var Child in Node.Children)
            {
                if (String.Equals((Child.Name ?? "").TrimStart('/'), Name, StringComparison.OrdinalIgnoreCase))
                    return Child;
                var Res = FindChild(Child, Name);
                if (Res != null)
                    return Res;
            }
            return null;
        }

        private static byte[] GetChildData(ImageBuildNode Child)
        {
            if (!Child.Dirty && (Child.Raw != null))
                return Child.Raw;

            var Writer = new ImageBuildPlanWriter(new ImageBuildPlan(Child));
            var Result = Writer.Build(out _);
            return (Result != null) ? Result : Child.Raw;
        }
    }
}
