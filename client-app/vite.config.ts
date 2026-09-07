import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

export default defineConfig({
    plugins: [vue()],
    resolve: {
        alias: {
            '@': path.resolve(__dirname, './src')
        }
    },
    server: {
        // Allow specific hosts
        allowedHosts: ['0dab-37-224-180-54.ngrok-free.app'],
        port: 3000,
        strictPort: false,
        cors: true,
        proxy: {
            '/videocallhub': {
                target: 'https://4e97-194-238-97-224.ngrok-free.app',
                ws: true,
                changeOrigin: true
            }
        }
    },
    build: {
        outDir: '../client-app/dist',
        emptyOutDir: true,
        rollupOptions: {
            output: {
                manualChunks: {
                    'vendor': ['vue', 'vue-router'],
                    'signalr': ['@microsoft/signalr']
                }
            }
        }
    }
})
