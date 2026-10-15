import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Self-contained server bundle for Azure App Service or a container
  // (docs/DEPLOYMENT.md). Netlify and Vercel package Next.js their own way,
  // so leave it off there (they set NETLIFY / VERCEL during builds).
  output: process.env.NETLIFY || process.env.VERCEL ? undefined : "standalone",
  poweredByHeader: false,
};

export default nextConfig;
