using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>Advanced設定。</summary>
[AddSingleton]
public class AdvancedSettingsModel(IServiceProvider service) {
	public IServiceProvider ScopedService { get; } = service;
	/// <summary>
	/// バイトオフセット作成間隔(行数)
	/// </summary>
	public ReactiveProperty<int> ByteOffsetMapChunkSize {
		get;
	} = new(10000);
}

public class AdvancedSettingsModelForJson {
	public required int ByteOffsetMapChunkSize {
		get;
		set;
	}

	public static AdvancedSettingsModel CreateModel(AdvancedSettingsModelForJson json, IServiceProvider service) {
		var model = service.GetRequiredService<AdvancedSettingsModel>();
		model.ByteOffsetMapChunkSize.Value = json.ByteOffsetMapChunkSize;
		return model;
	}
	public static AdvancedSettingsModelForJson CreateJson(AdvancedSettingsModel model) {
		return new AdvancedSettingsModelForJson {
			ByteOffsetMapChunkSize = model.ByteOffsetMapChunkSize.Value
		};
	}
}
