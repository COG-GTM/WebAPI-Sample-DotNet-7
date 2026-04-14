# GraphQL API Specification

## 1. Overview

### Purpose

This specification document outlines the design and implementation plan for adding a GraphQL API to the existing ASP.NET Core Web API (.NET 10) sample project. The GraphQL API will provide an alternative interface to the same underlying data and business logic that the REST API currently uses, with both APIs coexisting and sharing the same PostgreSQL database.

### Current Architecture

The project follows Clean Architecture with four distinct layers:

```
┌─────────────────────────────────────────────────────────────┐
│                        WebApi Layer                          │
│  (REST Controllers, Program.cs configuration)                │
│  src/WebApi/                                                 │
├─────────────────────────────────────────────────────────────┤
│                     Application Layer                        │
│  (Services, DTOs, IUnitOfWork interface)                    │
│  src/Application/                                            │
├─────────────────────────────────────────────────────────────┤
│                       Domain Layer                           │
│  (Entities, Repository interfaces)                          │
│  src/Domain/                                                 │
├─────────────────────────────────────────────────────────────┤
│                    Infrastructure Layer                      │
│  (DbContext, Repositories, UnitOfWork implementation)       │
│  src/Infrastructure/                                         │
└─────────────────────────────────────────────────────────────┘
```

### Existing Education Entity

The current API manages Education records with the following fields:

| Field | Type | Constraints |
|-------|------|-------------|
| Id | UUID (Guid) | Primary Key |
| Degree | string | Required, max 50 characters |
| FieldOfStudy | string | Required, max 250 characters |
| School | string | Required, max 250 characters |
| Description | string | Optional, max 1000 characters |

---

## 2. Technical Stack

### GraphQL Framework

**HotChocolate** (v14.x+) will be used as the GraphQL server implementation for ASP.NET Core. HotChocolate is the most popular and feature-rich GraphQL server for .NET with excellent performance and comprehensive tooling.

#### Required NuGet Packages

```xml
<!-- Core HotChocolate packages -->
<PackageReference Include="HotChocolate.AspNetCore" Version="14.3.0" />
<PackageReference Include="HotChocolate.Data" Version="14.3.0" />
<PackageReference Include="HotChocolate.Data.EntityFramework" Version="14.3.0" />

<!-- Optional: For authorization -->
<PackageReference Include="HotChocolate.AspNetCore.Authorization" Version="14.3.0" />
```

### Integration Points with Existing Services

The GraphQL layer will integrate with the existing codebase by:

1. **Reusing `IEducationService`** (`src/Application/Service/Interfaces/IEducationService.cs`) - All business logic remains in the Application layer
2. **Reusing `EducationDto`** (`src/Application/Dtos/EducationDto.cs`) - Consistent data transfer objects across both APIs
3. **Reusing `IUnitOfWork`** (`src/Application/UoW/IUnitOfWork.cs`) - Same unit of work pattern for data access
4. **Sharing `SampleDbContext`** (`src/Infrastructure/DbContexts/SampleDbContext.cs`) - Single database context for both APIs

---

## 3. Project Structure

### New GraphQL Project Location

GraphQL components will be added directly to the existing `WebApi` project to maintain simplicity and avoid unnecessary project proliferation. The GraphQL-specific code will be organized in a dedicated folder structure:

```
src/WebApi/
├── Controllers/
│   └── EducationsController.cs          # Existing REST controller
├── GraphQL/
│   ├── Queries/
│   │   └── EducationQueries.cs          # Query resolvers
│   ├── Mutations/
│   │   └── EducationMutations.cs        # Mutation resolvers
│   ├── Types/
│   │   ├── EducationType.cs             # GraphQL object type
│   │   └── EducationInputType.cs        # GraphQL input types
│   └── DataLoaders/
│       └── EducationDataLoader.cs       # DataLoader for batching
├── Program.cs                            # Updated with GraphQL configuration
└── ...
```

