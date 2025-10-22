using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;

namespace RemoteLogViewer.Views.Settings;

public sealed partial class SettingsWindow : Window {
	public SettingsWindow() {
		this.InitializeComponent();

		// ウィンドウサイズを設定
		this.AppWindow?.Resize(new Windows.Graphics.SizeInt32(1000, 650));

		this.CategoryList.SelectedIndex = 0;
	}

	private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (this.CategoryList.SelectedItem is not ListViewItem item) {
			return;
		}
		if ((item.Content as string) == "Highlight") {
			this.ContentFrame.Navigate(typeof(HighlightSettingsPage));
		}
	}
}
