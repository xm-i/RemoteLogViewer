namespace RemoteLogViewer.Services.Ssh;

public record FileSystemObject(string Path, string FileName, FileSystemObjectType? FileSystemObjectType, long FileSize, DateTime? LastUpdated);

public enum FileSystemObjectType {
	File,
	Directory,
	Symlink
}
