/** @type {import('tailwindcss').Config} */
export default {
  darkMode: "class",
  content: [
    "./index.html",
    "./src/**/*.{ts,tsx,js,jsx}",
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          DEFAULT: "#20C1AC",      // main teal
          soft: "#35D6C0",
        },
        surface: {
          page: "#050505",
          card1: "#0A0A0A",
          card2: "#111111",
        },
        border: {
          DEFAULT: "#2A2A2A",
          deep: "#1A1A1A",
        },
        text: {
          primary: "#F3F4F6",
          secondary: "#A1A1AA",
          tertiary: "#737373",
        },
        pulse: {
          accent: "#38BDF8", // sky-like for Social
        },
        nova: {
          accent: "#8B5CF6", // violet-like for Learn/Twin
        },
      },
      borderRadius: {
        xl: "1rem",
        "2xl": "1.25rem",
      },
      fontSize: {
        xs: "0.8rem",
        sm: "0.9rem",
        base: "1rem",
      },
      keyframes: {
        typingDots: {
          "0%, 20%": { opacity: "0.2", transform: "translateY(0)" },
          "50%": { opacity: "1", transform: "translateY(-1px)" },
          "100%": { opacity: "0.2", transform: "translateY(0)" },
        },
        fadeIn: {
          "0%": { opacity: "0", transform: "translateY(-4px)" },
          "100%": { opacity: "1", transform: "translateY(0)" },
        },
      },
      animation: {
        typingDots: "typingDots 1s infinite",
        fadeIn: "fadeIn 0.12s ease-out",
      },
    },
  },
  plugins: [],
};
