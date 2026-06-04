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
      input: {
        landing: path.resolve(rootDir, "ClientApp/src/main.jsx"),
        product: path.resolve(rootDir, "ClientApp/src/product.jsx")
      },
      output: {
        entryFileNames: (chunkInfo) =>
          chunkInfo.name === "product" ? "assets/bookslot-product.js" : "assets/bookslot-landing.js",
        chunkFileNames: "assets/bookslot-[name].js",
        assetFileNames: (assetInfo) => {
          const name = assetInfo.names?.[0] || assetInfo.name || "";
          return name.includes("product")
            ? "assets/bookslot-product[extname]"
            : "assets/bookslot-landing[extname]";
        }
      }
    }
  }
});
