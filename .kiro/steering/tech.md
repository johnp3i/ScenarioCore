# Tech Stack

- Language: C# (.NET 8.0)
- Project type: Console applications
- SDK: Microsoft.NET.Sdk
- IDE: Visual Studio 2022 (v17.14+)
- Solution format: Visual Studio Solution (.sln)
- Nullable reference types: enabled
- Implicit usings: enabled

## Key Libraries

- `System.Net.Http` — HTTP client for REST API calls
- `System.Net.Http.Json` — JSON serialization for HTTP requests
- `System.Text.Json` — JSON parsing and document handling

## Environment Configuration

- `RUNWAYML_API_SECRET` — API key for RunwayML, read from environment variables

## Common Commands

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run a specific project
dotnet run --project ConsolePoC
dotnet run --project PoC.Console

# Clean build artifacts
dotnet clean
```
