const FileTabArea = {
	components: { TextFileViewer },
	template: `
<div id="file-tab-container" class="tab-container">
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
			<text-file-viewer></text-file-viewer>
		</div>
	</div>
</div>
	`,
	data() {
		return {
			tabs: [],
			activeTab: null
		};
	},
	methods: {
		addTab: function (pageKey, tabHeader) {
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
		removeTab: function (pageKey) {
			this.tabs = this.tabs.filter(x => x != pageKey);
			this.activeTab = this.tabs[0];
		},
		change(pageKey) {
			this.activeTab = pageKey;
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
			}
		});
	}
};