### Solution Updates

The `Sample.sln` file does not require modifications since GraphQL components are added to the existing `WebApi` project.

Update `src/WebApi/WebApi.csproj` to include HotChocolate packages:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="7.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="7.0.9" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="7.0.10">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    
    <!-- HotChocolate GraphQL packages -->
    <PackageReference Include="HotChocolate.AspNetCore" Version="14.3.0" />
    <PackageReference Include="HotChocolate.Data" Version="14.3.0" />
    <PackageReference Include="HotChocolate.Data.EntityFramework" Version="14.3.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

---

## 4. Schema Design

### GraphQL Schema Definition

```graphql
# Types
type Education {
  id: UUID!
  degree: String!
  fieldOfStudy: String!
  school: String!
  description: String
}

# Input Types
input CreateEducationInput {
  degree: String!
  fieldOfStudy: String!
  school: String!
  description: String
}

input UpdateEducationInput {
  id: UUID!
  degree: String!
  fieldOfStudy: String!
  school: String!
  description: String
}

# Queries
type Query {
  """
  Retrieve all education records
  """
  educations: [Education!]!
  
  """
  Retrieve a single education record by ID
  """
  education(id: UUID!): Education
}

# Mutations
type Mutation {
  """
  Create a new education record
  """
  createEducation(input: CreateEducationInput!): EducationPayload!
  
  """
  Update an existing education record
  """
  updateEducation(input: UpdateEducationInput!): EducationPayload!
  
  """
  Delete an education record by ID
  """
  deleteEducation(id: UUID!): DeleteEducationPayload!
}

# Payload Types
type EducationPayload {
  education: Education
  errors: [Error!]
}

type DeleteEducationPayload {
  success: Boolean!
  errors: [Error!]
}

type Error {
  message: String!
  code: String
}
```

### Query Operations

| Operation | Description | Arguments | Return Type |
|-----------|-------------|-----------|-------------|
| `educations` | Fetch all education records | None | `[Education!]!` |
| `education` | Fetch single record by ID | `id: UUID!` | `Education` |

### Mutation Operations

| Operation | Description | Arguments | Return Type |
|-----------|-------------|-----------|-------------|
| `createEducation` | Create new education record | `input: CreateEducationInput!` | `EducationPayload!` |
| `updateEducation` | Update existing record | `input: UpdateEducationInput!` | `EducationPayload!` |
| `deleteEducation` | Delete record by ID | `id: UUID!` | `DeleteEducationPayload!` |

---

## 5. Implementation Components

### Query Resolvers

**File: `src/WebApi/GraphQL/Queries/EducationQueries.cs`**

```csharp
using Application.Dtos;
using Application.Service.Interfaces;

namespace WebApi.GraphQL.Queries;

public class EducationQueries
{
    public async Task<IEnumerable<EducationDto>> GetEducations(
        [Service] IEducationService educationService)
    {
        return await educationService.GetAll();
    }

    public async Task<EducationDto?> GetEducation(
        Guid id,
        [Service] IEducationService educationService)
    {
        return await educationService.GetById(id);
    }
}
```

### Mutation Resolvers

**File: `src/WebApi/GraphQL/Mutations/EducationMutations.cs`**

