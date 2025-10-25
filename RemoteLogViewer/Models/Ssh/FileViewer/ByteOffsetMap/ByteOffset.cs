namespace RemoteLogViewer.Models.Ssh.FileViewer.ByteOffsetMap;

public readonly record struct ByteOffset(long LineNumber, ulong Bytes);
