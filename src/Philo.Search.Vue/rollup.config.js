import peerDepsExternal from "rollup-plugin-peer-deps-external";
import resolve from "@rollup/plugin-node-resolve";
import commonjs from "@rollup/plugin-commonjs";
import typescript from "rollup-plugin-typescript2";
import scss from 'rollup-plugin-scss'
import vue from "rollup-plugin-vue";

import packageJson from "./package.json"  assert {type: 'json'};;
const outdir = 'dist'

export default {
  input: "src/index.ts",
  output: [
    {
      file: `${outdir}/${packageJson.main}`,
      format: "cjs",
      sourcemap: true
    }//,
    // {
    //   file: `${outdir}/${packageJson.module}`,
    //   format: "esm",
    //   sourcemap: true
    // }
  ],
  plugins: [ commonjs(), vue(), peerDepsExternal(), resolve(), typescript(), scss()]
};
