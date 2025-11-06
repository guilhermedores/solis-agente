# =============================================================================
# Dockerfile para Agente PDV (.NET)
# =============================================================================

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# =============================================================================
# Stage: Build
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivo de projeto
COPY ["*.csproj", "./"]

# Restaurar dependências
RUN dotnet restore

# Copiar todo o código fonte
COPY . .

# Build da aplicação
RUN dotnet build -c Release -o /app/build

# =============================================================================
# Stage: Publish
# =============================================================================
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# =============================================================================
# Stage: Final
# =============================================================================
FROM base AS final
WORKDIR /app

# Instalar dependências para periféricos (se necessário)
RUN apt-get update && apt-get install -y \
    libusb-1.0-0 \
    && rm -rf /var/lib/apt/lists/*

# Copiar arquivos publicados
COPY --from=publish /app/publish .

# Criar diretórios necessários
RUN mkdir -p /app/logs /app/fiscal /app/impressao

# Criar usuário não-root
RUN useradd -m -u 1001 dotnetuser && \
    chown -R dotnetuser:dotnetuser /app

USER dotnetuser

# Healthcheck
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

# Iniciar aplicação
ENTRYPOINT ["dotnet", "Solis.AgentePDV.dll"]