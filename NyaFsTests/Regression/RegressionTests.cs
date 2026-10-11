using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace NyaFsTests.Regression
{
    /// <summary>
    /// Characterization ("golden") regression over the collected image corpus.
    ///
    /// It loads every image from the corpus through the same pipeline as NyaImageTool
    /// and snapshots a content-based signature (file hashes/modes/owners, kernel hash,
    /// canonical DTB hash). Timestamps and compression specifics are intentionally NOT
    /// part of the signature.
    ///
    /// Environment variables:
    ///   NYAIMAGETOOL_IMAGES  - corpus root (default: F:\TEMP\Images)
    ///   NYAIMAGETOOL_GOLDEN  - golden file path (default: &lt;this dir&gt;\golden.json)
    ///   NYAIMAGETOOL_REGEN   - set to "1" to (re)write the golden file instead of asserting
    /// </summary>
    public class RegressionTests
    {
        private readonly ITestOutputHelper Output;

        public RegressionTests(ITestOutputHelper output)
        {
            Output = output;
        }

        private static readonly JsonSerializerOptions Pretty = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions Compact = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static string ThisDir([CallerFilePath] string path = null) => Path.GetDirectoryName(path);

        private static string ImagesRoot()
        {
            var env = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (!string.IsNullOrEmpty(env)) return env;
            return @"F:\TEMP\Images";
        }

        private static string GoldenPath()
        {
            var env = Environment.GetEnvironmentVariable("NYAIMAGETOOL_GOLDEN");
            if (!string.IsNullOrEmpty(env)) return env;
            return Path.Combine(ThisDir(), "golden.json");
        }

        private static SortedDictionary<string, ImageSignature> Collect(string root)
        {
            var res = new SortedDictionary<string, ImageSignature>(StringComparer.Ordinal);

            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(root, file);
                var dir = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];

                if (dir.Equals("scripts", StringComparison.OrdinalIgnoreCase)) continue;

                var name = Path.GetFileName(file);
                if (name.Equals("README.md", StringComparison.OrdinalIgnoreCase)) continue;
                if (name.Equals("manifest.csv", StringComparison.OrdinalIgnoreCase)) continue;

                var key = rel.Replace('\\', '/');
                res[key] = RegressionSupport.Build(file);
            }

            return res;
        }

        [Fact]
        public void Images_Match_Golden()
        {
            var root = ImagesRoot();
            if (!Directory.Exists(root))
            {
                Output.WriteLine($"Corpus not found at '{root}', skipping regression.");
                return;
            }

            var current = Collect(root);
            Output.WriteLine($"Collected {current.Count} image signatures from {root}.");

            var goldenPath = GoldenPath();

            if (Environment.GetEnvironmentVariable("NYAIMAGETOOL_REGEN") == "1")
            {
                File.WriteAllText(goldenPath, JsonSerializer.Serialize(current, Pretty));
                Output.WriteLine($"Golden file written: {goldenPath}");
                return;
            }

            if (!File.Exists(goldenPath))
            {
                Assert.Fail($"Golden file not found: {goldenPath}. Run with NYAIMAGETOOL_REGEN=1 to create it.");
                return;
            }

            var golden = JsonSerializer.Deserialize<SortedDictionary<string, ImageSignature>>(
                File.ReadAllText(goldenPath));

            var failures = new List<string>();

            foreach (var kv in current)
            {
                if (!golden.ContainsKey(kv.Key))
                {
                    failures.Add($"[new] {kv.Key}");
                    continue;
                }

                var a = JsonSerializer.Serialize(golden[kv.Key], Compact);
                var b = JsonSerializer.Serialize(kv.Value, Compact);

                if (a != b)
                    failures.Add($"[changed] {kv.Key}\n  golden:   {a}\n  current:  {b}");
            }

            foreach (var key in golden.Keys)
                if (!current.ContainsKey(key))
                    failures.Add($"[missing] {key}");

            if (failures.Count > 0)
            {
                Output.WriteLine($"{failures.Count} differences:");
                foreach (var f in failures) Output.WriteLine(f);
                Assert.Fail($"{failures.Count} image signature(s) differ from golden. See test output.");
            }
        }
    }
}
