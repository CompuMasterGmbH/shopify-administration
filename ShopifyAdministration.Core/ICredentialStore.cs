using System;

namespace ShopifyAdministration.Core
{
    // Platform adapters protect the ENTIRE profile. Plaintext persistence is never a fallback.
    public interface ICredentialStore
    {
        bool CanPersist { get; }
        ConnectionSettings Load();
        void Save(ConnectionSettings settings);
        void Delete();
    }
    public sealed class SessionOnlyCredentialStore : ICredentialStore
    {
        public bool CanPersist => false;
        public ConnectionSettings Load() => null;
        public void Save(ConnectionSettings settings) => throw new PlatformNotSupportedException(
            "Kein sicherer Speicher verfügbar. Bitte ohne Speichern anmelden.");
        public void Delete() { }
    }
}
