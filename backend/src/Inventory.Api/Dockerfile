# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy entire repo (dev-friendly, simple)
COPY . .

# restore & publish
RUN dotnet restore Inventory.sln
RUN dotnet publish backend/src/Inventory.Api/Inventory.Api.csproj -c Release -o /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# container listens on 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]