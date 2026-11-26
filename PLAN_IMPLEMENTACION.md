aplicar lo mas recomendado # Plan de Implementación — ProTask API

Guía paso a paso para completar el proyecto **ProTask API** desde la estructura Clean Architecture actual hasta el despliegue en Azure con CI/CD en GitHub Actions.

**Especificación base:** [README.md](./README.md)  
**Solución:** `ProTask.slnx` (.NET 10 — Clean Architecture)

---

## Estado Inicial Ya Completado

- [x] Solución única limpia en `ProTask.slnx` con carpetas virtuales `/src/` y `/tests/`.
- [x] Referencias entre proyectos (`<ProjectReference>`) respetando la Regla de Dependencia (`Domain` ← `Application` ← `Infrastructure` ← `API` ← `Tests`).
- [x] Estructura depurada y alineada al alcance del `README.md`.
- [x] Documentación inicial del proyecto en `README.md`.

---

## Fase 1 — Configuración Inicial (Git y Paquetes NuGet)

### 1.1 Inicializar Git y `.gitignore`
- [x] Generar un `.gitignore` estándar para .NET (`bin/`, `obj/`, `.vs/`, `.idea/`, `appsettings.*.local.json`, secretos):
  ```bash
  dotnet new gitignore --force
  ```
- [x] Inicializar repositorio local y hacer el commit base:
  ```bash
  git init
  git branch -M main
  git add .
  git commit -m "chore: initial clean architecture structure"
  ```

### 1.2 Elegir motor de base de datos relacional
- [x] Definir el motor relacional a usar: **PostgreSQL** con `Npgsql.EntityFrameworkCore.PostgreSQL` *(despliegue en Azure diferido para fases posteriores)*.

### 1.3 Instalar paquetes NuGet por capa
- [x] **`ProTask.Domain`**: mantener **0 paquetes externos** (dominio puro).
- [x] **`ProTask.Application`**:
  - `FluentValidation`
  - `FluentValidation.DependencyInjectionExtensions`
  - Mapeo manual explícito mediante métodos de extensión (sin `AutoMapper`)
  - `Microsoft.Extensions.DependencyInjection.Abstractions`
- [x] **`ProTask.Infrastructure`**:
  - `Microsoft.EntityFrameworkCore`
  - `Npgsql.EntityFrameworkCore.PostgreSQL`
  - `Microsoft.EntityFrameworkCore.Tools`
  - `Microsoft.Extensions.Configuration.Abstractions`
- [x] **`ProTask.API`**:
  - `Microsoft.AspNetCore.OpenApi` + `Swashbuckle.AspNetCore.SwaggerUI` (exponiendo `/swagger` desde `/openapi/v1.json`)
  - `Microsoft.EntityFrameworkCore.Design` *(necesario para ejecutar `dotnet ef migrations`)*
- [x] **Proyectos de pruebas en espejo 1 a 1 con `src/`:**
  - `ProTask.Domain.Tests`: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` *(solo referencia `ProTask.Domain`)*.
  - `ProTask.Application.Tests`: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Moq`, `FluentValidation`.
  - `ProTask.Infrastructure.Tests`: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` *(contra PostgreSQL real en `protask_infra_test`)*.
  - `ProTask.API.Tests`: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Moq`, `Microsoft.AspNetCore.Mvc.Testing` *(controladores + HTTP E2E contra `protask_api_test`)*.

---

## Fase 2 — Capa de Dominio (`src/ProTask.Domain`)

> **Regla de oro:** Sin atributos de EF Core, sin referencias a ASP.NET Core y sin dependencias externas.

- [x] **Enums:**
  - `Enums/TaskItemStatus.cs`: `Pendiente`, `EnProgreso`, `Completada`.
  - `Enums/TaskPriority.cs`: `Baja`, `Media`, `Alta`, `Critica`.
