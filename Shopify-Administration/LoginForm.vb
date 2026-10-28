Imports System.Threading
Imports ShopifyAdministration.Core

Public Class LoginForm
    Inherits Form
    Private ReadOnly session As LoginSession
    Private ReadOnly cancellation As New CancellationTokenSource()
    Private ReadOnly canRemember As Boolean
    Private ReadOnly urlBox As New TextBox With {.Name = "ApiUrlTextBox", .Dock = DockStyle.Fill}
    Private ReadOnly keyBox As New TextBox With {.Name = "ApiKeyTextBox", .Dock = DockStyle.Fill}
    Private ReadOnly passwordBox As New TextBox With {.Name = "PasswordTextBox", .Dock = DockStyle.Fill, .UseSystemPasswordChar = True}
    Private ReadOnly visibilityButton As New Button With {.Name = "ShowPasswordButton", .Text = "Anzeigen", .Dock = DockStyle.Right, .Width = 110}
    Private ReadOnly rememberBox As New CheckBox With {.Name = "RememberCheckBox", .Text = "URL und Zugangsdaten verschlüsselt auf diesem Computer speichern", .AutoSize = True, .Checked = False}
    Private ReadOnly errorLabel As New Label With {.Name = "LoginStatusLabel", .Dock = DockStyle.Fill, .ForeColor = Color.DarkRed}
    Private ReadOnly loginButton As New Button With {.Name = "SignInButton", .Text = "Anmelden", .AutoSize = True}
    Private ReadOnly cancelActionButton As New Button With {.Name = "CancelLoginButton", .Text = "Abbrechen", .AutoSize = True, .DialogResult = DialogResult.Cancel}
    Private submitting As Boolean

    Public Sub New(loginSession As LoginSession, canPersist As Boolean, Optional initial As ConnectionSettings = Nothing, Optional message As String = "")
        session = loginSession
        canRemember = canPersist
        Text = "Bei Shopify anmelden"
        Name = "LoginForm"
        StartPosition = FormStartPosition.CenterParent
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        AutoScaleMode = AutoScaleMode.Dpi
        ClientSize = New Size(700, 390)
        Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .ColumnCount = 2, .RowCount = 7}
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        For Each rowHeight As Integer In {48, 46, 46, 42, 58, 55, 45}
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, rowHeight))
        Next
        layout.Controls.Add(New Label With {.Text = "Shop-API-URL", .AutoSize = True}, 0, 0)
        layout.Controls.Add(urlBox, 1, 0)
        layout.Controls.Add(New Label With {.Text = "API-Schlüssel / Benutzer", .AutoSize = True}, 0, 1)
        layout.Controls.Add(keyBox, 1, 1)
        layout.Controls.Add(New Label With {.Text = "Passwort / Access-Token", .AutoSize = True}, 0, 2)
        Dim passwordPanel As New Panel With {.Dock = DockStyle.Fill}
        passwordPanel.Controls.Add(passwordBox)
        passwordPanel.Controls.Add(visibilityButton)
        layout.Controls.Add(passwordPanel, 1, 2)
        layout.Controls.Add(rememberBox, 0, 3)
        layout.SetColumnSpan(rememberBox, 2)
        Dim help As New Label With {.Dock = DockStyle.Fill, .Text = "API-URL: https://shop.myshopify.com/admin/api/JJJJ-MM/" & Environment.NewLine & "Für einen Access-Token ohne Basic-Authentifizierung den API-Schlüssel leer lassen."}
        layout.Controls.Add(help, 0, 4)
        layout.SetColumnSpan(help, 2)
        layout.Controls.Add(errorLabel, 0, 5)
        layout.SetColumnSpan(errorLabel, 2)
        Dim buttons As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft}
        buttons.Controls.Add(cancelActionButton)
        buttons.Controls.Add(loginButton)
        layout.Controls.Add(buttons, 0, 6)
        layout.SetColumnSpan(buttons, 2)
        Controls.Add(layout)
        AcceptButton = loginButton
        Me.CancelButton = cancelActionButton
        If initial IsNot Nothing Then
            urlBox.Text = initial.ApiBaseUrl
            keyBox.Text = initial.ApiKey
            passwordBox.Text = initial.Password
        End If
        rememberBox.Enabled = canPersist
        errorLabel.Text = If(canPersist, message, "Auf dieser Plattform ist kein sicherer Speicher verfügbar. Anmeldung ohne Speichern ist möglich.")
        AddHandler visibilityButton.Click, Sub()
                                               passwordBox.UseSystemPasswordChar = Not passwordBox.UseSystemPasswordChar
                                               visibilityButton.Text = If(passwordBox.UseSystemPasswordChar, "Anzeigen", "Verbergen")
                                           End Sub
        AddHandler loginButton.Click, AddressOf SignIn
        AddHandler FormClosing, Sub() cancellation.Cancel()
    End Sub

    Private Async Sub SignIn(sender As Object, e As EventArgs)
        If submitting Then Return
        submitting = True
        SetEditing(False)
        errorLabel.ForeColor = Color.DimGray
        errorLabel.Text = "Anmeldung wird geprüft …"
        Try
            Await session.LoginAsync(New ConnectionSettings With {.ApiBaseUrl = urlBox.Text, .ApiKey = keyBox.Text, .Password = passwordBox.Text}, rememberBox.Checked, cancellation.Token)
            If IsDisposed OrElse cancellation.IsCancellationRequested Then Return
            DialogResult = DialogResult.OK
            Close()
        Catch ex As OperationCanceledException
            If Not IsDisposed AndAlso Not cancellation.IsCancellationRequested Then errorLabel.Text = "Zeitüberschreitung. Bitte erneut versuchen."
        Catch ex As Exception
            If Not IsDisposed Then
                errorLabel.ForeColor = Color.DarkRed
                errorLabel.Text = If(TypeOf ex Is ArgumentException OrElse TypeOf ex Is InvalidOperationException, ex.Message, "Die Anmeldung ist fehlgeschlagen. Bitte erneut versuchen.")
            End If
        Finally
            If Not IsDisposed Then SetEditing(True)
            submitting = False
        End Try
    End Sub
    Private Sub SetEditing(enabled As Boolean)
        urlBox.Enabled = enabled
        keyBox.Enabled = enabled
        passwordBox.Enabled = enabled
        visibilityButton.Enabled = enabled
        loginButton.Enabled = enabled
        rememberBox.Enabled = enabled AndAlso canRemember
    End Sub
End Class
