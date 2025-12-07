using System.Collections.Generic;
using System.Text.Json;

namespace RemoteLogViewer.WinUI.Views.Ssh.FileViewer;

public class WebMessage {
	public required string pageKey {
		get;
		set;
	}
	public required int requestId {
		get;
		set;
	}
	public required string type {
		get;
		set;
	}
	private static readonly Dictionary<string, Type> TypeMap = new()
	{
		{ "Request", typeof(RequestWebMessage) },
		{ "StartGrep", typeof(StartGrepWebMessage) },
		{ "CancelGrep", typeof(CancelGrepWebMessage) },
		{ "Ready", typeof(ReadyWebMessage) },
		{ "SaveRangeRequest", typeof(SaveRangeRequestWebMessage) },
		{ "ChangeEncoding", typeof(ChangeEncodingWebMessage) },
		{ "UpdateTotalLine", typeof(UpdateTotalLineWebMessage) },
		{ "FileClose", typeof(FileCloseWebMessage) },
	};

	public static WebMessage Create(string json) {
		using var doc = JsonDocument.Parse(json);
		var type = doc.RootElement.GetProperty("type").GetString();

		return type == null || !TypeMap.TryGetValue(type, out var targetType)
			? throw new InvalidOperationException($"Unknown WebMessage type: {type}")
			: (WebMessage)JsonSerializer.Deserialize(json, targetType)!;
	}
}

public class RequestWebMessage : WebMessage {
	public required long start {
		get;
		set;
	}
	public required long end {
		get;
		set;
	}
}

public class StartGrepWebMessage : WebMessage {
	public required string keyword {
		get;
		set;
	}
	public required long startLine {
		get;
		set;
	}
}


public class CancelGrepWebMessage : WebMessage {
}

public class ReadyWebMessage : WebMessage {
}

public class SaveRangeRequestWebMessage : WebMessage {
	public required long start {
		get;
		set;
	}
	public required long end {
		get;
		set;
	}
}
public class ChangeEncodingWebMessage : WebMessage {
	public required string? encoding {
		get;
		set;
	}
}

public class UpdateTotalLineWebMessage : WebMessage {
}
public class FileCloseWebMessage : WebMessage {
}