```csharp
using Application.Dtos;
using Application.Service.Interfaces;
using WebApi.GraphQL.Types;

namespace WebApi.GraphQL.Mutations;

public class EducationMutations
{
    public async Task<EducationPayload> CreateEducation(
        CreateEducationInput input,
        [Service] IEducationService educationService)
    {
        try
        {
            var dto = new EducationDto
            {
                Degree = input.Degree,
                FieldOfStudy = input.FieldOfStudy,
                School = input.School,
                Description = input.Description
            };

            var result = await educationService.Add(dto);
            return new EducationPayload(result);
        }
        catch (Exception ex)
        {
            return new EducationPayload(new Error(ex.Message, "CREATE_FAILED"));
        }
    }

    public async Task<EducationPayload> UpdateEducation(
        UpdateEducationInput input,
        [Service] IEducationService educationService)
    {
        try
        {
            var dto = new EducationDto
            {
                Id = input.Id,
                Degree = input.Degree,
                FieldOfStudy = input.FieldOfStudy,
                School = input.School,
                Description = input.Description
            };

            var success = await educationService.Update(input.Id, dto);
            if (!success)
            {
                return new EducationPayload(new Error("Education record not found or update failed", "UPDATE_FAILED"));
            }

            var updated = await educationService.GetById(input.Id);
            return new EducationPayload(updated);
        }
        catch (Exception ex)
        {
            return new EducationPayload(new Error(ex.Message, "UPDATE_FAILED"));
        }
    }

    public async Task<DeleteEducationPayload> DeleteEducation(
        Guid id,
        [Service] IEducationService educationService)
    {
        try
        {
            var success = await educationService.Delete(id);
            if (!success)
            {
                return new DeleteEducationPayload(false, new Error("Education record not found", "NOT_FOUND"));
            }
            return new DeleteEducationPayload(true);
        }
        catch (Exception ex)
        {
            return new DeleteEducationPayload(false, new Error(ex.Message, "DELETE_FAILED"));
        }
    }
}
```

### Object Types

**File: `src/WebApi/GraphQL/Types/EducationType.cs`**

```csharp
using Application.Dtos;

namespace WebApi.GraphQL.Types;

public class EducationType : ObjectType<EducationDto>
{
    protected override void Configure(IObjectTypeDescriptor<EducationDto> descriptor)
    {
        descriptor.Name("Education");
        
        descriptor.Field(e => e.Id)
            .Type<NonNullType<UuidType>>()
            .Description("The unique identifier of the education record");

        descriptor.Field(e => e.Degree)
            .Type<NonNullType<StringType>>()
            .Description("The degree obtained (max 50 characters)");

        descriptor.Field(e => e.FieldOfStudy)
            .Type<NonNullType<StringType>>()
            .Description("The field of study (max 250 characters)");

        descriptor.Field(e => e.School)
            .Type<NonNullType<StringType>>()
            .Description("The school or institution name (max 250 characters)");

        descriptor.Field(e => e.Description)
            .Type<StringType>()
            .Description("Optional description of the education (max 1000 characters)");
    }
}
```

### Input Types

**File: `src/WebApi/GraphQL/Types/EducationInputType.cs`**

```csharp
namespace WebApi.GraphQL.Types;

public record CreateEducationInput(
    string Degree,
    string FieldOfStudy,
    string School,
    string? Description);

public record UpdateEducationInput(
    Guid Id,
    string Degree,
    string FieldOfStudy,
    string School,
    string? Description);

public class CreateEducationInputType : InputObjectType<CreateEducationInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<CreateEducationInput> descriptor)
    {
        descriptor.Name("CreateEducationInput");

        descriptor.Field(i => i.Degree)
            .Type<NonNullType<StringType>>()
            .Description("The degree to be obtained (required, max 50 characters)");

        descriptor.Field(i => i.FieldOfStudy)
            .Type<NonNullType<StringType>>()
            .Description("The field of study (required, max 250 characters)");

        descriptor.Field(i => i.School)
            .Type<NonNullType<StringType>>()
            .Description("The school or institution name (required, max 250 characters)");

        descriptor.Field(i => i.Description)
            .Type<StringType>()
            .Description("Optional description of the education (max 1000 characters)");
    }
}

public class UpdateEducationInputType : InputObjectType<UpdateEducationInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<UpdateEducationInput> descriptor)
    {
        descriptor.Name("UpdateEducationInput");

        descriptor.Field(i => i.Id)
            .Type<NonNullType<UuidType>>()
            .Description("The unique identifier of the education record to update");

        descriptor.Field(i => i.Degree)
            .Type<NonNullType<StringType>>()
            .Description("The degree (required, max 50 characters)");

        descriptor.Field(i => i.FieldOfStudy)
            .Type<NonNullType<StringType>>()
            .Description("The field of study (required, max 250 characters)");

        descriptor.Field(i => i.School)
            .Type<NonNullType<StringType>>()
            .Description("The school or institution name (required, max 250 characters)");

        descriptor.Field(i => i.Description)
            .Type<StringType>()
            .Description("Optional description of the education (max 1000 characters)");
    }
}
```

