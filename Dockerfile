# Multi-stage Dockerfile for MidgardAddressBook (Blazor Web App on .NET 10).

# ------------------------------------------------------------------
# Stage 1: Build front-end assets (Bootstrap) with Node.
# ------------------------------------------------------------------
FROM node:24.15.0-alpine AS assets
WORKDIR /src/web
RUN corepack enable && corepack prepare yarn@4.14.1 --activate
COPY src/MidgardAddressBook.Web/package.json src/MidgardAddressBook.Web/yarn.lock src/MidgardAddressBook.Web/.yarnrc.yml ./
RUN yarn install --immutable
COPY src/MidgardAddressBook.Web/build-assets.mjs ./
RUN yarn build

# ------------------------------------------------------------------
# Stage 2: Restore + build + publish the .NET solution.
# ------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first to maximize restore caching.
COPY MidgardAddressBook.slnx ./
COPY src/MidgardAddressBook.Core/MidgardAddressBook.Core.csproj src/MidgardAddressBook.Core/
COPY src/MidgardAddressBook.DAL/MidgardAddressBook.DAL.csproj   src/MidgardAddressBook.DAL/
COPY src/MidgardAddressBook.BLL/MidgardAddressBook.BLL.csproj   src/MidgardAddressBook.BLL/
COPY src/MidgardAddressBook.Web/MidgardAddressBook.Web.csproj   src/MidgardAddressBook.Web/
RUN dotnet restore MidgardAddressBook.slnx

# Copy the rest of the source.
COPY src/ src/

# Bring in the prebuilt Bootstrap assets from the assets stage.
COPY --from=assets /src/web/wwwroot/css/ src/MidgardAddressBook.Web/wwwroot/css/
COPY --from=assets /src/web/wwwroot/js/  src/MidgardAddressBook.Web/wwwroot/js/

RUN dotnet publish src/MidgardAddressBook.Web/MidgardAddressBook.Web.csproj \
    -c Release \
    -o /app/publish \
    /p:SkipYarnBuild=true

# ------------------------------------------------------------------
# Stage 3: Runtime image.
# ------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish ./

# Render / PaaS injects $PORT; default to 8080 for local use.
ENV ASPNETCORE_ENVIRONMENT=Production \
    PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MidgardAddressBook.Web.dll"]
