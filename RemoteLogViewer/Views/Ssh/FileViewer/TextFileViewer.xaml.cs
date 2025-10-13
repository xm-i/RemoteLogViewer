using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using RemoteLogViewer.Models.Ssh.FileViewer;
using RemoteLogViewer.ViewModels.Ssh.FileViewer;

namespace RemoteLogViewer.Views.Ssh.FileViewer; 
/// <summary>
///     テキストファイルビューア。スクロール位置に応じて行を部分的に読み込みます。
/// </summary>
public sealed partial class TextFileViewer {
	public TextFileViewerViewModel? ViewModel { get; set; }

	public TextFileViewer() {
		this.InitializeComponent();
	}

	private void ContentViewer_SizeChanged(object sender, SizeChangedEventArgs e) {
		if(this.ViewModel == null) {
			return;
		}
		const double lineHeight = 16;
		var visibleLines = Math.Max(1, (int)(e.NewSize.Height / lineHeight));
		this.ViewModel.VisibleLineCount.Value = visibleLines;
	}
}
