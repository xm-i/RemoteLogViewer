using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteLogViewer.Services.Ssh; 
public record FileSystemObject(string FileName, FileSystemObjectType? FileSystemObjectType, long FileSize, DateTime? LastUpdated);

public enum FileSystemObjectType {
	File,
	Directory,
	Symlink
}
