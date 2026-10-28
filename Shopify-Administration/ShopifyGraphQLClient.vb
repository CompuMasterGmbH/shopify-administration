Imports GraphQL
Imports GraphQL.Client.Http

Public Class ShopifyGraphQLClient

    Public ReadOnly Property BaseUrl As String

    Public Sub New(configuration As ShopifyAdministration.Core.ConnectionSettings)
        BaseUrl = configuration.Normalize().ApiBaseUrl
    End Sub

    Public _GraphQLClient As GraphQLHttpClient
    Public ReadOnly Property GraphQLClient As GraphQLHttpClient
        Get
            If _GraphQLClient Is Nothing Then
                '_GraphQLClient = New GraphQLHttpClient(Me.BaseUrl, New Newtonsoft.Json.JsonSerializer)
                '_GraphQLClient = New GraphQLHttpClient(Me.BaseUrl, New NewtonsoftJsonJsonSerializer)
            End If
            Return _GraphQLClient
        End Get
    End Property

    Public Async Function QueryJsonAsync2(url As String) As Task(Of String)
        Dim heroRequest As New GraphQLRequest(query:="
            {
                hero {
                    name
                }
            }")
        Dim Response = Await GraphQLClient.SendQueryAsync(Of ProductList)(heroRequest)
        If Response.Errors.Length <> 0 Then
            Dim Errors As String = ""
            For MyCounter As Integer = 0 To Response.Errors.Length - 1
                If MyCounter <> 0 Then Errors &= System.Environment.NewLine
                Errors &= Response.Errors(MyCounter).Path.ToString & ": " & Response.Errors(MyCounter).Message
            Next
            Throw New InvalidOperationException("ERROR: " & Errors)
        End If
        Return Response.Data.ToString
    End Function

    Private ShopNameRequestQuery As New GraphQLRequest(query:="{
	        shop {
		        name
	        }
        }")

    Public Function QueryTable(url As String) As System.Data.DataTable
        'Dim Result As Object = Newtonsoft.Json.JsonConvert.DeserializeObject(Me.QueryJson(url))
        Return Nothing
    End Function

End Class
