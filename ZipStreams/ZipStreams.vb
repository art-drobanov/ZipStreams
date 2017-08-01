Imports System.IO
Imports ICSharpCode.SharpZipLib.Core
Imports ICSharpCode.SharpZipLib.Zip

Public Class ZipStreams
    Private Class CopyTask
        Private Class CopyCounter
            Public Property Value As Long
        End Class

        Private _allStreamsLengthTotal As Long
        Private _streamsWrittenInfo As Dictionary(Of String, CopyCounter)
        Private _syncRoot As New Object

        Public Property ContinueRunning As Boolean

        Public ReadOnly Property Progress As Single
            Get
                SyncLock _syncRoot
                    Return CSng(_streamsWrittenInfo.Values.Sum(Function(item) item.Value) / CDbl(_allStreamsLengthTotal))
                End SyncLock
            End Get
        End Property

        Public Sub New(allStreamsLengthTotal As Long)
            _allStreamsLengthTotal = allStreamsLengthTotal
            _streamsWrittenInfo = New Dictionary(Of String, CopyCounter)
            ContinueRunning = True
        End Sub

        Public Sub UpdateInfo(name As String, writtenTotal As Long)
            SyncLock _syncRoot
                If Not _streamsWrittenInfo.ContainsKey(name) Then
                    _streamsWrittenInfo.Add(name, New CopyCounter)
                End If
                _streamsWrittenInfo(name).Value = writtenTotal

                If Progress > 100 Then
                    Throw New Exception("ZipStreams: Progress > 100")
                End If
            End SyncLock
        End Sub
    End Class

    Private Const _bufferSize = 4096
    Private Const _AESKeySize = 0 'ZipCrypto
    Private Const _minCompressionLevel = 0
    Private Const _maxCompressionLevel = 9
    Private Const _progressUpdateMs = 100

    Private _compressionMethod As CompressionMethod
    Private _streams As New Dictionary(Of String, MemoryStream)
    Private _copyTask As CopyTask
    Private _syncRoot As New Object

    Public Delegate Sub ProgressUpdatedDelegate(progress As Single)
    Public Event ProgressUpdated As ProgressUpdatedDelegate

    Public ReadOnly Property Streams As Dictionary(Of String, MemoryStream)
        Get
            SyncLock _syncRoot
                Return _streams
            End SyncLock
        End Get
    End Property

    Public ReadOnly Property ZipEntries As HashSet(Of String)
        Get
            SyncLock _syncRoot
                Return New HashSet(Of String)(_streams.Keys)
            End SyncLock
        End Get
    End Property

    Public ReadOnly Property ZipEntriesList As List(Of String)
        Get
            SyncLock _syncRoot
                Return _streams.Keys.ToList()
            End SyncLock
        End Get
    End Property

    Public Sub New(compressionMethod As CompressionMethod)
        If compressionMethod <> CompressionMethod.Stored AndAlso compressionMethod <> CompressionMethod.Deflated Then
            Throw New Exception("ZipStreams: Compression method is not supported!")
        End If
        _compressionMethod = compressionMethod
    End Sub

    Public Sub New()
        Me.New(CompressionMethod.Deflated)
    End Sub

    Public Sub Clear()
        SyncLock _syncRoot
            _streams.Clear()
        End SyncLock
    End Sub

    Public Function TryToAdd(entryName As String, stream As MemoryStream, name As String) As Boolean
        SyncLock _syncRoot
            entryName = ZipEntry.CleanName(entryName)
            If _streams.ContainsKey(entryName) Then
                Return False
            End If
            stream.Seek(0, SeekOrigin.Begin)
            _streams.Add(entryName, stream)
            Return True
        End SyncLock
    End Function

    Public Sub LoadFromFile(fileName As String)
        Dim folderName = Path.GetDirectoryName(fileName)
        Dim folderOffset As Integer = folderName.Length + (If(folderName.EndsWith("\"), 0, 1))
        LoadFromFile(fileName, folderOffset)
    End Sub

    Private Sub LoadFromFile(fileName As String, folderOffset As Integer)
        SyncLock _syncRoot
            Dim fi As New FileInfo(fileName)
            Dim entryName As String = fileName.Substring(folderOffset)
            entryName = ZipEntry.CleanName(entryName)
            _copyTask = New CopyTask(fi.Length)
            Using streamReader As FileStream = File.OpenRead(fileName)
                Dim ms = New MemoryStream()
                StreamCopy(streamReader, ms, entryName)
                If Not TryToAdd(entryName, ms, entryName) Then
                    Throw New Exception(String.Format("ZipStreams: Entry {0} already exists in archive!", entryName))
                End If
            End Using
            _copyTask = Nothing
        End SyncLock
    End Sub

    Public Sub LoadFromFolder(folderName As String)
        Dim folderOffset As Integer = folderName.Length + (If(folderName.EndsWith("\"), 0, 1))
        LoadFromFolder(folderName, folderOffset)
    End Sub

    Private Sub LoadFromFolder(path As String, folderOffset As Integer)
        Dim files As String() = Directory.GetFiles(path)
        For Each filename As String In files
            LoadFromFile(filename, folderOffset)
        Next
        Dim folders As String() = Directory.GetDirectories(path)
        For Each folder As String In folders
            LoadFromFolder(folder, folderOffset)
        Next
    End Sub

    Public Sub LoadFromZip(zipFileName As String, zipPassword As String)
        Using fs = File.OpenRead(zipFileName)
            LoadFromStream(fs, zipPassword, zipFileName, False)
            fs.Close()
        End Using
    End Sub

    Public Sub LoadFromStream(stream As Stream, zipPassword As String,
                              name As String, Optional seekBegin As Boolean = True)
        SyncLock _syncRoot
            zipPassword = CheckString(zipPassword)
            If seekBegin Then
                stream.Seek(0, SeekOrigin.Begin)
            End If
            _copyTask = New CopyTask(stream.Length)
            Dim zipStreamTotal As Long = 0
            Using inputZipStream As New ZipInputStream(stream)
                If zipPassword IsNot Nothing Then
                    inputZipStream.Password = zipPassword
                End If
                _streams.Clear()
                Dim zipEntry = GetNextZipEntry(inputZipStream)
                Do While zipEntry IsNot Nothing
                    If zipEntry.IsFile Then
                        zipEntry.Size = If(zipEntry.Size < 0, 0, zipEntry.Size)
                        Dim ms As MemoryStream = Nothing
                        Dim buffer = New Byte(zipEntry.Size - 1) {}
                        inputZipStream.Read(buffer, 0, zipEntry.Size)
                        ms = New MemoryStream(buffer)
                        _streams.Add(zipEntry.Name, ms)
                    Else
                        Dim bytes = Text.Encoding.UTF8.GetBytes(zipEntry.Name)
                        Dim ms = New MemoryStream(bytes)
                        _streams.Add(zipEntry.Name, ms)
                    End If
                    zipStreamTotal += zipEntry.CompressedSize
                    _copyTask.UpdateInfo(name, zipStreamTotal) : RaiseEvent ProgressUpdated(_copyTask.Progress)
                    zipEntry = GetNextZipEntry(inputZipStream)
                Loop
                RaiseEvent ProgressUpdated(1)
            End Using
            _copyTask = Nothing
        End SyncLock
    End Sub

    Public Sub Save(zipFileName As String, level As Integer, comment As String, zipPassword As String)
        If File.Exists(zipFileName) Then
            File.SetAttributes(zipFileName, FileAttributes.Normal)
            File.Delete(zipFileName)
        End If
        Using fs = File.Open(zipFileName, FileMode.CreateNew)
            Save(fs, level, comment, zipPassword)
            With fs
                .Flush()
                .Close()
            End With
        End Using
    End Sub

    Public Sub Save(stream As Stream, level As Integer, comment As String, zipPassword As String)
        SyncLock _syncRoot
            level = If(level < _minCompressionLevel, _minCompressionLevel, level)
            level = If(level > _maxCompressionLevel, _maxCompressionLevel, level)
            comment = CheckString(comment)
            zipPassword = CheckString(zipPassword)
            Dim streamsTotalLength = _streams.Sum(Function(item) If(item.Value IsNot Nothing, item.Value.Length, 0))
            _copyTask = New CopyTask(streamsTotalLength)
            Using outputZipStream As New ZipOutputStream(stream) With {.UseZip64 = False}
                Dim now As Date = DateTime.Now
                With outputZipStream
                    .SetLevel(level)
                    If comment IsNot Nothing Then
                        .SetComment(comment)
                    End If
                    If zipPassword IsNot Nothing Then
                        .Password = zipPassword
                    End If
                End With
                For Each streamKVP In _streams
                    Dim zipEntry = New ZipEntry(streamKVP.Key) With {.DateTime = now, .Size = streamKVP.Value.Length}
                    If zipEntry.IsDirectory Then
                        With zipEntry
                            .CompressionMethod = CompressionMethod.Stored
                            .AESKeySize = 0
                            .CompressedSize = .Size
                            .IsUnicodeText = True
                        End With
                    End If
                    If zipEntry.IsFile Then
                        If zipEntry.Size <= 0 Then
                            With zipEntry
                                .CompressionMethod = CompressionMethod.Stored
                                .CompressedSize = .Size
                            End With
                        Else
                            With zipEntry
                                .AESKeySize = _AESKeySize
                                .CompressionMethod = _compressionMethod
                            End With
                        End If
                    End If
                    outputZipStream.PutNextEntry(zipEntry)
                    If zipEntry.Size > 0 Then
                        StreamCopy(streamKVP.Value, outputZipStream, zipEntry.Name)
                    End If
                    outputZipStream.CloseEntry()
                Next
                With outputZipStream
                    .IsStreamOwner = False
                    .Close()
                End With
                RaiseEvent ProgressUpdated(1)
            End Using
            _copyTask = Nothing
        End SyncLock
    End Sub

    Private Function CheckString(str As String) As String
        If String.IsNullOrEmpty(str) Then
            Return Nothing
        Else
            Return str
        End If
    End Function

    Private Function GetNextZipEntry(inputZipStream As ZipInputStream) As ZipEntry
        Dim zipEntry = inputZipStream.GetNextEntry()
        If zipEntry IsNot Nothing Then
            If Not zipEntry.CanDecompress Then
                Throw New Exception(String.Format("ZipStreams: Can't decompress {0}!", zipEntry.Name))
            End If
            If zipEntry.IsCrypted And inputZipStream.Password = String.Empty Then
                Throw New Exception(String.Format("ZipStreams: Can't decrypt {0}, there is no password set!", zipEntry.Name))
            End If
            Return zipEntry
        Else
            Return Nothing
        End If
    End Function

    Private Sub StreamCopy(source As Stream, target As Stream, name As String, Optional seekBegin As Boolean = True)
        Dim buffer = New Byte(_bufferSize - 1) {}
        If source.CanSeek Then
            If seekBegin Then
                source.Seek(0, SeekOrigin.Begin)
            End If
            StreamUtils.Copy(source, target, buffer, AddressOf ProgressUpdatedHandler, New TimeSpan(0, 0, 0, 0, _progressUpdateMs), Me, name)
            _copyTask.UpdateInfo(name, Math.Min(source.Length, target.Length))
        Else
            Throw New Exception("ZipStreams: Can't do seek in source stream (StreamCopy)!")
        End If
    End Sub

    Private Sub ProgressUpdatedHandler(sender As Object, e As ProgressEventArgs)
        If _copyTask IsNot Nothing Then
            If Not _copyTask.ContinueRunning Then
                e.ContinueRunning = False
            Else
                _copyTask.UpdateInfo(e.Name, e.Processed)
            End If
            RaiseEvent ProgressUpdated(_copyTask.Progress)
        Else
            Throw New Exception("ZipStreams: Copy task is nothing!")
        End If
    End Sub
End Class
