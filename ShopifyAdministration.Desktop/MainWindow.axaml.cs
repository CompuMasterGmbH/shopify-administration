using System.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using ShopifyAdministration.Core;

namespace ShopifyAdministration.Desktop;

/// <summary>Hosts the desktop session without depending on Windows Forms.</summary>
public sealed partial class MainWindow : Window
{
    private readonly LoginSession session;
    private readonly LoginPresenter login;
    private readonly RetrievalPresenter data;
    private readonly bool offline;
    private readonly CancellationTokenSource shutdown = new();
    private readonly List<ApiWindow> browsers = new();
    private bool loading;
    private bool exporting;
    private CancellationTokenSource exportCancellation;
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
        Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new Uri("avares://ShopifyAdministration.Desktop/Assets/app.png")));
        this.session = session; this.offline = offline;
        login = new LoginPresenter(session, canPersist);
        data = new RetrievalPresenter(session);
        data.PropertyChanged += (_, _) =>
        {
            SetLoading(data.IsLoading || exporting);
            CancelLoad.IsVisible = data.IsLoading;
            Status.Text = data.Status;
            if (!data.CanExport) { table = null; Results.ItemsSource = null; Results.Columns.Clear(); }
        };
        ShowLogin();
        Opened += async (_, _) =>
        {
            if (!offline && await login.TryAutomaticLoginAsync(shutdown.Token) && !shutdown.IsCancellationRequested) ShowSession();
        };
        Closed += (_, _) => { shutdown.Cancel(); exportCancellation?.Cancel(); CloseBrowsers(); data.Dispose(); login.Dispose(); session.Dispose(); };
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
        data.Clear(); exportCancellation?.Cancel(); session.Logout(); CloseBrowsers(); table = null;
        Results.ItemsSource = null; Results.Columns.Clear(); Export.IsEnabled = false;
        login.Prefill(); Title = "Shopify Administration"; ShowLogin();
    }
    private void CloseBrowsers() { foreach (var browser in browsers.ToArray()) browser.Close(); browsers.Clear(); }
    private async void LoadData(object sender, RoutedEventArgs e)
    {
        var client = session.ActiveClient;
        if (client == null || loading) return;
        try
        {
            if (!Enum.TryParse<ShopifyDataset>(((Button)sender).Tag as string, out var dataset)) return;
            await data.LoadAsync(dataset, shutdown.Token);
            var snapshot = data.Records;
            if (!data.CanExport) return;
            var next = await Task.Run(() => RecordTable.Create(snapshot), shutdown.Token);
            if (!ReferenceEquals(client, session.ActiveClient) || shutdown.IsCancellationRequested) return;
            if (data.IsLoading || !ReferenceEquals(snapshot, data.Records)) return;
            table = next; Results.Columns.Clear();
            foreach (DataColumn column in table.Columns)
                Results.Columns.Add(new DataGridTextColumn { Header = column.ColumnName, Binding = new Binding("[" + column.Ordinal + "]") });
            Results.ItemsSource = table.Rows.Cast<DataRow>().Select(row => row.ItemArray).ToArray();
            Status.Text = data.Status;
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            if (ReferenceEquals(client, session.ActiveClient)) Status.Text = "Datenabruf fehlgeschlagen. Verbindung und Berechtigungen prüfen.";
        }
        finally { SetLoading(data.IsLoading || exporting); }
    }
    private void CancelLoadClick(object sender, RoutedEventArgs e) => data.Cancel();
    private void ExplainPurchasing(object sender, RoutedEventArgs e) => Status.Text =
        "Lieferantenstamm und Bestellansicht sind noch nicht freigegeben (GitHub #11). Produkt-Vendors sind keine Lieferantenstammdaten. Es werden keine Bestellungen oder Wareneingänge verändert.";
    private void SetLoading(bool value)
    {
        loading = value; Actions.IsEnabled = !value; Progress.IsVisible = value;
        Cursor = value ? new Cursor(StandardCursorType.Wait) : Cursor.Default;
        Export.IsEnabled = !value && data.CanExport && table != null;
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
        var operation = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
        exportCancellation = operation;
        exporting = true; SetLoading(true);
        try
        {
            var bytes = await Task.Run(() => SpreadsheetExport.Write(snapshot));
            if (!ReferenceEquals(currentClient, session.ActiveClient)) return;
            if (await new DesktopFiles(this).SaveSpreadsheetAsync(bytes, operation.Token)) Status.Text = "Excel-Datei gespeichert.";
        }
        catch (OperationCanceledException) { }
        catch (Exception) { Status.Text = "Excel-Datei konnte nicht gespeichert werden."; }
        finally { operation.Dispose(); exportCancellation = null; exporting = false; SetLoading(data.IsLoading); snapshot.Dispose(); }
    }
}
