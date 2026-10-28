Public MustInherit Class PrimaryEntryBase

    ''' <summary>Gets or sets the stable API identity used to reject duplicate pages.</summary>
    Public MustOverride Property ID As String
    Public MustOverride Sub AddToTableAsDataRows(table As System.Data.DataTable)

End Class
