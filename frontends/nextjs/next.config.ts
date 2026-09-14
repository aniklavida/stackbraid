import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin("./src/shared/i18n/request.ts");

const nextConfig: NextConfig = {
  // The generated TypeScript client (clients/typescript) ships its
  // source directly with no build step — see its own README. Next.js
  // only compiles workspace code it is told to, so the package must be
  // named here to be transpiled like the rest of the app.
  transpilePackages: ["@stackbraid/client-typescript"],
  // clients/typescript lives outside frontends/nextjs (a sibling under the
  // repository root, not a nested node_modules package) — this is what
  // lets Next.js follow the `file:../../clients/typescript` symlink at
  // all, rather than refusing anything resolved outside the app directory.
  experimental: {
    externalDir: true,
  },
};

export default withNextIntl(nextConfig);
