import path from 'path';
import { fileURLToPath } from 'url';
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'
import basicSsl from '@vitejs/plugin-basic-ssl'
import { TanStackRouterVite } from '@tanstack/router-plugin/vite'

// https://vitejs.dev/config/
export default defineConfig({
  
  // base: "/",
  
  plugins: [
    basicSsl(),
    TanStackRouterVite(),
    react()
  ],
  resolve: {
    alias: {
      '~': path.resolve(__dirname, './src/'),
    },
  },
  server: {
    proxy: {
      '/AutodeskDM': 'http://ecs-799f9ad3.ecs.ads.autodesk.com',
    },
  },
})