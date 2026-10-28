Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class ProductOption
    <JsonExtensionData> Public ExtendedData As IDictionary(Of String, JToken)
End Class
