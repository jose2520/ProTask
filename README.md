<p align="center">
  <img src="docs/assets/protask-banner.svg" alt="ProTask API Banner" width="100%" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/PostgreSQL-EF%20Core%2010-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL" />
  <img src="https://img.shields.io/badge/Architecture-Clean%20Architecture-0EA5E9?style=for-the-badge" alt="Clean Architecture" />
  <img src="https://img.shields.io/badge/OpenAPI-Swagger%20UI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black" alt="Swagger UI" />
  <img src="https://img.shields.io/badge/Tests-96%20passing-10B981?style=for-the-badge&logo=xunit&logoColor=white" alt="Tests 96 passing" />
</p>

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

- **Health check (PostgreSQL + JSON detallado):** `GET /health`
- **Liveness probe (ligero, sin BD):** `GET /health/live`
- **Especificación OpenAPI (enums en texto):** `GET /openapi/v1.json`
- **Swagger UI:** `GET /swagger`

Ejemplo de respuesta de `GET /health`:

```json
{
  "status": "Healthy",
  "totalDurationMs": 12.45,
  "checks": [
    {
      "name": "postgresql",
      "status": "Healthy",
      "description": "Conexión exitosa a PostgreSQL.",
      "durationMs": 11.82
    }
  ]
}
```

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
- [x] **Pruebas automatizadas:** 96 pruebas organizadas en espejo 1 a 1 con `src/` (`Category=Unit` y `Category=Integration`):
  - `ProTask.Domain.Tests` (**15 pruebas** unitarias puras de entidades).
  - `ProTask.Application.Tests` (**44 pruebas** unitarias de servicios, validadores y mapeos).
  - `ProTask.Infrastructure.Tests` (**15 pruebas**: 2 DI + 13 de integración contra PostgreSQL real en `protask_infra_test`).
  - `ProTask.API.Tests` (**22 pruebas**: 11 unitarias de controladores + 11 de integración HTTP contra PostgreSQL real en `protask_api_test`).
- [ ] **Despliegue en Azure:** Publicación de la API (Azure App Service) y base de datos PostgreSQL en Microsoft Azure *(pendiente para fase posterior)*.
- [ ] **CI/CD con GitHub Actions:** Pipeline automatizado (`dotnet build`, `dotnet test` y despliegue automático) *(pendiente para fase posterior)*.

---

## Estructura de la Solución

