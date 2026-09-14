/** @type {import('tailwindcss').Config} */
module.exports = {
  // Layout utilities only — Angular Material owns component styling and
  // theme tokens (src/styles.scss). Tailwind never targets Material's own
  // internal markup, so the two systems don't fight over the same classes.
  content: ["./src/**/*.{html,ts}"],
  corePlugins: {
    // Tailwind's preflight resets conflict with Material's own base styles.
    preflight: false,
  },
  theme: {
    extend: {},
  },
  plugins: [],
};
