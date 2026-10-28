using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ShopifyAdministration.Core.Updates;

namespace ShopifyAdministration.Platform;

/// <summary>Stores only non-secret update choices outside version-specific installation directories.</summary>
public sealed class UpdatePreferences : IUpdatePreferences
{
    private readonly string path;
    private readonly SemaphoreSlim gate = new(1, 1);
    /// <summary>Creates a store for an explicit preferences file; tests must supply a test-owned path.</summary>
    public UpdatePreferences(string path) => this.path = Path.GetFullPath(path);
    /// <summary>Gets the stable per-user update-preference path, separate from encrypted credentials.</summary>
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CompuMaster", "ShopifyAdministration", "update-preferences.json");
    /// <inheritdoc />
    public async Task<string> GetSkippedAsync(string scope, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try { return (string)(await ReadAsync(cancellationToken))[scope]; }
        finally { gate.Release(); }
    }
    /// <inheritdoc />
    public async Task SetSkippedAsync(string scope, string version, CancellationToken cancellationToken = default)
    {
        if (!UpdateVersion.TryParse(version, out var parsed)) throw new ArgumentException("Invalid skipped version", nameof(version));
        await gate.WaitAsync(cancellationToken);
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            var values = await ReadAsync(cancellationToken); values[scope] = parsed.Key;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            await File.WriteAllTextAsync(temporary, values.ToString(), new UTF8Encoding(false), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); gate.Release(); }
    }
    private async Task<JObject> ReadAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!File.Exists(path)) return new JObject();
        if (new FileInfo(path).Length > 32768) throw new InvalidDataException("Update preferences too large");
        return JObject.Parse(await File.ReadAllTextAsync(path, token));
    }
}
