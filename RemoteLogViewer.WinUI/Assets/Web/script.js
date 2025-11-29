const { createApp } = Vue;

const app = createApp(App);
app.mount("#app");

Split(['#main-area', '#tab-area'], {
	sizes: [70, 30],
	direction: 'vertical'
});