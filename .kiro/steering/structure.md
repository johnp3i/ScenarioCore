# Project Structure

```
ScenarioCore.sln              # Solution file (only ConsolePoC is included)
├── ConsolePoC/               # Main PoC — full RunwayML text-to-video integration
│   ├── ConsolePoC.csproj
│   └── Program.cs            # API call, polling loop, video download
├── PoC.Console/              # Secondary PoC — minimal scaffold, not in solution
│   ├── PoC.Console.csproj
│   └── Program.cs            # Stub with env var check only
└── .kiro/
    └── steering/             # AI assistant steering rules
```

## Notes

- `ConsolePoC` is the active project included in the solution file. `PoC.Console` exists on disk but is not referenced in `ScenarioCore.sln`.
- Each project is a standalone console app with its own `Program.cs` entry point.
- No shared libraries, test projects, or class libraries exist yet.
- No NuGet package references beyond the implicit SDK packages.
