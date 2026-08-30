# Pitfalls — Assets and importing


**⚠ NEVER ignore `.meta` files in `.gitignore`.** Unity stores each asset's **GUID** there. A missing
`.meta` loses every reference pointing at the asset: scripts detached from their GameObjects, sprites
emptied. The project's `.gitignore` contains no `*.meta` rule, and that is deliberate.

**⚠ `Art/` and `Resources/` are not equivalent — and getting it wrong raises nothing.** [inherited]
`Resources/` is loaded **by path** (`Resources.Load<Sprite>("Ui/button")`) and embedded **whole** in
the binary, including whatever is never used. `Art/` is consumed **by GUID reference**. Writing an
asset into the wrong one of the two: the generator announces "written", and the game shows the old
image. Keep a destination table and refer to it.

**⚠ A file written into `Assets/` does not exist until Unity has reimported it.** A batchmode build
takes care of that, but an open editor may serve the old version from its asset database. On a
**new** file ignored by git, `AssetDatabase.ImportAsset` alone is not enough: an
`AssetDatabase.Refresh()` is needed first for the database to discover it.


**⚠ A PNG dropped into a project in 3D mode is imported as a TEXTURE, not as a sprite.**
`ProjectSettings/EditorSettings.asset` carries `m_DefaultBehaviorMode: 0` (3D Mode): the default
`textureType` of an imported image is `Default`. `Resources.Load<Sprite>` then returns **`null`** —
exactly as if the file did not exist — and the `Image` stays empty without any error being raised.
The case is all the more discreet because the file is produced by a script and imported in batchmode:
nobody opens the inspector to see it. Countermeasure adopted on 2026-08-28:
`Assets/Editor/ImportIllustrations.cs`, an `AssetPostprocessor` that forces `textureType = Sprite` on
everything in `Resources/Illustrations/`. ⚠ A hand-fixed `.meta` would not have held: it is rewritten
on the next reimport. Check: `grep textureType` in the `.meta` must return `8`.
