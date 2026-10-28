using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ShopifyAdministration.Core.Updates
{
    /// <summary>Describes a safe update-check outcome.</summary>
    public enum UpdateStatus
    {
        /// <summary>No newer matching approved release exists.</summary>
        Current,
        /// <summary>A newer matching approved release is available.</summary>
        Available,
        /// <summary>No qualified channel is enabled for this build.</summary>
        Disabled,
        /// <summary>The package manager owns this installation's updates.</summary>
        Managed,
        /// <summary>The operator skipped this offered version.</summary>
        Skipped,
        /// <summary>The check failed safely without affecting normal application use.</summary>
        Error
    }
    /// <summary>Contains validated update information without Shopify credentials.</summary>
    public sealed class UpdateResult
    {
        internal UpdateResult(UpdateStatus status, string message, string version = null, Uri destination = null, string sha256 = null)
        { Status = status; Message = message; Version = version; Destination = destination; Sha256 = sha256; }
        /// <summary>Gets the outcome kind.</summary>
        public UpdateStatus Status { get; }
        /// <summary>Gets a safe localized status message.</summary>
        public string Message { get; }
        /// <summary>Gets the offered version, if available.</summary>
        public string Version { get; }
        /// <summary>Gets the validated download or signed ClickOnce manifest URL.</summary>
        public Uri Destination { get; }
        /// <summary>Gets the advertised integrity checksum; opening a browser does not verify downloaded bytes.</summary>
        public string Sha256 { get; }
    }

    /// <summary>Reads bounded HTTPS metadata using a separate unauthenticated HTTP client.</summary>
    public sealed class UpdateChecker : IDisposable
    {
        /// <summary>Gets the sole production metadata endpoint for this application.</summary>
        public static Uri Feed { get; } = new Uri("https://compumastergmbh.github.io/shopify-administration/app-updates.json");
        private readonly HttpClient client;
        /// <summary>Creates an update client with redirects/cookies/default credentials disabled.</summary>
        public UpdateChecker(HttpMessageHandler handler = null, TimeSpan? requestTimeout = null)
        {
            var timeout = requestTimeout ?? TimeSpan.FromSeconds(8);
            if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromSeconds(30)) throw new ArgumentOutOfRangeException(nameof(requestTimeout));
            client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, UseDefaultCredentials = false }, true)
            { Timeout = timeout };
        }
        /// <summary>Checks only the installed channel, OS, architecture and deployment; never offers a downgrade.</summary>
        public async Task<UpdateResult> CheckAsync(InstallationProfile installation, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (installation.Deployment == DeploymentKind.PackageManager)
                return new UpdateResult(UpdateStatus.Managed, "Diese Installation wird über die Paketverwaltung aktualisiert. Bitte deren Update-Funktion verwenden.");
            if (!installation.CanCheck)
                return new UpdateResult(UpdateStatus.Disabled, "Für diesen Entwicklungs-/Testbuild ist kein freigegebener Update-Kanal aktiv.");
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, Feed))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    if (response.StatusCode != HttpStatusCode.OK) return Error("Update-Prüfung nicht verfügbar. Bitte später erneut versuchen.");
                    if (response.Content.Headers.ContentLength > 524288) return Error("Ungültige Update-Metadaten.");
                    string json;
                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var buffer = new MemoryStream())
                    {
                        var bytes = new byte[8192]; int count;
                        while ((count = await stream.ReadAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            if (buffer.Length + count > 524288) return Error("Ungültige Update-Metadaten.");
                            buffer.Write(bytes, 0, count);
                        }
                        json = Encoding.UTF8.GetString(buffer.ToArray());
                    }
                    return Select(JObject.Parse(json), installation);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return Error("Zeitüberschreitung bei der Update-Prüfung."); }
            catch (HttpRequestException) { return Error("Updateserver nicht erreichbar. Die Anwendung kann weiter verwendet werden."); }
            catch (Newtonsoft.Json.JsonException) { return Error("Ungültige Update-Metadaten."); }
            catch (InvalidDataException) { return Error("Ungültige oder nicht vertrauenswürdige Update-Metadaten."); }
            catch (IOException) { return Error("Update-Prüfung konnte nicht abgeschlossen werden."); }
            catch (ArgumentException) { return Error("Ungültige Update-Metadaten."); }
            catch (FormatException) { return Error("Ungültige Update-Metadaten."); }
            catch (InvalidCastException) { return Error("Ungültige Update-Metadaten."); }
            catch (OverflowException) { return Error("Ungültige Update-Metadaten."); }
        }
        private static UpdateResult Select(JObject feed, InstallationProfile installation)
        {
            if (feed["schemaVersion"]?.Type != JTokenType.Integer || (int?)feed["schemaVersion"] != 1 || (string)feed["appId"] != InstallationProfile.AppId || !(feed["releases"] is JArray releases) || releases.Count > 1000)
                throw new InvalidDataException();
            UpdateVersion best = installation.Version; UpdateResult result = new UpdateResult(UpdateStatus.Current, "Die Anwendung ist aktuell.");
            foreach (var release in releases)
            {
                if (!(release is JObject item)) throw new InvalidDataException();
                if ((string)item["rid"] != installation.Rid || (string)item["channel"] != installation.Channel.ToString().ToLowerInvariant() ||
                    (string)item["deploymentType"] != installation.Deployment.ToString().ToLowerInvariant() || item["approved"]?.Type != JTokenType.Boolean || !(bool)item["approved"]) continue;
                if (!UpdateVersion.TryParse((string)item["version"], out var version) ||
                    !Regex.IsMatch((string)item["sha256"] ?? "", "^[0-9a-fA-F]{64}$") ||
                    !TryDestination((string)item["url"], installation.Deployment, installation.Channel, out var destination)) throw new InvalidDataException();
                if (installation.Channel == UpdateChannel.Stable && version.IsPrerelease) throw new InvalidDataException();
                var suffix = installation.Rid == "linux-x64" ? ".tar.gz" : ".zip";
                var filename = "shopify-administration-" + ((string)item["version"]).TrimStart('v', 'V') + "-" + installation.Rid + suffix;
                if (installation.Deployment == DeploymentKind.Portable && !destination.AbsolutePath.EndsWith("/" + filename, StringComparison.Ordinal)) throw new InvalidDataException();
                if (installation.Deployment == DeploymentKind.ClickOnce && (item["signed"]?.Type != JTokenType.Boolean || !(bool)item["signed"])) continue;
                if (version.CompareTo(best) <= 0) continue;
                best = version;
                result = new UpdateResult(UpdateStatus.Available, "Neue Version verfügbar: " + (string)item["version"], (string)item["version"], destination, (string)item["sha256"]);
            }
            return result;
        }
        /// <summary>Checks a destination again immediately before platform launch.</summary>
        public static bool TryDestination(string value, DeploymentKind deployment, UpdateChannel channel, out Uri uri)
        {
            uri = null;
            if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps || parsed.Port != 443 ||
                parsed.UserInfo != "" || parsed.Query != "" || parsed.Fragment != "" || value.IndexOf('%') >= 0) return false;
            var valid = deployment == DeploymentKind.ClickOnce
                ? parsed.IdnHost == "compumastergmbh.github.io" && parsed.AbsolutePath == "/shopify-administration/clickonce/" + channel.ToString().ToLowerInvariant() + "/ShopifyAdministration.application"
                : deployment == DeploymentKind.Portable && parsed.IdnHost == "github.com" && parsed.AbsolutePath.StartsWith("/CompuMasterGmbH/shopify-administration/releases/download/", StringComparison.Ordinal) &&
                    (parsed.AbsolutePath.EndsWith(".zip", StringComparison.Ordinal) || parsed.AbsolutePath.EndsWith(".tar.gz", StringComparison.Ordinal));
            if (valid) uri = parsed;
            return valid;
        }
        private static UpdateResult Error(string message) => new UpdateResult(UpdateStatus.Error, message);
        /// <inheritdoc />
        public void Dispose() => client.Dispose();
    }
}
