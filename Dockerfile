# 1. Etapa de compilación con el SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copia todo el contenido del repositorio
COPY . .

# Restaura y compila el proyecto
RUN dotnet restore
RUN dotnet publish -c Release -o /out

# 2. Etapa de ejecución con el Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .

# Configura el puerto para Render
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Reemplaza "NombreDeTuProyecto" por el nombre exacto de tu archivo .csproj
ENTRYPOINT ["dotnet", "NombreDeTuProyecto.dll"]