using Microsoft.Extensions.Logging;
using RemoteLogViewer.Stores.Settings;

namespace RemoteLogViewer.ViewModels.Settings;

[AddTransient]
public class AdvancedSettingsPageViewModel : SettingsPageViewModel<AdvancedSettingsPageViewModel> {
	public BindableReactiveProperty<int> ByteOffsetMapChunkSize { get; }
	public AdvancedSettingsPageViewModel(SettingsStoreModel settingsStoreModel, ILogger<AdvancedSettingsPageViewModel> logger) : base("Advanced", logger) {
		this.ByteOffsetMapChunkSize = settingsStoreModel.SettingsModel.AdvancedSettings.ByteOffsetMapChunkSize.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
	}
}
