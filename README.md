# ProTask API

API REST para la gestión de proyectos y sus respectivas tareas, construida bajo Clean Architecture en **.NET 10** con **PostgreSQL** (Entity Framework Core / Npgsql).

## Objetivo

Desarrollar una API REST que permita gestionar proyectos y sus respectivas tareas, garantizando validaciones de negocio, manejo consistente de excepciones bajo RFC 7807, persistencia relacional en PostgreSQL, documentación interactiva con OpenAPI/Swagger, pruebas automatizadas y preparación para despliegue automatizado en Azure mediante CI/CD.

---

## Modelo de Datos y Entidades

### Relación

- **1:N (Uno a Muchos):** Un proyecto puede tener muchas tareas, pero una tarea solo puede pertenecer a un proyecto.

### Proyecto (`Project`)

| Campo | Tipo | Descripción |
| :--- | :--- | :--- |
| `Id` | `int` | Identificador único del proyecto |
| `Name` | `string` (máx. 150) | Nombre del proyecto (obligatorio) |
| `Description` | `string?` (máx. 1000) | Descripción detallada del proyecto |
| `StartDate` | `DateTime` (UTC) | Fecha de inicio del proyecto |
| `EndDate` | `DateTime` (UTC) | Fecha de finalización (`EndDate >= StartDate`) |
| `TotalTasks` *(DTO)* | `int` | Total de tareas asociadas al proyecto |
| `CompletedTasks` *(DTO)* | `int` | Cantidad de tareas en estado `Completada` |
| `ProgressPercentage` *(DTO)* | `double` | Porcentaje de avance (`0` a `100`, redondeado a 2 decimales) |

### Tarea (`TaskItem`)

| Campo | Tipo | Descripción |
| :--- | :--- | :--- |
| `Id` | `int` | Identificador único de la tarea |
| `Title` | `string` (máx. 200) | Título de la tarea (obligatorio) |
| `Description` | `string?` (máx. 2000) | Descripción de la tarea |
| `Status` | `TaskItemStatus` | Estado actual: `Pendiente`, `EnProgreso`, `Completada` |
| `Priority` | `TaskPriority` | Prioridad: `Baja`, `Media`, `Alta`, `Critica` |
| `DueDate` | `DateTime` (UTC) | Fecha límite de entrega |
| `ProjectId` | `int` | Identificador del proyecto al que pertenece (clave foránea) |
| `IsOverdue` *(DTO)* | `bool` | Indica si `DueDate < UtcNow` y `Status != Completada` |

---

## Endpoints de la API

### Proyectos (`/api/projects`)

| Método | Ruta | Descripción | Respuestas |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/projects` | Listar todos los proyectos con `%` de avance | `200` |
| `GET` | `/api/projects/{id}` | Obtener un proyecto por ID con `%` de avance | `200`, `404` |
| `GET` | `/api/projects/{id}/tasks` | Consultar todas las tareas de un proyecto | `200`, `404` |
| `POST` | `/api/projects` | Crear un nuevo proyecto | `201`, `400` |
| `PUT` | `/api/projects/{id}` | Actualizar un proyecto existente | `200`, `400`, `404` |
| `DELETE` | `/api/projects/{id}` | Eliminar un proyecto (bloqueado si tiene tareas `Pendiente` o `EnProgreso`) | `204`, `404`, `409` |

### Tareas (`/api/tasks`)

| Método | Ruta | Descripción | Respuestas |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/tasks` | Listar tareas con filtros opcionales `?status=...&priority=...` | `200`, `400` |
| `GET` | `/api/tasks/overdue` | Listar tareas vencidas no completadas | `200` |
| `GET` | `/api/tasks/{id}` | Obtener una tarea por ID | `200`, `404` |
| `POST` | `/api/tasks` | Crear una nueva tarea asociada a un proyecto existente | `201`, `400`, `404` |
| `PUT` | `/api/tasks/{id}` | Actualizar una tarea existente | `200`, `400`, `404` |
| `PATCH` | `/api/tasks/{id}/status` | Cambiar únicamente el estado de una tarea | `200`, `400`, `404` |
| `DELETE` | `/api/tasks/{id}` | Eliminar una tarea | `204`, `404` |

### Diagnóstico y Documentación

