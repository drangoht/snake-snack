# Pitfalls — Pure logic and engine-free tests (`Rules/` + `dotnet test`)


**⚠ `dotnet test` compiles `Rules/` in a MORE PERMISSIVE context than Unity: green does not prove the
build will pass.** `tests/SnakeSnack.Tests.csproj` targets `net8.0` with `ImplicitUsings=enable` and
`Nullable=enable`; Unity 6000.5 compiles the same file as C# 9, `netstandard2.1`, with no implicit
usings and with the nullable context disabled. Three ways to be green and break afterwards, none
caught by the runner:

- a forgotten `using System;` (supplied implicitly on the test side) → **CS0246 on the Unity side**;
- an `object?` / `string?` annotation with no `#nullable enable` at the top of the file → **CS8632 on
  the Unity side**. It is a *warning*, so the build "succeeds" — and the project's rule is zero new
  warnings. `Assets/Scripts/Rules/Cell.cs` therefore carries the directive on its first line;
- any C# 10+ syntax (file-scoped namespace, `record struct`) → **error on the Unity side**, while it
  is perfectly legal in the files under `tests/`, which are compiled as net8.0.

**The countermeasure costs ten seconds** and avoids waiting for a Unity build: compile `Rules/` in a
throwaway project **outside the repository** (`$TEMP`), with `EnableDefaultCompileItems=false`, a
`<Compile Include="...\Assets\Scripts\Rules\*.cs" />`, `TargetFramework=netstandard2.1`,
`LangVersion=9.0`, `Nullable=disable`, `ImplicitUsings=disable`, `TreatWarningsAsErrors=true`.
⚠ Putting it **inside** the repository would have Unity pick it up as an asset.

**⚠ The csproj glob is NOT recursive.** `..\Assets\Scripts\Rules\*.cs` does not descend into
subfolders: a rule file filed under `Rules/Movement/` does **not** enter the test assembly. Nothing
reports it — `dotnet test` stays green, with a rule simply never exercised, while Unity compiles it
and the game uses it. Keep `Rules/` **flat**, or move the glob to `**\*.cs` knowingly.

**⚠ A new script has no `.meta` until Unity has imported it.** The five files in `Rules/` written on
2026-08-27 only got their GUID at the next `tools/build.ps1`. Committing scripts **without** their
`.meta` loses every future reference pointing at them: run a build before committing a new file under
`Assets/`.

**⚠ A shared random generator breaks a bench's pairing without a single test failing.** The game's
only randomness goes through `RandomSource`, and the game instance serves the apple alone (GDD §4.4).
A visual or audio effect drawing a number from it would shift the whole apple sequence: the tests
stay green (they seed their own instance), the game stays playable, and two runs meant to be
identical stop being so — which invalidates a paired bench with nothing to report it. **What works**:
any need for randomness other than the apple takes its own instance. First example:
`SnakeGame._sessionSeeds`.
