const { defineConfig } = require('electron-vite')
const vue = require('@vitejs/plugin-vue')
const { resolve } = require('node:path')

const srcPath = resolve(__dirname, 'src')
const alias = {
  '@': srcPath
}

module.exports = defineConfig({
  main: {
    resolve: {
      alias
    },
    build: {
      lib: {
        entry: resolve(__dirname, 'src/main/index.js')
      }
    }
  },
  preload: {
    resolve: {
      alias
    },
    build: {
      lib: {
        entry: resolve(__dirname, 'src/preload/index.js')
      }
    }
  },
  renderer: {
    resolve: {
      alias
    },
    plugins: [vue()],
    root: resolve(__dirname, 'src/renderer'),
    build: {
      rollupOptions: {
        input: {
          index: resolve(__dirname, 'src/renderer/index.html')
        }
      }
    }
  }
})