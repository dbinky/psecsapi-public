# Build stage
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files and install dependencies
COPY package.json package-lock.json* ./
RUN npm ci

# Copy source and build
COPY tsconfig.json ./
COPY src/ src/
COPY content/ content/
# Build, then prune devDependencies so the runtime stage gets a clean install
RUN npm run build && npm prune --omit=dev

# Runtime stage
FROM node:22-alpine AS runtime
WORKDIR /app

# Copy built output and production dependencies
COPY --from=build /app/dist dist/
COPY --from=build /app/content content/
COPY --from=build /app/package.json .
COPY --from=build /app/node_modules node_modules/

EXPOSE 3001

# Run as non-root user to limit container compromise blast radius
USER node

ENTRYPOINT ["node", "dist/server.js", "--http", "--oauth", "--host", "0.0.0.0", "--port", "3001"]
