using Microsoft.Extensions.Logging;

using RemoteLogViewer.Core.Stores.Settings;
using RemoteLogViewer.Core.Utils.Extensions;

namespace RemoteLogViewer.Core.ViewModels.Settings;

[Inject(InjectServiceLifetime.Transient)]
public class AdvancedSettingsPageViewModel : SettingsPageViewModel<AdvancedSettingsPageViewModel> {
	public BindableReactiveProperty<int> ByteOffsetMapChunkSize {
		get;
	}
	public AdvancedSettingsPageViewModel(SettingsStoreModel settingsStoreModel, ILogger<AdvancedSettingsPageViewModel> logger) : base("Advanced", logger) {
		this.ByteOffsetMapChunkSize = settingsStoreModel.SettingsModel.AdvancedSettings.ByteOffsetMapChunkSize.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
	}
}
