using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopifyAdministration.Core.Updates
{
    /// <summary>Persists only non-secret skipped update versions, scoped to the installation channel and deployment.</summary>
    public interface IUpdatePreferences
    {
        /// <summary>Reads a skipped version from the supplied application/channel/RID/deployment scope.</summary>
        Task<string> GetSkippedAsync(string scope, CancellationToken cancellationToken = default);
        /// <summary>Atomically stores a skipped version in the supplied scope.</summary>
        Task SetSkippedAsync(string scope, string version, CancellationToken cancellationToken = default);
    }

    /// <summary>Allows one automatic check per app session, while keeping manual checks available.</summary>
    public sealed class UpdateSession
    {
        private readonly UpdateChecker checker;
        private readonly InstallationProfile installation;
        private readonly IUpdatePreferences preferences;
        private int started;
        /// <summary>Composes a checker, qualified installation and non-secret preference store.</summary>
        public UpdateSession(UpdateChecker checker, InstallationProfile installation, IUpdatePreferences preferences)
        { this.checker = checker; this.installation = installation; this.preferences = preferences; }
        /// <summary>Checks automatically once, or on every explicit manual request; manual requests ignore skips.</summary>
        /// <returns>Null for a duplicate automatic request; otherwise the check result.</returns>
        public async Task<UpdateResult> CheckAsync(bool manual, CancellationToken cancellationToken = default)
        {
            if (!manual && Interlocked.CompareExchange(ref started, 1, 0) != 0) return null;
            var result = await checker.CheckAsync(installation, cancellationToken).ConfigureAwait(false);
            if (!manual && result.Status == UpdateStatus.Available)
            {
                try
                {
                    var skipped = await preferences.GetSkippedAsync(installation.Scope, cancellationToken).ConfigureAwait(false);
                    if (UpdateVersion.TryParse(skipped, out var version) && UpdateVersion.TryParse(result.Version, out var offered) && version.CompareTo(offered) == 0)
                        return new UpdateResult(UpdateStatus.Skipped, "Diese Version wurde übersprungen.");
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                { return new UpdateResult(UpdateStatus.Error, "Update-Einstellungen konnten nicht gelesen werden."); }
            }
            return result;
        }
        /// <summary>Persists a validated offered version for this exact installation scope.</summary>
        public Task SkipAsync(UpdateResult result, CancellationToken cancellationToken = default)
        {
            if (result == null || result.Status != UpdateStatus.Available || !UpdateVersion.TryParse(result.Version, out var version)) throw new ArgumentException("No offered update", nameof(result));
            return preferences.SetSkippedAsync(installation.Scope, version.Key, cancellationToken);
        }
    }
}
