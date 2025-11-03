using Microsoft.UI.Xaml;

using RemoteLogViewer.ViewModels.Settings;
using RemoteLogViewer.ViewModels.Settings.Highlight;

namespace RemoteLogViewer.Views.Settings;

[AddTransient]
public sealed partial class SettingsWindow : Window {
	public SettingsWindow(SettingsWindowViewModel vm) {
		this.ViewModel = vm;
		this.InitializeComponent();
		this.AppWindow?.Resize(new Windows.Graphics.SizeInt32(1200, 800));
		this.ViewModel.SelectedSettingsPage.Subscribe(vm => {
			if (vm is null) {
				return;
			}
			Type view;
			switch (vm) {
				case HighlightSettingsPageViewModel _:
					view = typeof(HighlightSettingsPage);
					break;
				case WorkspaceSettingsPageViewModel _:
					view = typeof(WorkspaceSettingsPage);
					break;
				case TextViewerSettingsPageViewModel _:
					view = typeof(TextViewerSettingsPage);
					break;
				case AdvancedSettingsPageViewModel _:
					view = typeof(AdvancedSettingsPage);
					break;
				default:
					return;
			}

			this.ContentFrame.Navigate(view, vm);
		});
	}

	public SettingsWindowViewModel ViewModel {
		get;
	}
}
