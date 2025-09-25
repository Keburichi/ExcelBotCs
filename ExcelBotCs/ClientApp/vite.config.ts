import {defineConfig, loadEnv} from 'vite'
import {fileURLToPath, URL} from 'node:url'
import vue from '@vitejs/plugin-vue'
import path from 'node:path'

export default defineConfig(({mode}) => {
    const env = loadEnv(mode, process.cwd(), '')
    // Default to local API on port 8080; can be overridden via VITE_BACKEND_URL
    const backend = env.VITE_BACKEND_URL || 'http://localhost:8080'

    return {
        plugins: [vue()],
        resolve: {
            alias: {
                '@': fileURLToPath(new URL('./src', import.meta.url))
            }
        },
        server: {
            port: 5173,
            strictPort: true,
            proxy: {
                '/api': {
                    target: backend,
                    changeOrigin: true,
                    secure: false
                }
            }
        },
        build: {
            outDir: path.resolve(__dirname, '../wwwroot'),
            emptyOutDir: true
        },
        base: '/'
    }
})
