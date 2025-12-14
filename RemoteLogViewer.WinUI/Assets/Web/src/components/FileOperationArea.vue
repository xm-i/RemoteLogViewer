<template>
	<div class="file-operation-area">
		<div class="filepath">
			<div class="title">Filepath</div>
			{{ openedFilepath }}
		</div>

		<div class="file-stats">
			<div class="title">File Status</div>
			<div class="content">
				{{ totalLines }} Lines<br />
				({{ formattedTotalBytes }})
			</div>
			<div class="progress-bar-container">
				<div class="progress-bar"
						 :style="{ width: fileLoadProgress + '%' }">
				</div>
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
				</select><br />
				<button @click="applyEncodingClicked" :disabled="clientOperating || isDisconnected">Apply</button>
			</div>
		</div>

		<div class="check-update">
			<div class="title">Check Update</div>
			<div class="content">
				<button @click="checkUpdateClicked" :disabled="isFileLoadUpdating || clientOperating || isDisconnected">
					CHECK<br />UPDATE
				</button>
			</div>
		</div>

		<div class="range-save">
			<div class="title">Download</div>
			<div class="content">
				<input name="startLine" type="number" v-model.number="startLine" min="1" placeholder="1" :disabled="isDisconnected" />
				-
				<input name="endLine" type="number" v-model.number="endLine" min="1" placeholder="1000" :disabled="isDisconnected" />
				<br />
				<button @click="saveRangeClicked" :disabled="isSaving || clientOperating || isDisconnected">
					Download
				</button>
			</div>
			<div class="progress-bar-container">
				<div class="progress-bar"
						 :style="{ width: saveProgress + '%' }"></div>
			</div>
		</div>
		<div class="wrap-lines">
			<div class="title">Wrap Lines</div>
			<div class="content">
				<input type="checkbox" nmae="wrapLines" v-model="wrapLines" @change="$emit('update:wrapLines', $event.target.checked)" />
			</div>
		</div>
		<div class="file-close">
			<div class="title">File Close</div>
			<div class="content">
				<button @click="fileCloseClicked">
					FILE<br />CLOSE
				</button>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
	import { ref, computed, watch, onMounted } from 'vue';

	interface Props {
		pageKey: string
		isDisconnected?: boolean
	}

	const props = withDefaults(defineProps<Props>(), {
		isDisconnected: false
	});

	// File info
	const openedFilepath = ref<string | null>(null);
	const totalLines = ref(0);
	const totalBytes = ref(0);
	const fileLoadProgress = ref(0);
	const isFileLoadUpdating = ref(false);

	// Save range
	const startLine = ref<number | null>(null);
	const endLine = ref<number | null>(null);
	const isSaving = ref(false);
	const saveProgress = ref(0);

	// Encoding
	const selectedEncoding = ref<string | null>(null);
	const availableEncodings = ref<string[] | null>(null);

	// Wrap Lines
	const wrapLines = ref<boolean>(false);

	// Client operation
	const clientOperating = ref(false);
	const currentRequestId = ref(0);

	const formattedTotalBytes = computed(() => {
		return formatBytes(totalBytes.value);
	});

	const formatBytes = (bytes: number): string => {
		if (bytes >= (1024 ** 3)) {
			return (bytes / (1024 ** 3)).toFixed(1) + 'GB';
		}
		if (bytes >= (1024 ** 2)) {
			return (bytes / (1024 ** 2)).toFixed(1) + 'MB';
		}
		if (bytes >= 1024) {
			return (bytes / 1024).toFixed(1) + 'KB';
		}
		return bytes + 'B';
	};

	const checkUpdateClicked = () => {
		clientOperating.value = true;
		const request = {
			pageKey: props.pageKey,
			requestId: ++currentRequestId.value,
			type: 'UpdateTotalLine'
		} as const;

		if (window.chrome?.webview) {
			window.chrome.webview.postMessage(request);
		}
	};

	const saveRangeClicked = () => {
		clientOperating.value = true;
		if (!startLine.value || !endLine.value || endLine.value < startLine.value) {
			alert('Please enter valid start and end lines.');
			return;
		}

		const request = {
			pageKey: props.pageKey,
			type: 'SaveRangeRequest',
			requestId: ++currentRequestId.value,
			start: startLine.value,
			end: endLine.value
		} as const;

		if (window.chrome?.webview) {
			window.chrome.webview.postMessage(request);
		}
	};

	const applyEncodingClicked = () => {
		clientOperating.value = true;
		const request = {
			pageKey: props.pageKey,
			type: 'ChangeEncoding',
			requestId: ++currentRequestId.value,
			encoding: selectedEncoding.value
		} as const;

		if (window.chrome?.webview) {
			window.chrome.webview.postMessage(request);
		}
	};

	const fileCloseClicked = () => {
		const request = {
			pageKey: props.pageKey,
			type: 'FileClose',
			requestId: ++currentRequestId.value
		} as const;

		if (window.chrome?.webview) {
			window.chrome.webview.postMessage(request);
		}
	};

	watch(clientOperating, () => {
		if (!clientOperating.value) {
			return;
		}
		setTimeout(() => {
			clientOperating.value = false;
		}, 100);
	});

	onMounted(() => {
		if (!window.chrome?.webview) {
			return;
		}

		// C# → JS 通信
		window.chrome.webview.addEventListener('message', e => {
			const message = e.data;
			if (message.pageKey !== props.pageKey) {
				return;
			}
			switch (message.type) {
				case 'IsFileLoadRunningUpdated':
					isFileLoadUpdating.value = message.data;
					break;
				case 'FileLoadProgressUpdated':
					fileLoadProgress.value = message.data * 100;
					break;
				case 'TotalLinesUpdated':
					totalLines.value = message.data;
					break;
				case 'TotalBytesUpdated':
					totalBytes.value = message.data;
					break;
				case 'OpenedFilePathChanged':
					openedFilepath.value = message.data;
					break;
				case 'IsRangeContentSavingUpdated':
					isSaving.value = message.data;
					break;
				case 'SaveRangeProgressUpdated':
					saveProgress.value = message.data * 100;
					break;
				case 'AvailableEncodingsUpdated':
					availableEncodings.value = message.data;
					break;
			}
		});
	});
</script>

<style scoped>
	/* ファイル操作エリア */
	.file-operation-area {
		display: flex;
		flex-direction: row;
		flex-wrap: wrap;
		gap: 16px;
		align-items: stretch;
		align-content: flex-start;
		padding: 2px 4px;

		.title {
			position: relative;
			top: 0;
			width: 100%;
			text-align: center;
			font-size: 0.9em;
			color: #555;
			padding-bottom: 4px;
		}

		div .content {
			display: flex;
			justify-content: center;
			align-items: center;
		}

		.filepath .content {
			width: 100px;
		}

		.file-stats .content {
			min-width: 100px;
		}

		.range-save .content {
			input {
				width: 60px;
			}
		}

		.wrap-lines {
			display: flex;
			flex-direction: column;
			gap: 8px;
		}
	}
</style>
