Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class Collection
    Inherits PrimaryEntryBase

    Public Property ID As String
    Public Property Title As String
    Public Property handle As String
    Public Property template_suffix As String
    Public Property status As String
    Public Property sort_order As String
    Public Property admin_graphql_api_id As String
    Public Property Image As Image
    Public Property Created_At As DateTime
    Public Property Updated_At As DateTime
    Public Property Published_At As DateTime?
    Public Property published_scope As String
    Public Property body_html As String

    <JsonExtensionData> Public ExtendedData As IDictionary(Of String, JToken)

    Private Sub EnsureTableColumns(table As DataTable)
        'Common details
        EnsureTableColumn(table, "ID", GetType(String))
        EnsureTableColumn(table, "published_scope", GetType(String))
        EnsureTableColumn(table, "Title", GetType(String))
        EnsureTableColumn(table, "sort_order", GetType(String))
        EnsureTableColumn(table, "ImageUrl", GetType(String))
        'EnsureTableColumn(table, "Image", GetType(Image))
        EnsureTableColumn(table, "body_html", GetType(String))
        EnsureTableColumn(table, "handle", GetType(String))
        EnsureTableColumn(table, "template_suffix", GetType(String))
        EnsureTableColumn(table, "Created_At", GetType(DateTime))
        EnsureTableColumn(table, "Updated_At", GetType(DateTime))
        EnsureTableColumn(table, "Published_At", GetType(DateTime))
        EnsureTableColumn(table, "admin_graphql_api_id", GetType(String))
    End Sub

    Private Sub EnsureTableColumn(table As DataTable, columnName As String, dataType As Type)
        If table.Columns.Contains(columnName) = False Then
            table.Columns.Add(columnName, dataType)
        End If
    End Sub

    Public Overrides Sub AddToTableAsDataRows(table As DataTable)
        Me.EnsureTableColumns(table)
        Dim Row As DataRow = table.NewRow

        'Common details
        Row("ID") = ID
        Row("published_scope") = published_scope
        Row("admin_graphql_api_id") = admin_graphql_api_id
        Row("Title") = Title
        Row("body_html") = body_html
        Row("handle") = handle
        Row("template_suffix") = template_suffix
        Row("sort_order") = sort_order
        Row("ImageUrl") = Image?.Src
        'Row("Image") = Image
        'Row("Images") = Images
        'Row("Variants") = Variants
        'Row("Options") = Options
        Row("Created_At") = Created_At
        Row("Updated_At") = Updated_At
        Row("Published_At") = CompuMaster.Data.Utils.NullableTypeWithItsValueOrDBNull(Of DateTime)(Published_At)
        'Variant details

        table.Rows.Add(Row)
    End Sub

End Class
