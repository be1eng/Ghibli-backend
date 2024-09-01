#!/bin/bash
echo "Configurando la aplicación..."

# Ejemplo: Ejecutar migraciones de Entity Framework
dotnet ef migrations add CreateCommentsTable
dotnet ef database update

echo "Configuración completada."
echo "iniciando el Proyecto"

dotnet run