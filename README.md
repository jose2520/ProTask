# ProTask API

API REST para la gestión de proyectos y sus respectivas tareas, construida bajo Clean Architecture en .NET 10.

## Objetivo

Desarrollar una API REST que permita gestionar proyectos y sus respectivas tareas, garantizando validaciones de negocio, manejo consistente de excepciones, persistencia relacional, documentación interactiva con Swagger y despliegue automatizado en Azure mediante CI/CD.

---

## Modelo de Datos y Entidades

### Relación

- **1:N (Uno a Muchos):** Un proyecto puede tener muchas tareas, pero una tarea solo puede pertenecer a un proyecto.

### Proyecto (`Project`)

| Campo | Descripción |
| :--- | :--- |
| `Id` | Identificador único del proyecto |
| `Nombre` | Nombre del proyecto |
| `Descripcion` | Descripción detallada del proyecto |
| `FechaInicio` | Fecha de inicio del proyecto |
| `FechaFinalizacion` | Fecha de finalización del proyecto |

### Tarea (`TaskItem`)

| Campo | Descripción |
| :--- | :--- |
| `Id` | Identificador único de la tarea |
| `Titulo` | Título de la tarea |
| `Descripcion` | Descripción de la tarea |
| `Estado` | Estado actual: `Pendiente`, `EnProgreso`, `Completada` |
| `Prioridad` | Nivel de prioridad de la tarea |
| `FechaLimite` | Fecha límite de entrega |
| `ProyectoId` | Identificador del proyecto al que pertenece (clave foránea) |

---

## Requerimientos Funcionales

- [ ] **CRUD de proyectos y tareas:** Crear, consultar, actualizar y eliminar proyectos y tareas.
- [ ] **Consultar tareas por proyecto:** Obtener todas las tareas asociadas a un proyecto específico.
- [ ] **Filtrar tareas:** Filtrar tareas por **estado** y **prioridad**.
- [ ] **Consultar tareas vencidas:** Listar las tareas cuya fecha límite haya pasado y que aún no estén completadas.
- [ ] **Cambiar el estado de una tarea:** Actualizar el estado de una tarea (`Pendiente`, `EnProgreso`, `Completada`).
- [ ] **Cálculo de porcentaje de avance:** Calcular el porcentaje de avance de un proyecto con base en sus tareas completadas sobre el total de tareas.
- [ ] **Protección de eliminación:** Impedir la eliminación de proyectos que tengan tareas pendientes.
- [ ] **Validaciones y manejo de excepciones:** Validar los datos de entrada y manejar errores/excepciones de forma centralizada con respuestas HTTP apropiadas.

---

## Requerimientos Técnicos

- [ ] **Base de datos relacional:** Persistencia de datos en un motor relacional (por ejemplo, Azure SQL Database / PostgreSQL) usando Entity Framework Core y migraciones.
- [ ] **Documentación con Swagger:** Especificación OpenAPI e interfaz interactiva de Swagger para explorar y probar los endpoints.
- [ ] **Control de versiones en GitHub:** Repositorio configurado con historial de commits y `.gitignore` para .NET.
- [ ] **Despliegue en Azure:** Publicación de la API (Azure App Service) y la base de datos relacional en Microsoft Azure.
- [ ] **CI/CD con GitHub Actions:** Pipeline automatizado que ejecute:
  1. Compilación de la solución (`dotnet build`).
  2. Ejecución de pruebas automatizadas (`dotnet test`).
  3. Despliegue automático hacia Azure.

---

## Estructura de la Solución

```text
ProTask/
├── ProTask.slnx
├── README.md
├── src/
│   ├── ProTask.Domain/           # Entidades, enums, excepciones e interfaces de dominio
│   ├── ProTask.Application/      # DTOs, servicios, mapeos y validadores
│   ├── ProTask.Infrastructure/   # DbContext, configuraciones EF Core, migraciones y repositorios
│   └── ProTask.API/              # Controladores, middlewares, Swagger y configuración HTTP
└── tests/
    └── ProTask.Tests/            # Pruebas unitarias, de integración y funcionales
```

---

## Entregables

- **Repositorio de GitHub:** _Pendiente por agregar URL_
- **URL de la API desplegada (Azure):** _Pendiente por agregar URL_
- **Documentación Swagger (Azure):** _Pendiente por agregar URL_
- **Evidencia del pipeline CI/CD (GitHub Actions):** _Pendiente por agregar enlace / capturas de ejecución exitosa (build, test y deploy)_
