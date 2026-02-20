# Stage 1: build da SPA com Node
FROM node:20-alpine AS build
WORKDIR /app

# Variável injetada em tempo de build (Vite substitui import.meta.env.VITE_API_URL na compilação)
ARG VITE_API_URL=http://localhost:5002/api
ENV VITE_API_URL=$VITE_API_URL

COPY package.json package-lock.json* ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 2: servir com Nginx
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
