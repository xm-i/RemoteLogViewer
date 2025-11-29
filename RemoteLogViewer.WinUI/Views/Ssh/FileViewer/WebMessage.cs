using System.Collections.Generic;
using System.Text.Json;

namespace RemoteLogViewer.WinUI.Views.Ssh.FileViewer;

public class WebMessage {
	public required string Type {
		get;
		set;
	}
	private static readonly Dictionary<string, Type> TypeMap = new()
	{
		{ "Request", typeof(RequestWebMessage) },
		{ "StartGrep", typeof(StartGrepWebMessage) },
		{ "CancelGrep", typeof(CancelGrepWebMessage) },
	};

	public static WebMessage Create(string json) {
		using var doc = JsonDocument.Parse(json);
		var type = doc.RootElement.GetProperty("Type").GetString();

		return type == null || !TypeMap.TryGetValue(type, out var targetType)
			? throw new InvalidOperationException($"Unknown WebMessage type: {type}")
			: (WebMessage)JsonSerializer.Deserialize(json, targetType)!;
	}
}

public class RequestWebMessage : WebMessage {
	public required long Start {
		get;
		set;
	}
	public required long End {
		get;
		set;
	}
}

public class StartGrepWebMessage : WebMessage {
	public required string Keyword {
		get;
		set;
	}
	public required long StartLine {
		get;
		set;
	}
}


public class CancelGrepWebMessage : WebMessage {
}

