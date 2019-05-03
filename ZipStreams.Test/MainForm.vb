Imports System.ComponentModel
Imports ICSharpCode.SharpZipLib
Imports ICSharpCode.SharpZipLib.Zip

Public Class MainForm
    Private WithEvents _zipStreams As New ZipStreams()

    Private WithEvents _loadFromZipBW As New BackgroundWorker()
    Private WithEvents _loadFromFileBW As New BackgroundWorker()
    Private WithEvents _loadFromFolderBW As New BackgroundWorker()
    Private WithEvents _saveZipBW As New BackgroundWorker()

    Private _fbd As FolderBrowserDialog
    Private _ofd As OpenFileDialog
    Private _sfd As SaveFileDialog

    Private Sub _loadFromZipButton_Click(sender As Object, e As EventArgs) Handles _loadFromZipButton.Click
        _ofd = New OpenFileDialog()
        With _ofd
            .RestoreDirectory = True
            .AddExtension = True
            .DefaultExt = ".zip"
            .Filter = "ZIP files (*.zip)|*.zip"
        End With
        If _ofd.ShowDialog() = DialogResult.OK Then
            _loadFromZipButton.Enabled = False : _loadFromZipBW.RunWorkerAsync()
        End If
    End Sub

    Private Sub _loadFromFileButton_Click(sender As Object, e As EventArgs) Handles _loadFromFileButton.Click
        _ofd = New OpenFileDialog()
        With _ofd
            .RestoreDirectory = True
            .AddExtension = True
            .DefaultExt = ".*"
            .Filter = "All files (*.*)|*.*"
        End With
        If _ofd.ShowDialog() = DialogResult.OK Then
            _loadFromFileButton.Enabled = False : _loadFromFileBW.RunWorkerAsync()
        End If
    End Sub

    Private Sub _loadFromFolderButton_Click(sender As Object, e As EventArgs) Handles _loadFromFolderButton.Click
        _fbd = New FolderBrowserDialog()
        If _fbd.ShowDialog() = DialogResult.OK Then
            _loadFromFolderButton.Enabled = False : _loadFromFolderBW.RunWorkerAsync()
        End If
    End Sub

    Private Sub _saveZipButton_Click(sender As Object, e As EventArgs) Handles _saveZipButton.Click
        _sfd = New SaveFileDialog()
        With _sfd
            .RestoreDirectory = True
            .AddExtension = True
            .DefaultExt = ".zip"
            .Filter = "ZIP files (*.zip)|*.zip"
        End With
        If _sfd.ShowDialog() = DialogResult.OK Then
            _saveZipButton.Enabled = False : _saveZipBW.RunWorkerAsync()
        End If
    End Sub

    Private Sub ProgressUpdated(progress As Single) Handles _zipStreams.ProgressUpdated
        Me.Invoke(Sub()
                      _progressBar.Value = CInt(progress * 100)
                  End Sub)
    End Sub

    Private Sub _loadFromZipBW_DoWork(sender As Object, e As DoWorkEventArgs) Handles _loadFromZipBW.DoWork
        Try
            Dim zipFileName = String.Empty
            Dim zipPassword = String.Empty
            Me.Invoke(Sub()
                          zipFileName = _ofd.FileName
                          zipPassword = _zipPasswordTextBox.Text
                      End Sub)
            Try
                _zipStreams.LoadFromZip(zipFileName, zipPassword, _appendCheckBox.Checked)
            Catch ex As Exception
                MessageBox.Show(ex.Message)
            End Try
            Me.Invoke(Sub()
                          _zipEntriesListBox.Items.Clear()
                          For Each zipEntryName In _zipStreams.Names
                              _zipEntriesListBox.Items.Add(zipEntryName)
                          Next
                      End Sub)
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub _loadFromZipBW_RunWorkerCompleted() Handles _loadFromZipBW.RunWorkerCompleted
        Me.Invoke(Sub()
                      _loadFromZipButton.Enabled = True
                  End Sub)
    End Sub

    Private Sub _loadFromFileBW_DoWork(sender As Object, e As DoWorkEventArgs) Handles _loadFromFileBW.DoWork
        Try
            Try
                _zipStreams.LoadFromFile(_ofd.FileName)
            Catch ex As Exception
                MessageBox.Show(ex.Message)
            End Try
            Me.Invoke(Sub()
                          _zipEntriesListBox.Items.Clear()
                          For Each zipEntryName In _zipStreams.Names
                              _zipEntriesListBox.Items.Add(zipEntryName)
                          Next
                      End Sub)
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub _loadFromFileBW_RunWorkerCompleted() Handles _loadFromFileBW.RunWorkerCompleted
        Me.Invoke(Sub()
                      _loadFromFileButton.Enabled = True
                  End Sub)
    End Sub

    Private Sub _loadFromFolderBW_DoWork(sender As Object, e As DoWorkEventArgs) Handles _loadFromFolderBW.DoWork
        Try
            Try
                _zipStreams.LoadFromFolder(_fbd.SelectedPath)
            Catch ex As Exception
                MessageBox.Show(ex.Message)
            End Try
            Me.Invoke(Sub()
                          _zipEntriesListBox.Items.Clear()
                          For Each zipEntryName In _zipStreams.Names
                              _zipEntriesListBox.Items.Add(zipEntryName)
                          Next
                      End Sub)
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub _loadFromFolderBW_RunWorkerCompleted() Handles _loadFromFolderBW.RunWorkerCompleted
        Me.Invoke(Sub()
                      _loadFromFolderButton.Enabled = True
                  End Sub)
    End Sub

    Private Sub _saveZipBW_DoWork(sender As Object, e As DoWorkEventArgs) Handles _saveZipBW.DoWork
        Try
            Dim zipFileName = String.Empty
            Dim compressionLevel = 0
            Dim comment = String.Empty
            Dim zipPassword = String.Empty
            Me.Invoke(Sub()
                          zipFileName = _sfd.FileName
                          compressionLevel = _compressionLevelTrackBar.Value
                          comment = _commentTextBox.Text
                          zipPassword = _zipPasswordTextBox.Text
                      End Sub)
            _zipStreams.Save(zipFileName, compressionLevel, comment, zipPassword)
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub _saveZipBW_RunWorkerCompleted() Handles _saveZipBW.RunWorkerCompleted
        Me.Invoke(Sub()
                      _saveZipButton.Enabled = True
                  End Sub)
    End Sub

    Private Sub _compressionLevelTrackBar_ValueChanged(sender As Object, e As EventArgs) Handles _compressionLevelTrackBar.ValueChanged
        _compressionLabel.Text = String.Format("Compression Level: {0}", _compressionLevelTrackBar.Value)
    End Sub

    Private Sub _removeSelectedStreamButton_Click(sender As Object, e As EventArgs) Handles _removeSelectedStreamButton.Click
        Dim zipEntriesListBoxSelectedIndex = _zipEntriesListBox.SelectedIndex
        If zipEntriesListBoxSelectedIndex >= 0 Then
            Dim selectedItem = CType(_zipEntriesListBox.Items(zipEntriesListBoxSelectedIndex), String)
            _zipStreams.WipeAndRemoveStream(selectedItem)
        End If
    End Sub

    Private Sub _clearAllButton_Click(sender As Object, e As EventArgs) Handles _clearAllButton.Click
        _zipStreams.WipeAndRemoveAllStreams()
        _zipEntriesListBox.Items.Clear()
    End Sub
End Class
