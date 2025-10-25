namespace RemoteLogViewer.Models.Ssh.FileViewer.ByteOffset;

public readonly record struct ByteOffset(long LineNumber, ulong Bytes);
