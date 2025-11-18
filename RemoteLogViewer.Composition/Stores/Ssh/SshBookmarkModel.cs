using R3;
using R3.JsonConfig.Attributes;

using RemoteLogViewer.Composition.Utils.Attributes;

namespace RemoteLogViewer.Composition.Stores.Ssh;

/// <summary>
/// SSH 接続設定に紐づくブックマークを表します。
/// </summary>
[AddTransient]
[GenerateR3JsonConfigDto]
public class SshBookmarkModel {
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