```text
ProTask/
├── .gitignore
├── LICENSE
├── PLAN_IMPLEMENTACION.md
├── ProTask.slnx
├── README.md
├── docs/
│   └── assets/
│       └── protask-banner.svg
├── src/
│   ├── ProTask.Domain/
│   │   ├── ProTask.Domain.csproj
│   │   ├── Abstractions/
│   │   │   ├── IProjectRepository.cs
│   │   │   └── ITaskRepository.cs
│   │   ├── Entities/
│   │   │   ├── Project.cs
│   │   │   └── TaskItem.cs
│   │   ├── Enums/
│   │   │   ├── TaskItemStatus.cs
│   │   │   └── TaskPriority.cs
│   │   └── Exceptions/
│   │       ├── BusinessRuleException.cs
│   │       └── DomainException.cs
│   ├── ProTask.Application/
│   │   ├── ProTask.Application.csproj
│   │   ├── DependencyInjection.cs
│   │   ├── DTOs/
│   │   │   ├── Projects/
│   │   │   │   ├── CreateProjectDto.cs
│   │   │   │   ├── ProjectDto.cs
│   │   │   │   └── UpdateProjectDto.cs
│   │   │   └── Tasks/
│   │   │       ├── CreateTaskItemDto.cs
│   │   │       ├── TaskItemDto.cs
│   │   │       ├── UpdateTaskItemDto.cs
│   │   │       └── UpdateTaskStatusDto.cs
│   │   ├── Exceptions/
│   │   │   └── NotFoundException.cs
│   │   ├── Interfaces/
│   │   │   ├── IProjectService.cs
│   │   │   └── ITaskService.cs
│   │   ├── Mappings/
│   │   │   └── MappingExtensions.cs
│   │   ├── Services/
│   │   │   ├── ProjectService.cs
│   │   │   └── TaskService.cs
│   │   └── Validators/
│   │       ├── CreateProjectValidator.cs
│   │       ├── CreateTaskItemValidator.cs
│   │       ├── UpdateProjectValidator.cs
│   │       ├── UpdateTaskItemValidator.cs
│   │       └── UpdateTaskStatusValidator.cs
│   ├── ProTask.Infrastructure/
│   │   ├── ProTask.Infrastructure.csproj
│   │   ├── DependencyInjection.cs
│   │   └── Persistence/
│   │       ├── AppDbContext.cs
│   │       ├── DatabaseHealthCheck.cs
│   │       ├── Configurations/
│   │       │   ├── ProjectConfiguration.cs
│   │       │   └── TaskItemConfiguration.cs
│   │       ├── Migrations/
│   │       │   ├── 20261125223220_InitialCreate.cs
│   │       │   ├── 20261125223220_InitialCreate.Designer.cs
│   │       │   └── AppDbContextModelSnapshot.cs
│   │       └── Repositories/
│   │           ├── ProjectRepository.cs
│   │           └── TaskRepository.cs
│   └── ProTask.API/
│       ├── ProTask.API.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Controllers/
│       │   ├── ProjectsController.cs
│       │   └── TasksController.cs
│       ├── Middleware/
│       │   └── ExceptionHandlingMiddleware.cs
│       └── Properties/
│           └── launchSettings.json
└── tests/
    ├── ProTask.Domain.Tests/
    │   ├── ProTask.Domain.Tests.csproj
    │   └── Entities/
    │       ├── ProjectTests.cs
    │       └── TaskItemTests.cs
    ├── ProTask.Application.Tests/
    │   ├── ProTask.Application.Tests.csproj
    │   ├── Mappings/
    │   │   └── MappingExtensionsTests.cs
    │   ├── Services/
    │   │   ├── ProjectServiceTests.cs
    │   │   └── TaskServiceTests.cs
    │   └── Validators/
    │       ├── CreateProjectValidatorTests.cs
    │       ├── CreateTaskItemValidatorTests.cs
    │       ├── UpdateProjectValidatorTests.cs
    │       ├── UpdateTaskItemValidatorTests.cs
    │       └── UpdateTaskStatusValidatorTests.cs
    ├── ProTask.Infrastructure.Tests/
    │   ├── ProTask.Infrastructure.Tests.csproj
    │   ├── DependencyInjectionTests.cs
    │   ├── Fixtures/
    │   │   ├── InfrastructureCollection.cs
    │   │   └── PostgreSqlFixture.cs
    │   └── Persistence/
    │       ├── MigrationTests.cs
    │       └── Repositories/
    │           ├── ProjectRepositoryTests.cs
    │           └── TaskRepositoryTests.cs
    └── ProTask.API.Tests/
        ├── ProTask.API.Tests.csproj
        ├── Controllers/
        │   ├── ProjectsControllerTests.cs
        │   └── TasksControllerTests.cs
        ├── Endpoints/
        │   ├── HealthAndOpenApiEndpointsTests.cs
        │   ├── ProjectsEndpointsTests.cs
        │   └── TasksEndpointsTests.cs
        └── Fixtures/
            ├── ApiCollection.cs
            ├── ApiPostgreSqlFixture.cs
            └── ProTaskApiFactory.cs
```

### Responsabilidad de cada Carpeta y Archivo

#### Raíz del Repositorio y Documentación

| Carpeta / Archivo | Responsabilidad |
| :--- | :--- |
| **`ProTask.slnx`** | Archivo de solución .NET 10 que agrupa los 4 proyectos de código fuente (`/src/`) y los 4 proyectos de pruebas (`/tests/`). |
| **`README.md`** | Documentación principal del proyecto: modelo de datos, endpoints, arquitectura, ejecución local, pruebas y entregables. |
| **`PLAN_IMPLEMENTACION.md`** | Hoja de ruta por fases con el progreso completado y los pasos pendientes de despliegue en Azure y CI/CD. |
| **`.gitignore`** | Reglas de exclusión de Git para binarios (`bin/`, `obj/`), archivos de IDEs y configuraciones locales sensibles. |
| **`LICENSE`** | Licencia de uso del repositorio. |
| **`docs/assets/protask-banner.svg`** | Banner vectorial corporativo mostrado en el encabezado del `README.md`. |