- **Health check:** `GET /health`
- **Especificación OpenAPI:** `GET /openapi/v1.json`
- **Swagger UI:** `GET /swagger`

---

## Requerimientos Funcionales

- [x] **CRUD de proyectos y tareas:** Crear, consultar, actualizar y eliminar proyectos y tareas.
- [x] **Consultar tareas por proyecto:** Obtener todas las tareas asociadas a un proyecto específico (`GET /api/projects/{id}/tasks`).
- [x] **Filtrar tareas:** Filtrar tareas por **estado** y/o **prioridad** (`GET /api/tasks?status=&priority=`).
- [x] **Consultar tareas vencidas:** Listar tareas cuya fecha límite haya pasado y que aún no estén completadas (`GET /api/tasks/overdue`).
- [x] **Cambiar el estado de una tarea:** Actualizar el estado de una tarea (`PATCH /api/tasks/{id}/status`).
- [x] **Cálculo de porcentaje de avance:** Calcular el porcentaje de avance de un proyecto con base en sus tareas completadas sobre el total de tareas (`0%` si no tiene tareas).
- [x] **Protección de eliminación:** Impedir la eliminación de proyectos que tengan tareas `Pendiente` o `EnProgreso` (`409 Conflict`).
- [x] **Validaciones y manejo de excepciones:** Validaciones con FluentValidation y middleware global respondiendo bajo **RFC 7807 (`application/problem+json`)** con `traceId`.

---

## Requerimientos Técnicos

- [x] **Base de datos relacional:** Persistencia configurada con **PostgreSQL** (`Npgsql.EntityFrameworkCore.PostgreSQL`), Entity Framework Core 10 y migración inicial `InitialCreate`.
- [x] **Documentación con Swagger:** OpenAPI (`/openapi/v1.json`) e interfaz interactiva de Swagger UI (`/swagger`).
- [x] **Control de versiones en GitHub:** Repositorio configurado con historial de commits y `.gitignore` para `.NET`.
- [x] **Pruebas automatizadas:** 57 pruebas (unitarias, integración con SQLite en memoria y funcionales E2E con `WebApplicationFactory<Program>`).
- [ ] **Despliegue en Azure:** Publicación de la API (Azure App Service) y base de datos PostgreSQL en Microsoft Azure *(pendiente para fase posterior)*.
- [ ] **CI/CD con GitHub Actions:** Pipeline automatizado (`dotnet build`, `dotnet test` y despliegue automático) *(pendiente para fase posterior)*.

---

## Estructura de la Solución

```text
ProTask/
├── ProTask.slnx
├── README.md
├── PLAN_IMPLEMENTACION.md
├── src/
│   ├── ProTask.Domain/           # Entidades, enums, excepciones e interfaces de dominio (0 paquetes externos)
│   ├── ProTask.Application/      # DTOs, validadores FluentValidation, mapeo manual y servicios
│   ├── ProTask.Infrastructure/   # DbContext, configuraciones Fluent API, migraciones PostgreSQL y repositorios
│   └── ProTask.API/              # Controladores, middleware RFC 7807, OpenAPI/Swagger y Program.cs
└── tests/
    └── ProTask.Tests/            # Pruebas unitarias, de integración (SQLite) y funcionales E2E
```

---

## Ejecución Local y Pruebas

### 1. Configurar PostgreSQL local (sin guardar contraseñas en Git)

```bash
dotnet user-secrets init --project src/ProTask.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=protask_dev;Username=postgres;Password=TU_PASSWORD" --project src/ProTask.API
```

### 2. Aplicar la migración inicial

```bash
dotnet ef database update --project src/ProTask.Infrastructure --startup-project src/ProTask.API
```

### 3. Compilar, probar y ejecutar la API

```bash
dotnet build ProTask.slnx --configuration Release
dotnet test ProTask.slnx --configuration Release --no-build
dotnet run --project src/ProTask.API
```

Swagger UI quedará disponible en `http://localhost:5184/swagger`.

---

## Entregables

- **Repositorio de GitHub:** https://github.com/jose2520/ProTask
- **URL de la API desplegada (Azure):** _Pendiente por desplegar en Azure_
- **Documentación Swagger (Azure):** _Pendiente por desplegar en Azure_
- **Evidencia del pipeline CI/CD (GitHub Actions):** _Pendiente por configurar y ejecutar pipeline_
