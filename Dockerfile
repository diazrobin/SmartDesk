# Etapa 1: compilar y publicar con el SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar primero solo los .csproj: esta capa se cachea mientras no cambien las dependencias
COPY src/SmartDesk.Domain/*.csproj src/SmartDesk.Domain/
COPY src/SmartDesk.Application/*.csproj src/SmartDesk.Application/
COPY src/SmartDesk.Infrastructure/*.csproj src/SmartDesk.Infrastructure/
COPY src/SmartDesk.Api/*.csproj src/SmartDesk.Api/
RUN dotnet restore src/SmartDesk.Api/SmartDesk.Api.csproj

# Después, el código fuente
COPY . .
RUN dotnet publish src/SmartDesk.Api/SmartDesk.Api.csproj -c Release -o /app/publish --no-restore

# Etapa 2: solo el runtime, sin SDK ni código fuente
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Escuchar en 8080 (sin privilegios) y ejecutar con el usuario no-root que trae la imagen
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "SmartDesk.Api.dll"]
