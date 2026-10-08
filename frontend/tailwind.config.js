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
        // Bookline vNext Refined Dark Palette
        bg: {
          DEFAULT: '#090B10',
          light: '#FAF8F5',
        },
        surface: {
          DEFAULT: '#111620',
          secondary: '#151B27',
          elevated: '#1A2130',
          light: '#FFFFFF',
          'light-secondary': '#F3F0EB',
        },
        border: {
          DEFAULT: '#273142',
          strong: '#344054',
          light: '#DED8CF',
        },
        txt: {
          primary: '#F4F6FA',
          secondary: '#C3CAD6',
          tertiary: '#8F9AAF',
          disabled: '#687386',
          'light-primary': '#181A1F',
          'light-secondary': '#555D6B',
          'light-tertiary': '#707989',
        },
        accent: {
          DEFAULT: '#E8546A',
          hover: '#F06A7D',
          pressed: '#C94358',
          light: '#D94C62',
        },
        status: {
          success: '#34D399',
          warning: '#FBBF24',
          error: '#F87171',
          info: '#60A5FA',
          'light-success': '#158A63',
          'light-warning': '#A66B00',
          'light-error': '#C74444',
        },
        slot: {
          free: '#34D399',
          held: '#FBBF24',
          booked: '#64748B',
          selected: '#E8546A',
        },
      },
      fontFamily: {
        heading: ['Playfair Display', 'serif'],
        sans: ['DM Sans', 'sans-serif'],
      },
    },
  },
  plugins: [],
}