- [x] **Excepciones de dominio** (crear en `src/ProTask.Domain/Exceptions/`):
  - `DomainException.cs` (base)
  - `NotFoundException.cs` (recurso no encontrado → HTTP `404`)
  - `BusinessRuleException.cs` (violación de regla de negocio, ej. eliminar proyecto con tareas pendientes o en progreso → HTTP `409`)
- [x] **Entidades y relación 1:N:**
  - `Entities/Project.cs`:
    - Propiedades: `Id`, `Name`, `Description`, `StartDate`, `EndDate`, colección `ICollection<TaskItem> Tasks`.
    - Lógica de dominio: método para calcular porcentaje de avance (`0%` si no tiene tareas; `(completadas / total) * 100` redondeado a 2 decimales), normalización UTC y validación para impedir borrado si existen tareas `Pendiente` o `EnProgreso` (`Tasks.Any(t => t.Status != TaskItemStatus.Completada)`).
  - `Entities/TaskItem.cs`:
    - Propiedades: `Id`, `Title`, `Description`, `Status` (por defecto `Pendiente`), `Priority`, `DueDate`, `ProjectId`, navegación `Project`.
    - Lógica de dominio: método para cambiar estado y método para saber si está vencida (`DueDate < DateTime.UtcNow && Status != TaskItemStatus.Completada`).
