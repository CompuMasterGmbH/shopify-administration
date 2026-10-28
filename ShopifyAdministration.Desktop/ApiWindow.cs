using Avalonia.Controls;
using ShopifyAdministration.Core;

namespace ShopifyAdministration.Desktop;

internal sealed class ApiWindow : Window
{
    private readonly TextBox resource = new() { Text = "products.json?limit=2" };
    private readonly TextBox response = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private readonly Button send = new() { Content = "GET abrufen" };
    private readonly CancellationTokenSource closed = new();
    public ApiWindow(ShopifyApiClient client)
    {
        Title = "API-Browser – einzelne Antwortseite"; Width = 900; Height = 650;
        var panel = new DockPanel { Margin = new Avalonia.Thickness(16) };
        var heading = new StackPanel { Spacing = 8, Children = { new TextBlock { Text = "GET innerhalb der angemeldeten API; zeigt eine einzelne Antwortseite." }, resource, send } };
        DockPanel.SetDock(heading, Dock.Top); panel.Children.Add(heading); panel.Children.Add(response); Content = panel;
        Closed += (_, _) => { closed.Cancel(); response.Text = ""; };
        send.Click += async (_, _) =>
        {
            send.IsEnabled = resource.IsEnabled = false;
            try
            {
                using var result = await client.GetAsync(resource.Text ?? "", closed.Token);
                var json = await result.Content.ReadAsStringAsync(closed.Token);
                if (!closed.IsCancellationRequested) response.Text = json;
            }
            catch (OperationCanceledException) { if (!closed.IsCancellationRequested) response.Text = "Anfrage abgebrochen oder Zeitüberschreitung."; }
            catch (Exception) { if (!closed.IsCancellationRequested) response.Text = "Abruf fehlgeschlagen. URL, Verbindung und Berechtigungen prüfen."; }
            finally { send.IsEnabled = resource.IsEnabled = true; }
        };
    }
}
