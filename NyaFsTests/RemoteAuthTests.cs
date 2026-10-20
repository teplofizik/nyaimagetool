using NyaFs.Remote;
using System;
using System.IO;
using Xunit;

namespace NyaFsTests
{
    public class RemoteAuthTests
    {
        [Fact]
        public void FileCredentialStore_RoundTrip()
        {
            var Path = TempFile();

            try
            {
                var Store = new FileCredentialStore(Path);
                Store.Set("dev", RemoteAuth.FromPassword("root", "secret"));

                var Password = Store.Get("dev");
                Assert.Equal("root", Password.Login);
                Assert.Equal("secret", Password.Password);
                Assert.Equal("password", Password.Kind);

                Store.Set("keyed", RemoteAuth.FromPrivateKey("root", "id_ed25519", "phrase"));

                var Key = Store.Get("keyed");
                Assert.Equal("root", Key.Login);
                Assert.Equal("id_ed25519", Key.PrivateKeyPath);
                Assert.Equal("phrase", Key.Passphrase);
                Assert.Equal("key", Key.Kind);

                var List = Store.List();
                Assert.Equal(2, List.Length);
                Assert.Equal("dev", List[0].Name);
                Assert.Equal("keyed", List[1].Name);

                var Text = File.ReadAllText(Path);
                Assert.DoesNotContain("password=secret", Text);
                Assert.DoesNotContain("passphrase=phrase", Text);

                Assert.True(Store.Remove("dev"));
                Assert.False(Store.Remove("dev"));
                Assert.Single(Store.List());
            }
            finally
            {
                if (File.Exists(Path))
                    File.Delete(Path);
            }
        }

        [Fact]
        public void Resolver_InlinePasswordAndProfile()
        {
            var Path = TempFile();

            try
            {
                var Store = new FileCredentialStore(Path);
                Store.Set("device", RemoteAuth.FromPassword("root", "secret"));

                var Inline = RemoteAuthResolver.Resolve("admin", "pass", Store);
                Assert.Equal("admin", Inline.Login);
                Assert.Equal("pass", Inline.Password);

                var Empty = RemoteAuthResolver.Resolve("admin", "-", Store);
                Assert.Equal("", Empty.Password);

                var Profile = RemoteAuthResolver.Resolve("@device", "-", Store);
                Assert.Equal("root", Profile.Login);
                Assert.Equal("secret", Profile.Password);

                Assert.Throws<ArgumentException>(() => RemoteAuthResolver.Resolve("@", "-", Store));
                Assert.Throws<InvalidOperationException>(() => RemoteAuthResolver.Resolve("@missing", "-", Store));
            }
            finally
            {
                if (File.Exists(Path))
                    File.Delete(Path);
            }
        }

        [Fact]
        public void FileCredentialStore_RejectsInvalidProfile()
        {
            var Path = TempFile();

            try
            {
                var Store = new FileCredentialStore(Path);

                Assert.Throws<ArgumentException>(() => Store.Set("bad name", RemoteAuth.FromPassword("u", "p")));
                Assert.Throws<InvalidOperationException>(() => Store.Set("empty", new RemoteAuth { Login = "u" }));
            }
            finally
            {
                if (File.Exists(Path))
                    File.Delete(Path);
            }
        }

        private static string TempFile() =>
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_auth_" + Guid.NewGuid().ToString("N") + ".txt");
    }
}
