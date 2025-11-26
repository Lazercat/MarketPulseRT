import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@app/design-system': path.resolve(__dirname, '../../packages/design-system/src')
    }
  },
  server: {
    port: 3000
  },
  build: {
    sourcemap: true,
    rollupOptions: {
      output: {
        manualChunks: {
          react: ['react', 'react-dom'],
          aggrid: ['ag-grid-react', 'ag-grid-community'],
          signalr: ['@microsoft/signalr']
        }
      }
    }
  }
})
