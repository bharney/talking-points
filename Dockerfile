FROM node:lts-alpine AS builder
WORKDIR /talking-points

COPY package*.json ./
RUN npm install
COPY . .
RUN rm -rf ./src/server

# Ensure the env var is available while Next.js builds so it gets inlined in the client bundle
RUN echo "Building with NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL" && npm run build

FROM node:lts-alpine
WORKDIR /talking-points
ENV NODE_ENV=production

COPY --from=builder /talking-points/next.config.ts ./
COPY --from=builder /talking-points/public ./public
COPY --from=builder /talking-points/.next/standalone ./
COPY --from=builder /talking-points/.next/static ./.next/static
EXPOSE 3000
CMD ["node", "server.js"]
