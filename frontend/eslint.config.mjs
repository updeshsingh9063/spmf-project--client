// Flat config, loading next's rules directly.
//
// The FlatCompat shim that ships with create-next-app fails under eslint 9 with
// eslint-config-next 16: validating the compat-wrapped config walks a circular
// reference and throws before any file is linted. Importing the plugins directly
// avoids the shim entirely.

import js from "@eslint/js";
import nextPlugin from "@next/eslint-plugin-next";
import tsParser from "@typescript-eslint/parser";
import tsPlugin from "@typescript-eslint/eslint-plugin";
import reactHooks from "eslint-plugin-react-hooks";

export default [
  {
    ignores: [
      "node_modules/**",
      ".next/**",
      "out/**",
      "build/**",
      "playwright-report/**",
      "test-results/**",
      "next-env.d.ts",
    ],
  },

  js.configs.recommended,

  {
    files: ["**/*.{ts,tsx}"],
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        ecmaVersion: "latest",
        sourceType: "module",
        ecmaFeatures: { jsx: true },
      },
      globals: {
        window: "readonly",
        document: "readonly",
        console: "readonly",
        crypto: "readonly",
        fetch: "readonly",
        setTimeout: "readonly",
        clearTimeout: "readonly",
        localStorage: "readonly",
        sessionStorage: "readonly",
        URLSearchParams: "readonly",
        URL: "readonly",
        process: "readonly",
        React: "readonly",
        HTMLElement: "readonly",
        MouseEvent: "readonly",
        BeforeUnloadEvent: "readonly",
        Response: "readonly",
        RequestInit: "readonly",
        HeadersInit: "readonly",
      },
    },
    plugins: {
      "@typescript-eslint": tsPlugin,
      "@next/next": nextPlugin,
      "react-hooks": reactHooks,
    },
    rules: {
      ...tsPlugin.configs.recommended.rules,
      ...nextPlugin.configs.recommended.rules,
      ...nextPlugin.configs["core-web-vitals"].rules,
      ...reactHooks.configs.recommended.rules,

      // TypeScript already reports undefined identifiers, and it understands types
      // the base rule cannot.
      "no-undef": "off",

      // Reading persisted state once on mount genuinely requires setState in an
      // effect: the value is not available during render, and there is no cascade
      // because the effect runs once. Kept as a warning so a real cascading-render
      // mistake is still visible.
      "react-hooks/set-state-in-effect": "warn",

      // Unused arguments are often there to document a signature; a leading
      // underscore marks the intent.
      "@typescript-eslint/no-unused-vars": [
        "error",
        { argsIgnorePattern: "^_", varsIgnorePattern: "^_" },
      ],
    },
  },
];
