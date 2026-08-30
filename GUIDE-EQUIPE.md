# Guide — Snake Snack's agent team

How the project's agents and skills are organised, and when to invoke which.

## First: delegating has a price

An agent **starts from cold**. It knows nothing of the current session: it re-reads `CLAUDE.md`, the
GDD, the map, the pitfalls — in the order of **8,000 tokens before its first action**, often the same
documents one has just read oneself.

**Delegate when the task is in its speciality *and* big enough to amortise that**: designing or
balancing a system, a full test pass, a release, an asset production run. **Do it yourself**: a
ten-line fix, a question, a read, a rename, a unit test to replay.

And when delegating: **write in the instruction what you already know** — the files concerned, the
decision taken, the pitfall identified, the exact error message. An agent given its starting point
does not rediscover it.

⚠ The full chain below ("How a piece of work chains together") applies to a **piece of work**. Running
it for a value tweak costs five cold starts for three changed lines.

## The 9 agents (`.claude/agents/`)

| Agent | When to invoke it | Model |
|---|---|---|
| **`developpeur`** | Code, architecture, build, tests | opus |
| **`game-designer`** | Design, balancing, tuning values, scope | opus |
| **`game-tester`** | After every major implementation — plays and documents | sonnet |
| **`release-manager`** | Publish a version end to end + write the devlog | sonnet |
| **`directeur-artistique`** | Visual identity, consistency, graphic briefs | sonnet |
| **`graphiste`** | Sprites, VFX, icons — through the Python generators | sonnet |
| **`musicien`** | Music, SFX, mixing, audio pipeline | sonnet |
| **`story-teller`** | In-game text, names, descriptions, localisation | sonnet |
| **`marketing`** | itch page, pitch, screenshot briefs | sonnet |

⚠ The agent and skill names stay in French: they are what the author types. Everything they contain is
in English.

## The 4 skills (`.claude/skills/`)

- **`/carte-projet`** — the code index: where such a system, screen, piece of data or tool lives, plus
  the wiring checklists. **To be invoked before any exploration** rather than Glob/Grep from cold.
- **`/verifier-en-jeu`** — build, launch, inject real inputs, capture. To be invoked every time one is
  about to write "that should work".
- **`/rediger-le-gdd`** — fill `docs/GDD.md` section by section, by interview, in the order the
  decisions are really taken. To be invoked at the start, and as soon as a section has stayed empty
  while one is about to code the system it should describe.
- **`/publier-itch`** — the publishing procedure, short version.

## How a piece of work chains together

```
observation (a session played, or a measurement)
   → game-designer  : diagnosis + proposed rule, carried back into the GDD
   → developpeur    : implementation + tests (pure logic in Assets/Scripts/Rules/)
   → measurement    : the bench, if the subject can be put in numbers
   → game-tester    : what the measurement cannot say — the feel
   → release-manager: publication + devlog
```

**The order matters.** The shortcut "implement, then measure afterwards" costs several round trips: on
a previous project, a difficulty step was published without ever having been played, and the tester
felt nothing.

## The three rules learned the hard way

1. **A single game settles nothing.** The variance between two games can reach a factor of 2.4 before
   the setting under test even acts. A balancing verdict is taken on a **paired bench**, using the sign
   test.
2. **The bench does not say what is *felt*.** It measures the pressure the content exerts, not the
   experience. The two have already contradicted each other — the tester was right.
3. **When a fix does not move the metric, suspect the instrument.** Carrying on with the dosage is the
   most expensive way of being wrong.

## Documentation — what answers what

| Question | Document |
|---|---|
| Current phase, conventions | `CLAUDE.md` (loaded automatically) |
| *Why* the game is tuned this way | `docs/GDD.md` (index) → `docs/gdd/<system>.md` — to fill it: `/rediger-le-gdd` |
| *Where* something lives | skill `/carte-projet` |
| Which pitfalls lie in wait | `docs/pitfalls/<domain>.md` (index: `docs/PITFALLS_UNITY.md`) |
| What has been tested | `docs/TEST_REPORT.md` |
| What has actually shipped | `docs/DEVLOG.md` |
| Publishing | `docs/RELEASE.md` + `/publier-itch` |

## Making an agent evolve

If an agent systematically takes a bad decision on some point, **enrich its `.md` file** — that is the
mechanism provided for capitalising experience, and it is cheaper than correcting it at every session.
The `.claude/` files are versioned in the same way as the code.

⚠ **An agent describing a stale state of the project is worse than an absent agent**: it gives false
instructions with authority. When a phase ends, re-read the agents it concerns.

## The local LLM (optional)

If a `local-llm` MCP server is registered (LM Studio), it makes it possible to query a file **too big
to be read**: it reads the file **at its end** and returns only the answer. Measured on a previous
project: **83,000 tokens read locally → 675 returned**.

⚠ Three guard rails, learned by measurement:
1. **It is slow** (~6-7 min for 290 KB): fire the call **before** whatever one was about to do.
2. **A `max_tokens` set too low truncates the answer without raising an error.** Aim for 1500-2500.
3. **Good on prose, to be banned on figures and on code to be edited.** If a deterministic tool
   exists, it wins. And to locate something, `Grep` is instant and exact.

⚠ An agent declares a **closed** `tools:` list: if it does not declare the MCP tool there, it *cannot*
call it, whatever the instruction written elsewhere. *A capability documented without being wired does
not exist.*
