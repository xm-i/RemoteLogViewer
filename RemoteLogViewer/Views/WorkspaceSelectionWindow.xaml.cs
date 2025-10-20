using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;
using RemoteLogViewer.ViewModels;

namespace RemoteLogViewer.Views;

/// <summary>ワークスペース選択ウィンドウ。</summary>
[AddTransient]
public sealed partial class WorkspaceSelectionWindow : Window {
	/// <summary>選択イベント。(path, persist)</summary>
	public event Action<string, bool>? WorkspaceSelected;
	public WorkspaceSelectionWindowViewModel ViewModel { get; }

	public WorkspaceSelectionWindow(WorkspaceSelectionWindowViewModel vm) {
		this.InitializeComponent();
		this.ViewModel = vm;
		this.ViewModel.Confirmed += (path, persist) => {
			this.WorkspaceSelected?.Invoke(path, persist);
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
