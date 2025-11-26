# Build the web dashboard (expects repo root as build context)
FROM node:20-slim AS build
WORKDIR /app
ENV COREPACK_ENABLE_STRICT=0
RUN corepack enable

# Copy root manifests and workspaces
COPY package.json yarn.lock tsconfig.base.json turbo.json .yarnrc.yml ./
COPY apps/web-dashboard ./apps/web-dashboard
COPY packages ./packages

# Install deps and build the dashboard workspace only
ENV SKIP_DOTNET_RESTORE=1
ENV YARN_NODE_LINKER=node-modules
RUN yarn install --mode=skip-build --immutable
# Build shared design-system first so the dashboard can import built artifacts if needed
RUN yarn workspace @app/design-system build
RUN yarn workspace web-dashboard build

FROM node:20-slim
WORKDIR /app
ENV PORT=4173
RUN npm install -g serve
COPY --from=build /app/apps/web-dashboard/dist ./dist
COPY apps/web-dashboard/serve.json /app/serve.json
EXPOSE 4173
CMD ["serve", "-s", "dist", "-l", "4173", "--config", "/app/serve.json"]
