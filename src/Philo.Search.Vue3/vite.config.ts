import { defineConfig } from 'vite'
const path = require('path')
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  build: {
    lib: {
      entry: path.resolve(__dirname, 'src/main.ts'),
      name: 'Philo.Search.Vue3',
      fileName: (format) => `philo-search-vue3.${format}.js`
    },
    rollupOptions: {
      // make sure to externalize deps that shouldn't be bundled
      // into your library
      external: [
        '@jobinsjp/vue3-datatable',
        'deep-equal',
        'linq-es2015',
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
