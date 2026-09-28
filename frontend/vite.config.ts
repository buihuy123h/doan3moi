import path from "path"
import { defineConfig } from "vite"
import { svelte } from "@sveltejs/vite-plugin-svelte"
import { getAllDatabaseData, getDatabaseStatus } from "./sql-bridge.js"

// https://vite.dev/config/
export default defineConfig({
  base: '/',
  plugins: [
    svelte({
      onwarn(warning, defaultHandler) {
        if (warning.code && warning.code.startsWith('a11y_')) return;
        if (warning.code === 'element_invalid_self_closing_tag') return;
        defaultHandler(warning);
      }
    }),
    {
      name: 'nexus-sqlserver-api',
      configureServer(server) {
        server.middlewares.use('/api/nexus/status', (_req, res) => {
          res.setHeader('Content-Type', 'application/json');
          res.setHeader('Access-Control-Allow-Origin', '*');
          const status = getDatabaseStatus();
          res.end(JSON.stringify(status));
        });

        server.middlewares.use('/api/nexus/all', (_req, res) => {
          res.setHeader('Content-Type', 'application/json');
          res.setHeader('Access-Control-Allow-Origin', '*');
          const data = getAllDatabaseData();
          if (!data) {
            res.statusCode = 500;
            res.end(JSON.stringify({ status: 'Error', message: 'Could not fetch from SQL Server' }));
          } else {
            res.end(JSON.stringify(data));
          }
        });
      }
    }
  ],
  server: {
    port: 3000,
    proxy: {
      '/api/Technical': {
        target: 'http://localhost:5105',
        changeOrigin: true,
      },
    },
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  build: {
    chunkSizeWarningLimit: 1600,
  },
});
