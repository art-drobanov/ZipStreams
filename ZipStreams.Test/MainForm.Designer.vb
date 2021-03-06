﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
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
        Me._loadFromZipButton = New System.Windows.Forms.Button()
        Me._zipPasswordTextBox = New System.Windows.Forms.TextBox()
        Me._zipPasswordLabel = New System.Windows.Forms.Label()
        Me._saveZipButton = New System.Windows.Forms.Button()
        Me._loadFromFolderButton = New System.Windows.Forms.Button()
        Me._loadFromFileButton = New System.Windows.Forms.Button()
        Me._logoPictureBox = New System.Windows.Forms.PictureBox()
        Me._progressBar = New System.Windows.Forms.ProgressBar()
        Me._commentLabel = New System.Windows.Forms.Label()
        Me._commentTextBox = New System.Windows.Forms.TextBox()
        Me._compressionLevelTrackBar = New System.Windows.Forms.TrackBar()
        Me._compressionLabel = New System.Windows.Forms.Label()
        Me._removeSelectedStreamButton = New System.Windows.Forms.Button()
        Me._clearAllButton = New System.Windows.Forms.Button()
        Me._zipEntriesListBox = New System.Windows.Forms.ListBox()
        Me._unicodeCheckBox = New System.Windows.Forms.CheckBox()
        CType(Me._logoPictureBox, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me._compressionLevelTrackBar, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        '_loadFromZipButton
        '
        Me._loadFromZipButton.Location = New System.Drawing.Point(12, 500)
        Me._loadFromZipButton.Name = "_loadFromZipButton"
        Me._loadFromZipButton.Size = New System.Drawing.Size(69, 58)
        Me._loadFromZipButton.TabIndex = 2
        Me._loadFromZipButton.Text = "Load From ZIP"
        Me._loadFromZipButton.UseVisualStyleBackColor = True
        '
        '_zipPasswordTextBox
        '
        Me._zipPasswordTextBox.Location = New System.Drawing.Point(231, 516)
        Me._zipPasswordTextBox.Name = "_zipPasswordTextBox"
        Me._zipPasswordTextBox.PasswordChar = Global.Microsoft.VisualBasic.ChrW(9632)
        Me._zipPasswordTextBox.Size = New System.Drawing.Size(139, 20)
        Me._zipPasswordTextBox.TabIndex = 6
        '
        '_zipPasswordLabel
        '
        Me._zipPasswordLabel.AutoSize = True
        Me._zipPasswordLabel.Location = New System.Drawing.Point(228, 500)
        Me._zipPasswordLabel.Name = "_zipPasswordLabel"
        Me._zipPasswordLabel.Size = New System.Drawing.Size(104, 13)
        Me._zipPasswordLabel.TabIndex = 0
        Me._zipPasswordLabel.Text = "Password (AES-256)"
        '
        '_saveZipButton
        '
        Me._saveZipButton.Location = New System.Drawing.Point(378, 500)
        Me._saveZipButton.Name = "_saveZipButton"
        Me._saveZipButton.Size = New System.Drawing.Size(69, 88)
        Me._saveZipButton.TabIndex = 8
        Me._saveZipButton.Text = "Save ZIP"
        Me._saveZipButton.UseVisualStyleBackColor = True
        '
        '_loadFromFolderButton
        '
        Me._loadFromFolderButton.Location = New System.Drawing.Point(12, 608)
        Me._loadFromFolderButton.Name = "_loadFromFolderButton"
        Me._loadFromFolderButton.Size = New System.Drawing.Size(69, 36)
        Me._loadFromFolderButton.TabIndex = 4
        Me._loadFromFolderButton.Text = "Load From Folder"
        Me._loadFromFolderButton.UseVisualStyleBackColor = True
        '
        '_loadFromFileButton
        '
        Me._loadFromFileButton.Location = New System.Drawing.Point(12, 566)
        Me._loadFromFileButton.Name = "_loadFromFileButton"
        Me._loadFromFileButton.Size = New System.Drawing.Size(69, 36)
        Me._loadFromFileButton.TabIndex = 3
        Me._loadFromFileButton.Text = "Load From File"
        Me._loadFromFileButton.UseVisualStyleBackColor = True
        '
        '_logoPictureBox
        '
        Me._logoPictureBox.BackgroundImage = Global.ZipStreams.Test.My.Resources.Resources.pkziplogo
        Me._logoPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me._logoPictureBox.Location = New System.Drawing.Point(90, 542)
        Me._logoPictureBox.Name = "_logoPictureBox"
        Me._logoPictureBox.Size = New System.Drawing.Size(110, 102)
        Me._logoPictureBox.TabIndex = 7
        Me._logoPictureBox.TabStop = False
        '
        '_progressBar
        '
        Me._progressBar.Location = New System.Drawing.Point(12, 650)
        Me._progressBar.Name = "_progressBar"
        Me._progressBar.Size = New System.Drawing.Size(435, 23)
        Me._progressBar.TabIndex = 0
        '
        '_commentLabel
        '
        Me._commentLabel.AutoSize = True
        Me._commentLabel.Location = New System.Drawing.Point(87, 500)
        Me._commentLabel.Name = "_commentLabel"
        Me._commentLabel.Size = New System.Drawing.Size(51, 13)
        Me._commentLabel.TabIndex = 0
        Me._commentLabel.Text = "Comment"
        '
        '_commentTextBox
        '
        Me._commentTextBox.Location = New System.Drawing.Point(90, 516)
        Me._commentTextBox.Name = "_commentTextBox"
        Me._commentTextBox.Size = New System.Drawing.Size(139, 20)
        Me._commentTextBox.TabIndex = 5
        '
        '_compressionLevelTrackBar
        '
        Me._compressionLevelTrackBar.Location = New System.Drawing.Point(231, 566)
        Me._compressionLevelTrackBar.Maximum = 9
        Me._compressionLevelTrackBar.Name = "_compressionLevelTrackBar"
        Me._compressionLevelTrackBar.Size = New System.Drawing.Size(141, 45)
        Me._compressionLevelTrackBar.TabIndex = 7
        Me._compressionLevelTrackBar.Value = 9
        '
        '_compressionLabel
        '
        Me._compressionLabel.AutoSize = True
        Me._compressionLabel.Location = New System.Drawing.Point(228, 545)
        Me._compressionLabel.Name = "_compressionLabel"
        Me._compressionLabel.Size = New System.Drawing.Size(108, 13)
        Me._compressionLabel.TabIndex = 0
        Me._compressionLabel.Text = "Compression Level: 9"
        '
        '_removeSelectedStreamButton
        '
        Me._removeSelectedStreamButton.Location = New System.Drawing.Point(231, 621)
        Me._removeSelectedStreamButton.Name = "_removeSelectedStreamButton"
        Me._removeSelectedStreamButton.Size = New System.Drawing.Size(139, 23)
        Me._removeSelectedStreamButton.TabIndex = 10
        Me._removeSelectedStreamButton.Text = "Remove Stream"
        Me._removeSelectedStreamButton.UseVisualStyleBackColor = True
        '
        '_clearAllButton
        '
        Me._clearAllButton.Location = New System.Drawing.Point(378, 621)
        Me._clearAllButton.Name = "_clearAllButton"
        Me._clearAllButton.Size = New System.Drawing.Size(69, 23)
        Me._clearAllButton.TabIndex = 11
        Me._clearAllButton.Text = "Clear All"
        Me._clearAllButton.UseVisualStyleBackColor = True
        '
        '_zipEntriesListBox
        '
        Me._zipEntriesListBox.FormattingEnabled = True
        Me._zipEntriesListBox.Location = New System.Drawing.Point(12, 12)
        Me._zipEntriesListBox.Name = "_zipEntriesListBox"
        Me._zipEntriesListBox.Size = New System.Drawing.Size(435, 472)
        Me._zipEntriesListBox.TabIndex = 1
        '
        '_unicodeCheckBox
        '
        Me._unicodeCheckBox.AutoSize = True
        Me._unicodeCheckBox.Checked = True
        Me._unicodeCheckBox.CheckState = System.Windows.Forms.CheckState.Checked
        Me._unicodeCheckBox.Location = New System.Drawing.Point(378, 594)
        Me._unicodeCheckBox.Name = "_unicodeCheckBox"
        Me._unicodeCheckBox.Size = New System.Drawing.Size(66, 17)
        Me._unicodeCheckBox.TabIndex = 9
        Me._unicodeCheckBox.Text = "Unicode"
        Me._unicodeCheckBox.UseVisualStyleBackColor = True
        '
        'MainForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(459, 684)
        Me.Controls.Add(Me._unicodeCheckBox)
        Me.Controls.Add(Me._zipEntriesListBox)
        Me.Controls.Add(Me._clearAllButton)
        Me.Controls.Add(Me._removeSelectedStreamButton)
        Me.Controls.Add(Me._compressionLabel)
        Me.Controls.Add(Me._compressionLevelTrackBar)
        Me.Controls.Add(Me._commentLabel)
        Me.Controls.Add(Me._commentTextBox)
        Me.Controls.Add(Me._progressBar)
        Me.Controls.Add(Me._logoPictureBox)
        Me.Controls.Add(Me._loadFromFileButton)
        Me.Controls.Add(Me._loadFromFolderButton)
        Me.Controls.Add(Me._saveZipButton)
        Me.Controls.Add(Me._zipPasswordLabel)
        Me.Controls.Add(Me._zipPasswordTextBox)
        Me.Controls.Add(Me._loadFromZipButton)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "MainForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "ZipStreams Test"
        CType(Me._logoPictureBox, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me._compressionLevelTrackBar, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents _loadFromZipButton As Button
    Friend WithEvents _zipPasswordTextBox As TextBox
    Friend WithEvents _zipPasswordLabel As Label
    Friend WithEvents _saveZipButton As Button
    Friend WithEvents _loadFromFolderButton As Button
    Friend WithEvents _loadFromFileButton As Button
    Friend WithEvents _logoPictureBox As PictureBox
    Friend WithEvents _progressBar As ProgressBar
    Friend WithEvents _commentLabel As Label
    Friend WithEvents _commentTextBox As TextBox
    Friend WithEvents _compressionLevelTrackBar As TrackBar
    Friend WithEvents _compressionLabel As Label
    Friend WithEvents _removeSelectedStreamButton As Button
    Friend WithEvents _clearAllButton As Button
    Friend WithEvents _zipEntriesListBox As ListBox
    Friend WithEvents _unicodeCheckBox As CheckBox
End Class
