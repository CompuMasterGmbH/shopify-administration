Imports ShopifyAdministration.Core

''' <summary>Configured adapter retained for future ShopifySharp features.</summary>
Public Class ShopifySharp
    Public ReadOnly Property BaseUrl As String
    Public ReadOnly Property InstanceNameInUrl As String
    Public ReadOnly Property ProductService As Global.ShopifySharp.ProductService
    Public Sub New(configuration As ConnectionSettings)
        Dim settings = configuration.Normalize()
        BaseUrl = settings.ApiBaseUrl
        InstanceNameInUrl = New Uri(BaseUrl).Host
        ProductService = New Global.ShopifySharp.ProductService(InstanceNameInUrl, settings.Password)
    End Sub
End Class
