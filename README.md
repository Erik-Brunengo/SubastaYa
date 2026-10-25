# SubastaYa — Plataforma de Subastas en Tiempo Real

![.NET 8](https://img.shields.io/badge/.NET%208-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![EF Core 8](https://img.shields.io/badge/EF%20Core%208-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-WebSockets-0078D4?style=for-the-badge&logo=microsoft&logoColor=white)
![MySQL 8](https://img.shields.io/badge/MySQL%208-4479A1?style=for-the-badge&logo=mysql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Clean Architecture](https://img.shields.io/badge/Clean%20Architecture-CQRS-success?style=for-the-badge)
![xUnit](https://img.shields.io/badge/Tests-xUnit%20%2B%20NSubstitute-5C2D91?style=for-the-badge)

**SubastaYa** es una plataforma web full-stack de subastas electrónicas en tiempo real construida con **ASP.NET Core 8**, **Entity Framework Core 8 (Code-First)**, **SignalR**, **MySQL 8** y una **SPA en JavaScript Vanilla**, diseñada bajo **Clean Architecture (4 capas)** y el patrón **CQRS**.

---

## Aspectos Técnicos Destacados

1. **Garantía Financiera Atómica (Escrow en Billetera Virtual):**
   Toda oferta está respaldada por saldo real. Al registrar una puja superadora, el sistema **libera automáticamente el saldo retenido del líder anterior y bloquea el saldo del nuevo postor** dentro de una misma transacción ACID, dejando trazabilidad contable inmutable en el libro mayor (`TransaccionLedger`).
2. **Regla Anti-Sniping en Tiempo Real:**
   Si entra una puja válida dentro de los **últimos 60 segundos** antes del cierre, la subasta **extiende automáticamente su finalización por 2 minutos adicionales** y notifica instantáneamente a todos los participantes conectados a la sala mediante **WebSockets (SignalR)**.
3. **Control de Concurrencia Optimista (`RowVersion` / HTTP 409):**
   Las entidades críticas (`Subasta` y `Billetera`) implementan tokens de concurrencia optimista. Ante dos ofertas simultáneas en el mismo milisegundo, la base de datos confirma únicamente la primera (`201 Created`) y rechaza la colisión con `409 Conflict` (`ProblemDetails` RFC 7807).
4. **Liquidación Automática en Segundo Plano (`BackgroundService`):**
   El worker [`AuctionStatusWorker`](Backend/src/SubastaYa.WebApi/Workers/AuctionStatusWorker.cs) monitorea periódicamente las subastas: activa las programadas cuando llega su fecha de inicio, adjudica las vencidas transfiriendo los fondos retenidos al vendedor (`Finalizada`) o las declara `Desierta` si no recibieron ofertas.
5. **Auditoría Inmutable en Transacción Independiente (`IDbContextFactory`):**
   Los eventos críticos —incluyendo pujas rechazadas por saldo insuficiente, subasta vencida, autopuja o colisión de concurrencia— se persisten en `AuditoriaLog` mediante un `DbContext` efímero e independiente para que el registro de auditoría sobreviva al `Rollback` de la transacción principal.

---

## Arquitectura del Sistema (Clean Architecture + CQRS)

La solución está dividida en **4 capas desacopladas** siguiendo el Principio de Inversión de Dependencias (DIP):

```mermaid
flowchart TD
    subgraph Presentation["1. Presentation (SubastaYa.WebApi)"]
        Controllers["Controllers RESTful"]
        Hubs["SignalR Hub (AuctionHub)"]
        Worker["Background Worker (AuctionStatusWorker)"]
        Middleware["ExceptionMiddleware (RFC 7807)"]
    end

    subgraph Application["2. Application (SubastaYa.Application)"]
        UseCases["UseCases (Commands / Queries / Handlers)"]
        Interfaces["Contratos (IRepositories, IUnitOfWork, IAuditService)"]
        DTOs["DTOs & Mappings"]
    end

    subgraph Domain["3. Domain (SubastaYa.Domain)"]
        Entities["Entidades Ricas (Subasta, Billetera, Puja, Usuario)"]
        Enums["Enums (EstadoSubasta, TipoTransaccion)"]
        Exceptions["Excepciones de Dominio"]
    end

    subgraph Infrastructure["4. Infrastructure (SubastaYa.Infrastructure)"]
        EFCore["SubastaYaDbContext & Fluent API Configurations"]
        Repos["Repositories, Queries & UnitOfWork"]
        Services["AuditService (DbContextFactory) & SmtpEmailSender"]
    end

    Presentation --> Application
    Application --> Domain
    Infrastructure --> Application
    Infrastructure --> Domain
```

### Estructura de Proyectos

```text
├── Backend/src/
│   ├── SubastaYa.Domain/           # Entidades con comportamiento (Rich Domain Model), Enums y Excepciones
│   ├── SubastaYa.Application/      # Casos de uso CQRS (Commands, Queries, Handlers), Interfaces, DTOs y Mappings
│   ├── SubastaYa.Infrastructure/   # EF Core 8, Fluent API Configurations, Migraciones, Repositorios y Servicios
│   ├── SubastaYa.WebApi/           # Controladores REST, SignalR Hub, Background Worker, JWT y ExceptionMiddleware
│   └── SubastaYa.UnitTests/        # Tests unitarios con xUnit y NSubstitute
├── Fronted/SubastaYa-Fronted/      # Single Page Application (HTML5, CSS3, JS ES6+ y cliente SignalR)
├── docker-compose.yml              # Orquestación de API + MySQL 8 + Mailpit (SMTP)
└── stress-test.sh / .ps1           # Scripts de prueba de estrés y concurrencia optimista (HTTP 409)
```

---

## Modelo de Datos (DER)

```mermaid
erDiagram
    USUARIO ||--o| BILLETERA : posee
    USUARIO ||--o{ SUBASTA : publica
    USUARIO ||--o{ PUJA : realiza
    CATEGORIA ||--o{ SUBASTA : clasifica
    SUBASTA ||--o{ PUJA : recibe
    BILLETERA ||--o{ TRANSACCION_LEDGER : registra
    SUBASTA ||--o{ TRANSACCION_LEDGER : origina

    USUARIO {
        int Id PK
        string Nombre
        string Email
        bool EmailVerificado
    }
    BILLETERA {
        int Id PK
        int UsuarioId FK
        decimal SaldoTotal
        decimal SaldoRetenido
        byte[] Version "ConcurrencyToken"
    }
    SUBASTA {
        int Id PK
        int VendedorId FK
        int CategoriaId FK
        string Titulo
        decimal PrecioBase
        decimal IncrementoMinimo
        datetime FechaInicio
        datetime FechaFin
        int Estado "Programada | Activa | Finalizada | Desierta"
        byte[] Version "ConcurrencyToken"
    }
    PUJA {
        int Id PK
        int SubastaId FK
        int CompradorId FK
        decimal Monto
        datetime FechaPuja
    }
    TRANSACCION_LEDGER {
        int Id PK
        int BilleteraId FK
        int SubastaId FK
        string Tipo "Deposito | Retencion | Liberacion | Pago | Cobro"
        decimal Monto
        datetime Fecha
    }
    AUDITORIA_LOG {
        int Id PK
        string Entidad
        int EntidadId
        string Accion
        int UsuarioId
        string DetalleJson
        datetime Fecha
    }
```

---

## Endpoints Principales de la API REST (`v1`)

| Método | Ruta | Descripción | Auth |
| :--- | :--- | :--- | :---: |
| `POST` | `/api/v1/auth/tokens` | Autenticación de usuario y emisión de token JWT | No |
| `POST` | `/api/v1/users` | Registro de nuevo usuario con verificación por email | No |
| `GET` | `/api/v1/auctions` | Catálogo de subastas con filtros (estado, categoría, precio, orden) | No |
| `GET` | `/api/v1/auctions/{id}` | Detalle de subasta en vivo, oferta actual e historial de pujas | No |
| `POST` | `/api/v1/auctions` | Publicación de una nueva subasta | JWT |
| `POST` | `/api/v1/auctions/{auctionId}/bids` | Registro de puja (valida saldo, aplica Escrow y Anti-Sniping) | JWT |
| `GET` | `/api/v1/wallets/balance` | Consulta de Saldo Total, Retenido en Garantía y Disponible | JWT |
| `GET` | `/api/v1/wallets/transactions` | Historial de movimientos contables en el Ledger | JWT |
| `POST` | `/api/v1/wallets/deposits` | Carga simulada de saldo en la billetera virtual | JWT |
| `GET` | `/api/v1/users/auctions` | Panel del vendedor: mis subastas publicadas | JWT |
| `GET` | `/api/v1/users/bids` | Panel del comprador: subastas en las que participé | JWT |

---

## Ejecución Rápida con Docker Compose (Recomendado)

El repositorio incluye un entorno contenerizado que levanta **MySQL 8**, **Mailpit** (servidor SMTP con bandeja web) y la **Web API + Frontend SPA**, aplicando automáticamente las migraciones y cargando los datos semilla (`DbInitializer`).

### 1. Levantar todos los servicios
```bash
docker compose up --build
```

### 2. Accesos directos
* **Aplicación Web (Frontend SPA):** [http://localhost:5000/index.html](http://localhost:5000/index.html)
* **Documentación Interactiva (Swagger UI):** [http://localhost:5000/swagger](http://localhost:5000/swagger)
* **Bandeja de Correos (Mailpit Web UI):** [http://localhost:8025](http://localhost:8025)
* **Hub WebSockets (SignalR):** `http://localhost:5000/hubs/auction`

### 3. Usuarios de Prueba Precargados (Seed Data)

| Rol / Escenario | Email | Contraseña | Saldo Total | Saldo Retenido | Saldo Disponible |
| :--- | :--- | :--- | :---: | :---: | :---: |
| **Vendedor** | `vendedor@test.com` | `123456` | $0 | $0 | $0 |
| **Comprador 1 (Líder)** | `comprador1@test.com` | `123456` | $150.000 | $45.000 | $105.000 |
| **Comprador 2** | `comprador2@test.com` | `123456` | $200.000 | $18.000 | $182.000 |
| **Sin Fondos** | `sinfondos@test.com` | `123456` | $500 | $0 | $500 |

### 4. Detener el entorno
```bash
docker compose down
```
*(Para reiniciar la base de datos desde cero eliminando el volumen: `docker compose down -v`).*

---

## Ejecución Local y Tests Unitarios (Sin Docker)

### Compilar la solución
```bash
dotnet build Backend/src/SubastaYa.WebApi/SubastaYa.WebApi.csproj
```

### Ejecutar los Tests Unitarios (xUnit + NSubstitute)
```bash
dotnet test Backend/src/SubastaYa.UnitTests/SubastaYa.UnitTests.csproj
```

### Aplicar Migraciones y Ejecutar la API
```bash
dotnet ef database update --project Backend/src/SubastaYa.Infrastructure --startup-project Backend/src/SubastaYa.WebApi
dotnet run --project Backend/src/SubastaYa.WebApi/SubastaYa.WebApi.csproj
```

---

## Prueba de Estrés y Concurrencia Optimista (Stress Test)

Para demostrar que dos ofertas simultáneas por el mismo monto no corrompen los saldos ni duplican la puja ganadora, el repositorio incluye scripts de estrés en **PowerShell** ([`stress-test.ps1`](stress-test.ps1)) y **Bash** ([`stress-test.sh`](stress-test.sh)).

```powershell
# En Windows PowerShell:
./stress-test.ps1
```
```bash
# En Linux / macOS / Git Bash:
./stress-test.sh
```

### Resultado del Test de Concurrencia
El script dispara dos peticiones `POST /api/v1/auctions/{id}/bids` en paralelo en el mismo milisegundo:
1. **Petición 1 (Ganadora):** Retiene el saldo en garantía, actualiza la versión de la fila y devuelve **`HTTP 201 Created`**.
2. **Petición 2 (Colisión):** Detecta el cambio de estado/versión, hace rollback, audita el conflicto en `AuditoriaLog` y devuelve **`HTTP 409 Conflict`**:
   ```json
   {
     "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
     "title": "Conflicto de concurrencia",
     "status": 409,
     "detail": "La subasta fue modificada por otro usuario. Por favor intente nuevamente."
   }
   ```
