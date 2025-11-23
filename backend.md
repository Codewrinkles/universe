# Backend Project Setup - .NET 10

> **Purpose**: Setup the .NET backend project structure for Codewrinkles. This document focuses ONLY on creating the solution, projects, and folder structure. Feature implementation will be planned separately, one feature at a time.

---

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Technology Stack](#technology-stack)
3. [Project Structure](#project-structure)
4. [Setup Steps](#setup-steps)
5. [Configuration](#configuration)

---

## Architecture Overview

### Clean Architecture Layered Monolith

**Layers:**
- **API Layer** (`Codewrinkles.API`): Minimal APIs, endpoints, middleware
- **Application Layer** (`Codewrinkles.Application`): Use cases, commands, queries, Kommand handlers
- **Domain Layer** (`Codewrinkles.Domain`): Entities, value objects, domain events, business logic
- **Infrastructure Layer** (`Codewrinkles.Infrastructure`): EF Core, DbContext, external services, OAuth

**Dependency Flow:**
```
API → Application + Infrastructure
Application → Domain
Infrastructure → Application + Domain
Domain → (nothing)
```

**Key Principles:**
- ✅ Single database with schema-per-module (`identity`, `pulse`, `nova`)
- ✅ Single `ApplicationDbContext` with all DbSets
- ✅ Modules organized as folders (not separate projects)
- ✅ Kommand for CQRS and cross-module communication
- ✅ OpenTelemetry from the ground up
- ✅ Feature-based organization within modules (NOT convention-based like Commands/Queries folders)
- ✅ Custom authentication (no ASP.NET Core Identity)
- ✅ JWT + refresh tokens with revocation support
- ✅ OAuth with Google and GitHub
- 🚨 **NEVER add OSS libraries unless very complicated to build ourselves** (see CLAUDE.md)

---

## Technology Stack

### Core Framework
- **.NET 10** (latest)
- **C# 13**
- **ASP.NET Core Minimal APIs**

### Database
- **SQL Server** (latest)
- **Entity Framework Core 10**
- Schema-per-module approach

### Key Libraries
- **Kommand**: CQRS/mediator (https://github.com/Atherio-Ltd/Kommand)
- **BCrypt.Net-Next**: Password hashing
- **System.IdentityModel.Tokens.Jwt**: JWT token generation/validation
- **OpenTelemetry**: Distributed tracing and metrics
- **Scalar.AspNetCore**: OpenAPI documentation

### Testing
- **xUnit**: Unit and integration testing framework (plain assertions only)

### Development Tools
- **Docker Desktop**: SQL Server container (local development)
- **SQL Server Management Studio** or **Azure Data Studio**: Database management

---

## Project Structure

```
apps/
└── backend/
    ├── Codewrinkles.sln
    ├── src/
    │   ├── Codewrinkles.API/
    │   │   ├── Modules/
    │   │   │   ├── Identity/          # Identity endpoints
    │   │   │   ├── Pulse/             # Pulse endpoints
    │   │   │   └── Nova/              # Nova endpoints
    │   │   ├── Middleware/
    │   │   ├── Filters/
    │   │   ├── Extensions/
    │   │   ├── Program.cs
    │   │   ├── appsettings.json
    │   │   ├── appsettings.Development.json
    │   │   └── Codewrinkles.API.csproj
    │   │
    │   ├── Codewrinkles.Application/
    │   │   ├── Identity/              # Feature-based organization
    │   │   ├── Pulse/                 # Feature-based organization
    │   │   ├── Nova/                  # Feature-based organization
    │   │   ├── Common/
    │   │   ├── DependencyInjection.cs
    │   │   └── Codewrinkles.Application.csproj
    │   │
    │   ├── Codewrinkles.Domain/
    │   │   ├── Identity/              # Identity domain models
    │   │   ├── Pulse/                 # Pulse domain models
    │   │   ├── Nova/                  # Nova domain models
    │   │   ├── Common/
    │   │   └── Codewrinkles.Domain.csproj
    │   │
    │   └── Codewrinkles.Infrastructure/
    │       ├── Persistence/
    │       │   ├── ApplicationDbContext.cs
    │       │   ├── Configurations/
    │       │   │   ├── Identity/
    │       │   │   ├── Pulse/
    │       │   │   └── Nova/
    │       │   ├── Migrations/
    │       │   └── Interceptors/
    │       ├── Identity/              # Identity infrastructure (services, OAuth)
    │       ├── Pulse/                 # Pulse infrastructure
    │       ├── Nova/                  # Nova infrastructure
    │       ├── Common/
    │       ├── Options/
    │       ├── DependencyInjection.cs
    │       └── Codewrinkles.Infrastructure.csproj
    │
    └── tests/
        ├── Codewrinkles.UnitTests/
        └── Codewrinkles.IntegrationTests/
```

**Note**: Within each module folder (Identity, Pulse, Nova), use **feature-based organization** - NOT convention-based folders like Commands/Queries. We'll define the internal structure when implementing each feature.

---

## Setup Steps

### Step 1: Create Solution and Projects

Run these commands from the repository root (`D:\Dev\repos\universe`):

```bash
# Navigate to apps directory
cd apps

# Create backend directory
mkdir backend
cd backend

# Create solution
dotnet new sln -n Codewrinkles

# Create src directory
mkdir src

# Create projects
dotnet new webapi -n Codewrinkles.API -o src/Codewrinkles.API
dotnet new classlib -n Codewrinkles.Application -o src/Codewrinkles.Application
dotnet new classlib -n Codewrinkles.Domain -o src/Codewrinkles.Domain
dotnet new classlib -n Codewrinkles.Infrastructure -o src/Codewrinkles.Infrastructure

# Add projects to solution
dotnet sln add src/Codewrinkles.API/Codewrinkles.API.csproj
dotnet sln add src/Codewrinkles.Application/Codewrinkles.Application.csproj
dotnet sln add src/Codewrinkles.Domain/Codewrinkles.Domain.csproj
dotnet sln add src/Codewrinkles.Infrastructure/Codewrinkles.Infrastructure.csproj

# Add project references
dotnet add src/Codewrinkles.API/Codewrinkles.API.csproj reference src/Codewrinkles.Application/Codewrinkles.Application.csproj
dotnet add src/Codewrinkles.API/Codewrinkles.API.csproj reference src/Codewrinkles.Infrastructure/Codewrinkles.Infrastructure.csproj

dotnet add src/Codewrinkles.Application/Codewrinkles.Application.csproj reference src/Codewrinkles.Domain/Codewrinkles.Domain.csproj

dotnet add src/Codewrinkles.Infrastructure/Codewrinkles.Infrastructure.csproj reference src/Codewrinkles.Application/Codewrinkles.Application.csproj
dotnet add src/Codewrinkles.Infrastructure/Codewrinkles.Infrastructure.csproj reference src/Codewrinkles.Domain/Codewrinkles.Domain.csproj
```

---

### Step 2: Create Folder Structure

Create the high-level module folders:

```bash
# From apps/backend directory

# API module folders
mkdir -p src/Codewrinkles.API/Modules/Identity
mkdir -p src/Codewrinkles.API/Modules/Pulse
mkdir -p src/Codewrinkles.API/Modules/Nova
mkdir -p src/Codewrinkles.API/Middleware
mkdir -p src/Codewrinkles.API/Filters
mkdir -p src/Codewrinkles.API/Extensions

# Application module folders
mkdir -p src/Codewrinkles.Application/Identity
mkdir -p src/Codewrinkles.Application/Pulse
mkdir -p src/Codewrinkles.Application/Nova
mkdir -p src/Codewrinkles.Application/Common

# Domain module folders
mkdir -p src/Codewrinkles.Domain/Identity
mkdir -p src/Codewrinkles.Domain/Pulse
mkdir -p src/Codewrinkles.Domain/Nova
mkdir -p src/Codewrinkles.Domain/Common

# Infrastructure folders
mkdir -p src/Codewrinkles.Infrastructure/Persistence/Configurations/Identity
mkdir -p src/Codewrinkles.Infrastructure/Persistence/Configurations/Pulse
mkdir -p src/Codewrinkles.Infrastructure/Persistence/Configurations/Nova
mkdir -p src/Codewrinkles.Infrastructure/Persistence/Migrations
mkdir -p src/Codewrinkles.Infrastructure/Persistence/Interceptors
mkdir -p src/Codewrinkles.Infrastructure/Identity
mkdir -p src/Codewrinkles.Infrastructure/Pulse
mkdir -p src/Codewrinkles.Infrastructure/Nova
mkdir -p src/Codewrinkles.Infrastructure/Common
mkdir -p src/Codewrinkles.Infrastructure/Options

# Test projects directory (projects will be created in Step 3)
mkdir -p tests
```

---

### Step 3: Install NuGet Packages

#### Codewrinkles.API
```bash
cd src/Codewrinkles.API

dotnet add package Microsoft.AspNetCore.OpenApi
dotnet add package Scalar.AspNetCore

dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package Azure.Monitor.OpenTelemetry.Exporter

dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

cd ../..
```

#### Codewrinkles.Application
```bash
cd src/Codewrinkles.Application

# Add Kommand (adjust based on how it's published)
# If from NuGet:
dotnet add package Kommand
# If you need to build from source, clone the repo and add as project reference

cd ../..
```

#### Codewrinkles.Domain
```bash
# No external packages needed for pure domain layer
```

#### Codewrinkles.Infrastructure
```bash
cd src/Codewrinkles.Infrastructure

dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore

dotnet add package BCrypt.Net-Next
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.IdentityModel.Tokens

cd ../..
```

#### Test Projects (Optional - Setup when needed)
```bash
# Unit Tests
dotnet new xunit -n Codewrinkles.UnitTests -o tests/Codewrinkles.UnitTests
dotnet sln add tests/Codewrinkles.UnitTests/Codewrinkles.UnitTests.csproj

# Integration Tests
dotnet new xunit -n Codewrinkles.IntegrationTests -o tests/Codewrinkles.IntegrationTests
dotnet sln add tests/Codewrinkles.IntegrationTests/Codewrinkles.IntegrationTests.csproj

cd tests/Codewrinkles.IntegrationTests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
cd ../..
```

---

### Step 4: Setup SQL Server (Docker)

Run SQL Server in Docker for local development:

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sqlserver-codewrinkles \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

**Connection String:**
```
Server=localhost,1433;Database=Codewrinkles;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
```

---

### Step 5: Create Basic Application DependencyInjection

**File: `src/Codewrinkles.Application/DependencyInjection.cs`**
```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Codewrinkles.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Kommand from this assembly
        // services.AddKommand(typeof(DependencyInjection).Assembly);

        return services;
    }
}
```

---

### Step 6: Create Basic Infrastructure DependencyInjection

**File: `src/Codewrinkles.Infrastructure/DependencyInjection.cs`**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Codewrinkles.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database (uncomment when ApplicationDbContext is created)
        // services.AddDbContext<ApplicationDbContext>(options =>
        // {
        //     options.UseSqlServer(
        //         configuration.GetConnectionString("DefaultConnection"),
        //         b => b.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName));
        // });

        return services;
    }
}
```

---

### Step 7: Setup Program.cs

**File: `src/Codewrinkles.API/Program.cs`**
```csharp
using Codewrinkles.Application;
using Codewrinkles.Infrastructure;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add Application layer
builder.Services.AddApplication();

// Add Infrastructure layer
builder.Services.AddInfrastructure(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // React frontend
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Codewrinkles.API"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("Codewrinkles.*")
        .AddConsoleExporter()
    )
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddConsoleExporter()
    );

// Add OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();

// Map endpoints (will be added per feature)

app.Run();
```

---

### Step 8: Configure appsettings.json

**File: `src/Codewrinkles.API/appsettings.json`**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=Codewrinkles;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
  },
  "Jwt": {
    "SecretKey": "your-super-secret-key-min-32-chars-long-change-this-in-production",
    "Issuer": "Codewrinkles",
    "Audience": "Codewrinkles",
    "AccessTokenExpiryMinutes": 15
  },
  "OAuth": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret",
      "RedirectUri": "http://localhost:5173/oauth/google/callback"
    },
    "GitHub": {
      "ClientId": "your-github-client-id",
      "ClientSecret": "your-github-client-secret",
      "RedirectUri": "http://localhost:5173/oauth/github/callback"
    }
  }
}
```

**File: `src/Codewrinkles.API/appsettings.Development.json`**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

---

### Step 9: Verify Setup

Run the API to verify everything compiles:

```bash
cd src/Codewrinkles.API
dotnet build
dotnet run
```

You should see:
- API starts successfully
- Scalar API documentation available at `http://localhost:5000/scalar/v1` (or similar port)
- No compilation errors

---

## Configuration

### Development Environment
- **Frontend**: `http://localhost:5173` (React dev server)
- **Backend**: `http://localhost:5000` (or assigned port)
- **Database**: SQL Server on `localhost:1433`

### Project References Summary
```
Codewrinkles.API
  ├─> Codewrinkles.Application
  └─> Codewrinkles.Infrastructure

Codewrinkles.Application
  └─> Codewrinkles.Domain

Codewrinkles.Infrastructure
  ├─> Codewrinkles.Application
  └─> Codewrinkles.Domain

Codewrinkles.Domain
  └─> (no dependencies)
```

---

## Next Steps

After project setup is complete:

1. **Plan Feature 1** - Define the first feature to implement (e.g., User Registration)
2. **Implement Feature** - Create domain entities, handlers, endpoints for that feature
3. **Database Migration** - Add EF Core migration for that feature
4. **Test Feature** - Verify feature works end-to-end
5. **Repeat** - Plan and implement next feature

**Feature implementation will be planned one at a time, not all upfront.**

---

## Notes

### Key Setup Decisions:
- ✅ Solution location: `apps/backend/Codewrinkles.sln`
- ✅ Source projects: `apps/backend/src/`
- ✅ Test projects: `apps/backend/tests/` (xUnit)
- ✅ Clean Architecture with 4 layers
- ✅ OpenTelemetry configured from the start
- ✅ CORS configured for React frontend
- ✅ Scalar for API documentation (instead of Swagger)
- ✅ Kommand ready to use
- ✅ Module folders created (Identity, Pulse, Nova)
- ✅ Feature-based organization (not convention-based)

### What's NOT Included Yet:
- ❌ Domain entities (will be added per feature)
- ❌ Command/query handlers (will be added per feature)
- ❌ API endpoints (will be added per feature)
- ❌ Database schema (will be added per feature)
- ❌ EF Core configurations (will be added per feature)
- ❌ Authentication middleware (will be added when implementing auth feature)

### Important Reminders:
- 🚨 **ALWAYS ASK before adding any NuGet package or library** - see CLAUDE.md Library Usage Policy
- ✅ Build features ourselves unless very complicated
- ✅ Keep dependencies minimal
- ✅ Feature-based organization, not convention-based

---

**End of Backend Project Setup**
