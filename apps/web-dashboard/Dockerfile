# Build the web dashboard
FROM node:20-slim AS build
WORKDIR /app
ENV COREPACK_ENABLE_STRICT=0
RUN corepack enable
COPY package.json yarn.lock tsconfig.base.json turbo.json ./
COPY apps/web-dashboard ./apps/web-dashboard
COPY packages ./packages
# Install and build only the dashboard workspace
RUN yarn install --immutable
RUN yarn build --filter web-dashboard

FROM node:20-slim
WORKDIR /app
ENV PORT=4173
COPY --from=build /app/apps/web-dashboard/dist ./dist
RUN corepack enable
RUN yarn set version stable
EXPOSE 4173
# Serve with Vite preview (static)
CMD ["yarn", "workspace", "web-dashboard", "preview", "--host", "0.0.0.0", "--port", "4173"]
