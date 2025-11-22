namespace RemoteLogViewer.Core.Models.Ssh.FileViewer;

public record TextLine(long LineNumber, string? Content, bool IsLoaded = true);
