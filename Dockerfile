FROM node:18-alpine AS builder
WORKDIR /talking-points
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

FROM node:18-alpine
WORKDIR /talking-points
ENV NODE_ENV=production
ENV HOSTNAME=0.0.0.0
COPY --from=builder /talking-points/next.config.ts ./
COPY --from=builder /talking-points/public ./public
COPY --from=builder /talking-points/.next/standalone ./
COPY --from=builder /talking-points/.next/static ./.next/static
EXPOSE 3000
CMD ["node", "server.js"]