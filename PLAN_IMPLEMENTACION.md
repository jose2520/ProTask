# Plan de Implementación — ProTask API

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
- [ ] Generar un `.gitignore` estándar para .NET (`bin/`, `obj/`, `.vs/`, `.idea/`, `appsettings.*.local.json`, secretos):
  ```bash
  dotnet new gitignore --force
  ```
- [ ] Inicializar repositorio local y hacer el commit base:
  ```bash
  git init
  git branch -M main
  git add .
  git commit -m "chore: initial clean architecture structure"
  ```

### 1.2 Elegir motor de base de datos relacional para Azure
- [ ] Definir el motor relacional a usar (opción recomendada para Azure: **Azure SQL Database** con `Microsoft.EntityFrameworkCore.SqlServer`, o alternativa **Azure Database for PostgreSQL** con `Npgsql.EntityFrameworkCore.PostgreSQL`).

### 1.3 Instalar paquetes NuGet por capa
- [ ] **`ProTask.Domain`**: mantener **0 paquetes externos** (dominio puro).
- [ ] **`ProTask.Application`**:
  - `FluentValidation`
  - `FluentValidation.DependencyInjectionExtensions`
  - `AutoMapper` (o mapeo manual explícito si prefieres evitar dependencias)
  - `Microsoft.Extensions.DependencyInjection.Abstractions`
- [ ] **`ProTask.Infrastructure`**:
  - `Microsoft.EntityFrameworkCore`
  - `Microsoft.EntityFrameworkCore.SqlServer` *(o `Npgsql.EntityFrameworkCore.PostgreSQL`)*
  - `Microsoft.EntityFrameworkCore.Tools`
- [ ] **`ProTask.API`**:
  - `Swashbuckle.AspNetCore.SwaggerUI` *(para exponer la interfaz gráfica Swagger UI consumiendo `/openapi/v1.json` de `Microsoft.AspNetCore.OpenApi`, o `Swashbuckle.AspNetCore` completo)*
  - `Microsoft.EntityFrameworkCore.Design` *(necesario para ejecutar `dotnet ef migrations`)*
- [ ] **`ProTask.Tests`**:
  - `Microsoft.NET.Test.Sdk`
  - `xunit`
  - `xunit.runner.visualstudio`
  - `Moq` *(o `NSubstitute`)*
  - `Microsoft.AspNetCore.Mvc.Testing` *(para pruebas funcionales de endpoints)*
  - `Microsoft.EntityFrameworkCore.Sqlite` *(o `InMemory` para pruebas de integración de repositorios)*

---

## Fase 2 — Capa de Dominio (`src/ProTask.Domain`)

> **Regla de oro:** Sin atributos de EF Core, sin referencias a ASP.NET Core y sin dependencias externas.

- [ ] **Enums:**
  - `Enums/TaskItemStatus.cs`: `Pendiente`, `EnProgreso`, `Completada`.
  - `Enums/TaskPriority.cs`: `Baja`, `Media`, `Alta` *(y opcional `Critica`)*.
- [ ] **Excepciones de dominio** (crear en `src/ProTask.Domain/Exceptions/`):
  - `DomainException.cs` (base)
  - `NotFoundException.cs` (recurso no encontrado → HTTP `404`)
  - `BusinessRuleException.cs` (violación de regla de negocio, ej. eliminar proyecto con tareas pendientes → HTTP `409` o `400`)
- [ ] **Entidades y relación 1:N:**
  - `Entities/Project.cs`:
    - Propiedades: `Id`, `Name`, `Description`, `StartDate`, `EndDate`, colección `ICollection<TaskItem> Tasks`.
    - Lógica de dominio: método para calcular porcentaje de avance (`0%` si no tiene tareas; `(completadas / total) * 100` redondeado a 2 decimales) y método/propiedad para verificar si tiene tareas pendientes (`Tasks.Any(t => t.Status == TaskItemStatus.Pendiente)`).
  - `Entities/TaskItem.cs`:
    - Propiedades: `Id`, `Title`, `Description`, `Status` (por defecto `Pendiente`), `Priority`, `DueDate`, `ProjectId`, navegación `Project`.
    - Lógica de dominio: método para cambiar estado y método para saber si está vencida (`DueDate < DateTime.UtcNow && Status != TaskItemStatus.Completada`).
