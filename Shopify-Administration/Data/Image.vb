Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class Image
    Public Property Attachment As List(Of Byte)
    Public Property Created_At As DateTime
    Public Property Alt As String
    Public Property Width As Integer
    Public Property Height As Integer
    Public Property Src As String

    <JsonExtensionData> Public ExtendedData As IDictionary(Of String, JToken)
End Class
