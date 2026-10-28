using System;
using System.IO;
using ShopifyAdministration.Core;

namespace ShopifyAdministration.Platform
{
    public static class CredentialStoreFactory
    {
        public static ICredentialStore Create()
        {
            if (!OperatingSystem.IsWindows()) return new SessionOnlyCredentialStore();
            return new WindowsCredentialStore(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CompuMaster", "ShopifyAdministration", "credentials.v1.bin"));
        }
    }
}
