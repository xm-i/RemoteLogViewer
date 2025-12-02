namespace RemoteLogViewer.Core.Models.Ssh.FileViewer;

public record struct LogFileLoadRequest(int RequestId, long Start, long End);