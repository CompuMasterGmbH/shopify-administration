Imports System.Threading
Imports System.Threading.Tasks
Imports ShopifyAdministration.Core
Imports ShopifyAdministration.Platform

Public Class MainForm
    Private ReadOnly credentialStore As ICredentialStore
    Private ReadOnly session As LoginSession
    Private ReadOnly shutdown As New CancellationTokenSource()
    Private queries As ShopifyQueries
    Private bulk As BulkQueries
    Private loading As Boolean
    Private loginOpen As Boolean
    Private starting As Boolean = True

    Public Sub New()
        Me.New(CredentialStoreFactory.Create())
    End Sub

    Public Sub New(store As ICredentialStore, Optional loginSession As LoginSession = Nothing)
        InitializeComponent()
        credentialStore = store
        session = If(loginSession, New LoginSession(credentialStore))
        UpdateSessionUi()
    End Sub

    Protected Overrides Async Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        Dim message As String = ""
        Try
            Dim saved = session.LoadRememberedConnection()
            If saved IsNot Nothing Then
                ToolStripStatusLabelRowCountInfo.Text = "Gespeicherte Anmeldung wird geprüft …"
                Await session.LoginAsync(saved, True, shutdown.Token)
                If IsDisposed Then Return
                ApplyConnection()
                Return
            End If
        Catch ex As OperationCanceledException
            If shutdown.IsCancellationRequested Then Return
            message = "Zeitüberschreitung bei der automatischen Anmeldung. Bitte erneut anmelden."
        Catch ex As Exception
            If shutdown.IsCancellationRequested OrElse IsDisposed Then Return
            message = If(TypeOf ex Is InvalidOperationException, ex.Message, "Die automatische Anmeldung ist fehlgeschlagen. Bitte erneut anmelden.")
        Finally
            starting = False
            If Not IsDisposed Then UpdateSessionUi()
        End Try
        ShowLogin(message)
    End Sub

    Private Sub ApplyConnection()
        queries = New ShopifyQueries(session.ActiveClient)
        bulk = New BulkQueries(queries)
        UpdateSessionUi()
    End Sub

    Private Sub ShowLogin(Optional message As String = "")
        If loginOpen OrElse IsDisposed Then Return
        loginOpen = True
        Try
            Using form As New LoginForm(session, credentialStore.CanPersist, session.LastConnection, message)
                If form.ShowDialog(Me) = DialogResult.OK Then ApplyConnection()
            End Using
        Finally
            loginOpen = False
            UpdateSessionUi()
        End Try
    End Sub

    Private Sub LoginToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoginToolStripMenuItem.Click
        If session.ActiveClient IsNot Nothing Then
            session.Logout()
            For Each child As Form In OwnedForms
                child.Close()
            Next
            DataGridView.DataSource = Nothing
            queries = Nothing
            bulk = Nothing
            UpdateSessionUi()
        End If
        ShowLogin()
    End Sub

    Private Sub UpdateSessionUi()
        Dim connected = session IsNot Nothing AndAlso session.ActiveClient IsNot Nothing
        ProdukteToolStripMenuItem.Enabled = connected AndAlso Not loading
        KategorienToolStripMenuItem.Enabled = connected AndAlso Not loading
        SmartCollectionsToolStripMenuItem.Enabled = connected AndAlso Not loading
        APIBrowserToolStripMenuItem.Enabled = connected AndAlso Not loading
        ExportZuXLSXToolStripMenuItem.Enabled = connected AndAlso Not loading AndAlso DataGridView.DataSource IsNot Nothing
        LoginToolStripMenuItem.Text = If(connected, "&Abmelden / Konto wechseln", "&Anmelden")
        LoginToolStripMenuItem.Enabled = Not starting
        Text = If(connected, session.ShopName & " – Shopify Administration", "Shopify Administration – nicht angemeldet")
        ToolStripStatusLabelRowCountInfo.Text = If(connected, "Angemeldet bei " & New Uri(session.ActiveClient.BaseUrl).Host, "Nicht angemeldet")
        ' These existing menu entries have no implementation yet.
        LieferantenToolStripMenuItem.Enabled = False
        WareneingängeToolStripMenuItem.Enabled = False
    End Sub

    Private Async Function LoadDataAsync(load As Func(Of BulkQueries, Task(Of DataTable))) As Task
        Dim requestClient = session.ActiveClient
        Dim requestBulk = bulk
        If requestClient Is Nothing OrElse loading Then Return
        loading = True
        UpdateSessionUi()
        Try
            Dim table = Await load(requestBulk)
            If Not IsDisposed AndAlso Object.ReferenceEquals(requestClient, session.ActiveClient) Then DataGridView.DataSource = table
        Catch ex As OperationCanceledException
            ' Logout cancels requests; never repopulate another account's table.
        Catch ex As Exception
            If Not IsDisposed AndAlso Object.ReferenceEquals(requestClient, session.ActiveClient) Then
                MessageBox.Show(Me, "Die Daten konnten nicht geladen werden. Bitte Verbindung und Berechtigungen prüfen.", "Shopify-Datenabruf", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Finally
            loading = False
            If Not IsDisposed Then UpdateSessionUi()
        End Try
    End Function

    Private Async Sub ProdukteToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ProdukteToolStripMenuItem.Click
        Await LoadDataAsync(Async Function(connection) (Await connection.ProductsListAsync()).ToDataTable())
    End Sub
    Private Async Sub KategorienToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles KategorienToolStripMenuItem.Click
        Await LoadDataAsync(Async Function(connection) (Await connection.CollectionsListAsync()).ToDataTable())
    End Sub
    Private Async Sub SmartCollectionsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SmartCollectionsToolStripMenuItem.Click
        Await LoadDataAsync(Async Function(connection) (Await connection.SmartCollectionsListAsync()).ToDataTable())
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If session.ActiveClient IsNot Nothing AndAlso DataGridView.DataSource IsNot Nothing Then
            ToolStripStatusLabelRowCountInfo.Text = session.ShopName & ": " & CType(DataGridView.DataSource, DataTable).Rows.Count & " Datensätze"
        End If
    End Sub

    Private Sub ExportZuXLSXToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExportZuXLSXToolStripMenuItem.Click
        If session.ActiveClient Is Nothing OrElse DataGridView.DataSource Is Nothing Then Return
        Try
            Using dialog As New SaveFileDialog With {.DefaultExt = ".xlsx", .Filter = "MS Excel (*.xlsx)|*.xlsx"}
                If dialog.ShowDialog(Me) = DialogResult.OK Then
                    CompuMaster.Data.XlsEpplus.WriteDataTableToXlsFile(dialog.FileName, CType(DataGridView.DataSource, DataTable), "Shopify")
                    System.Diagnostics.Process.Start(New ProcessStartInfo(dialog.FileName) With {.UseShellExecute = True})
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show(Me, "Die Excel-Datei konnte nicht geschrieben oder geöffnet werden.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub BeendenToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles BeendenToolStripMenuItem.Click
        Close()
    End Sub
    Private Sub APIBrowserToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles APIBrowserToolStripMenuItem.Click
        If queries IsNot Nothing Then
            Dim browser As New ApiBrowser(queries)
            browser.Show(Me)
        End If
    End Sub
    Private Sub APIAccessScopesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles APIAccessScopesToolStripMenuItem.Click
        OpenDocumentation("https://shopify.dev/docs/api/usage/access-scopes")
    End Sub
    Private Sub RESTAPIToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RESTAPIToolStripMenuItem.Click
        OpenDocumentation("https://shopify.dev/docs/api/admin-rest")
    End Sub
    Private Sub OpenDocumentation(url As String)
        Try
            System.Diagnostics.Process.Start(New ProcessStartInfo(url) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show(Me, "Der Browser konnte nicht geöffnet werden.", "Dokumentation")
        End Try
    End Sub
    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        shutdown.Cancel()
        session.Dispose()
        MyBase.OnFormClosed(e)
    End Sub
End Class
