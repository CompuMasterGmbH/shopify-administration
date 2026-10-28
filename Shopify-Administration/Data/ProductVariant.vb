Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class ProductVariant
    Public Property ID As String
    Public Property Created_At As DateTime
    Public Property Updated_At As DateTime
    Public Property product_id As String
    Public Property Title As String
    Public Property Price As Decimal?
    Public Property SKU As String
    Public Property Position As Integer?
    Public Property inventory_policy As String
    Public Property compare_at_price As Decimal?
    Public Property fulfillment_service As String
    Public Property inventory_management As String
    Public Property option1 As String
    Public Property option2 As String
    Public Property option3 As String
    Public Property taxable As Boolean
    Public Property barcode As String
    Public Property grams As String
    Public Property image_id As String
    Public Property weight As Decimal?
    Public Property weight_unit As String
    Public Property inventory_item_id As Long
    Public Property inventory_quantity As Integer
    Public Property old_inventory_quantity As Integer
    Public Property requires_shipping As Boolean
    Public Property admin_graphql_api_id As String

    <JsonExtensionData> Public ExtendedData As IDictionary(Of String, JToken)
End Class
