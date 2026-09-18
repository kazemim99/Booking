# IMPORTANT: the build context for this Dockerfile is the REPOSITORY ROOT, not this
# directory -- see `context: .` in .github/workflows/deploy.yml (and deploy-staging.yml).
#
# src/main.ts imports '../../packages/design-tokens/tokens.css', the shared brand token
# sheet, which lives outside booksy-frontend/. With the old `context: ./booksy-frontend`
# that path simply was not in the build context, and the image build failed with:
#   Could not resolve "../../packages/design-tokens/tokens.css" from "src/main.ts"
# (The same import also needs those files to be committed -- they were being swallowed
# by a .gitignore rule; see the !**/[Pp]ackages/design-tokens/ exception there.)

# Build stage
FROM node:20-alpine AS build

WORKDIR /repo

# Copy the manifest + lockfile first so the dependency layer stays cached when only
# application source changes.
COPY booksy-frontend/package.json booksy-frontend/package-lock.json booksy-frontend/

RUN cd booksy-frontend && npm ci --prefer-offline --no-audit

# The shared design tokens must land before the build: main.ts imports them by relative
# path out of this directory.
COPY packages/ packages/
COPY booksy-frontend/ booksy-frontend/

RUN cd booksy-frontend && npm run build

# Production stage
FROM nginx:alpine

# Install curl for health checks
RUN apk add --no-cache curl

# Copy built files from build stage
COPY --from=build /repo/booksy-frontend/dist /usr/share/nginx/html

# Copy nginx configuration
COPY booksy-frontend/nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
