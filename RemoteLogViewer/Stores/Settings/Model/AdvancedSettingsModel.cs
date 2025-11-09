namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>Advanced設定。</summary>
[AddSingleton]
[GenerateSettingsJsonDto]
public class AdvancedSettingsModel(IServiceProvider service) {
	public IServiceProvider ScopedService { get; } = service;
	/// <summary>
	/// バイトオフセット作成間隔(行数)
	/// </summary>
	public ReactiveProperty<int> ByteOffsetMapChunkSize {
		get;
	} = new(10000);
}
