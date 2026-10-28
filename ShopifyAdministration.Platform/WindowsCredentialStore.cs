using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ShopifyAdministration.Core;

namespace ShopifyAdministration.Platform
{
    [SupportedOSPlatform("windows")]
    public sealed class WindowsCredentialStore : ICredentialStore
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CompuMaster.ShopifyAdministration.Credentials.v1");
        private static readonly byte[] Header = Encoding.ASCII.GetBytes("SAC1");
        private readonly string path;
        public bool CanPersist => OperatingSystem.IsWindows();
        public WindowsCredentialStore(string path)
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
            this.path = path;
        }
        public ConnectionSettings Load()
        {
            if (!File.Exists(path)) return null;
            byte[] plain = null;
            try
            {
                var info = new FileInfo(path);
                if (info.Length > 65536 || info.Length < 5) throw new InvalidDataException();
                var file = File.ReadAllBytes(path);
                for (int i = 0; i < Header.Length; i++)
                    if (file[i] != Header[i]) throw new InvalidDataException();
                var cipher = new byte[file.Length - Header.Length];
                Buffer.BlockCopy(file, Header.Length, cipher, 0, cipher.Length);
                plain = ProtectedData.Unprotect(cipher, Entropy, DataProtectionScope.CurrentUser);
                var settings = JsonConvert.DeserializeObject<ConnectionSettings>(Encoding.UTF8.GetString(plain));
                if (settings == null) throw new InvalidDataException();
                return settings.Normalize();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                ex is CryptographicException || ex is JsonException || ex is ArgumentException)
            {
                throw new InvalidOperationException("Die gespeicherte Anmeldung konnte nicht entschlüsselt werden. Bitte erneut anmelden; die Datei wird nicht automatisch überschrieben.");
            }
            finally { if (plain != null) Array.Clear(plain, 0, plain.Length); }
        }
        public void Save(ConnectionSettings settings)
        {
            var normalized = settings.Normalize();
            byte[] plain = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(normalized));
            string temporary = null;
            try
            {
                var cipher = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
                var directory = Path.GetDirectoryName(path);
                Directory.CreateDirectory(directory);
                temporary = Path.Combine(directory, ".credentials-" + Guid.NewGuid().ToString("N") + ".tmp");
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(Header);
                    stream.Write(cipher);
                    stream.Flush(true);
                }
                File.Move(temporary, path, true);
                temporary = null;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is CryptographicException)
            {
                throw new InvalidOperationException("Die Anmeldung konnte nicht sicher gespeichert werden. Bitte Berechtigungen prüfen oder ohne Speichern anmelden.");
            }
            finally
            {
                Array.Clear(plain, 0, plain.Length);
                if (temporary != null && File.Exists(temporary)) File.Delete(temporary);
            }
        }
        public void Delete()
        {
            try { File.Delete(path); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new InvalidOperationException("Die bisher gespeicherte Anmeldung konnte nicht entfernt werden. Bitte die lokalen Dateiberechtigungen prüfen.");
            }
        }
    }
}
