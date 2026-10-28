<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        Dim resources As ComponentModel.ComponentResourceManager = New ComponentModel.ComponentResourceManager(GetType(MainForm))
        MenuStrip1 = New MenuStrip()
        DateiToolStripMenuItem = New ToolStripMenuItem()
        LoginToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator1 = New ToolStripSeparator()
        BeendenToolStripMenuItem = New ToolStripMenuItem()
        ProdukteToolStripMenuItem = New ToolStripMenuItem()
        KategorienToolStripMenuItem = New ToolStripMenuItem()
        SmartCollectionsToolStripMenuItem = New ToolStripMenuItem()
        LieferantenToolStripMenuItem = New ToolStripMenuItem()
        WareneingängeToolStripMenuItem = New ToolStripMenuItem()
        ExportZuXLSXToolStripMenuItem = New ToolStripMenuItem()
        APIToolStripMenuItem = New ToolStripMenuItem()
        APIBrowserToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator2 = New ToolStripSeparator()
        DokumentationToolStripMenuItem = New ToolStripMenuItem()
        RESTAPIToolStripMenuItem = New ToolStripMenuItem()
        APIAccessScopesToolStripMenuItem = New ToolStripMenuItem()
        StatusStrip1 = New StatusStrip()
        ToolStripStatusLabelRowCountInfo = New ToolStripStatusLabel()
        Timer1 = New Timer(components)
        DataGridView = New DataGridView()
        MenuStrip1.SuspendLayout()
        StatusStrip1.SuspendLayout()
        CType(DataGridView, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        '
        ' MenuStrip1
        '
        MenuStrip1.Items.AddRange(New ToolStripItem() {DateiToolStripMenuItem, ProdukteToolStripMenuItem, KategorienToolStripMenuItem, SmartCollectionsToolStripMenuItem, LieferantenToolStripMenuItem, WareneingängeToolStripMenuItem, ExportZuXLSXToolStripMenuItem, APIToolStripMenuItem})
        MenuStrip1.Location = New Point(0, 0)
        MenuStrip1.Name = "MenuStrip1"
        MenuStrip1.Size = New Size(800, 24)
        MenuStrip1.TabIndex = 0
        MenuStrip1.Text = "MenuStrip1"
        '
        ' DateiToolStripMenuItem
        '
        DateiToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {LoginToolStripMenuItem, ToolStripSeparator1, BeendenToolStripMenuItem})
        DateiToolStripMenuItem.Name = "DateiToolStripMenuItem"
        DateiToolStripMenuItem.Size = New Size(46, 20)
        DateiToolStripMenuItem.Text = "&Datei"
        '
        ' LoginToolStripMenuItem
        '
        LoginToolStripMenuItem.Name = "LoginToolStripMenuItem"
        LoginToolStripMenuItem.Size = New Size(120, 22)
        LoginToolStripMenuItem.Text = "&Login"
        '
        ' ToolStripSeparator1
        '
        ToolStripSeparator1.Name = "ToolStripSeparator1"
        ToolStripSeparator1.Size = New Size(117, 6)
        '
        ' BeendenToolStripMenuItem
        '
        BeendenToolStripMenuItem.Name = "BeendenToolStripMenuItem"
        BeendenToolStripMenuItem.Size = New Size(120, 22)
        BeendenToolStripMenuItem.Text = "&Beenden"
        '
        ' ProdukteToolStripMenuItem
        '
        ProdukteToolStripMenuItem.Name = "ProdukteToolStripMenuItem"
        ProdukteToolStripMenuItem.Size = New Size(67, 20)
        ProdukteToolStripMenuItem.Text = "Produkte"
        '
        ' KategorienToolStripMenuItem
        '
        KategorienToolStripMenuItem.Name = "KategorienToolStripMenuItem"
        KategorienToolStripMenuItem.Size = New Size(76, 20)
        KategorienToolStripMenuItem.Text = "Kategorien"
        '
        ' SmartCollectionsToolStripMenuItem
        '
        SmartCollectionsToolStripMenuItem.Name = "SmartCollectionsToolStripMenuItem"
        SmartCollectionsToolStripMenuItem.Size = New Size(112, 20)
        SmartCollectionsToolStripMenuItem.Text = "Smart Collections"
        '
        ' LieferantenToolStripMenuItem
        '
        LieferantenToolStripMenuItem.Name = "LieferantenToolStripMenuItem"
        LieferantenToolStripMenuItem.Size = New Size(78, 20)
        LieferantenToolStripMenuItem.Text = "Lieferanten"
        '
        ' WareneingängeToolStripMenuItem
        '
        WareneingängeToolStripMenuItem.Name = "WareneingängeToolStripMenuItem"
        WareneingängeToolStripMenuItem.Size = New Size(102, 20)
        WareneingängeToolStripMenuItem.Text = "Wareneingänge"
        '
        ' ExportZuXLSXToolStripMenuItem
        '
        ExportZuXLSXToolStripMenuItem.Name = "ExportZuXLSXToolStripMenuItem"
        ExportZuXLSXToolStripMenuItem.Size = New Size(97, 20)
        ExportZuXLSXToolStripMenuItem.Text = "&Export zu XLSX"
        '
        ' APIToolStripMenuItem
        '
        APIToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {APIBrowserToolStripMenuItem, ToolStripSeparator2, DokumentationToolStripMenuItem, RESTAPIToolStripMenuItem, APIAccessScopesToolStripMenuItem})
        APIToolStripMenuItem.Name = "APIToolStripMenuItem"
        APIToolStripMenuItem.Size = New Size(37, 20)
        APIToolStripMenuItem.Text = "API"
        '
        ' APIBrowserToolStripMenuItem
        '
        APIBrowserToolStripMenuItem.Name = "APIBrowserToolStripMenuItem"
        APIBrowserToolStripMenuItem.Size = New Size(171, 22)
        APIBrowserToolStripMenuItem.Text = "API Browser"
        '
        ' ToolStripSeparator2
        '
        ToolStripSeparator2.Name = "ToolStripSeparator2"
        ToolStripSeparator2.Size = New Size(168, 6)
        '
        ' DokumentationToolStripMenuItem
        '
        DokumentationToolStripMenuItem.Enabled = False
        DokumentationToolStripMenuItem.Name = "DokumentationToolStripMenuItem"
        DokumentationToolStripMenuItem.Size = New Size(171, 22)
        DokumentationToolStripMenuItem.Text = "Dokumentation"
        '
        ' RESTAPIToolStripMenuItem
        '
        RESTAPIToolStripMenuItem.Name = "RESTAPIToolStripMenuItem"
        RESTAPIToolStripMenuItem.Size = New Size(171, 22)
        RESTAPIToolStripMenuItem.Text = "REST API"
        '
        ' APIAccessScopesToolStripMenuItem
        '
        APIAccessScopesToolStripMenuItem.Name = "APIAccessScopesToolStripMenuItem"
        APIAccessScopesToolStripMenuItem.Size = New Size(171, 22)
        APIAccessScopesToolStripMenuItem.Text = "API Access Scopes"
        '
        ' StatusStrip1
        '
        StatusStrip1.Items.AddRange(New ToolStripItem() {ToolStripStatusLabelRowCountInfo})
        StatusStrip1.Location = New Point(0, 428)
        StatusStrip1.Name = "StatusStrip1"
        StatusStrip1.Size = New Size(800, 22)
        StatusStrip1.TabIndex = 2
        StatusStrip1.Text = "StatusStrip1"
        '
        ' ToolStripStatusLabelRowCountInfo
        '
        ToolStripStatusLabelRowCountInfo.Name = "ToolStripStatusLabelRowCountInfo"
        ToolStripStatusLabelRowCountInfo.Size = New Size(119, 17)
        ToolStripStatusLabelRowCountInfo.Text = "ToolStripStatusLabel1"
        '
        ' Timer1
        '
        Timer1.Enabled = True
        Timer1.Interval = 300
        '
        ' DataGridView
        '
        DataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        DataGridView.Dock = DockStyle.Fill
        DataGridView.Location = New Point(0, 24)
        DataGridView.Name = "DataGridView"
        DataGridView.RowTemplate.Height = 25
        DataGridView.Size = New Size(800, 404)
        DataGridView.TabIndex = 3
        '
        ' MainForm
        '
        Me.AutoScaleDimensions = New SizeF(7F, 15F)
        Me.AutoScaleMode = AutoScaleMode.Font
        Me.ClientSize = New Size(800, 450)
        Me.Controls.Add(DataGridView)
        Me.Controls.Add(StatusStrip1)
        Me.Controls.Add(MenuStrip1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Me.MainMenuStrip = MenuStrip1
        Me.Name = "MainForm"
        Me.Text = "TMM Shopify Administration"
        MenuStrip1.ResumeLayout(False)
        MenuStrip1.PerformLayout()
        StatusStrip1.ResumeLayout(False)
        StatusStrip1.PerformLayout()
        CType(DataGridView, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents DateiToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents BeendenToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ProdukteToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents KategorienToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LieferantenToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents WareneingängeToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LoginToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
    Friend WithEvents StatusStrip1 As StatusStrip
    Friend WithEvents ToolStripStatusLabelRowCountInfo As ToolStripStatusLabel
    Friend WithEvents Timer1 As Timer
    Friend WithEvents DataGridView As DataGridView
    Friend WithEvents SmartCollectionsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ExportZuXLSXToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents APIToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents APIAccessScopesToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents RESTAPIToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents APIBrowserToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator2 As ToolStripSeparator
    Friend WithEvents DokumentationToolStripMenuItem As ToolStripMenuItem
End Class
