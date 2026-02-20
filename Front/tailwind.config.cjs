/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      colors: {
        minerva: {
          navy: '#003366',
          green: '#8CC63F',
          blue: '#0072CE',
          'blue-light': '#E6F2FF',
        },
      },
    },
  },
  plugins: [],
}
