import { defineConfig } from 'vite';
import dts from "vite-plugin-dts";
import vue from '@vitejs/plugin-vue2';
import packageJson from "./package.json";

export default defineConfig({
  plugins: [
    vue(),
    dts({
      insertTypesEntry: true,
    }),
  ],
  optimizeDeps: {
    force: true,
  },
  build: {
    sourcemap: true,
    outDir: 'dist',
    lib: {
      // Could also be a dictionary or array of multiple entry points
      entry: './src/index.ts',
      name: 'Philo-Search-Vue',
      // the proper extensions will be added
      fileName: 'philo-search-vue',
    },
    commonjsOptions: {
      transformMixedEsModules: true,
    },
    rollupOptions: {
      
      // make sure to externalize deps that shouldn't be bundled
      // into your library
      external: ['vue', 'vuejs-datatable'],
      input: "src/index.ts",
      output: 
        {
          dir: 'dist',
          format: "cjs",
          sourcemap: true
        }
    },
  }
})