### Payload Types

**File: `src/WebApi/GraphQL/Types/PayloadTypes.cs`**

```csharp
using Application.Dtos;

namespace WebApi.GraphQL.Types;

public record Error(string Message, string? Code = null);

public class EducationPayload
{
    public EducationDto? Education { get; }
    public IReadOnlyList<Error>? Errors { get; }

    public EducationPayload(EducationDto? education)
    {
        Education = education;
    }

    public EducationPayload(Error error)
    {
        Errors = new[] { error };
    }

    public EducationPayload(IReadOnlyList<Error> errors)
    {
        Errors = errors;
    }
}

public class DeleteEducationPayload
{
    public bool Success { get; }
    public IReadOnlyList<Error>? Errors { get; }

    public DeleteEducationPayload(bool success, Error? error = null)
    {
        Success = success;
        if (error != null)
        {
            Errors = new[] { error };
        }
    }
}
```

---

## 6. Configuration

### Updates to `src/WebApi/Program.cs`

The following changes are required to integrate GraphQL with the existing application:

```csharp
using Application.Interfaces;
using Application.Service.Interfaces;
using Application.Service;
using Infrastructure.DbContexts;
using Infrastructure.UoW;
using Microsoft.EntityFrameworkCore;
using WebApi.GraphQL.Queries;
using WebApi.GraphQL.Mutations;
using WebApi.GraphQL.Types;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<SampleDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.UseNodaTime()));
builder.Services.AddHealthChecks().AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"), name: "SampleDB");

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEducationService, EducationService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// GraphQL Configuration
builder.Services
    .AddGraphQLServer()
    .AddQueryType<EducationQueries>()
    .AddMutationType<EducationMutations>()
    .AddType<EducationType>()
    .AddType<CreateEducationInputType>()
    .AddType<UpdateEducationInputType>()
    .AddFiltering()
    .AddSorting()
    .AddProjections();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map GraphQL endpoint
app.MapGraphQL("/graphql");

// Optional: Map Banana Cake Pop (GraphQL IDE) in development
if (app.Environment.IsDevelopment())
{
    app.MapBananaCakePop("/graphql-ui");
}

app.Run();
```

### Key Configuration Points

1. **Service Registration**: GraphQL services are registered using `AddGraphQLServer()`
2. **Query/Mutation Types**: Registered via `AddQueryType<>()` and `AddMutationType<>()`
3. **Object Types**: Custom types registered via `AddType<>()`
4. **Data Features**: Filtering, sorting, and projections enabled for future extensibility
5. **Endpoint Mapping**: GraphQL endpoint mapped to `/graphql`
6. **Development Tools**: Banana Cake Pop IDE available at `/graphql-ui` in development mode

---

## 7. Testing Strategy

### New Test Project Structure

Create a new test project following the existing xUnit and FakeItEasy patterns:

```
tests/
├── Application.Tests/
│   └── EducationServiceTests.cs         # Existing tests
├── WebApi.Tests/
│   └── EducationsControllerTests.cs     # Existing REST tests
└── GraphQL.Tests/                        # New GraphQL test project
    ├── GraphQL.Tests.csproj
    ├── Usings.cs
    ├── Queries/
    │   └── EducationQueriesTests.cs
    └── Mutations/
        └── EducationMutationsTests.cs
```

### Test Project Configuration

