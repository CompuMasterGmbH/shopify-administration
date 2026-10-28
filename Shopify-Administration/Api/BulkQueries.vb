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
        Return LoadCollectionsAsync()
    End Function
    Public Function SmartCollectionsListAsync() As Task(Of SmartCollectionsList)
        Return LoadListAsync(Of Collection, SmartCollectionsList)("smart_collections.json?limit=250")
    End Function
    Private Async Function LoadCollectionsAsync() As Task(Of CollectionList)
        Dim manual = Await LoadListAsync(Of Collection, CustomCollectionsList)("custom_collections.json?limit=250").ConfigureAwait(False)
        Dim smart = Await SmartCollectionsListAsync().ConfigureAwait(False)
        manual.Items.AddRange(smart.Items)
        Return manual
    End Function
    Private Async Function LoadListAsync(Of TItem As PrimaryEntryBase, TList As {ResponseList(Of TItem), New})(resource As String) As Task(Of TList)
        Dim result As New TList With {.Items = New List(Of TItem)}
        Dim seen As New HashSet(Of String)(StringComparer.Ordinal)
        Dim records As New HashSet(Of String)(StringComparer.Ordinal)
        Do
            Dim canonical = New Uri(New Uri(shopify.BaseUrl), resource).AbsoluteUri
            If Not seen.Add(canonical) Then Throw New InvalidOperationException("Shopify hat eine wiederholte Folgeseite geliefert.")
            Dim page = Await shopify.QueryJsonGetAsync(resource).ConfigureAwait(False)
            Dim parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(Of TList)(page.Json)
            If parsed Is Nothing OrElse parsed.Items Is Nothing Then Throw New InvalidOperationException("Ungültige Shopify-Listen-Antwort.")
            For Each item In parsed.Items
                If item Is Nothing OrElse String.IsNullOrWhiteSpace(item.ID) OrElse Not records.Add(item.ID) Then Throw New InvalidOperationException("Fehlende oder doppelte Datensatz-ID.")
                result.Items.Add(item)
            Next
            resource = page.NextPageLink
        Loop While resource IsNot Nothing
        Return result
    End Function
End Class
