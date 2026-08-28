# Pièges — Rendu (URP 2D)


**⚠ Unity range le pipeline actif dans `QualitySettings`, NIVEAU PAR NIVEAU.** [hérité]
Renseigner seulement `GraphicsSettings.defaultRenderPipeline` laisse les autres niveaux en Built-in :
le jeu change de pipeline dès que le joueur change de qualité. `RenderPipelineSetup.Apply()` boucle
sur tous les niveaux — c'est pour ça.

**⚠ Sous le Renderer 2D, un sprite sans `Light2D` globale est rendu NOIR.** [hérité]
Les sprites prennent `Sprite-Lit-Default` : sans une lumière globale dans la scène, tout le décor est
noir, sans la moindre erreur en console. `SceneBuilder.BuildGlobalLight()` en pose une.

