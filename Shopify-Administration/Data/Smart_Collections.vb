Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class SmartCollectionsList
    Inherits ResponseList(Of Collection)

    Public Property Smart_Collections As List(Of Collection)
        Get
            Return Me.Items
        End Get
        Set(value As List(Of Collection))
            Me.Items = value
        End Set
    End Property

End Class
