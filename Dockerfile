FROM node:22-bookworm-slim AS styles
WORKDIR /src

COPY package.json package-lock.json ./
RUN npm ci

COPY Components ./Components
COPY wwwroot ./wwwroot
RUN npm run sass:build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY HouseKeeper.csproj ./
RUN dotnet restore HouseKeeper.csproj

COPY . .
COPY --from=styles /src/Components ./Components
COPY --from=styles /src/wwwroot ./wwwroot

RUN dotnet publish HouseKeeper.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
EXPOSE 8080

CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet HouseKeeper.dll"]