- [ ] **Contratos de repositorios:**
  - `Interfaces/IProjectRepository.cs`: `GetAllAsync`, `GetByIdAsync`, `GetByIdWithTasksAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `ExistsAsync`.
  - `Interfaces/ITaskRepository.cs`: `GetAllAsync`, `GetByIdAsync`, `GetByProjectIdAsync`, `GetFilteredAsync(status?, priority?)`, `GetOverdueIncompleteAsync(DateTime referenceUtc)`, `AddAsync`, `UpdateAsync`, `DeleteAsync`.

---

## Fase 3 — Capa de Aplicación (`src/ProTask.Application`)

- [ ] **DTOs de Proyectos (`DTOs/Projects/`):**
  - `ProjectDto.cs`: `Id`, `Name`, `Description`, `StartDate`, `EndDate`, `TotalTasks`, `CompletedTasks`, `ProgressPercentage`.
  - `CreateProjectDto.cs`: `Name`, `Description`, `StartDate`, `EndDate`.
  - `UpdateProjectDto.cs`: `Name`, `Description`, `StartDate`, `EndDate`.
- [ ] **DTOs de Tareas (`DTOs/Tasks/`):**
  - `TaskItemDto.cs`: `Id`, `Title`, `Description`, `Status`, `Priority`, `DueDate`, `ProjectId`, `IsOverdue`.
  - `CreateTaskItemDto.cs`: `Title`, `Description`, `Priority`, `DueDate`, `ProjectId`.
  - `UpdateTaskItemDto.cs`: `Title`, `Description`, `Status`, `Priority`, `DueDate`.
  - `UpdateTaskStatusDto.cs`: `Status`.
- [ ] **Validadores (`Validators/`):**
  - `CreateProjectValidator.cs`: nombre requerido y longitud máxima, `EndDate >= StartDate`.
  - Crear `UpdateProjectValidator.cs`: mismas reglas de consistencia de nombre y fechas.
  - `CreateTaskItemValidator.cs`: título requerido, `ProjectId > 0`, `Priority` válida, `DueDate` válida.
  - Crear `UpdateTaskItemValidator.cs`: título requerido, `Status` y `Priority` válidas, `DueDate` válida.
  - Crear `UpdateTaskStatusValidator.cs`: validar que `Status` pertenezca al enum `TaskItemStatus`.
- [ ] **Mapeos (`Mappings/MappingProfile.cs`):**
  - Mapear `Project` ↔ `ProjectDto` (incluyendo `ProgressPercentage`), `CreateProjectDto`, `UpdateProjectDto`.
  - Mapear `TaskItem` ↔ `TaskItemDto`, `CreateTaskItemDto`, `UpdateTaskItemDto`.
- [ ] **Interfaces y Servicios (`Interfaces/` y `Services/`):**
  - `IProjectService.cs` y `ProjectService.cs`:
    - CRUD completo de proyectos.
    - Cálculo de porcentaje de avance al devolver proyectos.
    - **Regla crítica:** en `DeleteAsync`, cargar el proyecto con sus tareas; si no existe lanzar `NotFoundException`; si tiene tareas en estado `Pendiente`, lanzar `BusinessRuleException` e impedir la eliminación.
  - `ITaskService.cs` y `TaskService.cs`:
    - CRUD completo de tareas (verificando que el `ProjectId` exista antes de crear).
    - Consultar tareas por proyecto (verificando que el proyecto exista).
    - Filtrar tareas por `status` y/o `priority`.
    - Consultar tareas vencidas que no estén completadas (`DueDate < DateTime.UtcNow` y `Status != Completada`).
    - Cambiar únicamente el estado de una tarea (`UpdateStatusAsync`).
- [ ] **Inyección de dependencias (`DependencyInjection.cs`):**
  - Crear método de extensión `AddApplication()` registrando servicios, validadores y mapeos.

---

## Fase 4 — Capa de Infraestructura (`src/ProTask.Infrastructure`)

- [ ] **DbContext (`Data/ApplicationDbContext.cs`):**
  - Definir `DbSet<Project> Projects` y `DbSet<TaskItem> Tasks`.
  - Aplicar configuraciones automáticamente con `modelBuilder.ApplyConfigurationsFromAssembly(...)`.
- [ ] **Configuraciones Fluent API (`Data/Configurations/`):**
  - `ProjectConfiguration.cs`: clave primaria, longitudes máximas, campos requeridos y relación `HasMany(p => p.Tasks).WithOne(t => t.Project).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade)` *(la regla de impedir borrar proyectos con tareas pendientes se valida en dominio/servicio; así, cuando el proyecto sí pueda eliminarse teniendo solo tareas completadas, no fallará por llave foránea)*.
  - `TaskItemConfiguration.cs`: clave primaria, longitudes, conversión de enums (string o int consistente), índices en `ProjectId`, `Status`, `Priority` y `DueDate`.
- [ ] **Repositorios (`Repositories/`):**
  - `ProjectRepository.cs` implementando `IProjectRepository` (usando `Include(p => p.Tasks)` cuando se requiera calcular avance o validar borrado, y `AsNoTracking()` en consultas de solo lectura).
  - `TaskRepository.cs` implementando `ITaskRepository` (filtros combinables por `status` y `priority`, consulta por `projectId` y consulta de vencidas no completadas).
- [ ] **Inyección de dependencias (`DependencyInjection.cs`):**
  - Crear método de extensión `AddInfrastructure(IConfiguration configuration)` registrando `ApplicationDbContext` con `ConnectionStrings:DefaultConnection` y ambos repositorios.
- [ ] **Migración inicial (`Data/Migrations/`):**
  - Configurar cadena de conexión local en `src/ProTask.API/appsettings.Development.json` (o `dotnet user-secrets`).
  - Generar migración inicial:
    ```bash
    dotnet ef migrations add InitialCreate --project src/ProTask.Infrastructure --startup-project src/ProTask.API --output-dir Data/Migrations
    ```
  - Probar aplicación de la migración en base de datos local:
    ```bash
    dotnet ef database update --project src/ProTask.Infrastructure --startup-project src/ProTask.API
    ```

---

## Fase 5 — Capa de Presentación / API (`src/ProTask.API`)

- [ ] **Manejo global de excepciones:**
  - Crear `Middlewares/ExceptionHandlingMiddleware.cs` que capture excepciones y responda con estándar **RFC 7807 (`ProblemDetails`)**:
    - `ValidationException` → `400 Bad Request` (con detalle de errores por campo)
    - `NotFoundException` → `404 Not Found`
    - `BusinessRuleException` → `409 Conflict`
    - Excepciones no controladas → `500 Internal Server Error` (sin exponer stack traces en producción)
- [ ] **Controladores delgados (*thin controllers*):**
  - `Controllers/ProjectsController.cs`:
    - `GET /api/projects` — listar proyectos (con `%` de avance)
    - `GET /api/projects/{id}` — obtener proyecto por ID (con `%` de avance)
    - `GET /api/projects/{id}/tasks` — consultar tareas de un proyecto
    - `POST /api/projects` — crear proyecto (`201 Created`)
    - `PUT /api/projects/{id}` — actualizar proyecto
    - `DELETE /api/projects/{id}` — eliminar proyecto (bloqueado si tiene tareas pendientes)
  - `Controllers/TasksController.cs`:
    - `GET /api/tasks` — listar tareas con filtros opcionales `?status=...&priority=...`
    - `GET /api/tasks/overdue` — consultar tareas vencidas no completadas
    - `GET /api/tasks/{id}` — obtener tarea por ID
    - `POST /api/tasks` — crear tarea (`201 Created`)
    - `PUT /api/tasks/{id}` — actualizar tarea
    - `PATCH /api/tasks/{id}/status` — cambiar el estado de una tarea
    - `DELETE /api/tasks/{id}` — eliminar tarea
- [ ] **Configurar `Program.cs`:**
  - Eliminar el endpoint de plantilla `/weatherforecast`.
  - Registrar `AddControllers()` (con serialización de enums como texto si se desea mayor claridad en Swagger), `AddApplication()`, `AddInfrastructure(builder.Configuration)`.
  - Configurar **Swagger / OpenAPI** (habilitar Swagger UI en `/swagger` también en producción o controlado por configuración para que sea visible en la URL entregada de Azure).
  - Registrar `ExceptionHandlingMiddleware` al inicio del pipeline HTTP.
  - Agregar `public partial class Program { }` al final de `Program.cs` para habilitar `WebApplicationFactory<Program>` en pruebas funcionales.
  - Opcional: agregar endpoint `/health` para verificar salud del servicio desplegado en Azure.

---

## Fase 6 — Pruebas Automatizadas (`tests/ProTask.Tests`)

### Casos críticos que deben quedar cubiertos sí o sí
1. Proyecto sin tareas devuelve **0%** de avance (sin error de división por cero).
2. Proyecto con `2` de `4` tareas completadas devuelve **50%** de avance.
3. Intentar eliminar un proyecto con al menos una tarea `Pendiente` falla con error de negocio (`409 Conflict`).
4. Eliminar un proyecto sin tareas (o sin tareas pendientes) funciona correctamente.
5. `GET /api/tasks/overdue` devuelve únicamente tareas con `DueDate < UtcNow` y `Status != Completada` (excluye completadas aunque tengan fecha pasada).
6. Filtrar tareas por `status`, por `priority` y por ambos combinados devuelve solo coincidencias.
7. Crear proyecto con `EndDate < StartDate` o tarea con `ProjectId` inexistente devuelve `400` / `404` controlado.

### Archivos de pruebas a implementar
- [ ] `Services/DomainEntityTests.cs`: reglas de `Project` (`ProgressPercentage`, tareas pendientes) y `TaskItem` (vencimiento, cambio de estado).
- [ ] `Services/ProjectServiceTests.cs`: CRUD, cálculo de avance, bloqueo de borrado y `NotFoundException`.
- [ ] `Services/TaskServiceTests.cs`: CRUD, filtros, tareas vencidas, cambio de estado y proyecto inexistente.
- [ ] `Services/DependencyInjectionTests.cs`: verificar que el contenedor DI resuelve todos los servicios y repositorios.
- [ ] `Controllers/ProjectsControllerTests.cs` y `Controllers/TasksControllerTests.cs`: códigos HTTP esperados (`200`, `201`, `204`, `400`, `404`, `409`).
- [ ] `Integration/ProjectRepositoryTests.cs` y `Integration/TaskRepositoryTests.cs`: consultas reales contra EF Core (SQLite en memoria) validando relación 1:N y consultas de filtros/vencidas.
- [ ] `Functional/ProjectsEndpointsTests.cs`: flujo end-to-end contra la API usando `WebApplicationFactory<Program>` (incluyendo respuestas de error del middleware).
- [ ] Ejecutar verificación local completa:
  ```bash
  dotnet build
  dotnet test
  ```

---

## Fase 7 — Repositorio en GitHub

- [ ] Crear repositorio en GitHub (público o con acceso para evaluadores).
- [ ] Conectar remoto y subir la rama `main`:
  ```bash
  git remote add origin https://github.com/<tu-usuario>/<tu-repo>.git
  git push -u origin main
  ```
- [ ] Verificar que **no** se hayan subido contraseñas ni cadenas de conexión reales (usar GitHub Secrets y Azure App Settings).

---

## Fase 8 — Infraestructura en Azure (API + Base de Datos)

- [ ] Crear un **Resource Group** en Azure (ej. `rg-protask`).
- [ ] Crear la **base de datos relacional en Azure**:
  - **Azure SQL Server + Azure SQL Database** *(o Azure Database for PostgreSQL Flexible Server)*.
  - Configurar reglas de firewall permitiendo acceso a servicios de Azure (*Allow Azure services and resources to access this server*).
- [ ] Crear **Azure App Service** (Web App en Linux con runtime **.NET 10**).
- [ ] Configurar en el App Service → **Environment variables / Connection strings**:
  - `ConnectionStrings__DefaultConnection` con la cadena de conexión de Azure SQL / PostgreSQL.
  - `ASPNETCORE_ENVIRONMENT=Production` *(y bandera si deseas habilitar Swagger en producción)*.
- [ ] Aplicar las migraciones de EF Core sobre la base de datos en Azure (desde el pipeline o manualmente antes del primer arranque).
- [ ] Descargar el **Publish Profile** del App Service (o configurar OpenID Connect / Service Principal) para GitHub Actions.

---

## Fase 9 — CI/CD con GitHub Actions

- [ ] Guardar el secreto en GitHub → *Settings → Secrets and variables → Actions*:
  - `AZURE_WEBAPP_PUBLISH_PROFILE` *(o credenciales OIDC de Azure)*.
  - Opcional: `AZURE_SQL_CONNECTION_STRING` si el workflow aplica migraciones automáticamente.
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

## Fase 10 — Verificación Final y Entregables

- [ ] Probar todos los endpoints directamente en **Swagger UI en Azure** (`https://<tu-app>.azurewebsites.net/swagger`):
  - Crear proyecto → crear tareas (pendiente, completada, vencida) → consultar tareas por proyecto → filtrar por estado y prioridad → consultar vencidas → verificar `%` de avance → intentar borrar proyecto con tarea pendiente (verificar bloqueo) → cambiar estado a completada → borrar proyecto.
- [ ] Actualizar la sección **Entregables** en [README.md](./README.md) con:
  - [ ] URL del repositorio de GitHub.
  - [ ] URL de la API desplegada y URL de Swagger en Azure.
  - [ ] Evidencia del pipeline de CI/CD en GitHub Actions (enlace a la ejecución exitosa y/o capturas de pantalla).
- [ ] Hacer el commit final actualizando `README.md` y comprobar que el pipeline vuelva a pasar en verde.
