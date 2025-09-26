# TodoApp

Aplicación de lista de tareas para Android desarrollada con .NET 8 y AndroidX siguiendo el patrón MVVM.

## Requisitos previos

- .NET 8 SDK con soporte para `net8.0-android`
- Android SDK y emulador o dispositivo físico
- Visual Studio 2022 o Rider con workloads de Android instalados

## Ejecución

```bash
dotnet restore
msbuild TodoApp.sln /t:Build /p:Configuration=Debug
```

Para desplegar en un emulador o dispositivo:

```bash
dotnet build TodoApp/TodoApp.csproj -t:Install -p:Configuration=Debug
```

## Arquitectura

- **MVVM** mediante `CommunityToolkit.Mvvm`
- Capa de datos con `Microsoft.Data.Sqlite`
- UI basada en AndroidX (`RecyclerView`, `MaterialComponents`)

## Estructura de carpetas

- `Models`: entidades del dominio (`TodoItem`)
- `Services`: repositorio de datos (`TodoRepository`)
- `ViewModels`: lógica de presentación (`TodoListViewModel`)
- `Views`: adaptadores y componentes de UI (`TodoAdapter`)
- `Resources`: layouts y recursos XML

## Funcionalidades

- Crear, completar y eliminar tareas
- Persistencia local mediante SQLite
- Orden automático por estado (pendientes primero)
- Indicador visual cuando no existen tareas
