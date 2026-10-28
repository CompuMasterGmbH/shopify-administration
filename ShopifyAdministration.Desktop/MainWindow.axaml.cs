using System.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using ShopifyAdministration.Core;
using TMM_Shopify_Administration;

namespace ShopifyAdministration.Desktop;

/// <summary>Hosts the desktop session without depending on Windows Forms.</summary>
public sealed partial class MainWindow : Window
{
    private readonly LoginSession session;
    private readonly LoginPresenter login;
    private readonly bool offline;
    private readonly CancellationTokenSource shutdown = new();
    private readonly List<ApiWindow> browsers = new();
    private bool loading;
    private DataTable table;
    /// <summary>Creates a safe offline design/smoke window without operational storage.</summary>
    public MainWindow() : this(new LoginSession(new SessionOnlyCredentialStore()), false, true) { }
    /// <summary>Creates a window owning the supplied session and its shutdown cancellation.</summary>
    /// <param name="session">Session disposed when this window closes.</param>
    /// <param name="canPersist">Whether the selected store can encrypt profiles.</param>
    /// <param name="offline">Disables automatic login for isolated UI tests.</param>
    public MainWindow(LoginSession session, bool canPersist, bool offline = false)
    {
        InitializeComponent();
        this.session = session; this.offline = offline;
        login = new LoginPresenter(session, canPersist);
        ShowLogin();
        Opened += async (_, _) =>
        {
            if (!offline && await login.TryAutomaticLoginAsync(shutdown.Token) && !shutdown.IsCancellationRequested) ShowSession();
        };
        Closed += (_, _) => { shutdown.Cancel(); CloseBrowsers(); login.Dispose(); session.Dispose(); };
    }
    private void ShowLogin()
    {
        var view = new LoginView { DataContext = login };
        view.SignedIn += (_, _) => ShowSession();
        LoginHost.Content = view; LoginHost.IsVisible = true;
        Results.IsVisible = Actions.IsVisible = Account.IsVisible = false;
        Status.Text = offline ? "Offline-Oberflächentest – kein Profil und keine Shopify-Anfragen" : "Nicht angemeldet";
    }
    private void ShowSession()
    {
        LoginHost.Content = null; LoginHost.IsVisible = false;
        Results.IsVisible = Actions.IsVisible = Account.IsVisible = true;
        Title = session.ShopName + " – Shopify Administration";
        Status.Text = "Angemeldet bei " + session.ShopName;
    }
    private void Logout(object sender, RoutedEventArgs e)
    {
        session.Logout(); CloseBrowsers(); table = null;
        Results.ItemsSource = null; Results.Columns.Clear(); Export.IsEnabled = false;
        login.Prefill(); Title = "Shopify Administration"; ShowLogin();
    }
    private void CloseBrowsers() { foreach (var browser in browsers.ToArray()) browser.Close(); browsers.Clear(); }
    private async void LoadData(object sender, RoutedEventArgs e)
    {
        var client = session.ActiveClient;
        if (client == null || loading) return;
        SetLoading(true);
        try
        {
            var bulk = new BulkQueries(new ShopifyQueries(client));
            var next = (((Button)sender).Tag as string) switch
            {
                "products" => (await bulk.ProductsListAsync()).ToDataTable(),
                "collections" => (await bulk.CollectionsListAsync()).ToDataTable(),
                _ => (await bulk.SmartCollectionsListAsync()).ToDataTable()
            };
            if (!ReferenceEquals(client, session.ActiveClient) || shutdown.IsCancellationRequested) return;
            table = next; Results.Columns.Clear();
            foreach (DataColumn column in table.Columns)
                Results.Columns.Add(new DataGridTextColumn { Header = column.ColumnName, Binding = new Binding("[" + column.Ordinal + "]") });
            Results.ItemsSource = table.Rows.Cast<DataRow>().Select(row => row.ItemArray).ToArray();
            Status.Text = session.ShopName + ": " + table.Rows.Count + " Datensätze";
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            if (ReferenceEquals(client, session.ActiveClient)) Status.Text = "Datenabruf fehlgeschlagen. Verbindung und Berechtigungen prüfen.";
        }
        finally { SetLoading(false); }
    }
    private void SetLoading(bool value)
    {
        loading = value; Actions.IsEnabled = !value; Progress.IsVisible = value;
        Cursor = value ? new Cursor(StandardCursorType.Wait) : Cursor.Default;
        Export.IsEnabled = !value && table != null;
    }
    private void OpenBrowser(object sender, RoutedEventArgs e)
    {
        if (session.ActiveClient == null) return;
        var browser = new ApiWindow(session.ActiveClient);
        browsers.Add(browser); browser.Closed += (_, _) => browsers.Remove(browser); browser.Show(this);
    }
    private async void ExportData(object sender, RoutedEventArgs e)
    {
        if (table == null || loading || session.ActiveClient == null) return;
        var currentClient = session.ActiveClient;
        var snapshot = table.Copy();
        try
        {
            var bytes = await Task.Run(() => SpreadsheetExport.Write(snapshot));
            if (!ReferenceEquals(currentClient, session.ActiveClient)) return;
            if (await new DesktopFiles(this).SaveSpreadsheetAsync(bytes)) Status.Text = "Excel-Datei gespeichert.";
        }
        catch (Exception) { Status.Text = "Excel-Datei konnte nicht gespeichert werden."; }
    }
}
