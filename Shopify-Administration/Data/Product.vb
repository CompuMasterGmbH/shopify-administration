Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class Product
    Inherits PrimaryEntryBase

    Public Property ID As String
    Public Property Title As String
    Public Property vendor As String
    Public Property product_type As String
    Public Property handle As String
    Public Property template_suffix As String
    Public Property status As String
    Public Property published_scope As String
    Public Property tags As String
    Public Property admin_graphql_api_id As String
    Public Property Image As ProductImage
    Public Property Images As List(Of ProductImage)
    Public Property Variants As List(Of ProductVariant)
    Public Property Options As List(Of ProductOption)
    Public Property Created_At As DateTime
    Public Property Updated_At As DateTime
    Public Property Published_At As DateTime?
    Public Property body_html As String

    <JsonExtensionData> Public ExtendedData As IDictionary(Of String, JToken)

    Private Sub EnsureTableColumns(table As DataTable)
        'Common details
        EnsureTableColumn(table, "ID", GetType(String))
        EnsureTableColumn(table, "published_scope", GetType(String))
        EnsureTableColumn(table, "Title", GetType(String))
        EnsureTableColumn(table, "Barcode", GetType(String))
        EnsureTableColumn(table, "Price", GetType(Decimal))
        EnsureTableColumn(table, "compare_at_price", GetType(Decimal))
        EnsureTableColumn(table, "Variant_Title", GetType(String))
        EnsureTableColumn(table, "vendor", GetType(String))
        EnsureTableColumn(table, "product_type", GetType(String))
        EnsureTableColumn(table, "status", GetType(String))
        EnsureTableColumn(table, "Tags", GetType(String))
        EnsureTableColumn(table, "ImageUrl", GetType(String))
        'EnsureTableColumn(table, "Image", GetType(ProductImage))
        'EnsureTableColumn(table, "Images", GetType(List(Of ProductImage)))
        'EnsureTableColumn(table, "Variants", GetType(List(Of ProductVariant)))
        'EnsureTableColumn(table, "Options", GetType(List(Of ProductOptions)))
        EnsureTableColumn(table, "body_html", GetType(String))
        EnsureTableColumn(table, "handle", GetType(String))
        EnsureTableColumn(table, "template_suffix", GetType(String))
        EnsureTableColumn(table, "Created_At", GetType(DateTime))
        EnsureTableColumn(table, "Updated_At", GetType(DateTime))
        EnsureTableColumn(table, "Published_At", GetType(DateTime))
        EnsureTableColumn(table, "admin_graphql_api_id", GetType(String))
        'Variant details
        EnsureTableColumn(table, "Variant_ID", GetType(String))
        EnsureTableColumn(table, "Variant_ImageUrl", GetType(String))
        EnsureTableColumn(table, "Variant_ImageID", GetType(String))
        EnsureTableColumn(table, "Variant_Created_At", GetType(DateTime))
        EnsureTableColumn(table, "Variant_Updated_At", GetType(DateTime))
        EnsureTableColumn(table, "Variant_admin_graphql_api_id", GetType(String))
    End Sub

    Private Sub EnsureTableColumn(table As DataTable, columnName As String, dataType As Type)
        If table.Columns.Contains(columnName) = False Then
            table.Columns.Add(columnName, dataType)
        End If
    End Sub

    Public Overrides Sub AddToTableAsDataRows(table As DataTable)
        Me.EnsureTableColumns(table)
        For MyCounter As Integer = 0 To Me.Variants.Count - 1
            Dim Row As DataRow = table.NewRow

            'Common details
            Row("ID") = ID
            Row("published_scope") = published_scope
            Row("admin_graphql_api_id") = admin_graphql_api_id
            Row("Title") = Title
            Row("body_html") = body_html
            Row("vendor") = vendor
            Row("product_type") = product_type
            Row("handle") = handle
            Row("template_suffix") = template_suffix
            Row("status") = status
            Row("Tags") = tags
            Row("ImageUrl") = Image?.Src
            'Row("Image") = Image
            'Row("Images") = Images
            'Row("Variants") = Variants
            'Row("Options") = Options
            Row("Created_At") = Created_At
            Row("Updated_At") = Updated_At
            Row("Published_At") = CompuMaster.Data.Utils.NullableTypeWithItsValueOrDBNull(Of DateTime)(Published_At)
            'Variant details
            Row("Variant_ID") = Me.Variants(MyCounter).ID
            Row("barcode") = Me.Variants(MyCounter).barcode
            Row("Price") = CompuMaster.Data.Utils.NullableTypeWithItsValueOrDBNull(Of Decimal)(Me.Variants(MyCounter).Price)
            Row("compare_at_price") = CompuMaster.Data.Utils.NullableTypeWithItsValueOrDBNull(Of Decimal)(Me.Variants(MyCounter).compare_at_price)
            Row("Variant_admin_graphql_api_id") = Me.Variants(MyCounter).admin_graphql_api_id
            If Me.Variants(MyCounter).Title <> "Default Title" Then
                Row("Variant_Title") = Me.Variants(MyCounter).Title
            Else
                Row("Variant_Title") = DBNull.Value
            End If
            Row("Variant_Created_At") = Me.Variants(MyCounter).Created_At
            Row("Variant_Updated_At") = Me.Variants(MyCounter).Updated_At
            If Me.Variants(MyCounter).image_id <> Nothing Then
                Row("Variant_ImageUrl") = Me.GetImage(Me.Variants(MyCounter).image_id).Src
            End If
            Row("Variant_ImageID") = Me.Variants(MyCounter).image_id

            table.Rows.Add(Row)
        Next
    End Sub

    Public Function GetImage(id As String) As ProductImage
        For MyCounter As Integer = 0 To Me.Images.Count - 1
            If Me.Images(MyCounter).ID = id Then
                Return Me.Images(MyCounter)
            End If
        Next
        Throw New ArgumentOutOfRangeException("ID " & id & " not found in list of images", NameOf(id))
    End Function

End Class
