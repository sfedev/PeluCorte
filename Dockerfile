# ============================================================
#  Build stage — compila la aplicación
#  cache-bust: 2026-04-29-v3
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar dependencias primero (mejor caché Docker)
COPY PeluCorte.csproj ./
RUN dotnet restore PeluCorte.csproj

# Copiar el resto del código (lo que NO esté en .dockerignore)
COPY . .

# Publicar en Release.
# IMPORTANTE: NO usamos /p:UseAppHost=false porque rompe la generación
# del manifest de Static Web Assets (blazor.web.js no se serviría).
RUN dotnet publish PeluCorte.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Si por algún motivo blazor.web.js no se materializó en wwwroot/_framework/,
# lo copiamos manualmente desde el cache de NuGet donde lo deja el SDK.
RUN mkdir -p /app/publish/wwwroot/_framework && \
    if [ ! -f /app/publish/wwwroot/_framework/blazor.web.js ]; then \
        echo "blazor.web.js NO está en publish, copiando desde NuGet..."; \
        find /root/.nuget /usr/share/dotnet -name "blazor.web.js" 2>/dev/null | head -5; \
        BLAZOR_JS=$(find /root/.nuget -name "blazor.web.js" 2>/dev/null | head -1); \
        if [ -n "$BLAZOR_JS" ]; then \
            cp "$BLAZOR_JS" /app/publish/wwwroot/_framework/blazor.web.js; \
            echo "Copiado desde $BLAZOR_JS"; \
        else \
            echo "ERROR: blazor.web.js no encontrado en NuGet"; \
        fi; \
    else \
        echo "blazor.web.js OK en publish"; \
    fi && \
    ls -la /app/publish/wwwroot/_framework/ 2>&1 || true

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