#### Capa 1: `src/ProTask.Domain` *(0 paquetes NuGet, 0 dependencias de proyecto)*

| Carpeta / Archivo | Responsabilidad |
| :--- | :--- |
| **`ProTask.Domain.csproj`** | Biblioteca de clases .NET 10 pura que protege las reglas del negocio contra cualquier acoplamiento tecnológico. |
| **`Abstractions/`** | Contratos que invierten la dependencia hacia el dominio (DIP), implementados por `ProTask.Infrastructure`. |
| ↳ `IProjectRepository.cs` | Contrato de persistencia para `Project` (CRUD, carga con tareas, verificación de existencia y borrado atómico condicionado). |
| ↳ `ITaskRepository.cs` | Contrato de persistencia para `TaskItem` (CRUD, consulta por proyecto, filtros por estado/prioridad y tareas vencidas no completadas). |
| **`Entities/`** | Entidades de dominio encapsuladas (*Persistence Ignorance*: POCOs puros sin atributos de EF Core ni dependencias de infraestructura). |
| ↳ `Project.cs` | Entidad raíz de proyecto: encapsula `Name`, `Description`, fechas UTC (`StartDate`/`EndDate`) y la colección `_tasks` (`IReadOnlyCollection<TaskItem>`), calcula el `%` de avance, valida `EndDate >= StartDate` y aplica la regla de bloqueo de eliminación (`EnsureCanBeDeleted`). |
| ↳ `TaskItem.cs` | Entidad de tarea: encapsula `Title`, `Description`, `Status`, `Priority`, `DueDate` y `ProjectId`; valida cambios de estado (`ChangeStatus`), actualiza datos (`UpdateDetails`) y evalúa vencimiento frente a una fecha UTC explícita (`IsOverdue(DateTime)`). |
| **`Enums/`** | Enumeraciones fuertemente tipadas del negocio. |
| ↳ `TaskItemStatus.cs` | Estados posibles de una tarea: `Pendiente`, `EnProgreso`, `Completada`. |
| ↳ `TaskPriority.cs` | Niveles de prioridad de una tarea: `Baja`, `Media`, `Alta`, `Critica`. |
| **`Exceptions/`** | Excepciones propias lanzadas por el dominio cuando se viola una invariante de negocio. |
| ↳ `DomainException.cs` | Clase base abstracta para excepciones de dominio con código identificador (`ErrorCode`). |
| ↳ `BusinessRuleException.cs` | Excepción para violaciones de reglas de negocio (ej. eliminar proyecto con tareas incompletas o fechas invertidas). |

#### Capa 2: `src/ProTask.Application` *(referencia únicamente a `ProTask.Domain`)*

