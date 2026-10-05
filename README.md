# MindFlow Backend

Backend de **MindFlow**, una aplicación de bienestar y productividad personal. Expone una API REST construida en **.NET 10 (ASP.NET Core Web API)** siguiendo **Domain-Driven Design (DDD)** con Bounded Contexts.

El repositorio contiene **8 bounded contexts + el shared kernel**, repartidos entre los dos integrantes del equipo:

- **IAM, Journal, AI Assistant, Habits & Wellness** + shared kernel
- **Analytics & Reporting, Notifications, Subscriptions, Support**

## Stack tecnológico

- **.NET 10 / ASP.NET Core Web API**
- **Entity Framework Core** + **MySQL** (`MySql.EntityFrameworkCore`)
- **Cortex.Mediator** (patrón CQRS: comandos y queries)
- **JWT Bearer** para autenticación
- **BCrypt** para hash de contraseñas
- **AES** para cifrado de campos sensibles a nivel de base de datos
- **Redis** para cache (`StackExchangeRedisCache`)
- **Cloudinary** para almacenamiento de archivos multimedia (Journal)
- **Gemini API** (Google) para funcionalidades de IA
- **Stripe** para checkout y pagos de suscripción
- **QuestPDF** para generación de reportes en PDF
- **Envío de correos (SMTP)** para notificaciones por email
- **Serilog** para logging
- **Swagger / Swashbuckle** para documentación de la API
- **Docker Compose** para el entorno local (MySQL + Redis)

## Estructura del proyecto

```
MindFlow.Platform/
├── Program.cs                  # Composición raíz: DI, middlewares, pipeline HTTP
├── appsettings.json             # Configuración base (sin secretos, solo placeholders)
├── Migrations/                  # Migraciones de EF Core
│
├── shared/                      # Shared Kernel: usado por todos los bounded contexts
│   ├── domain/repositories       (IUnitOfWork, IBaseRepository<T>, IAuditableEntity)
│   ├── infrastructure/persistence (AppDbContext, BaseRepository, interceptors, cifrado AES)
│   ├── infrastructure/caching     (ICacheService, RedisCacheService)
│   └── interfaces/rest            (ProblemDetails, convenciones de rutas, middlewares)
│
├── iam/                          # Identity & Access Management
│   └── Registro, login, Google Auth, recuperación de contraseña, perfil, PIN
│
├── journal/                      # Diario personal
│   └── Entradas de diario, tags, adjuntos multimedia, búsqueda
│
├── AiAssistant/                  # Asistente de IA (3 sub-bounded contexts fusionados)
│   ├── Chat/                     # Conversaciones con el asistente
│   ├── AiIntegration/            # Integración con Gemini (servicio interno, sin endpoints propios aún)
│   └── AiFeedback/                # Calificación del feedback generado por IA
│
├── HabitsWellness/                # Hábitos y bienestar (3 sub-bounded contexts fusionados)
│   ├── habits/                    # Hábitos y su registro diario (logs)
│   ├── WellnessEngine/             # Chequeo de estrés / bienestar
│   └── WellnessContent/            # Catálogo de ejercicios de bienestar (respiración, meditación)
│
├── Analytics/                     # Analytics & Reporting: dashboard de métricas, exportes CSV/PDF
├── Notifications/                 # Notificaciones in-app (lectura, conteo de no leídas)
├── Subscriptions/                 # Planes de suscripción y checkout de pagos (Stripe)
└── Support/                       # Mesa de ayuda: tickets de soporte y mensajería
```

> Cada bounded context conserva su estructura interna original (`domain`, `application`, `infrastructure`, `interfaces`), tal como en el repo del que fue portado.

## Bounded Contexts y funcionalidad

### IAM (`api/v1/users`)
Registro, inicio de sesión (con JWT), autenticación con Google, recuperación de contraseña, visualización/edición de perfil, eliminación de cuenta (con borrado en cascada de datos relacionados), y PIN de acceso rápido.

### Journal (`/journal`)
CRUD de entradas de diario, etiquetas (tags), relación entrada-etiqueta, subida y consulta de archivos multimedia (vía Cloudinary), y sincronización de entradas.

