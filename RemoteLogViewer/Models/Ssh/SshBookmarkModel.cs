namespace RemoteLogViewer.Models.Ssh;

/// <summary>
/// SSH 接続設定に紐づくブックマークを表します。
/// </summary>
public class SshBookmarkModel : ModelBase {
	public SshBookmarkModel() {
	}
	public SshBookmarkModel(int order, string path, string name) {
		this.Order.Value = order;
		this.Path.Value = path;
		this.Name.Value = name;
	}
	/// <summary>表示順。</summary>
	public ReactiveProperty<int> Order { get; } = new(0);
	/// <summary>対象パス。</summary>
	public ReactiveProperty<string> Path { get; } = new(string.Empty);
	/// <summary>表示名。</summary>
	public ReactiveProperty<string> Name { get; } = new(string.Empty);
}

/// <summary>
/// JSON シリアライズ用ブックマーク DTO です。
/// </summary>
public class SshBookmarkModelForJson {
	/// <summary>表示順。</summary>
	public required int Order {
		get; init;
	}
	/// <summary>対象パス。</summary>
	public required string Path {
		get; init;
	}
	/// <summary>表示名。</summary>
	public required string Name {
		get; init;
	}

	/// <summary>
	/// JSON からモデルを生成します。
	/// </summary>
	public static SshBookmarkModel CreateModel(SshBookmarkModelForJson json) {
		var bm = new SshBookmarkModel();
		bm.Order.Value = json.Order;
		bm.Path.Value = json.Path;
		bm.Name.Value = json.Name;
		return bm;
	}

	/// <summary>
	/// モデルから JSON DTO を生成します。
	/// </summary>
	public static SshBookmarkModelForJson CreateJson(SshBookmarkModel model) {
		return new SshBookmarkModelForJson { Order = model.Order.Value, Path = model.Path.Value, Name = model.Name.Value };
	}
}
