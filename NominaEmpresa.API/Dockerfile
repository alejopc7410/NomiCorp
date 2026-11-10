
# Etapa 1: Compilar la API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore "NominaEmpresa.API.csproj"

RUN dotnet publish "NominaEmpresa.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Etapa 2: Ejecutar la aplicación
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "NominaEmpresa.API.dll"]
