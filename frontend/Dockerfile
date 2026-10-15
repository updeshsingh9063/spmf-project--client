# SPFM frontend.
#
#   docker build -f frontend/Dockerfile -t spfm-frontend frontend/
#
# NEXT_PUBLIC_* values are inlined at BUILD time, so each environment needs its own
# image or a rebuild. They are public identifiers (client and tenant ids, scopes),
# never secrets.

FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

ARG NEXT_PUBLIC_API_BASE_URL
ARG NEXT_PUBLIC_AUTH_MODE=entra
ARG NEXT_PUBLIC_DATA_MODE=api
ARG NEXT_PUBLIC_ENTRA_CLIENT_ID
ARG NEXT_PUBLIC_ENTRA_TENANT_ID
ARG NEXT_PUBLIC_ENTRA_API_SCOPE
ARG NEXT_PUBLIC_ENTRA_REDIRECT_URI
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production NEXT_TELEMETRY_DISABLED=1

RUN addgroup -S spfm && adduser -S spfm -G spfm
USER spfm

COPY --from=build --chown=spfm:spfm /app/.next ./.next
COPY --from=build --chown=spfm:spfm /app/public ./public
COPY --from=build --chown=spfm:spfm /app/node_modules ./node_modules
COPY --from=build --chown=spfm:spfm /app/package.json ./package.json

EXPOSE 3000
CMD ["npm", "run", "start"]
