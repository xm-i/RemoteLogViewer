import { createApp } from 'vue';
import App from './App.vue';

// スタイルをインポート
import './assets/styles/main.css';
import { OutgoingWebMessage } from './types/outgoingMessages';
import { IncomingWebMessage } from './types/incommingMessages';

// グローバルな型定義
declare global {
	interface Window {
		chrome?: {
			webview?: {
				postMessage: (message: OutgoingWebMessage) => void;
				addEventListener: (event: string, handler: (e: MessageEvent<IncomingWebMessage>) => void) => void;
			};
		};
	}
}

const app = createApp(App);
app.mount('#app');

// C#側へready通知
if (window.chrome?.webview) {
	window.chrome.webview.postMessage({
		pageKey: '*',
		requestId: 0,
		type: 'Ready'
	});
}
