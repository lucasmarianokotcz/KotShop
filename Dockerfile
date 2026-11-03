FROM node:22-bookworm-slim AS styles
WORKDIR /src/KotShop.Web
COPY KotShop.Web/package*.json ./
RUN npm ci
COPY KotShop.Web/ ./
RUN npm run css:build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY KotShop.Domain/KotShop.Domain.csproj KotShop.Domain/
COPY KotShop.Application/KotShop.Application.csproj KotShop.Application/
COPY KotShop.Data/KotShop.Data.csproj KotShop.Data/
COPY KotShop.Infrastructure/KotShop.Infrastructure.csproj KotShop.Infrastructure/
COPY KotShop.Web/KotShop.Web.csproj KotShop.Web/
RUN dotnet restore KotShop.Web/KotShop.Web.csproj
COPY KotShop.Web/ KotShop.Web/
COPY KotShop.Domain/ KotShop.Domain/
COPY KotShop.Application/ KotShop.Application/
COPY KotShop.Data/ KotShop.Data/
COPY KotShop.Infrastructure/ KotShop.Infrastructure/
COPY --from=styles /src/KotShop.Web/wwwroot/tailwind.css KotShop.Web/wwwroot/tailwind.css
RUN dotnet publish KotShop.Web/KotShop.Web.csproj -c Release -o /app/publish --no-restore -p:SkipTailwind=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "KotShop.Web.dll"]
