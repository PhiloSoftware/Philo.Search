import typescript from 'rollup-plugin-typescript2';
import VuePlugin from 'rollup-plugin-vue';
import commonjs from '@rollup/plugin-commonjs';
import pkg from '../package.json';
import css from 'rollup-plugin-css-only';
import { getBabelOutputPlugin } from '@rollup/plugin-babel';
import * as fs from 'fs';
let postcss = require('postcss');

import DeepEqual from 'deep-equal';

export default [
  // ESM build to be used with webpack/rollup.
  {
    input: 'src/main.ts',
    output: {
      file: pkg.module,
      format: `esm`,
      external: ['vue'],
    },
    plugins: [
      typescript({
        tsconfig: 'tsconfig.build.json',
        useTsconfigDeclarationDir: true,
      }),
      // Compile Vue for browser, ignore CSS (handled in cjs build)
      VuePlugin({
        css: false,
        preprocessStyles: true,
      }),
      css({
        output: false,
      }),
      // transpile esnext to es5 (IE support)
      getBabelOutputPlugin({
        presets: ['@babel/preset-env'],
      }),
      commonjs({
        include: /node_modules/,
        namedExports: {
          'deep-equal': Object.keys(DeepEqual),
        },
      }),
    ],
  },
  // SSR build.
  {
    input: 'src/main.ts',
    output: {
      file: pkg.main,
      format: `cjs`,
    },
    plugins: [
      typescript({
        tsconfig: 'tsconfig.build.json',
        useTsconfigDeclarationDir: true,
      }),
      // Compile Vue for node(SSR), generate CSS file
      VuePlugin({
        css: false,
        template: {
          optimizeSSR: true,
        },
        preprocessStyles: true,
      }),
      css({
        async output(styles) {
          // parse generated Styles using postcss to remove possible styles duplication
          const parsedStyles = await postcss([
            require('cssnano')({
              preset: 'default',
            }),
          ]).process(styles);
          fs.writeFileSync(pkg.style, parsedStyles.css);
        },
      }),
      commonjs({
        include: /node_modules/,
        namedExports: {
          'deep-equal': Object.keys(DeepEqual),
        },
      }),
      // transpile esnext to node 12 compatible build.
      getBabelOutputPlugin({
        presets: [
          [
            '@babel/preset-env',
            {
              targets: {
                node: '12',
              },
            },
          ],
        ],
      }),
    ],
  },
];
