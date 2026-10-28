using System.Data;
using OfficeOpenXml;

namespace ShopifyAdministration.Desktop;

/// <summary>Creates an XLSX document without invoking shell or platform services.</summary>
public static class SpreadsheetExport
{
    /// <summary>Writes a snapshot as an in-memory XLSX, retaining EPPlus 4 compatibility.</summary>
    public static byte[] Write(DataTable table)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Shopify");
        if (table.Columns.Count > 0) sheet.Cells[1, 1].LoadFromDataTable(table, true);
        return package.GetAsByteArray();
    }
}
