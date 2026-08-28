# Pièges — Logique pure et tests hors moteur (`Rules/` + `dotnet test`)


**⚠ `dotnet test` compile `Rules/` dans un contexte PLUS PERMISSIF qu'Unity : le vert ne prouve
pas que le build passera.** `tests/SnakeSnack.Tests.csproj` cible `net8.0` avec
`ImplicitUsings=enable` et `Nullable=enable` ; Unity 6000.5 compile le même fichier en C# 9,
`netstandard2.1`, sans usings implicites et avec le contexte nullable désactivé. Trois façons
d'être au vert et de casser ensuite, aucune détectée par le runner :

- un `using System;` oublié (fourni implicitement côté test) → **CS0246 côté Unity** ;
- une annotation `object?` / `string?` sans `#nullable enable` en tête de fichier → **CS8632 côté
  Unity**. C'est un *avertissement*, donc le build « réussit » — et la consigne du projet est zéro
  avertissement nouveau. `Assets/Scripts/Rules/Case.cs` porte donc la directive en première ligne ;
- toute syntaxe C# 10+ (namespace à portée de fichier, `record struct`) → **erreur côté Unity**,
  alors qu'elle est parfaitement légale dans les fichiers de `tests/`, eux compilés en net8.0.

**La parade coûte dix secondes** et évite d'attendre un build Unity : compiler `Rules/` dans un
projet jetable **hors du dépôt** (`$TEMP`), avec `EnableDefaultCompileItems=false`, un
`<Compile Include="...\Assets\Scripts\Rules\*.cs" />`, `TargetFramework=netstandard2.1`,
`LangVersion=9.0`, `Nullable=disable`, `ImplicitUsings=disable`, `TreatWarningsAsErrors=true`.
⚠ Le poser **dans** le dépôt le ferait ramasser par Unity comme un asset.

**⚠ Le glob du csproj n'est PAS récursif.** `..\Assets\Scripts\Rules\*.cs` ne descend pas dans les
sous-dossiers : un fichier de règles rangé dans `Rules/Deplacement/` n'entre **pas** dans l'assembly
de test. Rien ne le signale — `dotnet test` reste vert, avec une règle simplement jamais éprouvée,
pendant qu'Unity la compile et que le jeu s'en sert. Garder `Rules/` **plat**, ou passer le glob à
`**\*.cs` en connaissance de cause.

**⚠ Un script neuf n'a pas de `.meta` tant qu'Unity ne l'a pas importé.** Les cinq fichiers de
`Rules/` écrits le 2026-08-27 n'ont reçu leur GUID qu'au `tools/build.ps1` suivant. Commiter des
scripts **sans** leur `.meta` fait perdre toute référence future qui pointerait dessus : lancer un
build avant de commiter un fichier neuf de `Assets/`.

**⚠ Un générateur aléatoire partagé casse l'appariement d'un banc sans faire tomber un seul test.**
Le seul aléa du jeu passe par `Aleatoire`, et l'instance de la partie ne sert qu'à la pomme (GDD
§4.4). Un effet visuel ou audio qui y puiserait un nombre décalerait toute la suite des pommes : les
tests restent verts (ils sèment leur propre instance), le jeu reste jouable, et deux parties censées
être identiques cessent de l'être — ce qui invalide un banc apparié sans que rien ne le signale.
**Ce qui marche** : tout besoin d'aléa autre que la pomme prend sa propre instance. Premier exemple :
`JeuSnake._grainesDeSession`.