| Carpeta / Archivo | Responsabilidad |
| :--- | :--- |
| **`ProTask.Application.csproj`** | Proyecto de casos de uso y orquestación; incluye `FluentValidation` y abstracciones de inyección de dependencias. |
| **`DependencyInjection.cs`** | Método de extensión `AddApplication()` que registra `TimeProvider.System`, validadores de FluentValidation y los servicios de aplicación. |
| **`DTOs/Projects/`** | Contratos de entrada y salida de proyectos para aislar las entidades del dominio de la capa HTTP. |
| ↳ `ProjectDto.cs` | DTO de lectura de proyecto con métricas calculadas (`TotalTasks`, `CompletedTasks`, `ProgressPercentage`). |
| ↳ `CreateProjectDto.cs` | DTO de entrada para la creación de un nuevo proyecto. |
| ↳ `UpdateProjectDto.cs` | DTO de entrada para la actualización completa de un proyecto existente. |
| **`DTOs/Tasks/`** | Contratos de entrada y salida de tareas. |
| ↳ `TaskItemDto.cs` | DTO de lectura de tarea incluyendo el indicador calculado `IsOverdue`. |
| ↳ `CreateTaskItemDto.cs` | DTO de entrada para crear una tarea asociada a un `ProjectId`. |
| ↳ `UpdateTaskItemDto.cs` | DTO de entrada para actualizar título, descripción, estado, prioridad y fecha límite de una tarea. |
| ↳ `UpdateTaskStatusDto.cs` | DTO de entrada específico para el cambio parcial de estado (`PATCH /api/tasks/{id}/status`). |
| **`Exceptions/`** | Excepciones de flujo de casos de uso propias de la capa de aplicación. |
| ↳ `NotFoundException.cs` | Excepción lanzada cuando un proyecto o tarea solicitado no existe (`ErrorCode = "resource_not_found"`, mapeada a HTTP `404`). |
| **`Interfaces/`** | Contratos de servicios de aplicación consumidos por los controladores de `ProTask.API`. |
| ↳ `IProjectService.cs` | Define los casos de uso de proyectos (CRUD completo). |
| ↳ `ITaskService.cs` | Define los casos de uso de tareas (CRUD, filtros por estado/prioridad, consulta por proyecto, vencidas y cambio de estado). |
| **`Mappings/`** | Transformaciones explícitas entre entidades de dominio y DTOs (sin AutoMapper). |
| ↳ `MappingExtensions.cs` | Métodos de extensión estáticos (`ToDto`, `ToEntity`, `ApplyUpdate`) para `Project` y `TaskItem`. |
| **`Services/`** | Implementaciones de los casos de uso; orquestan validadores, entidades, `TimeProvider` y repositorios. |
| ↳ `ProjectService.cs` | Implementa `IProjectService`: valida DTOs, calcula avances al mapear y coordina el borrado atómico lanzando `NotFoundException` si no existe. |
| ↳ `TaskService.cs` | Implementa `ITaskService`: valida filtros/DTOs, verifica existencia del proyecto padre, evalúa tareas vencidas usando `TimeProvider` y gestiona actualizaciones parciales de estado. |
| **`Validators/`** | Reglas de validación formal de DTOs implementadas con `FluentValidation`. |
| ↳ `CreateProjectValidator.cs` | Valida campos obligatorios, longitudes máximas y consistencia `EndDate >= StartDate` al crear proyectos. |
| ↳ `UpdateProjectValidator.cs` | Valida campos obligatorios, longitudes máximas y consistencia de fechas al actualizar proyectos. |
| ↳ `CreateTaskItemValidator.cs` | Valida título, longitud máxima, prioridad válida, fecha límite y `ProjectId > 0`. |
| ↳ `UpdateTaskItemValidator.cs` | Valida título, descripción, obligatoriedad/validez de `Status` y `Priority`, y fecha límite al actualizar tareas. |
| ↳ `UpdateTaskStatusValidator.cs` | Valida que el nuevo estado no sea nulo y pertenezca al enum `TaskItemStatus`. |

#### Capa 3: `src/ProTask.Infrastructure` *(referencia únicamente a `ProTask.Domain`)*

