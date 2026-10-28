<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ApiBrowser
    Inherits System.Windows.Forms.Form

    'Das Formular überschreibt den Löschvorgang, um die Komponentenliste zu bereinigen.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Wird vom Windows Form-Designer benötigt.
    Private components As System.ComponentModel.IContainer

    'Hinweis: Die folgende Prozedur ist für den Windows Form-Designer erforderlich.
    'Das Bearbeiten ist mit dem Windows Form-Designer möglich.
    'Das Bearbeiten mit dem Code-Editor ist nicht möglich.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As ComponentModel.ComponentResourceManager = New ComponentModel.ComponentResourceManager(GetType(ApiBrowser))
        TextBoxUrl = New TextBox()
        TextBoxResponse = New TextBox()
        FlowLayoutPanelUrl = New FlowLayoutPanel()
        Label1 = New Label()
        FlowLayoutPanelUrl.SuspendLayout()
        SuspendLayout()
        '
        ' TextBoxUrl
        '
        TextBoxUrl.Dock = DockStyle.Fill
        TextBoxUrl.Location = New Point(83, 3)
        TextBoxUrl.Name = "TextBoxUrl"
        TextBoxUrl.Size = New Size(482, 23)
        TextBoxUrl.TabIndex = 0
        '
        ' TextBoxResponse
        '
        TextBoxResponse.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        TextBoxResponse.BorderStyle = BorderStyle.None
        TextBoxResponse.Location = New Point(-1, 30)
        TextBoxResponse.Multiline = True
        TextBoxResponse.Name = "TextBoxResponse"
        TextBoxResponse.ScrollBars = ScrollBars.Both
        TextBoxResponse.Size = New Size(879, 418)
        TextBoxResponse.TabIndex = 1
        '
        ' FlowLayoutPanelUrl
        '
        FlowLayoutPanelUrl.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        FlowLayoutPanelUrl.AutoScroll = True
        FlowLayoutPanelUrl.Controls.Add(Label1)
        FlowLayoutPanelUrl.Controls.Add(TextBoxUrl)
        FlowLayoutPanelUrl.Location = New Point(-1, -2)
        FlowLayoutPanelUrl.Name = "FlowLayoutPanelUrl"
        FlowLayoutPanelUrl.Size = New Size(879, 29)
        FlowLayoutPanelUrl.TabIndex = 3
        '
        ' Label1
        '
        Label1.AutoSize = True
        Label1.Location = New Point(3, 0)
        Label1.Name = "Label1"
        Label1.Size = New Size(74, 15)
        Label1.TabIndex = 3
        Label1.Text = "LabelBaseUri"
        '
        ' ApiBrowser
        '
        Me.AutoScaleDimensions = New SizeF(7F, 15F)
        Me.AutoScaleMode = AutoScaleMode.Font
        Me.ClientSize = New Size(879, 450)
        Me.Controls.Add(FlowLayoutPanelUrl)
        Me.Controls.Add(TextBoxResponse)
        Me.Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Me.Name = "ApiBrowser"
        Me.Text = "ApiBrowser"
        FlowLayoutPanelUrl.ResumeLayout(False)
        FlowLayoutPanelUrl.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents TextBoxUrl As TextBox
    Friend WithEvents TextBoxResponse As TextBox
    Friend WithEvents FlowLayoutPanelUrl As FlowLayoutPanel
    Friend WithEvents Label1 As Label
End Class
