# Pitfalls — Web build (WebGL)


**⚠ WebGL is the only platform whose default stripping is the most aggressive.** [inherited]
The Input System resolves its control layouts **by reflection**: at the high level, the game starts
normally and **no longer answers the keyboard**. Set `ManagedStrippingLevel.Low`.

**⚠ The browser cache mixes two builds.** [inherited] The WebGL output files always carry the same
name from one build to the next. The browser can therefore pair the `.data` of one build with the
`.wasm` of another. The symptom is **not** "stale version", it is:

```
Cannot load: RuntimeError: memory access out of bounds
  at wasm://wasm/0b2ac7ce:wasm-function[97296]:0x1712ca9
  ... three hundred lines of offsets, not a single method name ...
```

An hour was lost looking for that **in the game's code**. The countermeasure is twofold:
1. a build id injected into the page's URLs (`BuildTools.StampWebCacheBuster`);
2. ⚠ **the host page itself must never be cached** — it is the only one carrying that id. Cached, it
   keeps naming the files of the old build: *an invalidation mechanism carried by a cacheable
   resource cancels itself out.* The `http-equiv` tags are not enough (Chrome ignores them for the
   main document): real HTTP headers are needed, hence `tools/serve_web.py`.

**⚠ A single-threaded local server blocks the game's startup.** [inherited] `socketserver.TCPServer`
handles one request at a time; the browser keeps its connections open and a game preloading its
`StreamingAssets` in parallel blocks its own requests. The game sits on its startup bar — which even
appears to go backwards — **with no error at all**, neither on the browser side nor on the server
side.

**⚠ The itch channel's name decides whether the file is PLAYABLE in the browser.** [inherited]
`html5` (or `html`, or `web`) is recognised as such; any other name produces an archive to download,
which installs perfectly and does not play. Nothing reports it. Prerequisite on the itch side, to be
done once: *Kind of project* = **HTML**, and the file ticked "played in the browser".

**⚠ The mobile `devicePixelRatio` is the most profitable performance setting.** [inherited] A recent
phone announces 3: Unity then renders **nine times** more pixels than the logical panel shows, on a
GPU ten times weaker than a desktop card. The frame rate collapses with no error saying so. Force
`config.devicePixelRatio = 1` on mobile.

**⚠ The version manifest belongs to the downloadable target only.** [inherited] A web player is
always up to date (the page serves the current build). Pushing the manifest from a web release would
announce to every Windows player an update that does not exist.

**⚠ Unity drops a `Data/` folder (Burst code) at the ROOT of the project** during a WebGL build,
outside any build folder. An artefact — ignored by git.


**⚠ A web build modifies `ProjectSettings/ProjectSettings.asset` — and nothing restores it.**
Observed on 2026-08-28, at the project's first web build. `BuildTools` writes the WebGL settings
(`webGLTemplate: PROJECT:SnakeSnack`, compression format, heap size, `defaultScreenWidthWeb`) straight
into the **versioned** project settings, and the build script does not put them back afterwards. The
repository therefore comes out modified after a `-Target web`, exactly like the scene — except that
**the scene gets discarded, and those settings do not**: they are the project's real web settings, and
losing them would make the next build come out with Unity's default template. Commit them once, then
stop being surprised.
