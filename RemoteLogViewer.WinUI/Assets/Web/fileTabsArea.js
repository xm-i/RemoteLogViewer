const FileTabArea = {
	components: { TextFileViewer, FileOperationArea },
	template: `
<div id="file-tab-container" class="tab-container" v-if="tabs.length > 0">
	<ul class="tab-headers" v-if="tabs.length > 1">
	  <li
		v-for="tab in tabs"
		:key="tab.pageKey"
		v-on:click="change(tab.pageKey)"
		v-bind:class="{'active': activeTab === tab.pageKey}">{{ tab.tabHeader }}</li>
	</ul>

	<div class="tab-contents">
		<div v-for="tab in tabs"
			:key="tab.pageKey"
			v-show="activeTab === tab.pageKey"
			class="text-file-viewer-wrapper"
			:class="tab.pageKey">
			<file-operation-area :pageKey="tab.pageKey" :isDisconnected="isDisconnected"></file-operation-area>
			<text-file-viewer :pageKey="tab.pageKey" :isDisconnected="isDisconnected"></text-file-viewer>
		</div>
	</div>
</div>
<div class="empty-state" v-else>
	<h2>Ready</h2>
	<p>Select a file from the left pane to open it.</p>
</div>
	`,
	data() {
		return {
			tabs: [],
			activeTab: null,
			isDisconnected: false
		};
	},
	methods: {
		addTab(pageKey, tabHeader) {
			this.tabs.push({
				pageKey,
				tabHeader
			});
			this.activeTab = pageKey;

			this.$nextTick(() => {
				Split([`.${pageKey} .main-area`, `.${pageKey} .tab-area`], {
					sizes: [70, 30],
					direction: 'vertical'
				});
			});
		},
		removeTab(pageKey) {
			this.tabs = this.tabs.filter(x => x.pageKey !== pageKey);
			this.activeTab = this.tabs[0]?.pageKey;
		},
		change(pageKey) {
			this.activeTab = pageKey;
		},
		changeTabHeader(pageKey,tabHeader) {
			const tab = this.tabs.find(x => x.pageKey === pageKey);
			if (!tab) {
				return;
			}
			tab.tabHeader = tabHeader;
		}
	},
	mounted() {
		// テスト用
		if (!window.chrome.webview) {
			this.addTab("p1", "Page 1");
			this.addTab("p2", "Page 2");
			this.activeTab = "p1";
			return;
		}

		// C# → JS 通信
		window.chrome.webview.addEventListener("message", e => {
			const message = e.data;
			switch (message.type) {
				case "FileOpened":
					this.addTab(message.data.PageKey, message.data.TabHeader);
					break;
				case "FileClosed":
					this.removeTab(message.data);
					break;
				case "OpenedFilePathChanged":
					this.changeTabHeader(message.pageKey, message.data);
					break;
				case "IsDisconnectedUpdated":
					this.isDisconnected = message.data;
					break;
			}
		});
	}
};