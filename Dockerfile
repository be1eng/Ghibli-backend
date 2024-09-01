# Utilizar la imagen base del SDK de .NET para desarrollo
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development

# Establecer el directorio de trabajo dentro del contenedor
WORKDIR /app

# Instalar la herramienta dotnet-ef globalmente
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Copiar los archivos csproj y restaurar las dependencias
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto de los archivos del proyecto
COPY . .

# Exponer el puerto en el que la aplicación está escuchando
EXPOSE 5071

# Configurar el entorno para que use el puerto 5000
ENV ASPNETCORE_URLS=http://*:5071

# Configurar el comando para iniciar la aplicación con dotnet run
ENTRYPOINT ["dotnet", "run"]
