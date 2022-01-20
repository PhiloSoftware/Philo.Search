import { defineConfig } from 'vite'
const path = require('path')
import vue from '@vitejs/plugin-vue'
import dts from 'vite-plugin-dts'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue(), dts({
    outputDir: "dist",
  })],
  build: {
    lib: {
      entry: path.resolve(__dirname, 'src/main.ts'),
      name: 'Philo.Search.Vue3',
      fileName: (format) => `philo-search-vue3.${format}.js`
    },
    rollupOptions: {
      external: [
        '@jobinsjp/vue3-datatable',
        'deep-equal',
        'moment',
        'vue',
      ],
      output: {
        // Provide global variables to use in the UMD build
        // for externalized deps
        globals: {
          vue: 'Vue',
        }
      }
    }
  }
})
