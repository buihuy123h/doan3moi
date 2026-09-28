import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

/** @type {import('@sveltejs/vite-plugin-svelte').Options} */
export default {
  preprocess: vitePreprocess(),
  onwarn(warning, handler) {
    // Suppress accessibility linting warnings in terminal output
    if (warning.code && warning.code.startsWith('a11y_')) {
      return;
    }
    if (warning.code === 'element_invalid_self_closing_tag') {
      return;
    }
    handler(warning);
  }
};
