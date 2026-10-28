using System;
using System.Data;
using System.IO;
using NUnit.Framework;
using OfficeOpenXml;

namespace ShopifyAdministration.UiTests;

[TestFixture, Category("SpreadsheetExport")]
public sealed class SpreadsheetExportTests
{
    [Test]
    public void ResolvedEpplusAssemblyRemainsBelowVersionFive()
    {
        Assert.That(typeof(ExcelPackage).Assembly.GetName().Version.Major, Is.LessThan(5));
    }

    [Test]
    public void UpdatedWrapperWritesReadableTypedWorkbook()
    {
        var directory = Path.Combine(Path.GetTempPath(), "unittests_shopify_export_" + Guid.NewGuid().ToString("N"));
        Assert.That(Directory.Exists(directory), Is.False);
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "export.xlsx");
            using var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add("Price", typeof(decimal));
            table.Rows.Add("Test product – Ä", 3, 12.5m);
            CompuMaster.Data.XlsEpplus.WriteDataTableToXlsFile(path, table, "Test data");
            using var workbook = new ExcelPackage(new FileInfo(path));
            var sheet = workbook.Workbook.Worksheets["Test data"];
            Assert.That(sheet, Is.Not.Null);
            Assert.That(sheet.Cells[1, 1].Text, Is.EqualTo("Name"));
            Assert.That(sheet.Cells[2, 1].Text, Is.EqualTo("Test product – Ä"));
            Assert.That(sheet.Cells[2, 2].GetValue<int>(), Is.EqualTo(3));
            Assert.That(sheet.Cells[2, 3].GetValue<decimal>(), Is.EqualTo(12.5m));
        }
        finally { Directory.Delete(directory, true); }
    }
}
