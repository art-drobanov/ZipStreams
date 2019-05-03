Imports System.IO
Imports System.Threading
Imports ICSharpCode.SharpZipLib.Core
Imports ICSharpCode.SharpZipLib.Zip

Public Class ZipStreams
    Private Class CopyTaskInfo
        Public Shared Sub Create(ByRef target As CopyTaskInfo, totalSize As Long)
            If target IsNot Nothing Then
                Throw New Exception("ZipStreams: Can't set copy task (already started)")
            End If
            target = New CopyTaskInfo(totalSize)
        End Sub

        Private _totalSize As Long
        Private _copyTasks As Dictionary(Of String, Long)
        Private _syncRoot As New Object

        Public Property ContinueRunning As Boolean

        Public ReadOnly Property Progress As Single
            Get
                SyncLock _syncRoot
                    Return CSng(_copyTasks.Values.Sum(Function(item) item) / CDbl(_totalSize))
                End SyncLock
            End Get
        End Property

        Private Sub New(totalSize As Long)
            _totalSize = totalSize
            _copyTasks = New Dictionary(Of String, Long)
            ContinueRunning = True
        End Sub

        Public Sub UpdateInfo(name As String, writtenTotal As Long)
            SyncLock _syncRoot
                If Not _copyTasks.ContainsKey(name) Then
                    _copyTasks.Add(name, 0)
                End If
                _copyTasks(name) = writtenTotal
                If Progress > 100 Then
                    Throw New Exception("ZipStreams: Progress > 100")
                End If
            End SyncLock
        End Sub
    End Class

    Public Class NumberedMemoryStream
        Private Shared _sharedNumber As Long
        Friend Shared Sub StreamNumberReset()
            Interlocked.Exchange(_sharedNumber, 0)
        End Sub

        Public ReadOnly Property Number As Long
        Public ReadOnly Property MemoryStream As MemoryStream

        Public Sub New(memoryStream As MemoryStream)
            _Number = Interlocked.Increment(_sharedNumber)
            _MemoryStream = memoryStream
        End Sub
    End Class

    Private Const _bufferSize = 4096
    Private Const _AESKeySize = 256 'AES-256
    Private Const _minCompressionLevel = 0
    Private Const _maxCompressionLevel = 9
    Private Const _progressUpdateMs = 100

    Private _compressionMethod As CompressionMethod
    Private _numberedStreams As New Dictionary(Of String, NumberedMemoryStream)
    Private _copyTask As CopyTaskInfo
    Private _syncRoot As New Object

    Public Delegate Sub ProgressUpdatedDelegate(progress As Single)
    Public Event ProgressUpdated As ProgressUpdatedDelegate

    Public ReadOnly Property Names As List(Of String)
        Get
            SyncLock _syncRoot
                Return _numberedStreams.Keys.ToList()
            End SyncLock
        End Get
    End Property

    Public ReadOnly Property Stream(name As String)
        Get
            SyncLock _syncRoot
                If _numberedStreams.ContainsKey(name) Then
                    Return _numberedStreams(name)
                Else
                    Return Nothing
                End If
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

    Public Sub StreamNumberReset()
        SyncLock _syncRoot
            NumberedMemoryStream.StreamNumberReset()
        End SyncLock
    End Sub

    Public Sub WipeAndRemoveAllStreams()
        SyncLock _syncRoot
            For Each name In _numberedStreams.Keys.ToArray()
                WipeAndRemoveStream(name)
            Next
            _numberedStreams.Clear()
        End SyncLock
    End Sub

    Public Function WipeAndRemoveStream(name As String) As Boolean
        SyncLock _syncRoot
            Dim res = False
            If _numberedStreams.ContainsKey(name) Then
                Dim nms = _numberedStreams(name)
                If nms.MemoryStream IsNot Nothing Then
                    WipeMemoryStream(nms.MemoryStream)
                End If
                _numberedStreams.Remove(name)
                res = True
            End If
            Return res
        End SyncLock
    End Function

    Public Function TryToAdd(entryName As String, stream As MemoryStream) As Boolean
        SyncLock _syncRoot
            entryName = ZipEntry.CleanName(entryName)
            If _numberedStreams.ContainsKey(entryName) Then
                Return False
            End If
            stream.Seek(0, SeekOrigin.Begin)
            _numberedStreams.Add(entryName, New NumberedMemoryStream(stream))
            Return True
        End SyncLock
    End Function

    Public Sub LoadFromFile(fileName As String)
        Dim folderName = Path.GetDirectoryName(fileName)
        Dim folderOffset As Integer = folderName.Length + (If(folderName.EndsWith(Path.DirectorySeparatorChar), 0, 1))
        LoadFromFile(fileName, folderOffset)
    End Sub

    Private Sub LoadFromFile(fileName As String, folderOffset As Integer)
        SyncLock _syncRoot
            Dim fi As New FileInfo(fileName)
            Dim entryName As String = fileName.Substring(folderOffset)
            entryName = ZipEntry.CleanName(entryName)
            CopyTaskInfo.Create(_copyTask, fi.Length)
            Using source As FileStream = File.OpenRead(fileName)
                Dim ms = New MemoryStream()
                StreamCopy(source, ms, entryName)
                If Not TryToAdd(entryName, ms) Then
                    Throw New Exception(String.Format("ZipStreams.LoadFromFile(): Entry {0} already exists in archive!", entryName))
                End If
            End Using
            _copyTask = Nothing
        End SyncLock
    End Sub

    Public Sub LoadFromFolder(folderName As String)
        Dim folderOffset As Integer = folderName.Length + (If(folderName.EndsWith(Path.DirectorySeparatorChar), 0, 1))
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

    Public Sub LoadFromZip(zipFileName As String, zipPassword As String, append As Boolean)
        Using fs = File.OpenRead(zipFileName)
            LoadFromZipStream(fs, zipPassword, zipFileName, append, False)
            fs.Close()
        End Using
    End Sub

    Public Sub LoadFromZipStream(stream As Stream, zipPassword As String, name As String, append As Boolean,
                                 Optional seekBegin As Boolean = True)
        SyncLock _syncRoot
            If seekBegin Then
                stream.Seek(0, SeekOrigin.Begin)
            End If
            CopyTaskInfo.Create(_copyTask, stream.Length)
            Dim compressedSizeLoaded As Long = 0
            Using inputZipStream As New ZipInputStream(stream) With {.IsStreamOwner = False}
                zipPassword = FilterEmptyString(zipPassword)
                If zipPassword IsNot Nothing Then
                    inputZipStream.Password = zipPassword
                End If
                If Not append Then
                    WipeAndRemoveAllStreams()
                End If
                Dim zipEntry = GetNextZipEntry(inputZipStream)
                Do While zipEntry IsNot Nothing
                    If zipEntry.IsFile Then
                        Dim buffer = ReadFromStream(inputZipStream, zipEntry.Size)
                        _numberedStreams.Add(zipEntry.Name, New NumberedMemoryStream(New MemoryStream(buffer)))
                    End If
                    If zipEntry.IsDirectory Then
                        _numberedStreams.Add(zipEntry.Name, New NumberedMemoryStream(Nothing))
                    End If
                    compressedSizeLoaded += zipEntry.CompressedSize
                    _copyTask.UpdateInfo(name, compressedSizeLoaded) : RaiseEvent ProgressUpdated(_copyTask.Progress)
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

    Public Sub Save(stream As Stream, level As Integer, comment As String, zipPassword As String,
                    Optional unicodeNames As Boolean = False)
        SyncLock _syncRoot
            level = If(level < _minCompressionLevel, _minCompressionLevel, level)
            level = If(level > _maxCompressionLevel, _maxCompressionLevel, level)
            comment = FilterEmptyString(comment)
            zipPassword = FilterEmptyString(zipPassword)
            Dim streamsTotalLength = _numberedStreams.Sum(Function(item) If(item.Value.MemoryStream IsNot Nothing, item.Value.MemoryStream.Length, 0))
            CopyTaskInfo.Create(_copyTask, streamsTotalLength)
            Using outputZipStream As New ZipOutputStream(stream) With {.IsStreamOwner = False}
                Dim nowTime As Date = DateTime.Now
                With outputZipStream
                    .UseZip64 = False
                    .SetLevel(level)
                    If comment IsNot Nothing Then
                        .SetComment(comment)
                    End If
                    If zipPassword IsNot Nothing Then
                        .Password = zipPassword
                    End If
                End With
                For Each streamKVP In _numberedStreams.OrderBy(Function(item) item.Value.Number)
                    Dim zipEntry = New ZipEntry(streamKVP.Key) With
                        {
                            .DateTime = nowTime,
                            .Size = If(streamKVP.Value.MemoryStream IsNot Nothing, streamKVP.Value.MemoryStream.Length, 0),
                            .IsUnicodeText = unicodeNames
                        }
                    If zipEntry.IsDirectory Then
                        With zipEntry
                            .CompressionMethod = CompressionMethod.Stored
                            .AESKeySize = 0
                            .CompressedSize = .Size
                        End With
                    End If
                    If zipEntry.IsFile Then
                        If zipEntry.Size > 0 Then
                            With zipEntry
                                .AESKeySize = If(zipPassword IsNot Nothing, _AESKeySize, 0)
                                .CompressionMethod = _compressionMethod
                            End With
                        End If
                    End If
                    outputZipStream.PutNextEntry(zipEntry)
                    If zipEntry.Size > 0 Then
                        StreamCopy(streamKVP.Value.MemoryStream, outputZipStream, zipEntry.Name)
                    End If
                    outputZipStream.CloseEntry()
                Next
                With outputZipStream
                    .Flush()
                    .Close()
                End With
                RaiseEvent ProgressUpdated(1)
            End Using
            _copyTask = Nothing
        End SyncLock
    End Sub

    Private Function FilterEmptyString(str As String) As String
        If String.IsNullOrEmpty(str) Then
            Return Nothing
        Else
            Return str
        End If
    End Function

    Private Function ReadFromStream(stream As Stream, dataCount As Integer) As Byte()
        If dataCount > 0 Then
            Dim buffer = New Byte(dataCount - 1) {}
            Dim done As Integer = 0 : Dim task As Integer = dataCount
            While task > 0
                done += stream.Read(buffer, done, task) : task = dataCount - done
            End While
            Return buffer
        ElseIf dataCount < 0 Then
            Dim buffer = New Queue(Of Byte)()
            While True
                Try
                    buffer.Enqueue(stream.ReadByte())
                Catch
                    Exit While
                End Try
            End While
            Return buffer.ToArray()
        Else
            Return {}
        End If
    End Function

    Private Function GetNextZipEntry(inputZipStream As ZipInputStream) As ZipEntry
        Dim zipEntry As ZipEntry = Nothing
        Try
            zipEntry = inputZipStream.GetNextEntry()
        Catch
        End Try
        If zipEntry IsNot Nothing Then
            If Not zipEntry.CanDecompress Then
                Throw New Exception(String.Format("ZipStreams.GetNextZipEntry(): Can't decompress {0}!", zipEntry.Name))
            End If
            If zipEntry.IsCrypted And inputZipStream.Password = String.Empty Then
                Throw New Exception(String.Format("ZipStreams.GetNextZipEntry(): Can't decrypt {0}, there is no password set!", zipEntry.Name))
            End If
        End If
        Return zipEntry
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
            Throw New Exception("ZipStreams.StreamCopy(): Can't do seek in source stream!")
        End If
    End Sub

    Private Sub WipeMemoryStream(ms As MemoryStream)
        Try
            ms.Seek(0, SeekOrigin.Begin)
            Dim buffer4k = New Byte(4095) {}
            Dim N = ms.Length \ buffer4k.Length
            For i = 1 To N
                ms.Write(buffer4k, 0, buffer4k.Length)
            Next
            ms.Write(buffer4k, 0, ms.Length - N * buffer4k.Length)
        Catch ex As Exception
            Throw New Exception("ZipStreams.WipeMemoryStream(): Can't wipe memory stream!")
        End Try
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
            Throw New Exception("ZipStreams.ProgressUpdatedHandler(): Copy task is nothing!")
        End If
    End Sub
End Class