**File: `tests/GraphQL.Tests/GraphQL.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FakeItEasy" Version="7.4.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.5.0" />
    <PackageReference Include="xunit" Version="2.4.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.4.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="3.2.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="HotChocolate.Execution.Abstractions" Version="14.3.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Application\Application.csproj" />
    <ProjectReference Include="..\..\src\WebApi\WebApi.csproj" />
  </ItemGroup>

</Project>
```

### Sample Query Tests

**File: `tests/GraphQL.Tests/Queries/EducationQueriesTests.cs`**

```csharp
using Application.Dtos;
using Application.Service.Interfaces;
using FakeItEasy;
using WebApi.GraphQL.Queries;

namespace GraphQL.Tests.Queries;

public class EducationQueriesTests
{
    private readonly IEducationService _educationService;
    private readonly EducationQueries _queries;

    public EducationQueriesTests()
    {
        _educationService = A.Fake<IEducationService>();
        _queries = new EducationQueries();
    }

    [Fact]
    public async Task GetEducations_Returns_All_Education_Records()
    {
        // Arrange
        var expectedEducations = A.CollectionOfDummy<EducationDto>(3).ToList();
        A.CallTo(() => _educationService.GetAll()).Returns(expectedEducations);

        // Act
        var result = await _queries.GetEducations(_educationService);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        A.CallTo(() => _educationService.GetAll()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetEducation_With_Valid_Id_Returns_Education()
    {
        // Arrange
        var educationId = Guid.NewGuid();
        var expectedEducation = A.Dummy<EducationDto>();
        expectedEducation.Id = educationId;
        A.CallTo(() => _educationService.GetById(educationId)).Returns(expectedEducation);

        // Act
        var result = await _queries.GetEducation(educationId, _educationService);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(educationId, result.Id);
        A.CallTo(() => _educationService.GetById(educationId)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetEducation_With_Invalid_Id_Returns_Null()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        A.CallTo(() => _educationService.GetById(invalidId)).Returns((EducationDto?)null);

        // Act
        var result = await _queries.GetEducation(invalidId, _educationService);

        // Assert
        Assert.Null(result);
    }
}
```

### Sample Mutation Tests

**File: `tests/GraphQL.Tests/Mutations/EducationMutationsTests.cs`**

```csharp
using Application.Dtos;
using Application.Service.Interfaces;
using FakeItEasy;
using WebApi.GraphQL.Mutations;
using WebApi.GraphQL.Types;

namespace GraphQL.Tests.Mutations;

public class EducationMutationsTests
{
    private readonly IEducationService _educationService;
    private readonly EducationMutations _mutations;

    public EducationMutationsTests()
    {
        _educationService = A.Fake<IEducationService>();
        _mutations = new EducationMutations();
    }

    [Fact]
    public async Task CreateEducation_With_Valid_Input_Returns_Success_Payload()
    {
        // Arrange
        var input = new CreateEducationInput(
            "Bachelor's degree",
            "Computer Science",
            "MIT",
            "Description");

        var createdEducation = new EducationDto
        {
            Id = Guid.NewGuid(),
            Degree = input.Degree,
            FieldOfStudy = input.FieldOfStudy,
            School = input.School,
            Description = input.Description
        };

        A.CallTo(() => _educationService.Add(A<EducationDto>._)).Returns(createdEducation);

        // Act
        var result = await _mutations.CreateEducation(input, _educationService);

        // Assert
        Assert.NotNull(result.Education);
        Assert.Null(result.Errors);
        Assert.Equal(input.Degree, result.Education.Degree);
    }

    [Fact]
    public async Task DeleteEducation_With_Valid_Id_Returns_Success()
    {
        // Arrange
        var educationId = Guid.NewGuid();
        A.CallTo(() => _educationService.Delete(educationId)).Returns(true);

        // Act
        var result = await _mutations.DeleteEducation(educationId, _educationService);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task DeleteEducation_With_Invalid_Id_Returns_Error()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        A.CallTo(() => _educationService.Delete(invalidId)).Returns(false);

        // Act
        var result = await _mutations.DeleteEducation(invalidId, _educationService);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "NOT_FOUND");
    }
}
```