| Carpeta / Archivo | Responsabilidad |
| :--- | :--- |
| **`ProTask.Infrastructure.csproj`** | Proyecto de adaptadores tecnológicos (EF Core 10, Npgsql PostgreSQL y Health Checks); desacoplado de `ProTask.Application` y `ProTask.API`. |
| **`DependencyInjection.cs`** | Método de extensión `AddInfrastructure(IConfiguration)` que configura `AppDbContext` con `UseNpgsql`, reintentos automáticos (`EnableRetryOnFailure`), timeout de 30s, repositorios y el health check `"postgresql"`. |
| **`Persistence/`** | Encapsula todo el acceso a datos relacional con Entity Framework Core y PostgreSQL. |
| ↳ `AppDbContext.cs` | Contexto EF Core con `DbSet<Project>` y `DbSet<TaskItem>`; aplica automáticamente todas las configuraciones Fluent API del ensamblado. |
| ↳ `DatabaseHealthCheck.cs` | Implementa `IHealthCheck` verificando la conectividad real con PostgreSQL mediante `CanConnectAsync`. |
| **`Persistence/Configurations/`** | Mapeo relacional Fluent API (`IEntityTypeConfiguration<T>`) separado del dominio. |
| ↳ `ProjectConfiguration.cs` | Configura la tabla `projects`, restricciones de longitud, relación 1:N con borrado en cascada y acceso por *backing field* (`_tasks`) en la navegación `Tasks`. |
| ↳ `TaskItemConfiguration.cs` | Configura la tabla `tasks`, conversión de enums `Status` y `Priority` a texto e índices optimizados (`ProjectId`, `(Status, Priority)`, `(Status, DueDate)`, `Priority`). |
| **`Persistence/Migrations/`** | Historial Code-First de migraciones de esquema en PostgreSQL. |
| ↳ `20261125223220_InitialCreate.cs` | Migración inicial que crea las tablas `projects` y `tasks`, la clave foránea en cascada y los 4 índices. |
| ↳ `20261125223220_InitialCreate.Designer.cs` | Metadatos autogenerados del modelo asociado a la migración `InitialCreate`. |
| ↳ `AppDbContextModelSnapshot.cs` | Snapshot actual del modelo EF Core empleado para detectar cambios pendientes en futuras migraciones. |
| **`Persistence/Repositories/`** | Implementaciones concretas de las abstracciones definidas en `ProTask.Domain/Abstractions/`. |
| ↳ `ProjectRepository.cs` | Implementa `IProjectRepository` con `AsNoTracking()` en lectura, `.Include(p => p.Tasks)` y borrado atómico bajo transacción `Serializable` envuelta en `CreateExecutionStrategy()` delegando la regla a `project.EnsureCanBeDeleted(...)`. |
| ↳ `TaskRepository.cs` | Implementa `ITaskRepository` con filtros dinámicos por `status`/`priority`, consulta por `projectId` y consulta indexada de tareas vencidas no completadas. |

#### Capa 4: `src/ProTask.API` *(Presentación HTTP y Composition Root)*

| Carpeta / Archivo | Responsabilidad |
| :--- | :--- |
| **`ProTask.API.csproj`** | Proyecto web ASP.NET Core (`Microsoft.NET.Sdk.Web`) con OpenAPI, Swagger UI, EF Core Design y `UserSecretsId` local. |
| **`Program.cs`** | *Composition Root* y pipeline HTTP: registra `AddApplication()` y `AddInfrastructure()`, serialización de enums en texto, respuestas `InvalidModelStateResponseFactory` RFC 7807, OpenAPI + Swagger UI, `ExceptionHandlingMiddleware`, endpoints `/health` y `/health/live` con `.ShortCircuit()` y `public partial class Program { }` para pruebas funcionales. |
| **`appsettings.json`** | Configuración base segura (sin contraseñas reales; `Swagger:Enabled` en `false` por defecto). |
| **`appsettings.Development.json`** | Sobrescritura para desarrollo local (habilita Swagger y define host/base local sin contraseña). |
| **`Controllers/`** | Controladores delgados (*Thin Controllers*): solo manejan HTTP y delegan la lógica a los servicios de `ProTask.Application`. |
| ↳ `ProjectsController.cs` | Expone `/api/projects` (`GET`, `GET /{id}`, `GET /{id}/tasks`, `POST`, `PUT /{id}`, `DELETE /{id}`). |
| ↳ `TasksController.cs` | Expone `/api/tasks` (`GET ?status=&priority=`, `GET /overdue`, `GET /{id}`, `POST`, `PUT /{id}`, `PATCH /{id}/status`, `DELETE /{id}`). |
| **`Middleware/`** | Componentes transversales del pipeline HTTP. |
| ↳ `ExceptionHandlingMiddleware.cs` | Captura excepciones globalmente y las traduce a respuestas **RFC 7807 (`application/problem+json`)** con `traceId` (`400`, `404`, `409`, `500`), ocultando detalles internos fuera de `Development`. |
| **`Properties/launchSettings.json`** | Perfiles de ejecución local de Kestrel (`http://localhost:5184` y `https://localhost:7078`). |

#### Suite de Pruebas: `tests/` *(4 proyectos xUnit en espejo 1:1 con `src/`)*

