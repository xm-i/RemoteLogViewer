namespace RemoteLogViewer.Core.Services.Ssh;

public record FileSystemObject(string Path, string FileName, FileSystemObjectType FileSystemObjectType, ulong FileSize, DateTime? LastUpdated);

public enum FileSystemObjectType {
	File,
	Directory,
	SymlinkFile,
	SymlinkDirectory
}