---

## 8. Docker Configuration

### How GraphQL Fits into Existing Docker Setup

The existing Docker configuration in `Dockerfile` and `docker-compose.yml` requires minimal changes since GraphQL is added to the existing WebApi project.

#### Dockerfile

No changes required. The existing Dockerfile already builds and publishes the WebApi project, which will now include GraphQL components:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
RUN pwd && ls /
WORKDIR /src
COPY ["src/WebApi/WebApi.csproj", "src/WebApi/"]
RUN pwd && ls
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
RUN dotnet restore "./src/WebApi/WebApi.csproj"
COPY . .
WORKDIR "/src/src/WebApi"
RUN dotnet build "WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApi.dll"]
```

#### docker-compose.yml

No changes required. The existing configuration exposes port 5000, which will serve both REST and GraphQL endpoints:

- REST API: `http://localhost:5000/api/educations`
- GraphQL API: `http://localhost:5000/graphql`
- GraphQL IDE (dev): `http://localhost:5000/graphql-ui`

### Endpoint Summary

| Service | URL | Description |
|---------|-----|-------------|
| REST API | `http://localhost:5000/api/educations` | Existing REST endpoints |
| GraphQL API | `http://localhost:5000/graphql` | New GraphQL endpoint |
| GraphQL IDE | `http://localhost:5000/graphql-ui` | Banana Cake Pop (development only) |
| Swagger UI | `http://localhost:5000/swagger` | REST API documentation |
| PostgreSQL | `localhost:5433` | Database |
| PgAdmin | `http://localhost:8080` | Database management |

---

## 9. Documentation Requirements

### README Updates

The `README.md` file should be updated to include GraphQL API information:

#### Suggested Additions

```markdown
## API Endpoints

### REST API
- Base URL: `http://localhost:5000/api`
- Swagger UI: `http://localhost:5000/swagger`

### GraphQL API
- Endpoint: `http://localhost:5000/graphql`
- GraphQL IDE: `http://localhost:5000/graphql-ui` (development only)

## GraphQL Examples

### Query all educations
```graphql
query {
  educations {
    id
    degree
    fieldOfStudy
    school
    description
  }
}
```

### Query single education
```graphql
query {
  education(id: "your-uuid-here") {
    id
    degree
    fieldOfStudy
    school
    description
  }
}
```

### Create education
```graphql
mutation {
  createEducation(input: {
    degree: "Master's degree"
    fieldOfStudy: "Data Science"
    school: "Stanford University"
    description: "Graduate program"
  }) {
    education {
      id
      degree
      fieldOfStudy
      school
    }
    errors {
      message
      code
    }
  }
}
```

### Update education
```graphql
mutation {
  updateEducation(input: {
    id: "your-uuid-here"
    degree: "PhD"
    fieldOfStudy: "Machine Learning"
    school: "Stanford University"
    description: "Doctoral program"
  }) {
    education {
      id
      degree
    }
    errors {
      message
    }
  }
}
```

### Delete education
```graphql
mutation {
  deleteEducation(id: "your-uuid-here") {
    success
    errors {
      message
      code
    }
  }
}
```
```

---

## 10. Migration Path

### REST and GraphQL Coexistence

Both APIs will coexist and share the same underlying infrastructure:

```
                    ┌─────────────────────┐
                    │     Client Apps     │
                    └─────────┬───────────┘
                              │
              ┌───────────────┴───────────────┐
              │                               │
              ▼                               ▼
    ┌─────────────────┐             ┌─────────────────┐
    │    REST API     │             │   GraphQL API   │
    │  /api/educations│             │    /graphql     │
    └────────┬────────┘             └────────┬────────┘
             │                               │
             └───────────────┬───────────────┘
                             │
                             ▼
                   ┌─────────────────┐
                   │ IEducationService│
                   │ (Application)    │
                   └────────┬────────┘
                            │
                            ▼
                   ┌─────────────────┐
                   │   IUnitOfWork   │
                   │ (Infrastructure)│
                   └────────┬────────┘
                            │
                            ▼
                   ┌─────────────────┐
                   │  PostgreSQL DB  │
                   └─────────────────┘
