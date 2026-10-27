FROM node:22-alpine AS build

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build


FROM nginx:alpine AS runtime

COPY --from=build /app/dist /usr/share/nginx/html

COPY nginx.conf /etc/nginx/templates/default.conf.template

ENV PORT=10000
ENV NGINX_ENVSUBST_FILTER="^(API_UPSTREAM|PORT)$"

EXPOSE 10000

CMD ["nginx", "-g", "daemon off;"]