| Proyecto / Carpeta / Archivo | Responsabilidad |
| :--- | :--- |
| **`ProTask.Domain.Tests/`** | Pruebas unitarias puras de dominio (solo referencia `ProTask.Domain`). |
| ↳ `ProTask.Domain.Tests.csproj` | Proyecto de pruebas xUnit para la capa de dominio. |
| ↳ `Entities/ProjectTests.cs` | Prueba cálculo de `%` de avance (0%, 50%, redondeo, 100%), bloqueo de borrado y normalización/validación de fechas UTC. |
| ↳ `Entities/TaskItemTests.cs` | Prueba valores por defecto, evaluación de vencimiento (`IsOverdue`) y transiciones de estado válidas/inválidas. |
| **`ProTask.Application.Tests/`** | Pruebas unitarias de servicios, mapeos y validadores con `Moq` (sin base de datos). |
| ↳ `ProTask.Application.Tests.csproj` | Proyecto de pruebas xUnit + `Moq` + `FluentValidation`. |
| ↳ `Mappings/MappingExtensionsTests.cs` | Verifica conversiones DTO ↔ Entidad, normalización de textos/fechas UTC y cálculo de métricas/vencimiento. |
| ↳ `Services/ProjectServiceTests.cs` | Prueba casos de uso CRUD de `ProjectService`, propagación de `BusinessRuleException` y lanzamiento de `NotFoundException`. |
| ↳ `Services/TaskServiceTests.cs` | Prueba filtros, tareas vencidas con `TimeProvider` fijo, validación de `ProjectId` y cambios de estado en `TaskService`. |
| ↳ `Validators/CreateProjectValidatorTests.cs` | Verifica reglas de creación de proyecto (nombre vacío, longitudes, fechas invertidas y por defecto). |
| ↳ `Validators/UpdateProjectValidatorTests.cs` | Verifica reglas de actualización de proyecto. |
| ↳ `Validators/CreateTaskItemValidatorTests.cs` | Verifica reglas de creación de tarea (título, descripción, prioridad, fecha y `ProjectId`). |
| ↳ `Validators/UpdateTaskItemValidatorTests.cs` | Verifica reglas de actualización completa de tarea (incluyendo enums nulos/inválidos). |
| ↳ `Validators/UpdateTaskStatusValidatorTests.cs` | Verifica validación del DTO de cambio de estado. |
| **`ProTask.Infrastructure.Tests/`** | Pruebas de DI (Unit) y persistencia contra PostgreSQL real (`protask_infra_test`, Integration). |
| ↳ `ProTask.Infrastructure.Tests.csproj` | Proyecto de pruebas xUnit para la capa de infraestructura. |
| ↳ `DependencyInjectionTests.cs` | Verifica el registro de servicios, validadores, `AppDbContext`, repositorios y el health check `"postgresql"`. |
| ↳ `Fixtures/InfrastructureCollection.cs` | Colección xUnit compartida para secuenciar pruebas contra la base de pruebas. |
| ↳ `Fixtures/PostgreSqlFixture.cs` | Resuelve la cadena de conexión exigiendo el sufijo `_test`, ejecuta migraciones y resetea tablas (`TRUNCATE ... RESTART IDENTITY CASCADE`). |
| ↳ `Persistence/MigrationTests.cs` | Comprueba aplicación de `InitialCreate`, 0 migraciones pendientes, restricciones `varchar(150)` y almacenamiento UTC en PostgreSQL. |
| ↳ `Persistence/Repositories/ProjectRepositoryTests.cs` | Prueba persistencia 1:N con *backing field*, actualización y borrado atómico `Serializable` (bloqueo con tareas activas, cascada con completadas y `false` si no existe). |
| ↳ `Persistence/Repositories/TaskRepositoryTests.cs` | Prueba consultas por proyecto, filtros combinables, tareas vencidas no completadas y CRUD completo en PostgreSQL. |
| **`ProTask.API.Tests/`** | Pruebas unitarias de controladores + pruebas funcionales HTTP E2E contra `protask_api_test`. |
| ↳ `ProTask.API.Tests.csproj` | Proyecto de pruebas xUnit + `Moq` + `Microsoft.AspNetCore.Mvc.Testing`. |
| ↳ `Controllers/ProjectsControllerTests.cs` | Pruebas unitarias de los códigos de respuesta HTTP y delegación de `ProjectsController`. |
| ↳ `Controllers/TasksControllerTests.cs` | Pruebas unitarias de filtros, vencidas, creación, cambio de estado y borrado en `TasksController`. |
| ↳ `Endpoints/ProjectsEndpointsTests.cs` | Pruebas funcionales HTTP end-to-end del ciclo de vida de proyectos, cálculo de `%`, bloqueo `409` RFC 7807 y validaciones `400`. |
| ↳ `Endpoints/TasksEndpointsTests.cs` | Pruebas funcionales HTTP de filtros `?status=&priority=`, `/overdue`, CRUD, protección contra enums omitidos y errores controlados. |
| ↳ `Endpoints/HealthAndOpenApiEndpointsTests.cs` | Verifica `/health` (JSON + PostgreSQL Healthy), `/health/live`, `/swagger/index.html` y el esquema `/openapi/v1.json` con enums en texto. |
| ↳ `Fixtures/ApiCollection.cs` | Colección xUnit para las pruebas funcionales HTTP de la API. |
| ↳ `Fixtures/ApiPostgreSqlFixture.cs` | Prepara la base `protask_api_test`, aplica migraciones y gestiona el ciclo de vida de `ProTaskApiFactory`. |
| ↳ `Fixtures/ProTaskApiFactory.cs` | Subclase de `WebApplicationFactory<Program>` en entorno `Testing` apuntando a `protask_api_test` con Swagger habilitado. |

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
dotnet run --project src/ProTask.API
```

Swagger UI quedará disponible en `http://localhost:5184/swagger`.

