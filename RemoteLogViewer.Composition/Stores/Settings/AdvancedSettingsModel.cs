using System;

using R3;
using R3.JsonConfig.Attributes;

namespace RemoteLogViewer.Composition.Stores.Settings;

/// <summary>Advanced設定。</summary>
[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class AdvancedSettingsModel(IServiceProvider service) {
	public IServiceProvider ScopedService { get; } = service;
	/// <summary>
	/// バイトオフセット作成間隔(行数)
	/// </summary>
	public ReactiveProperty<int> ByteOffsetMapChunkSize {
		get;
	} = new(10000);
}
