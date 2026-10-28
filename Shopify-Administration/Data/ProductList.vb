Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class ProductList
    Inherits ResponseList(Of Product)

    Public Property Products As List(Of Product)
        Get
            Return Me.Items
        End Get
        Set(value As List(Of Product))
            Me.Items = value
        End Set
    End Property

End Class