- [x] **Contratos de repositorios:**
  - `Interfaces/IProjectRepository.cs`: `GetAllAsync`, `GetByIdAsync`, `GetByIdWithTasksAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `ExistsAsync`.
  - `Interfaces/ITaskRepository.cs`: `GetAllAsync`, `GetByIdAsync`, `GetByProjectIdAsync`, `GetFilteredAsync(status?, priority?)`, `GetOverdueIncompleteAsync(DateTime referenceUtc)`, `AddAsync`, `UpdateAsync`, `DeleteAsync`.

---

## Fase 3 — Capa de Aplicación (`src/ProTask.Application`)

- [x] **DTOs de Proyectos (`DTOs/Projects/`):**
  - `ProjectDto.cs`: `Id`, `Name`, `Description`, `StartDate`, `EndDate`, `TotalTasks`, `CompletedTasks`, `ProgressPercentage`.
  - `CreateProjectDto.cs`: `Name`, `Description`, `StartDate`, `EndDate`.
  - `UpdateProjectDto.cs`: `Name`, `Description`, `StartDate`, `EndDate`.
- [x] **DTOs de Tareas (`DTOs/Tasks/`):**
  - `TaskItemDto.cs`: `Id`, `Title`, `Description`, `Status`, `Priority`, `DueDate`, `ProjectId`, `IsOverdue`.
  - `CreateTaskItemDto.cs`: `Title`, `Description`, `Priority`, `DueDate`, `ProjectId`.
  - `UpdateTaskItemDto.cs`: `Title`, `Description`, `Status`, `Priority`, `DueDate`.
  - `UpdateTaskStatusDto.cs`: `Status`.
- [x] **Validadores (`Validators/`):**
  - `CreateProjectValidator.cs`: nombre requerido y longitud máxima, `EndDate >= StartDate` (normalizado a UTC).
  - `UpdateProjectValidator.cs`: mismas reglas de consistencia de nombre y fechas.
  - `CreateTaskItemValidator.cs`: título requerido, `ProjectId > 0`, `Priority` válida, `DueDate` válida.
  - `UpdateTaskItemValidator.cs`: título requerido, `Status` y `Priority` válidas, `DueDate` válida.
  - `UpdateTaskStatusValidator.cs`: validar que `Status` pertenezca al enum `TaskItemStatus`.
- [x] **Mapeos (`Mappings/MappingProfile.cs`):**
  - Mapeo manual por métodos de extensión entre `Project` ↔ `ProjectDto`, `CreateProjectDto`, `UpdateProjectDto` y `TaskItem` ↔ `TaskItemDto`, `CreateTaskItemDto`, `UpdateTaskItemDto`.
- [x] **Interfaces y Servicios (`Interfaces/` y `Services/`):**
  - `IProjectService.cs` y `ProjectService.cs`:
    - CRUD completo de proyectos.
    - Cálculo de porcentaje de avance al devolver proyectos.
    - **Regla crítica:** en `DeleteAsync`, cargar el proyecto con sus tareas; si no existe lanzar `NotFoundException`; si tiene tareas en estado `Pendiente` o `EnProgreso`, lanzar `BusinessRuleException` e impedir la eliminación.
  - `ITaskService.cs` y `TaskService.cs`:
    - CRUD completo de tareas (verificando que el `ProjectId` exista antes de crear).
    - Consultar tareas por proyecto (verificando que el proyecto exista).
    - Filtrar tareas por `status` y/o `priority`.
    - Consultar tareas vencidas que no estén completadas (`DueDate < DateTime.UtcNow` y `Status != Completada`).
    - Cambiar únicamente el estado de una tarea (`UpdateStatusAsync`).
- [x] **Inyección de dependencias (`DependencyInjection.cs`):**
  - Método de extensión `AddApplication()` registrando `TimeProvider`, validadores y servicios.

---

## Fase 4 — Capa de Infraestructura (`src/ProTask.Infrastructure`)

- [x] **DbContext (`Data/ApplicationDbContext.cs`):**
  - Definir `DbSet<Project> Projects` y `DbSet<TaskItem> Tasks`.
  - Aplicar configuraciones automáticamente con `modelBuilder.ApplyConfigurationsFromAssembly(...)`.
- [x] **Configuraciones Fluent API (`Data/Configurations/`):**
  - `ProjectConfiguration.cs`: tabla `projects`, clave primaria, longitudes máximas, campos requeridos y relación `HasMany(p => p.Tasks).WithOne(t => t.Project).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade)`.
  - `TaskItemConfiguration.cs`: tabla `tasks`, clave primaria, longitudes, conversión de enums a texto e índices en `ProjectId`, `(Status, Priority)`, `(Status, DueDate)` y `Priority`.
- [x] **Repositorios (`Repositories/`):**
  - `ProjectRepository.cs` implementando `IProjectRepository` (con `Include(p => p.Tasks)` y `AsNoTracking()` en lectura).
  - `TaskRepository.cs` implementando `ITaskRepository` (filtros combinables por `status` y `priority`, consulta por `projectId` y vencidas no completadas).
- [x] **Inyección de dependencias (`DependencyInjection.cs`):**
  - Método de extensión `AddInfrastructure(IConfiguration configuration)` registrando `ApplicationDbContext` con `UseNpgsql`, reintentos automáticos, timeout de 30s y ambos repositorios.
- [x] **Migración inicial (`Data/Migrations/`):**
  - [x] Generar migración inicial `InitialCreate` para PostgreSQL:
    ```bash
    dotnet ef migrations add InitialCreate --project src/ProTask.Infrastructure --startup-project src/ProTask.API --output-dir Data/Migrations
    ```
  - [x] Configurar contraseña local con `dotnet user-secrets` y aplicar la migración en PostgreSQL local (`protask_dev`):
    ```bash
    dotnet ef database update --project src/ProTask.Infrastructure --startup-project src/ProTask.API
    ```

---

## Fase 5 — Capa de Presentación / API (`src/ProTask.API`)

- [x] **Manejo global de excepciones:**
  - `Middlewares/ExceptionHandlingMiddleware.cs` + `InvalidModelStateResponseFactory` devolviendo **RFC 7807 (`application/problem+json`)** con `traceId`:
    - `ValidationException` / errores de model binding → `400 Bad Request`
    - `NotFoundException` → `404 Not Found`
    - `BusinessRuleException` → `409 Conflict`
    - Excepciones no controladas → `500 Internal Server Error` (sin stack traces en producción)
- [x] **Controladores delgados (*thin controllers*):**
  - `Controllers/ProjectsController.cs`: `GET`, `GET /{id}`, `GET /{id}/tasks`, `POST`, `PUT /{id}`, `DELETE /{id}`.
  - `Controllers/TasksController.cs`: `GET ?status=&priority=`, `GET /overdue`, `GET /{id}`, `POST`, `PUT /{id}`, `PATCH /{id}/status`, `DELETE /{id}`.
- [x] **Configurar `Program.cs` y Health Checks:**
  - Eliminado `/weatherforecast`, registrados `AddControllers()` y `ConfigureHttpJsonOptions` con `JsonStringEnumConverter`, `AddApplication()`, `AddInfrastructure()`, OpenAPI (`SchemaTransformer` para enums en texto) + Swagger UI en `/swagger`, `ExceptionHandlingMiddleware`, `Health/DatabaseHealthCheck.cs` en `/health` (JSON con chequeo real de PostgreSQL), `/health/live` y `public partial class Program { }`.

---

## Fase 6 — Pruebas Automatizadas (Espejo 1 a 1 en `tests/`)

### Casos críticos que deben quedar cubiertos sí o sí
1. Proyecto sin tareas devuelve **0%** de avance (sin error de división por cero).
2. Proyecto con `2` de `4` tareas completadas devuelve **50%** de avance.
3. Intentar eliminar un proyecto con al menos una tarea `Pendiente` o `EnProgreso` falla con error de negocio (`409 Conflict`).
4. Eliminar un proyecto sin tareas (o solo con tareas completadas) funciona correctamente.
5. `GET /api/tasks/overdue` devuelve únicamente tareas con `DueDate < UtcNow` y `Status != Completada` (excluye completadas aunque tengan fecha pasada).
6. Filtrar tareas por `status`, por `priority` y por ambos combinados devuelve solo coincidencias.
7. Crear proyecto con `EndDate < StartDate` o tarea con `ProjectId` inexistente devuelve `400` / `404` controlado.

### Proyectos y archivos de pruebas implementados
- [x] **`ProTask.Domain.Tests`** (15 pruebas `Unit`):
  - `Entities/ProjectTests.cs` y `Entities/TaskItemTests.cs`
- [x] **`ProTask.Application.Tests`** (44 pruebas `Unit`):
  - `Services/ProjectServiceTests.cs` y `TaskServiceTests.cs`
  - `Validators/` (5 archivos: uno por cada validador FluentValidation)
  - `Mappings/MappingProfileTests.cs`
- [x] **`ProTask.Infrastructure.Tests`** (15 pruebas: 2 `Unit` + 13 `Integration` contra `protask_infra_test`):
  - `Fixtures/PostgreSqlFixture.cs` e `InfrastructureCollection.cs`
  - `Data/MigrationTests.cs` y `DependencyInjectionTests.cs`
  - `Repositories/ProjectRepositoryTests.cs` y `TaskRepositoryTests.cs`
- [x] **`ProTask.API.Tests`** (22 pruebas: 11 `Unit` + 11 `Integration` contra `protask_api_test`):
  - `Controllers/ProjectsControllerTests.cs` y `TasksControllerTests.cs`
  - `Fixtures/ApiPostgreSqlFixture.cs`, `ProTaskApiFactory.cs` y `ApiCollection.cs`
  - `Endpoints/ProjectsEndpointsTests.cs`, `TasksEndpointsTests.cs` y `OpenApiTests.cs`
- [x] Verificación local completa en `Release` (`dotnet build -p:TreatWarningsAsErrors=true`: 0 errores, 0 advertencias; `dotnet test ProTask.slnx`: **96/96 pruebas** en paralelo).

---

## Fase 7 — Repositorio en GitHub

- [x] Crear repositorio en GitHub (`https://github.com/jose2520/ProTask`).
- [x] Conectar remoto `origin` a `https://github.com/jose2520/ProTask.git`.
- [ ] Subir los últimos commits de la rama `main`:
  ```bash
  git push -u origin main
  ```
