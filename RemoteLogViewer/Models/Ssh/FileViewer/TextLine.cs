using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteLogViewer.Models.Ssh.FileViewer;

public record TextLine(long LineNumber, string Content);
