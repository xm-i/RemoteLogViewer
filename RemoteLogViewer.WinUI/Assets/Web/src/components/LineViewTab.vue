<template>
	<div class="line-view-tab">
		<div class="line-view-results log-container">
			<div v-if="line !== null" class="log-line">
				<span class="line-number">{{ line.lineNumber }}</span>
				<span class="line-content wrap" v-html="line.content" tabindex="-1"></span>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
	import { ref } from 'vue';
	import type { TextLine } from '@/types';

	const line = ref<TextLine | null>(null);

	const setLine = (newLine: TextLine) => {
		line.value = newLine;
	};

	defineExpose({
		setLine
	});
</script>

<style scoped>
	.line-view-tab {
		padding: 8px;
		height: 100%;
		overflow: scroll;

		.line-number {
			pointer-events: none;
			color: black;
		}
	}
</style>