### AI Assistant
- **Chat** (`/chat`): conversaciones y mensajes con el asistente de IA.
- **AiIntegration**: servicio interno que conecta con la API de Gemini (usado por Chat).
- **AiFeedback** (`api/v1/ai-feedback`): calificación (1-5) del contenido generado por IA y resumen estadístico de esas calificaciones.

### Habits & Wellness
- **Habits** (`/habits`, `/habit-logs`): creación/edición/eliminación de hábitos, registro diario de cumplimiento, resumen de rachas (*streaks*) y sugerencias de hábitos.
- **WellnessEngine** (`/wellness/stress-check`): chequeo de nivel de estrés.
- **WellnessContent** (`/wellness/exercises`): catálogo de ejercicios de bienestar (respiración, meditación), con semilla inicial de datos.

### Analytics & Reporting (`api/v1/analytics`)
Dashboard con métricas del usuario (entradas de diario por sentimiento/categoría, hábitos completados), y exportación de reportes en CSV y PDF.

### Notifications (`api/v1/notifications`)
Listado de notificaciones del usuario, conteo de no leídas, y marcado de lectura (individual o masivo).

### Subscriptions (`api/v1/subscriptions`)
Planes disponibles (freemium/premium), suscripción actual del usuario, checkout de pago vía Stripe y webhook de confirmación.

### Support (`api/v1/support/tickets`)
Creación y seguimiento de tickets de soporte, mensajería entre el usuario y el staff, y actualización de estado/asignación (rol `Support`/`Admin`).

### Shared Kernel
Infraestructura común a todos los bounded contexts: `AppDbContext` (DbContext único compartido), repositorio base genérico, unidad de trabajo, auditoría automática de entidades (`CreatedAt`/`UpdatedAt`), cifrado AES de campos sensibles, cache con Redis, manejo global de excepciones, `ProblemDetails`, y convenciones de rutas/JSON (kebab-case en URLs, snake_case en JSON).

## Cómo correrlo en local

### 1. Requisitos
- .NET 10 SDK
- Docker Desktop

### 2. Levantar MySQL y Redis
```bash
docker compose up -d
```
Esto crea:
- MySQL en `localhost:3308` (usuario `root`, base de datos `MindflowPlatform`)
- Redis en `localhost:6380`

### 3. Configurar secretos locales
Los secretos **nunca** se guardan en el repositorio (`appsettings.json` solo tiene placeholders vacíos). Se configuran con `dotnet user-secrets`, por proyecto:

```bash
cd MindFlow.Platform
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=127.0.0.1;port=3308;user=root;password=<tu-password>;database=MindflowPlatform"
dotnet user-secrets set "ConnectionStrings:Redis" "127.0.0.1:6380"
dotnet user-secrets set "TokenSettings:Secret" "<clave-generada-con-openssl-rand>"
dotnet user-secrets set "Encryption:AesKey" "<clave-generada-con-openssl-rand>"
dotnet user-secrets set "AiSettings:GeminiApiKey" "<tu-api-key-de-gemini>"
dotnet user-secrets set "Stripe:SecretKey" "<tu-secret-key-de-stripe>"
dotnet user-secrets set "Stripe:PremiumPriceId" "<id-del-price-de-stripe>"
dotnet user-secrets set "Stripe:WebhookSecret" "<tu-webhook-secret-de-stripe>"
dotnet user-secrets set "Email:Username" "<tu-usuario-smtp>"
dotnet user-secrets set "Email:Password" "<tu-password-smtp>"
```

### 4. Aplicar migraciones y correr
```bash
dotnet run --project MindFlow.Platform
```
Las migraciones de EF Core se aplican automáticamente al iniciar (`Database.Migrate()`), junto con una siembra inicial de ejercicios de bienestar.

### 5. Verificar
- Swagger UI: `http://localhost:<puerto>/swagger`
- Health check (verifica conexión a BD): `http://localhost:<puerto>/health`

## Estado actual

Esqueleto funcional: los 8 bounded contexts + shared kernel compilan sin errores, la API corre localmente con base de datos y cache reales, y cada bounded context expone sus endpoints REST principales. Pendiente: pruebas automatizadas, pipeline de CI/CD, placeholders de `Stripe` en `appsettings.json` (actualmente ausentes), y validación de contratos contra el frontend.
