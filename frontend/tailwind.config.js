/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        bg: {
          DEFAULT: '#0A0C13',
          light: '#F9F7F4'
        },
        surface: {
          DEFAULT: '#111520',
          light: '#F0EDE6'
        },
        elevated: '#181D2C',
        border: '#212638',
        txt: {
          primary: '#ECEFFE',
          muted: '#7E88A8'
        },
        accent: {
          DEFAULT: '#E8546A',
          hover: '#D44359'
        },
        status: {
          free: '#34D399',
          held: '#FBBF24',
          booked: '#64748B'
        }
      },
      fontFamily: {
        heading: ['Playfair Display', 'serif'],
        sans: ['DM Sans', 'sans-serif'],
      },
    },
  },
  plugins: [],
}
