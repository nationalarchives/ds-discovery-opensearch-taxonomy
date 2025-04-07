# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://*:8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["tna.taxonomy.api/tna.taxonomy.api.csproj", "tna.taxonomy.api/"]
COPY ["Taxonomy.Common/NationalArchives.Taxonomy.Common.csproj", "Taxonomy.Common/"]
RUN dotnet restore "./tna.taxonomy.api/tna.taxonomy.api.csproj"
COPY . .
WORKDIR "/src/tna.taxonomy.api"
RUN dotnet build "./tna.taxonomy.api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./tna.taxonomy.api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
RUN addgroup -g 965 -S appuser && adduser -u 975 -S -D -h /app appuser appuser
USER appuser
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "tna.taxonomy.api.dll"]