- [x] Verificar que **no** se hayan subido contraseñas ni cadenas de conexión reales (escaneo de código limpio; usar `dotnet user-secrets`, GitHub Secrets y Azure App Settings).

---

## Fase 8 — Infraestructura en Azure (API + PostgreSQL) *(diferida por ahora)*

- [ ] Crear un **Resource Group** en Azure (ej. `rg-protask`).
- [ ] Crear la **base de datos relacional en Azure**:
  - **Azure Database for PostgreSQL Flexible Server**.
  - Configurar reglas de firewall permitiendo acceso a servicios de Azure (*Allow public access from any Azure service within Azure to this server*).
- [ ] Crear **Azure App Service** (Web App en Linux con runtime **.NET 10**).
- [ ] Configurar en el App Service → **Environment variables / Connection strings**:
  - `ConnectionStrings__DefaultConnection` con la cadena de conexión de PostgreSQL en Azure.
  - `ASPNETCORE_ENVIRONMENT=Production` y `Swagger__Enabled=true` *(para exponer Swagger en la URL entregada)*.
- [ ] Aplicar las migraciones de EF Core sobre PostgreSQL en Azure (desde el pipeline o manualmente antes del primer arranque).
- [ ] Descargar el **Publish Profile** del App Service (o configurar OpenID Connect / Service Principal) para GitHub Actions.

