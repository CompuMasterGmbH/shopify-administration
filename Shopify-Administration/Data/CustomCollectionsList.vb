''' <summary>Maps all manual collections rather than publication-channel listings.</summary>
Public Class CustomCollectionsList
    Inherits CollectionList
    Public Property Custom_Collections As List(Of Collection)
        Get
            Return Items
        End Get
        Set(value As List(Of Collection))
            Items = value
        End Set
    End Property
End Class
