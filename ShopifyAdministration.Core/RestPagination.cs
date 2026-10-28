using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShopifyAdministration.Core
{
    /// <summary>Resolves REST next links relative to the current page and enforces the authenticated origin.</summary>
    public static class RestPagination
    {
        /// <summary>Returns one validated next URL, or null when the response has no next relation.</summary>
        public static string Next(ShopifyApiClient client, string current, IEnumerable<string> headers)
        {
            var page = client.ResolveRequestUri(current);
            string next = null;
            foreach (var header in headers)
            {
                var foundNext = false;
                foreach (Match entry in Regex.Matches(header, @"<([^>]+)>([^<]*)"))
                {
                    var relation = Regex.Match(entry.Groups[2].Value, "(?:^|;)\\s*rel\\s*=\\s*(?:\"([^\"]+)\"|([^;,\\s]+))", RegexOptions.IgnoreCase);
                    var relations = relation.Groups[1].Success ? relation.Groups[1].Value : relation.Groups[2].Value;
                    if (!relations.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Contains("next", StringComparer.OrdinalIgnoreCase)) continue;
                    if (next != null) throw new InvalidOperationException("Mehrdeutige Shopify-Folgeseite.");
                    next = client.ResolveRequestUri(new Uri(page, entry.Groups[1].Value).AbsoluteUri).AbsoluteUri;
                    foundNext = true;
                }
                if (!foundNext && Regex.IsMatch(header, "rel\\s*=\\s*\"?next", RegexOptions.IgnoreCase))
                    throw new InvalidOperationException("Ungültige Shopify-Folgeseite.");
            }
            return next;
        }
    }
}
