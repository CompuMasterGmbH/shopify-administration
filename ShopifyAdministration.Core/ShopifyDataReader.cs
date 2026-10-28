using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ShopifyAdministration.Core
{
    /// <summary>Names the supported, read-only, fully paginated datasets.</summary>
    public enum ShopifyDataset
    {
        /// <summary>All product variants with their parent product metadata.</summary>
        Products,
        /// <summary>All manual and automated collections.</summary>
        Collections,
        /// <summary>Automated collections only.</summary>
        SmartCollections,
        /// <summary>Transfers and all their line items; no inventory changes are performed.</summary>
        Transfers
    }

    /// <summary>Reads complete GraphQL datasets, rejecting loops, duplicates and partial responses.</summary>
    public sealed class ShopifyDataReader
    {
        private readonly ShopifyApiClient client;
        /// <summary>Initializes a reader bound to the authenticated client lifetime.</summary>
        public ShopifyDataReader(ShopifyApiClient client) => this.client = client ?? throw new ArgumentNullException(nameof(client));

        /// <summary>Returns records only after every required page and nested line page succeeded.</summary>
        /// <param name="dataset">The supported read-only view.</param>
        /// <param name="progress">Optional completed-page progress, without record contents.</param>
        /// <param name="cancellationToken">Cancels the entire operation, including retry waits.</param>
        /// <returns>A complete dataset; never a successful partial list.</returns>
        /// <exception cref="InvalidOperationException">The API response is incomplete or inconsistent.</exception>
        public async Task<IReadOnlyList<JObject>> ReadAsync(ShopifyDataset dataset, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(typeof(ShopifyDataset), dataset)) throw new ArgumentOutOfRangeException(nameof(dataset));
            var query = dataset == ShopifyDataset.Products ? "Variants" : dataset == ShopifyDataset.Transfers ? "Transfers" : "Collections";
            var field = dataset == ShopifyDataset.Products ? "productVariants" : dataset == ShopifyDataset.Transfers ? "inventoryTransfers" : "collections";
            var variables = new JObject();
            if (query == "Collections") variables["filter"] = dataset == ShopifyDataset.SmartCollections ? "collection_type:smart" : null;
            var records = await PagesAsync(query, field, variables, progress, cancellationToken).ConfigureAwait(false);
            if (dataset != ShopifyDataset.Transfers) return records;
            var result = new List<JObject>();
            foreach (var transfer in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lines = await PagesAsync("TransferLines", "inventoryTransfer.lineItems", new JObject { ["id"] = transfer["id"] }, progress, cancellationToken).ConfigureAwait(false);
                if (lines.Count == 0) result.Add(new JObject { ["transfer"] = transfer });
                foreach (var line in lines) result.Add(new JObject { ["transfer"] = transfer.DeepClone(), ["line"] = line });
            }
            return result;
        }

        private async Task<List<JObject>> PagesAsync(string query, string field, JObject variables, IProgress<int> progress, CancellationToken token)
        {
            string document;
            using (var stream = typeof(ShopifyDataReader).Assembly.GetManifestResourceStream("ShopifyAdministration.Core.Queries." + query + ".graphql"))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException("API-Abfrage fehlt."))) document = reader.ReadToEnd();
            var result = new List<JObject>();
            var cursors = new HashSet<string>(StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var page = 1; page <= 10000; page++)
            {
                token.ThrowIfCancellationRequested();
                var payload = await client.ReadQueryAsync(document, variables, token).ConfigureAwait(false);
                var connection = payload.SelectToken(field) as JObject;
                if (!(connection?["nodes"] is JArray nodes) || !(connection["pageInfo"] is JObject info) || info["hasNextPage"]?.Type != JTokenType.Boolean)
                    throw new InvalidOperationException("Unvollständige Shopify-Seite; es werden keine Teildaten übernommen.");
                foreach (var node in nodes)
                {
                    if (!(node is JObject record) || record["id"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)record["id"]) || !ids.Add((string)record["id"]))
                        throw new InvalidOperationException("Fehlende oder doppelte Datensatz-ID; Abruf wiederholen.");
                    result.Add(record);
                }
                progress?.Report(page);
                if (!(bool)info["hasNextPage"]) return result;
                var cursor = info["endCursor"]?.Type == JTokenType.String ? (string)info["endCursor"] : null;
                if (nodes.Count == 0 || string.IsNullOrEmpty(cursor) || !cursors.Add(cursor))
                    throw new InvalidOperationException("Ungültige oder wiederholte Shopify-Folgeseite.");
                variables["after"] = cursor;
            }
            throw new InvalidOperationException("Sicherheitsgrenze für Seiten erreicht; keine Teildaten übernommen.");
        }
    }
}
