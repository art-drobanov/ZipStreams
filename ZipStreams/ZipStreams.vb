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
            Throw New Exception("ZipStreams: Compression method is not supported")
        End If
        _compressionMethod = compressionMethod
    End Sub

    Public Sub New()
        Me.New(CompressionMethod.Deflated)
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
            If stream IsNot Nothing Then
                stream.Seek(0, SeekOrigin.Begin)
            End If
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
            Dim entryName As String = fileName.Substring(folderOffset)
            Dim fi As New FileInfo(fileName)
            If fi.Attributes = FileAttributes.Directory Then
                entryName = ZipEntry.CleanName(entryName + Path.DirectorySeparatorChar)
                Dim bytes = Text.Encoding.UTF8.GetBytes(entryName)
                If Not TryToAdd(entryName, Nothing) Then 'Директория не имеет данных кроме имени
                    Throw New Exception(String.Format("ZipStreams: Entry '{0}' already exists, can't load", entryName))
                End If
            ElseIf fi.Exists Then
                entryName = ZipEntry.CleanName(entryName)
                CopyTaskInfo.Create(_copyTask, fi.Length)
                Using source As FileStream = File.OpenRead(fileName)
                    Dim ms = New MemoryStream()
                    StreamCopy(source, ms, entryName)
                    If Not TryToAdd(entryName, ms) Then
                        Throw New Exception(String.Format("ZipStreams: Entry '{0}' already exists, can't load", entryName))
                    End If
                End Using
                _copyTask = Nothing
            Else
                Throw New Exception(String.Format("ZipStreams: Can't access and load '{0}'", entryName))
            End If
        End SyncLock
    End Sub

    Public Sub LoadFromFolder(folderName As String)
        Dim folderOffset As Integer = folderName.Length + (If(folderName.EndsWith(Path.DirectorySeparatorChar), 0, 1))
        LoadFromFolder(folderName, folderOffset)
    End Sub

    Private Sub LoadFromFolder(path As String, folderOffset As Integer)
        Dim files As String() = Directory.GetFiles(path)
        For Each filename As String In files
            LoadFromFile(filename, folderOffset) 'Обычный файл
        Next
        Dim folders As String() = Directory.GetDirectories(path)
        For Each folder As String In folders
            LoadFromFile(folder, folderOffset) 'Добавляем папку
            LoadFromFolder(folder, folderOffset) 'Рекурсия
        Next
    End Sub

    Public Sub LoadFromZip(zipFileName As String, zipPassword As String)
        Using fs = File.OpenRead(zipFileName)
            LoadFromZipStream(fs, zipPassword, zipFileName, False)
            fs.Close()
        End Using
    End Sub

    Public Sub LoadFromZipStream(stream As Stream, zipPassword As String, name As String,
                                 Optional seekBegin As Boolean = True)
        SyncLock _syncRoot
            zipPassword = FilterEmptyString(zipPassword)
            If seekBegin Then
                stream.Seek(0, SeekOrigin.Begin)
            End If
            CopyTaskInfo.Create(_copyTask, stream.Length)
            Dim compressedSizeLoaded As Long = 0
            Using zipFile As New ZipFile(stream, True) 'True - не закрывать поток!
                If zipPassword IsNot Nothing Then
                    zipFile.Password = zipPassword
                End If
                Dim zipEnum = zipFile.GetEnumerator()
                While zipEnum.MoveNext()
                    Dim zipEntry = DirectCast(zipEnum.Current, ZipEntry)
                    zipEntry.Size = If(zipEntry.Size < 0, 0, zipEntry.Size)
                    If zipEntry.IsFile Then
                        Dim buffer = New Byte(zipEntry.Size - 1) {}
                        StreamUtils.ReadFully(zipFile.GetInputStream(zipEntry), buffer)
                        _numberedStreams.Add(zipEntry.Name, New NumberedMemoryStream(New MemoryStream(buffer, 0, zipEntry.Size, True, True)))
                    Else
                        _numberedStreams.Add(zipEntry.Name, New NumberedMemoryStream(Nothing)) 'Директория не имеет данных кроме имени
                    End If
                    compressedSizeLoaded += zipEntry.CompressedSize 'Фиксируем факт прохода записи Zip
                    _copyTask.UpdateInfo(name, compressedSizeLoaded) : RaiseEvent ProgressUpdated(_copyTask.Progress)
                End While
                zipFile.Close()
                RaiseEvent ProgressUpdated(0)
            End Using
            _copyTask = Nothing
        End SyncLock
    End Sub

    Public Sub SaveToZipFile(zipFileName As String, level As Integer, comment As String, zipPassword As String, timestamp As DateTime,
                             Optional unicodeNames As Boolean = True)
        If File.Exists(zipFileName) Then
            File.SetAttributes(zipFileName, FileAttributes.Normal)
            File.Delete(zipFileName)
        End If
        Using fs = File.Open(zipFileName, FileMode.CreateNew)
            SaveToZipStream(fs, level, comment, zipPassword, timestamp, unicodeNames)
            With fs
                .Flush()
                .Close()
            End With
        End Using
    End Sub

    Public Sub SaveToZipStream(stream As Stream, level As Integer, comment As String, zipPassword As String, timestamp As DateTime,
                               Optional unicodeNames As Boolean = True)
        SyncLock _syncRoot
            level = If(level < _minCompressionLevel, _minCompressionLevel, level)
            level = If(level > _maxCompressionLevel, _maxCompressionLevel, level)
            comment = FilterEmptyString(comment)
            zipPassword = FilterEmptyString(zipPassword)
            Dim streamsTotalLength = _numberedStreams.Sum(Function(item) If(item.Value.MemoryStream IsNot Nothing, item.Value.MemoryStream.Length, 0))
            CopyTaskInfo.Create(_copyTask, streamsTotalLength)
            Using outputZipStream As New ZipOutputStream(stream)
                With outputZipStream
                    .IsStreamOwner = False
                    .UseZip64 = UseZip64.Off
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
                            .DateTime = timestamp,
                            .Size = If(streamKVP.Value.MemoryStream IsNot Nothing, streamKVP.Value.MemoryStream.Length, -1),
                            .IsUnicodeText = unicodeNames
                        }
                    If zipEntry.IsDirectory Then
                        With zipEntry
                            .CompressionMethod = CompressionMethod.Stored
                            .AESKeySize = 0
                            .CompressedSize = .Size
                        End With
                    End If
                    If zipEntry.IsFile AndAlso zipEntry.Size > 0 Then
                        With zipEntry
                            .CompressionMethod = _compressionMethod
                            .AESKeySize = If(zipPassword IsNot Nothing, _AESKeySize, 0)
                        End With
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
                RaiseEvent ProgressUpdated(0)
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

    Private Sub StreamCopy(source As Stream, target As Stream, name As String)
        If source.CanSeek Then
            Dim buffer = New Byte(_bufferSize - 1) {}
            source.Seek(0, SeekOrigin.Begin)
            StreamUtils.Copy(source, target, buffer, AddressOf ProgressUpdatedHandler, New TimeSpan(0, 0, 0, 0, _progressUpdateMs), Me, name)
            _copyTask.UpdateInfo(name, Math.Min(source.Length, target.Length))
        Else
            Throw New Exception(String.Format("ZipStreams: Can't seek in source stream '{0}'", name))
        End If
    End Sub

    Private Sub WipeMemoryStream(ms As MemoryStream)
        If ms IsNot Nothing Then
            Dim arr = ms.GetBuffer()
            Array.Clear(arr, 0, arr.Length)
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
            Throw New Exception("ZipStreams: Copy task is nothing")
        End If
    End Sub
End Class
