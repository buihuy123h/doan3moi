# ===== Build stage =====
FROM node:20-alpine AS build
WORKDIR /app

# Dùng pnpm (phiên bản ghim trong packageManager của package.json)
RUN corepack enable

# Cài dependency trước để tận dụng cache layer
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile

# Copy source và build (Vite chèn VITE_API_URL lúc build — trỏ vào service api)
COPY . .
RUN pnpm run build

# ===== Runtime stage =====
FROM nginx:alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
