Option Strict On
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports ShopifyAdministration.Core

Public Class ShopifyQueries
    Private ReadOnly client As ShopifyApiClient
    Public Sub New(authenticatedClient As ShopifyApiClient)
        If authenticatedClient Is Nothing Then Throw New ArgumentNullException(NameOf(authenticatedClient))
        client = authenticatedClient
    End Sub
    Public ReadOnly Property BaseUrl As String
        Get
            Return client.BaseUrl
        End Get
    End Property
    Public Async Function QueryJsonGetAsync(resource As String) As Task(Of JsonQueryResultsPage)
        Using response = Await client.GetAsync(resource).ConfigureAwait(False)
            Dim result As New JsonQueryResultsPage With {.Json = Await response.Content.ReadAsStringAsync().ConfigureAwait(False)}
            Dim links As IEnumerable(Of String) = Nothing
            If response.Headers.TryGetValues("Link", links) Then
                result.NextPageLink = RestPagination.Next(client, resource, links)
            End If
            Return result
        End Using
    End Function
End Class