```

### Shared Components

| Component | Location | Used By |
|-----------|----------|---------|
| `IEducationService` | `src/Application/Service/Interfaces/` | REST + GraphQL |
| `EducationService` | `src/Application/Service/` | REST + GraphQL |
| `EducationDto` | `src/Application/Dtos/` | REST + GraphQL |
| `IUnitOfWork` | `src/Application/UoW/` | REST + GraphQL |
| `UnitOfWork` | `src/Infrastructure/UoW/` | REST + GraphQL |
| `SampleDbContext` | `src/Infrastructure/DbContexts/` | REST + GraphQL |
| `Education` (Entity) | `src/Domain/Entities/` | REST + GraphQL |

### Migration Considerations

1. **No Breaking Changes**: The REST API remains fully functional and unchanged
2. **Gradual Adoption**: Clients can migrate to GraphQL at their own pace
3. **Feature Parity**: GraphQL provides equivalent functionality to REST
4. **Consistent Data**: Both APIs operate on the same database and business logic
5. **Independent Scaling**: Each API can be monitored and optimized independently

---

## 11. Performance Considerations

### DataLoader Pattern

Implement DataLoader to prevent N+1 query problems when the schema expands to include related entities:

**File: `src/WebApi/GraphQL/DataLoaders/EducationDataLoader.cs`**

```csharp
using Application.Dtos;
using Application.Service.Interfaces;

namespace WebApi.GraphQL.DataLoaders;

public class EducationBatchDataLoader : BatchDataLoader<Guid, EducationDto>
{
    private readonly IEducationService _educationService;

    public EducationBatchDataLoader(
        IEducationService educationService,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _educationService = educationService;
    }

    protected override async Task<IReadOnlyDictionary<Guid, EducationDto>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        var educations = await _educationService.GetAll();
        return educations
            .Where(e => keys.Contains(e.Id))
            .ToDictionary(e => e.Id);
    }
}
```

### Query Complexity Analysis

Configure query complexity limits to prevent expensive queries:

```csharp
// In Program.cs
builder.Services
    .AddGraphQLServer()
    .AddQueryType<EducationQueries>()
    .AddMutationType<EducationMutations>()
    // ... other configurations
    .AddMaxExecutionDepthRule(10)
    .SetRequestOptions(_ => new HotChocolate.Execution.Options.RequestExecutorOptions
    {
        ExecutionTimeout = TimeSpan.FromSeconds(30)
    });
```

### Caching Strategies

1. **Response Caching**: Implement HTTP caching headers for GET-equivalent queries
2. **DataLoader Caching**: DataLoader provides request-scoped caching by default
3. **Persisted Queries**: Consider implementing persisted queries for production

```csharp
// Enable persisted queries (optional)
builder.Services
    .AddGraphQLServer()
    // ... other configurations
    .UsePersistedQueryPipeline()
    .AddReadOnlyFileSystemQueryStorage("./persisted-queries");
```

### Projections

Use HotChocolate projections to optimize database queries:

```csharp
// In EducationQueries.cs
[UseProjection]
public IQueryable<Education> GetEducationsOptimized(
    [Service] SampleDbContext context)
{
    return context.Educations;
}
```

---

## 12. Security

### Authorization

Integrate with ASP.NET Core authorization:

```csharp
// In Program.cs
builder.Services
    .AddGraphQLServer()
    // ... other configurations
    .AddAuthorization();

// In EducationMutations.cs
[Authorize]
public async Task<EducationPayload> CreateEducation(...)
{
    // Only authenticated users can create
}

