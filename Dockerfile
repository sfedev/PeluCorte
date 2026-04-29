# ============================================================
#  Build stage — compila la aplicación
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar dependencias primero (mejor caché Docker)
COPY PeluCorte.csproj ./
RUN dotnet restore PeluCorte.csproj

# Copiar el resto del código (lo que NO esté en .dockerignore)
COPY . .

# Publicar en Release
RUN dotnet publish PeluCorte.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ============================================================
#  Runtime stage — imagen final ligera
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# La imagen aspnet:10.0 ya incluye un usuario no-root llamado "app".
# Solo creamos el directorio de keys con permisos correctos.

# Copiar la publicación
COPY --from=build /app/publish ./

# Directorio donde se guardan las Data Protection keys (cookies, antiforgery, reset password).
# En Render free tier no hay disco persistente entre deploys: tras redeploy
# los usuarios deberán hacer login otra vez. Aceptable para v1.
RUN mkdir -p /data/keys && chown -R app:app /app /data

USER app

# Producción por defecto. Variables que NO son secretos van aquí;
# las sensibles (DEFAULT_CONNECTION, SMTP_*, etc.) se inyectan por Render Environment.
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_GENERATE_ASPNET_CERTIFICATE=false
ENV DATA_PROTECTION_KEYS_PATH=/data/keys

# Render (y la mayoría de PaaS) inyectan la variable PORT.
# Si no la hay (ej. ejecutando localmente con `docker run`), usamos 8080.
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet PeluCorte.dll"]
