import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { fileURLToPath } from "node:url";
import path from "node:path";

const rootDir = fileURLToPath(new URL(".", import.meta.url));

export default defineConfig({
  plugins: [react()],
  root: path.resolve(rootDir, "ClientApp"),
  build: {
    emptyOutDir: true,
    outDir: path.resolve(rootDir, "BookSlot/wwwroot/dist"),
    assetsDir: "assets",
    rollupOptions: {
      input: path.resolve(rootDir, "ClientApp/src/main.jsx"),
      output: {
        entryFileNames: "assets/bookslot-landing.js",
        chunkFileNames: "assets/bookslot-[name].js",
        assetFileNames: "assets/bookslot-landing[extname]"
      }
    }
  }
});
