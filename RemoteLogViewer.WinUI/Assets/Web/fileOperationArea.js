const FileOperationArea = {
	template: `
		<div class="file-operation-area">

			<div class="filepath">
				<div class="title">Filepath</div>
				{{ openedFilepath }}
			</div>

			<div class="file-stats">
				<div class="title">File Status</div>
				<div class="content">
					{{ totalLines }} Lines<br/>
					({{ formattedTotalBytes }})
				</div>
				<div class="progress-bar-container">
					<div
						class="progress-bar"
						:style="{ width: fileLoadProgress + '%' }"
					></div>
				</div>
			</div>

			<div class="encoding">
				<div class="title">Encoding</div>
				<div class="content">
					<select name="encoding" v-model="selectedEncoding">
						<option v-for="encoding in availableEncodings" 
								:key="encoding"
								:value="encoding">
							{{ encoding }}
						</option>
					</select><br/>
					<button @click="applyEncodingClicked" :disabled="clientOperationg || isDisconnected">Apply</button>
				</div>
			</div>

			<div class="check-update">
				<div class="title">Check Update</div>
				<div class="content">
					<button @click="checkUpdateClicked" :disabled="isFileLoadUpdating || clientOperationg || isDisconnected">
						CHECK<br/>UPDATE
					</button>
				</div>
			</div>

			<div class="range-save">
				<div class="title">Download</div>
				<div class="content">
					<input name="startLine" type="number" v-model.number="startLine" min="1" placeholder="1" :disabled="isDisconnected"/>
					-
					<input name="endLine" type="number" v-model.number="endLine" min="1" placeholder="1000" :disabled="isDisconnected"/>
					<br/>
					<button @click="saveRangeClicked" :disabled="isSaving || clientOperationg || isDisconnected">
						Download
					</button>
				</div>
				<div class="progress-bar-container">
					<div
						class="progress-bar"
						:style="{ width: saveProgress + '%' }"
					></div>
				</div>
			</div>
			<div class="file-close">
				<div class="title">File Close</div>
				<div class="content">
					<button @click="fileCloseClicked">
						FILE<br/>CLOSE
					</button>
				</div>
			</div>
		</div>
	`,
	props: {
		pageKey: null,
		isDisconnected: false
	},
	data() {
		return {
			// file info
			openedFilepath: null,
			totalLines: 0,
			totalBytes: 0,
			fileLoadProgress: 0,
			isFileLoadUpdating: false,
			// save range
			startLine: null,
			endLine: null,
			isSaving: false,
			saveProgress: 0,
			// encoding
			selectedEncoding: null,
			availableEncodings: null,
			// check update
			isCheckUpdating: false,
			// 
			clientOperationg: false,
			currentRequestId: 0,
			runningRequests: []
		};
	},
	computed: {
		formattedTotalBytes() {
			return this.formatBytes(this.totalBytes);
		},
	},
	watch: {
		clientOperationg: {
			handler() {
				if (!this.clientOperationg) {
					return;
				}
				setTimeout(() => {
					this.clientOperationg = false;
				}, 100);
			}
		}
	},
	methods: {
		formatBytes(bytes) {
			if (bytes >= 1_000_000_000) {
				return (bytes / 1_000_000_000).toFixed(1) + "GB";
			}
			if (bytes >= 1_000_000) {
				return (bytes / 1_000_000).toFixed(1) + "MB";
			}
			if (bytes >= 1_000) {
				return (bytes / 1_000).toFixed(1) + "KB";
			}
			return bytes + "B";
		},

		checkUpdateClicked() {
			this.clientOperationg = true;
			const request = {
				PageKey: this.pageKey,
				Type: "UpdateTotalLine",
				RequestId: ++this.currentRequestId
			}
			
			window.chrome.webview.postMessage(request);
		},

		saveRangeClicked() {
			this.clientOperationg = true;
			if (!this.startLine || !this.endLine || this.endLine < this.startLine) {
				alert("Please enter valid start and end lines.");
				return;
			}

			const request = {
				PageKey: this.pageKey,
				Type: "SaveRangeRequest",
				RequestId: ++this.currentRequestId,
				Start: this.startLine,
				End: this.endLine
			}
			
			window.chrome.webview.postMessage(request);
		},

		applyEncodingClicked() {
			this.clientOperationg = true;
			const request = {
				PageKey: this.pageKey,
				Type: "ChangeEncoding",
				RequestId: ++this.currentRequestId,
				Encoding: this.selectedEncoding
			}

			window.chrome.webview.postMessage(request);
		},
		fileCloseClicked() {
			const request = {
				PageKey: this.pageKey,
				Type: "FileClose",
				RequestId: ++this.currentRequestId
			}

			window.chrome.webview.postMessage(request);
		}
	},

	mounted() {
		// テスト用
		if (!window.chrome.webview) {
			return;
		}

		// C# → JS 通信
		window.chrome.webview.addEventListener("message", e => {
			const message = e.data;
			if (message.pageKey !== this.pageKey) {
				return;
			}
			switch (message.type) {
				case "IsFileLoadRunningUpdated":
					this.isFileLoadUpdating = message.data;
					break;
				case "FileLoadProgressUpdated":
					this.fileLoadProgress = message.data * 100;
					break;
				case "TotalLinesUpdated":
					this.totalLines = message.data;
					break;
				case "TotalBytesUpdated":
					this.totalBytes = message.data;
					break;
				case "OpenedFilePathChanged":
					this.openedFilepath = message.data;
					break;
				case "SelectedEncodingChanged":
					this.selectedEncoding = message.data;
					break;
				case "IsRangeContentSavingUpdated":
					this.isSaving = message.data;
					break;
				case "SaveRangeProgressUpdated":
					this.saveProgress = message.data * 100;
					break;
				case "AvailableEncodingsUpdated":
					this.availableEncodings = message.data;
					break;
			}
		});
	}
};
