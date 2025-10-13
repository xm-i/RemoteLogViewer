using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using RemoteLogViewer.ViewModels.Ssh.FileViewer;

using Windows.Foundation;
using Windows.Foundation.Collections;

namespace RemoteLogViewer.Views.Ssh.FileViewer; 
public sealed partial class TextFileViewer {
	/// <summary>
	/// 閲覧用 ViewModel を取得します。
	/// </summary>
	public TextFileViewerViewModel? ViewModel {
		get;
		set;
	}

	public TextFileViewer() {
		this.InitializeComponent();
	}
}