[Authorize(Roles = new[] { "Admin" })]
public async Task<DeleteEducationPayload> DeleteEducation(...)
{
    // Only admins can delete
}
```

### Query Depth Limiting

Prevent deeply nested queries that could cause performance issues:

```csharp
builder.Services
    .AddGraphQLServer()
    // ... other configurations
    .AddMaxExecutionDepthRule(10);  // Maximum query depth of 10 levels
```

### Rate Limiting

Implement rate limiting using ASP.NET Core middleware:

```csharp
// In Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Apply to GraphQL endpoint
app.UseRateLimiter();
```

### Input Validation

Leverage HotChocolate's built-in validation and add custom validators:

```csharp
public class CreateEducationInputValidator : AbstractValidator<CreateEducationInput>
{
    public CreateEducationInputValidator()
    {
        RuleFor(x => x.Degree)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.FieldOfStudy)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(x => x.School)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}
```

### Security Best Practices Summary

| Security Measure | Implementation | Purpose |
|-----------------|----------------|---------|
| Query Depth Limiting | `AddMaxExecutionDepthRule(10)` | Prevent resource exhaustion |
| Execution Timeout | `ExecutionTimeout = 30s` | Prevent long-running queries |
| Authorization | `[Authorize]` attribute | Protect sensitive operations |
| Rate Limiting | ASP.NET Core Rate Limiter | Prevent abuse |
| Input Validation | FluentValidation | Ensure data integrity |
| Introspection Control | Disable in production | Hide schema details |

### Disabling Introspection in Production

```csharp
if (!app.Environment.IsDevelopment())
{
    builder.Services
        .AddGraphQLServer()
        // ... other configurations
        .AddIntrospectionAllowedRule(); // Remove this line in production
}
```

---

## Appendix A: File Summary

| File Path | Purpose |
|-----------|---------|
| `src/WebApi/WebApi.csproj` | Add HotChocolate NuGet packages |
| `src/WebApi/Program.cs` | Configure GraphQL services and endpoints |
| `src/WebApi/GraphQL/Queries/EducationQueries.cs` | Query resolvers |
| `src/WebApi/GraphQL/Mutations/EducationMutations.cs` | Mutation resolvers |
| `src/WebApi/GraphQL/Types/EducationType.cs` | Education object type |
| `src/WebApi/GraphQL/Types/EducationInputType.cs` | Input types for mutations |
| `src/WebApi/GraphQL/Types/PayloadTypes.cs` | Response payload types |
| `src/WebApi/GraphQL/DataLoaders/EducationDataLoader.cs` | DataLoader for batching |
| `tests/GraphQL.Tests/GraphQL.Tests.csproj` | Test project configuration |
| `tests/GraphQL.Tests/Queries/EducationQueriesTests.cs` | Query unit tests |
| `tests/GraphQL.Tests/Mutations/EducationMutationsTests.cs` | Mutation unit tests |
| `README.md` | Documentation updates |

---

## Appendix B: Implementation Checklist

- [ ] Add HotChocolate NuGet packages to `src/WebApi/WebApi.csproj`
- [ ] Create `src/WebApi/GraphQL/` directory structure
- [ ] Implement `EducationQueries.cs`
- [ ] Implement `EducationMutations.cs`
- [ ] Implement `EducationType.cs`
- [ ] Implement `EducationInputType.cs`
- [ ] Implement `PayloadTypes.cs`
- [ ] Implement `EducationDataLoader.cs`
- [ ] Update `src/WebApi/Program.cs` with GraphQL configuration
- [ ] Create `tests/GraphQL.Tests/` project
- [ ] Implement query tests
- [ ] Implement mutation tests
- [ ] Update `Sample.sln` to include new test project
- [ ] Update `README.md` with GraphQL documentation
- [ ] Test GraphQL endpoint locally
- [ ] Verify Docker build and deployment
- [ ] Configure security measures for production
