using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Diagnostics;

using RemoteLogViewer.ViewModels.Ssh;

namespace RemoteLogViewer.Views;

public sealed partial class SshSessionControl : UserControl {
	public SshSessionViewModel ViewModel {
		get;
	}

	public SshSessionControl() {
		this.InitializeComponent();
		this.ViewModel = Ioc.Default.GetRequiredService<SshSessionViewModel>();
		this.DataContext = this.ViewModel;
	}

	private void SavedConnections_DoubleTapped(object sender, DoubleTappedRoutedEventArgs _) {
		if (sender is ListBox lb && lb.SelectedItem is SshConnectionInfoViewModel info) {
			this.ViewModel.SelectSshConnectionInfoCommand.Execute(info);
		}
	}

	private async void BrowsePrivateKeyButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel.SelectedSshConnectionInfo.Value is not { } vm) {
			return;
		}
		try {
			var picker = new FileOpenPicker {
				ViewMode = PickerViewMode.List,
				SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
			};
			var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
			InitializeWithWindow.Initialize(picker, hwnd);

			string[] exts = [".pem", ".ppk", ".key", ".pub", ".crt", ".der", ".rsa"];
			foreach	(var ext in exts){
				picker.FileTypeFilter.Add(ext);
			}

			var file = await picker.PickSingleFileAsync();
			if (file != null) {
				vm.PrivateKeyPath.Value = file.Path;
			}
		} catch (Exception ex) {
			Debug.WriteLine($"[BrowsePrivateKey] Error: {ex}");
		}
	}
}
