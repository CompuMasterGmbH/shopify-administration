using System;
using System.Text.RegularExpressions;

namespace ShopifyAdministration.Core
{
    // Never format or log instances: the complete profile contains authentication material.
    public sealed class ConnectionSettings
    {
        public string ApiBaseUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string Password { get; set; } = "";
        public ConnectionSettings Copy() => new ConnectionSettings
        {
            ApiBaseUrl = ApiBaseUrl, ApiKey = ApiKey, Password = Password
        };

        public ConnectionSettings Normalize()
        {
            if (!Uri.TryCreate((ApiBaseUrl ?? "").Trim(), UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps || uri.Port != 443 ||
                !uri.Host.EndsWith(".myshopify.com", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.Length <= ".myshopify.com".Length ||
                uri.UserInfo != "" || uri.Query != "" || uri.Fragment != "")
                throw new ArgumentException("Bitte eine HTTPS-Shop-Adresse unter *.myshopify.com ohne eingebettete Zugangsdaten eingeben.");
            var path = uri.AbsolutePath.TrimEnd('/');
            if (!Regex.IsMatch(path, "^/admin/api/(20[0-9]{2}-(01|04|07|10)|unstable)$"))
                throw new ArgumentException("Bitte die vollständige API-Basis-URL eingeben: https://shop.myshopify.com/admin/api/JJJJ-MM/.");
            var key = (ApiKey ?? "").Trim();
            if (key.IndexOfAny(new[] { ':', '\r', '\n' }) >= 0)
                throw new ArgumentException("Der API-Schlüssel enthält unzulässige Zeichen.");
            if (string.IsNullOrWhiteSpace(Password) || Password.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                throw new ArgumentException("Bitte ein Passwort beziehungsweise einen Access-Token ohne Zeilenumbrüche eingeben.");
            return new ConnectionSettings
            {
                ApiBaseUrl = uri.GetLeftPart(UriPartial.Authority) + path + "/",
                ApiKey = key, Password = Password
            };
        }
    }
}
