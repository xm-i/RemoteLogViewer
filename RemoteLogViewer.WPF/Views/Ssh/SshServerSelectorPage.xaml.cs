using Windows.Storage.Pickers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using RemoteLogViewer.Core.ViewModels.Ssh;
using WinRT.Interop;

namespace RemoteLogViewer.WinUI.Views.Ssh;

/// <summary>
/// SSH サーバー選択ページです。接続設定の選択および編集を行います。
/// </summary>
public sealed partial class SshServerSelectorPage : Page {
	private IDisposable? _subscription;

	private ILogger<SshServerSelectorPage> logger {
		get {
			return field ??= WinUI.App.LoggerFactory.CreateLogger<SshServerSelectorPage>();
		}
	}
	/// <summary>
	/// ビューモデルを取得します。
	/// </summary>
	public SshServerSelectorViewModel? ViewModel {
		get;
		private set;
	}

	/// <summary>
	/// インスタンスを初期化します。
	/// </summary>
	public SshServerSelectorPage() {
		this.InitializeComponent();
		this.PasswordBox.PasswordChanged += this.PasswordBox_PasswordChanged;
		this.PrivateKeyPassphraseBox.PasswordChanged += this.PrivateKeyPassphraseBox_PasswordChanged;
	}

	private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
		if (this.ViewModel?.SelectedSshConnectionInfo.Value is { } vm) {
			vm.Password.Value = this.PasswordBox.Password;
		}
	}

	private void PrivateKeyPassphraseBox_PasswordChanged(object sender, RoutedEventArgs e) {
		if (this.ViewModel?.SelectedSshConnectionInfo.Value is { } vm) {
			vm.PrivateKeyPassphrase.Value = this.PrivateKeyPassphraseBox.Password;
		}
	}

	/// <summary>
	/// ナビゲート時に ViewModel を受け取ります。
	/// </summary>
	protected override void OnNavigatedTo(NavigationEventArgs e) {
		if (e.Parameter is SshServerSelectorViewModel vm) {
			this.ViewModel = vm;
			this._subscription = this.ViewModel.SelectedSshConnectionInfo.ObservePropertyChanged(x => x.Value).Subscribe(this.OnSelectedConnectionChanged);
		} else {
			throw new InvalidOperationException("ViewModel is not passed.");
		}
		base.OnNavigatedTo(e);
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e) {
		this._subscription?.Dispose();
		this._subscription = null;
		base.OnNavigatedFrom(e);
	}

	private void OnSelectedConnectionChanged(SshConnectionInfoViewModel? vm) {
		if (vm == null) {
			this.PasswordBox.Password = string.Empty;
			this.PrivateKeyPassphraseBox.Password = string.Empty;
		} else {
			this.PasswordBox.Password = vm.Password.Value ?? string.Empty;
			this.PrivateKeyPassphraseBox.Password = vm.PrivateKeyPassphrase.Value ?? string.Empty;
		}
	}

	/// <summary>
	/// 保存済み接続一覧ダブルタップ時の処理です。
	/// </summary>
	private void SavedConnections_DoubleTapped(object sender, DoubleTappedRoutedEventArgs _) {
		if(this.ViewModel == null) {
			return;
		}
		if (sender is ListBox lb && lb.SelectedItem is SshConnectionInfoViewModel info) {
			this.ViewModel.SelectSshConnectionInfoCommand.Execute(info);
		}
	}

	/// <summary>
	/// 保存済み接続一覧の選択変更時の処理です (シングルクリック対応)。
	/// </summary>
	private void SavedConnections_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (sender is ListBox lb && lb.SelectedItem is SshConnectionInfoViewModel info) {
			this.ViewModel.SelectSshConnectionInfoCommand.Execute(info);
		}
	}

	/// <summary>
	/// 秘密鍵ファイル選択ボタン押下時の処理です。
	/// </summary>
	private async void BrowsePrivateKeyButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (this.ViewModel.SelectedSshConnectionInfo.Value is not { } vm) {
			return;
		}
		try {
			var picker = new FileOpenPicker {
				ViewMode = PickerViewMode.List,
				SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
			};
			var hwnd = WindowNative.GetWindowHandle(WinUI.App.MainWindow);
			InitializeWithWindow.Initialize(picker, hwnd);
			string[] exts = [".pem", ".ppk", ".key", ".pub", ".crt", ".der", ".rsa"];
			foreach (var ext in exts) {
				picker.FileTypeFilter.Add(ext);
			}
			var file = await picker.PickSingleFileAsync();
			if (file != null) {
				vm.PrivateKeyPath.Value = file.Path;
			}
		} catch (Exception ex) {
			this.logger.LogError(ex, "Exception");
		}
	}
}
