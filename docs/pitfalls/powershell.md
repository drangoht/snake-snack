# Pièges — PowerShell (scripts de build et de release)


**⚠ Ne JAMAIS tester `$?` après un exécutable natif en PowerShell 5.1.** `git`, Unity et Butler
écrivent leur progression sur **stderr même quand tout va bien**, ce qui met `$?` à `$false` alors
que le code retour vaut 0. Le script de release annonçait « git push échoue » à **chaque release
réussie**. Seul `$LASTEXITCODE` fait foi.

**⚠ `$ErrorActionPreference = 'Stop'` est un piège dans un script de build**, pour la même raison :
la moindre ligne de progression sur stderr avorte le script.

**⚠ Un script de release qu'on ne peut essayer qu'en publiant ne se teste jamais qu'en production.**
D'où `-DryRun`, qui va jusqu'au dossier de distribution et s'arrête avant tout effet visible.

