using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ShopifyAdministration.Desktop;

/// <summary>Uses the toolkit's platform-specific file picker without native OS calls.</summary>
internal sealed class DesktopFiles(Window owner)
{
    public async Task<bool> SaveSpreadsheetAsync(byte[] bytes)
    {
        if (!owner.StorageProvider.CanSave) throw new PlatformNotSupportedException();
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Excel exportieren", SuggestedFileName = "shopify-export.xlsx", DefaultExtension = "xlsx",
            FileTypeChoices = new[] { new FilePickerFileType("Excel") { Patterns = new[] { "*.xlsx" } } }
        });
        if (file == null) return false;
        await using var output = await file.OpenWriteAsync();
        if (output.CanSeek) output.SetLength(0);
        await output.WriteAsync(bytes);
        return true;
    }
}
