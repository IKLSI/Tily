# Tests backend (xUnit)

Projet : `tests/Dock.Core.Tests`. Lancer avec `dotnet test Dock.slnx`.

## Organisation

Un dossier par espace de noms testé (`Session/`, `Shell/`, `Terminal/`, `Git/`, `Worktrees/`, `Updates/`, `StatusLog/`), une classe par type testé, suffixe `Tests`.

Nommage des méthodes : `Méthode_QuandCondition_AlorsRésultat` en anglais technique (`Validate_WhenActivePaneUnknown_ThenFails`). Structure Given / When / Then séparée par des lignes vides, une assertion principale par test.

## Catégories

| Catégorie | Exemple | Contrainte |
| --- | --- | --- |
| Unitaires purs | `SessionValidatorTests`, `OscCwdParserTests` | Aucune E/S, exécution instantanée. |
| Persistance | `SessionRepositoryTests`, `StatusLogRepositoryTests` | Dossier temporaire unique par test, supprimé dans `Dispose`. |
| Intégration terminal | `TerminalManagerTests`, `PowerShellIntegrationTests` (wrapper de prompt exécuté dans un vrai Windows PowerShell 5.1 sans profil : `$?` rendu au prompt d'origine, séquence OSC 7) | Lance un vrai Windows PowerShell 5.1 avec le profil de la machine ; délai maximal de 30 s ; vérifie la variable `DOCK_PANE_ID`, le dossier courant et la mort des processus enfants, suivis par handle et non par PID, que Windows peut réattribuer aussitôt, et que Ctrl + C interrompt un programme même quand le processus hôte ignore Ctrl + C. |
| Intégration Git | `GitReadTests`, `GitChangeCommandsTests`, `GitLineCommandsTests`, `GitHistoryCommandsTests`, `GitBranchCommandsTests`, `GitRefDeleteCommandsTests`, `GitSyncCommandsTests` | Lance le `git` installé dans un dépôt temporaire créé par `GitSandbox` (dossier avec espaces et accents, supprimé dans `Dispose`), isolé de la configuration de la machine (`GIT_CONFIG_GLOBAL` vers un fichier du test, `GIT_CONFIG_NOSYSTEM`), sans hooks ni signature, `autocrlf` désactivé ; un dépôt distant nu et des clones locaux simulent le push, le pull et les autres postes. Les parseurs (`GitStatusParserTests`, `GitDiffParserTests`, `GitPathMarksTests`, qui vérifie aussi les marques d'un vrai dépôt `GitSandbox`), la reconstruction des patchs partiels (`GitPatchBuilderTests`) et le graphe (`GitGraphTests`) sont testés sans E/S ; `GitLineCommandsTests` vérifie le contenu réel de l'index et des fichiers après stage, unstage et abandon de lignes (chunk entier, ligne isolée, CRLF, fichier non suivi dont le nom contient une espace, fichier supprimé, nouveau fichier staged, empreinte périmée, annulation). |
| Worktrees | `WorktreeCreatorTests`, `WorktreeRemoverTests` (bac à sable `GitSandbox`), `WorktreeListerTests`, `WorktreeTargetTests` (purs), `PortRandomizerTests`, `DatabaseConfigTests` (dossier temporaire) | Création (nouvelle branche depuis `origin/develop` sans amont, branche locale, branche distante suivie), refus du plan en français, ports sur des fichiers temporaires avec une sonde de ports et un tirage fixés, configuration et réécriture des bases. La suppression passe un réplicateur factice pour vérifier le garde-fou de la base ; un fichier ouvert sans partage simule un dossier verrouillé : refus en français, processus cités, worktree intact puis supprimé au nouvel essai. Aucun test n'appelle Docker ni SQL Server : les réplicateurs réels se vérifient à la main sur une base de test (recette R32). |
| Mises à jour | `ReleaseParserTests`, `ReleaseNotesTests`, `UpdateInstallerTests` (purs, sauf un dossier temporaire pour `unins000.exe`), `UpdateClientTests` | Aucun appel réseau : `UpdateClient` reçoit un `HttpClient` au `HttpMessageHandler` factice (réponse GitHub, 404, 403, 500, installeur dont l'empreinte correspond ou non). Aucun test ne lance d'installeur : le flux complet se vérifie à la main (recette R37). |

Les tests d'intégration dépendent de la machine (profil PowerShell, oh-my-posh). Ils restent dans le même projet pour être lancés à chaque `dotnet test`, mais un échec doit être lu à la lumière de l'environnement avant de conclure à une régression.

## Règles

- Messages d'assertion et messages d'erreur attendus en français, comme le code.
- Ne jamais lancer `wtr` / `rmwt` réels dans un test, ni `PostgresDockerReplicator` / `SqlServerReplicator` : ils créent des worktrees et des bases de données. Injecter un `IDatabaseReplicator` factice dans `WorktreeDatabase`.
- Aucun test ne doit laisser de processus vivant ; utiliser `TerminalManager.Stop` ou `Dispose`.
