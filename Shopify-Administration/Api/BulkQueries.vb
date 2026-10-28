Imports System.Threading.Tasks

Public Class BulkQueries
    Private ReadOnly shopify As ShopifyQueries
    Public Sub New(connection As ShopifyQueries)
        shopify = connection
    End Sub
    Public Function ProductsListAsync() As Task(Of ProductList)
        Return LoadListAsync(Of Product, ProductList)("products.json?limit=250")
    End Function
    Public Function CollectionsListAsync() As Task(Of CollectionList)
        Return LoadListAsync(Of Collection, CollectionList)("collection_listings.json?limit=250")
    End Function
    Public Function SmartCollectionsListAsync() As Task(Of SmartCollectionsList)
        Return LoadListAsync(Of Collection, SmartCollectionsList)("smart_collections.json?limit=250")
    End Function
    Private Async Function LoadListAsync(Of TItem As PrimaryEntryBase, TList As {ResponseList(Of TItem), New})(resource As String) As Task(Of TList)
        Dim result As New TList With {.Items = New List(Of TItem)}
        Dim seen As New HashSet(Of String)(StringComparer.Ordinal)
        Do
            If Not seen.Add(resource) Then Throw New InvalidOperationException("Shopify hat eine wiederholte Folgeseite geliefert.")
            Dim page = Await shopify.QueryJsonGetAsync(resource).ConfigureAwait(False)
            Dim parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(Of TList)(page.Json)
            If parsed Is Nothing OrElse parsed.Items Is Nothing Then Throw New InvalidOperationException("Ungültige Shopify-Listen-Antwort.")
            result.Items.AddRange(parsed.Items)
            resource = page.NextPageLink
        Loop While resource IsNot Nothing
        Return result
    End Function
End Class
