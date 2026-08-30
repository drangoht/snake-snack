# docs/gdd/ — one system, one file

`docs/GDD.md` §4 is an **index**: one line per system, pointing here. The detail lives in one file
per system (`movement.md`, `score.md`, `difficulty.md`...).

Why this split: the GDD is re-read before every design or implementation task, by the main agent
**and** by every delegated one. A monolithic GDD makes whoever touches one system pay for the detail
of all of them — measured at 21 KB for §4 alone on a Snake, i.e. ~5,400 tokens reloaded on every
task.

What a system file contains:

- **What it does**, in one sentence — the same one as in the index.
- **Its values**, and above all the measurement or observation that justifies them. The numbers
  themselves live in `Assets/Scripts/Rules/`: here we write *why* they are what they are.
- **What was tried and ruled out** for this system, with the reason.

Ceiling: ~150 lines. Beyond that, the system is hiding two.
