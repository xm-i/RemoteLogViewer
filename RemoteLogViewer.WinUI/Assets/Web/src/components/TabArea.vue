<template>
	<div class="tab-container">
		<ul class="tab-headers" v-if="lineViewTabVisibility">
			<li @click="change('Grep')" :class="{ 'active': isActive === 'Grep' }">Grep</li>
			<li @click="change('LineView')" :class="{ 'active': isActive === 'LineView' }">Line View</li>
		</ul>

		<div class="tab-contents">
			<GrepTab :pageKey="pageKey"
							 v-show="isActive === 'Grep'"
							 @line-clicked="grepLineClicked"
							 :isDisconnected="isDisconnected" />
			<LineViewTab ref="lineViewTab" v-show="isActive === 'LineView'" />
		</div>
	</div>
</template>

<script setup lang="ts">
	import { ref } from 'vue';
	import GrepTab from './GrepTab.vue';
	import LineViewTab from './LineViewTab.vue';
	import type { TextLine } from '@/types';

	interface Props {
		pageKey: string
		isDisconnected: boolean
	}

	const props = withDefaults(defineProps<Props>(), {
		isDisconnected: false
	});

	const emit = defineEmits<{
		'grep-line-clicked': [lineNumber: number]
	}>();

	const isActive = ref('Grep');
	const lineViewTabVisibility = ref(false);
	const lineViewTab = ref<InstanceType<typeof LineViewTab>>();

	const change = (target: string) => {
		isActive.value = target;
	};

	const setLine = (line: TextLine) => {
		isActive.value = 'LineView';
		lineViewTabVisibility.value = true;
		lineViewTab.value?.setLine(line);
	};

	const grepLineClicked = (lineNumber: number) => {
		emit('grep-line-clicked', lineNumber);
	};

	defineExpose({
		setLine
	});
</script>
