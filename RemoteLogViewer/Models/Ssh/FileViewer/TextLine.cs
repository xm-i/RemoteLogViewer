namespace RemoteLogViewer.Models.Ssh.FileViewer;

public record TextLine(long LineNumber, string? Content, bool IsLoaded = true);
