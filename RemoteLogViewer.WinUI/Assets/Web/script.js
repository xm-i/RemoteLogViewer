const { createApp } = Vue;

const app = createApp(App);
app.mount("#app");

window.chrome.webview.postMessage({
	Type: "Ready"
});