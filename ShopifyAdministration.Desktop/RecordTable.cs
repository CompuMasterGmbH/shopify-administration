using System.Data;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShopifyAdministration.Desktop;

/// <summary>Maps complete API records to a lossless, read-only tabular projection.</summary>
public static class RecordTable
{
    /// <summary>Flattens nested scalar fields without treating null quantities as zero.</summary>
    public static DataTable Create(IEnumerable<JObject> records)
    {
        var table = new DataTable();
        foreach (var record in records)
        {
            var values = new Dictionary<string, object>(StringComparer.Ordinal);
            Flatten(record, "", values);
            foreach (var key in values.Keys) if (!table.Columns.Contains(key)) table.Columns.Add(key, typeof(object));
            var row = table.NewRow();
            foreach (var value in values) row[value.Key] = value.Value ?? DBNull.Value;
            table.Rows.Add(row);
        }
        return table;
    }
    private static void Flatten(JObject record, string prefix, Dictionary<string, object> result)
    {
        foreach (var property in record.Properties())
        {
            var name = prefix + property.Name;
            if (property.Value is JObject child) { Flatten(child, name + ".", result); continue; }
            object value = property.Value is JValue scalar ? scalar.Value : property.Value.ToString(Formatting.None);
            if (value is string text && (property.Name == "price" || property.Name == "compareAtPrice" || property.Name == "amount") &&
                decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var money)) value = money;
            result[name] = value;
        }
    }
}