---

## Fase 9 — CI/CD con GitHub Actions *(diferida por ahora)*

- [ ] Guardar el secreto en GitHub → *Settings → Secrets and variables → Actions*:
  - `AZURE_WEBAPP_PUBLISH_PROFILE` *(o credenciales OIDC de Azure)*.
  - Opcional: `POSTGRES_CONNECTION_STRING` si el workflow aplica migraciones automáticamente.
- [ ] Crear el archivo `.github/workflows/ci-cd.yml` con dos jobs en cadena:
  1. **Job `build-and-test`** (se ejecuta en `push` y `pull_request` a `main`):
     - `actions/checkout@v4`
     - `actions/setup-dotnet@v4` con `dotnet-version: '10.0.x'`
     - `dotnet restore`
     - `dotnet build --configuration Release --no-restore`
     - `dotnet test --configuration Release --no-build --verbosity normal`
     - `dotnet publish src/ProTask.API/ProTask.API.csproj -c Release -o ./publish --no-build`
     - `actions/upload-artifact@v4` (subir paquete publicado)
  2. **Job `deploy`** (`needs: build-and-test`, solo en `push` a `main`):
     - `actions/download-artifact@v4`
     - `azure/webapps-deploy@v3` desplegando hacia tu App Service en Azure.
- [ ] Hacer `push` a `main` y confirmar que el workflow termine en verde (**Build ✅ → Test ✅ → Deploy ✅**).

---

## Fase 10 — Verificación Final y Entregables *(diferida por ahora)*

- [ ] Probar todos los endpoints directamente en **Swagger UI en Azure** (`https://<tu-app>.azurewebsites.net/swagger`):
  - Crear proyecto → crear tareas (pendiente, completada, vencida) → consultar tareas por proyecto → filtrar por estado y prioridad → consultar vencidas → verificar `%` de avance → intentar borrar proyecto con tarea pendiente o en progreso (verificar bloqueo `409`) → cambiar estado a completada → borrar proyecto.
- [ ] Actualizar la sección **Entregables** en [README.md](./README.md) con:
  - [x] URL del repositorio de GitHub (`https://github.com/jose2520/ProTask`).
  - [ ] URL de la API desplegada y URL de Swagger en Azure.
  - [ ] Evidencia del pipeline de CI/CD en GitHub Actions (enlace a la ejecución exitosa y/o capturas de pantalla).
- [ ] Hacer el commit final actualizando `README.md` y comprobar que el pipeline vuelva a pasar en verde.
