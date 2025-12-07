<template>
	<div id="file-tab-container" class="tab-container" v-if="tabs.length > 0">
		<ul class="tab-headers" v-if="tabs.length > 1">
			<li
				v-for="tab in tabs"
				:key="tab.pageKey"
				@click="change(tab.pageKey)"
				:class="{ 'active': activeTab === tab.pageKey }"
			>
				{{ tab.tabHeader }}
			</li>
		</ul>

		<div class="tab-contents">
			<div
				v-for="tab in tabs"
				:key="tab.pageKey"
				v-show="activeTab === tab.pageKey"
				class="text-file-viewer-wrapper"
				:class="tab.pageKey"
			>
				<FileOperationArea :pageKey="tab.pageKey" :isDisconnected="isDisconnected" />
				<TextFileViewer :pageKey="tab.pageKey" :isDisconnected="isDisconnected" />
			</div>
		</div>
	</div>
	<div class="empty-state" v-else>
		<h2>Ready</h2>
		<p>Select a file from the left pane to open it.</p>
	</div>
</template>

<script setup lang="ts">
import { ref, nextTick, onMounted } from 'vue';
import Split from 'split.js';
import TextFileViewer from './components/TextFileViewer.vue';
import FileOperationArea from './components/FileOperationArea.vue';

const tabs = ref<{
		pageKey: string,
		tabHeader: string,
		isActive: boolean
	}[]>([]);
const activeTab = ref<string | null>(null);
const isDisconnected = ref<boolean>(false);

const addTab = (pageKey: string, tabHeader: string) => {
	tabs.value.push({
		pageKey,
		tabHeader,
		isActive: false
	});
	activeTab.value = pageKey;

	nextTick(() => {
		Split([`.${pageKey} .main-area`, `.${pageKey} .tab-area`], {
			sizes: [70, 30],
			direction: 'vertical'
		});
	});
};

const removeTab = (pageKey: string) => {
	tabs.value = tabs.value.filter(x => x.pageKey !== pageKey);
	activeTab.value = tabs.value[0]?.pageKey || null;
};

const change = (pageKey: string) => {
	activeTab.value = pageKey;
};

const changeTabHeader = (pageKey: string, tabHeader: string) => {
	const tab = tabs.value.find(x => x.pageKey === pageKey);
	if (tab) {
		tab.tabHeader = tabHeader;
	}
};

onMounted(() => {
	// テスト用
	if (!window.chrome?.webview) {
		addTab('p1', 'Page 1');
		addTab('p2', 'Page 2');
		activeTab.value = 'p1';
		return;
	}

	// C# → JS 通信
	window.chrome.webview.addEventListener('message', e => {
		const message = e.data;
		switch (message.type) {
			case 'FileOpened':
				addTab(message.data.pageKey, message.data.tabHeader);
				break;
			case 'FileClosed':
				removeTab(message.data);
				break;
			case 'OpenedFilePathChanged':
				changeTabHeader(message.pageKey, message.data);
				break;
			case 'IsDisconnectedUpdated':
				isDisconnected.value = message.data;
        break;
      case 'LineStyleChanged': 
        const styleTag = document.getElementById('dynamic-style');
        if (styleTag) {
          styleTag.textContent = message.data;
        }
        break;
		}
	});
});
</script>

<style scoped>
  .text-file-viewer-wrapper {
    height: 100%;
    display: flex;
    flex-direction: column;
  }

	.empty-state {
		height: 100%;
		width: 100%;
		display: flex;
		flex-direction: column;
		justify-content: center;
		align-items: center;
		text-align: center;
		padding: 20px;
		color: #555;
		position: relative;
		overflow: hidden;

		h2 {
			font-size: 56px;
			color: #333;
			margin-bottom: 24px;
			font-weight: 600;
			position: relative;
			display: inline-block;
			animation: shimmer 2.5s infinite;
			background: linear-gradient( 90deg, #333 0%, #666 20%, #333 40%, #333 100% );
			-webkit-background-clip: text;
			-webkit-text-fill-color: transparent;
		}

		p {
			font-size: 26px;
			color: #666;
		}

		&::before,
		&::after {
			content: "";
			position: absolute;
			top: -50%;
			left: -50%;
			width: 200%;
			height: 200%;
			background: radial-gradient(circle, rgba(255,255,255,0.8) 0%, transparent 60%);
			animation: sparkleMove 10s linear infinite;
			opacity: 0.3;
			pointer-events: none;
		}

		&::after {
			animation-duration: 14s;
			opacity: 0.2;
		}
	}

	@keyframes sparkleMove {
		0% {
			transform: translate(0, 0) rotate(0deg);
		}

		50% {
			transform: translate(10%, 10%) rotate(180deg);
		}

		100% {
			transform: translate(0, 0) rotate(360deg);
		}
	}

	@keyframes shimmer {
		0% {
			background-position: -200px 0;
		}

		100% {
			background-position: 200px 0;
		}
	}
</style>
