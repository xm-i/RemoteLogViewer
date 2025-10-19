namespace RemoteLogViewer.Models.Ssh.FileViewer;

public readonly record struct ByteOffset(long LineNumber, ulong bytes);