---

## Pruebas Automatizadas (Unitarias vs. Integración)

Cada proyecto en `tests/` espeja 1 a 1 su capa equivalente en `src/` y cada clase lleva el atributo `[Trait("Category", "Unit")]` o `[Trait("Category", "Integration")]`:

| Proyecto | Carpetas / Archivos | Categoría | Casos | Base de datos |
| :--- | :--- | :---: | :---: | :--- |
| `ProTask.Domain.Tests` | `Entities/` | `Unit` | 15 | No |
| `ProTask.Application.Tests` | `Services/`, `Validators/`, `Mappings/` | `Unit` | 44 | No (`Moq`) |
| `ProTask.Infrastructure.Tests` | `DependencyInjectionTests.cs` | `Unit` | 2 | No |
| `ProTask.Infrastructure.Tests` | `Persistence/` (`MigrationTests`, `Repositories/`) | `Integration` | 13 | PostgreSQL (`protask_infra_test`) |
| `ProTask.API.Tests` | `Controllers/` | `Unit` | 11 | No (`Moq`) |
| `ProTask.API.Tests` | `Endpoints/` | `Integration` | 11 | PostgreSQL (`protask_api_test`) |

- **Total:** **96 casos** (**72 `Unit`** + **24 `Integration`**).
- Las pruebas de integración toman automáticamente la conexión desde `PROTASK_TEST_CONNECTION` (o tus `user-secrets` de `ProTask.API`) y siempre apuntan a bases terminadas en `_test`, protegiendo `protask_dev`.

### Comandos para ejecutarlas

```bash
# Solo pruebas unitarias (rápidas, sin PostgreSQL)
dotnet test ProTask.slnx --configuration Release --no-build --filter Category=Unit

# Solo pruebas de integración (contra PostgreSQL real)
dotnet test ProTask.slnx --configuration Release --no-build --filter Category=Integration

# Por proyecto específico
dotnet test tests/ProTask.Domain.Tests
dotnet test tests/ProTask.Application.Tests
dotnet test tests/ProTask.Infrastructure.Tests
dotnet test tests/ProTask.API.Tests

# Todas las pruebas en paralelo
dotnet test ProTask.slnx --configuration Release --no-build
```

> En **JetBrains Rider**, puedes abrir la ventana **Unit Tests** y activar **Group by → Categories** para ver separadas visualmente las carpetas `Unit` e `Integration`.

---

## Entregables

- **Repositorio de GitHub:** https://github.com/jose2520/ProTask
- **URL de la API desplegada (Azure):** _Pendiente por desplegar en Azure_
- **Documentación Swagger (Azure):** _Pendiente por desplegar en Azure_
- **Evidencia del pipeline CI/CD (GitHub Actions):** _Pendiente por configurar y ejecutar pipeline_
