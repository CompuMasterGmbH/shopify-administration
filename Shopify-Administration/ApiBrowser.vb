Imports System.Threading.Tasks

Public Class ApiBrowser
    Private ReadOnly shopify As ShopifyQueries
    Private requesting As Boolean
    Protected Sub New()
        InitializeComponent()
    End Sub
    Public Sub New(connection As ShopifyQueries)
        InitializeComponent()
        shopify = connection
    End Sub
    Public Async Function RunGetRequestAsync() As Task
        If requesting OrElse shopify Is Nothing Then Return
        requesting = True
        Try
            Dim result = Await shopify.QueryJsonGetAsync(TextBoxUrl.Text)
            If Not IsDisposed Then TextBoxResponse.Text = result.Json
        Catch ex As OperationCanceledException
            If Not IsDisposed Then TextBoxResponse.Text = "Anfrage beendet."
        Catch ex As Exception
            If Not IsDisposed Then
                TextBoxResponse.Text = If(TypeOf ex Is InvalidOperationException, ex.Message, "Die Anfrage ist fehlgeschlagen.")
            End If
        Finally
            requesting = False
        End Try
    End Function
    Private Async Sub TextBoxUrl_TextChanged(sender As Object, e As EventArgs) Handles TextBoxUrl.Validated
        Await RunGetRequestAsync()
    End Sub
    Private Async Sub ApiBrowser_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Label1.Text = shopify.BaseUrl
        TextBoxUrl.Text = "products.json?limit=2"
        Await RunGetRequestAsync()
    End Sub
    Private Async Sub TextBoxUrl_KeyUp(sender As Object, e As KeyEventArgs) Handles TextBoxUrl.KeyUp
        If e.KeyCode = Keys.Enter OrElse e.KeyCode = Keys.Return Then
            e.Handled = True
            Await RunGetRequestAsync()
        End If
    End Sub
End Class
