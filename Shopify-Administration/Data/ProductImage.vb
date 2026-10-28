Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class ProductImage
    Public Property ID As String
    Public Property product_id As String
    Public Property Position As Integer

    Public Property Created_At As DateTime
    Public Property Updated_At As DateTime
    Public Property Alt As String
    Public Property Width As Integer
    Public Property Height As Integer
    Public Property Src As String
    Public Property variant_ids As List(Of String)
    Public Property admin_graphql_api_id As String

    <JsonExtensionData> Public ExtendedData As IDictionary(Of String, JToken)
End Class
