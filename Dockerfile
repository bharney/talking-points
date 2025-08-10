FROM node:lts-alpine AS builder
WORKDIR /talking-points
COPY package*.json ./
RUN npm install
COPY . .
RUN rm -rf ./src/server
RUN npm run build

FROM node:lts-alpine
WORKDIR /talking-points
ENV NODE_ENV=production
ENV NEXT_PUBLIC_API_URL=https://talkingpointsadmin-aae9dthfamaadwgj.centralus-01.azurewebsites.net
COPY --from=builder /talking-points/next.config.ts ./
COPY --from=builder /talking-points/public ./public
COPY --from=builder /talking-points/.next/standalone ./
COPY --from=builder /talking-points/.next/static ./.next/static
EXPOSE 3000
CMD ["node", "server.js"]