using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using RemoteLogViewer.Core.ViewModels.Settings;

using Windows.Storage.Pickers;

using WinRT.Interop;

namespace RemoteLogViewer.WinUI.Views;

/// <summary>ワークスペース選択ウィンドウ。</summary>
[Inject(InjectServiceLifetime.Transient)]
public sealed partial class WorkspaceSelectionWindow : Window {
	/// <summary>選択イベント。(path, persist)</summary>
	public event Action? WorkspaceSelected;
	public WorkspaceSettingsPageViewModel ViewModel {
		get;
	}

	public WorkspaceSelectionWindow(WorkspaceSettingsPageViewModel vm) {
		this.InitializeComponent();
		this.ViewModel = vm;
		this.ViewModel.Confirmed += () => {
			this.WorkspaceSelected?.Invoke();
			this.Close();
		};
		this.ViewModel.ErrorMessage.ObservePropertyChanged(x => x.Value).Subscribe(msg => {
			if (!string.IsNullOrWhiteSpace(msg)) {
				var dialog = new ContentDialog {
					XamlRoot = this.Content.XamlRoot,
					Title = "エラー",
					PrimaryButtonText = "OK",
					Content = msg
				};
				_ = dialog.ShowAsync();
			}
		});
	}

	private async void Browse_Click(object sender, RoutedEventArgs e) {
		var picker = new FolderPicker();
		picker.FileTypeFilter.Add("*");
		InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
		var folder = await picker.PickSingleFolderAsync();
		if (folder != null) {
			this.ViewModel.SelectedPath.Value = folder.Path;
		}
	}

	private void Ok_Click(object sender, RoutedEventArgs e) {
		this.ViewModel.ConfirmCommand.Execute(Unit.Default);
	}
}
