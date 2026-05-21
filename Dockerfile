# 1. Usamos la imagen oficial del SDK de .NET 8 para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# 2. Copiamos todo el codigo fuente a la caja
COPY . ./

# 3. Restauramos los paquetes apuntando directo al archivo de la pagina web
RUN dotnet restore LaVeguita.Web/LaVeguita.Web.csproj

# 4. Compilamos la capa Web en modo Release hacia la carpeta de salida
RUN dotnet publish LaVeguita.Web/LaVeguita.Web.csproj -c Release -o out

# 5. Creamos la imagen final ligera para ejecucion
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Configurar puertos de escucha
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "LaVeguita.Web.dll"]