Public Class ResponseList(Of T As PrimaryEntryBase)

    Public Property Items As List(Of T)

    Public Function ToDataTable() As DataTable
        Dim Result As New DataTable
        For MyCounter As Integer = 0 To Me.Items.Count - 1
            Me.Items(MyCounter).AddToTableAsDataRows(Result)
        Next
        Return Result
    End Function

End Class
