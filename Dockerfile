# Stage 1 — Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and all project files first (layer cache for restore)
COPY OnTime.sln ./
COPY src/OnTime.Domain/OnTime.Domain.csproj                     src/OnTime.Domain/
COPY src/OnTime.Application/OnTime.Application.csproj           src/OnTime.Application/
COPY src/OnTime.Infrastructure/OnTime.Infrastructure.csproj     src/OnTime.Infrastructure/
COPY src/OnTime.API/OnTime.API.csproj                           src/OnTime.API/

RUN dotnet restore src/OnTime.API/OnTime.API.csproj

# Copy rest of source and publish
COPY . .
RUN dotnet publish src/OnTime.API/OnTime.API.csproj \
    -c Release \
    -o /app/publish

# Stage 2 — Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "OnTime.API.dll"]
