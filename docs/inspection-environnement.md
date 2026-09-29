# Inspection de l’environnement local

Inspection du 18 septembre 2026. Lecture des fichiers et interrogation des versions uniquement : aucune fonction worktree, installation de dépendances, commande SQL ou suppression n’a été exécutée. Les profils et configurations personnels restent sur la machine ; ce document en décrit uniquement les comportements utiles à Dock.

## Environnement constaté

| Élément | Constat |
| --- | --- |
| Projets | `C:\Files\Projects`, chemin confirmé par l’utilisateur et présent sur la machine. |
| Worktrees | `C:\Files\Projects\worktrees`. |
| Éditeur | VS Code, accessible via `code.cmd`. |
| Shell actuel de WezTerm | `powershell.exe -NoLogo`, Windows PowerShell 5.1. |
| Windows PowerShell | Version observée : 5.1.26100.9444. |
| PowerShell 7 | `pwsh.exe` installé, version observée : 7.6.6. |
| Profil contenant les fonctions | `%USERPROFILE%\Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1`. |
| Profils utilisateur PowerShell 7 | Les fichiers `profile.ps1` et `Microsoft.PowerShell_profile.ps1` ne sont pas présents dans le dossier `Documents\PowerShell` inspecté. |
| Git Bash | `C:\Program Files\Git\bin\bash.exe` présent. Le `bash.exe` résolu par le PATH pointe vers System32 : ne pas le confondre avec Git Bash. |
| Claude | Claude Code CLI installé ; version observée : 2.1.276. |
| GPT | Codex CLI installé ; version observée : 0.155.0. L’agent retenu est Codex CLI. |
| Dépendances présentes | Git, pnpm, dotnet, Docker CLI et oh-my-posh. Leur présence ne prouve pas qu’un daemon ou serveur associé fonctionne. |

La commande de version Codex a renvoyé des avertissements d’accès refusé sur ses répertoires temporaires dans l’environnement restreint. Cela n’empêche pas l’identification du binaire ; aucune intégration fonctionnelle d’agent n’a été testée.

## Chargement du profil et dossier courant

Le profil Windows PowerShell configure UTF-8 et initialise oh-my-posh avec un thème local. Il expose des fonctions de navigation, de développement et de gestion de worktrees. Le copier dans le dépôt public n’est pas nécessaire pour l’intégration.

Il ajoute un wrapper au prompt qui émet une séquence OSC 7 avec le dossier courant, **uniquement si `WEZTERM_EXECUTABLE` est présent**. Dans Dock, ce wrapper ne sera pas activé naturellement. L’intégration shell devra annoncer le dossier indépendamment de WezTerm et conserver le prompt utilisateur.

Recommandation d’implémentation : utiliser initialement Windows PowerShell 5.1 pour retrouver le profil existant. Passer à PowerShell 7 nécessiterait une migration explicite ou un chargement compatible du profil ; ne pas supposer que le choix du binaire `pwsh.exe` donne automatiquement accès aux mêmes fonctions.

## Fonction wtr

Signature : `wtr <Branch> [Base] [-NoDb]`. La base par défaut est `develop`.

1. Vérifie le dépôt Git courant et récupère sa racine.
2. Forme un slug avec le dernier segment du nom de branche.
3. Construit le chemin `C:\Files\Projects\worktrees\<projet>-<slug>`.
4. Exécute un fetch de `origin/<Base>`, puis crée une nouvelle branche et son worktree depuis cette référence.
5. Change le dossier du shell vers le worktree.
6. Appelle `Invoke-RandomizeWorktreePorts` pour modifier des ports dans les fichiers de développement reconnus.
7. Exécute `pnpm install` ; un échec produit un avertissement sans arrêter toutes les étapes suivantes.
8. Sauf `-NoDb`, détecte la configuration de base de données, tente de créer une base répliquée et adapte la configuration locale du worktree.

Les helpers inspectés détectent des configurations JSON/XML et prévoient PostgreSQL et SQL Server. La sélection PostgreSQL peut consulter Docker et des variables de configuration du projet. Les fichiers de configuration et bases réels n’ont pas été lus ou manipulés pendant cette inspection.

La randomisation parcourt notamment Vite, package.json, fichiers .env, compose, launchSettings, appsettings et Program.cs. Le message de la fonction décrit un usage local ; le code inspecté modifie les fichiers et ne les rend pas automatiquement invisibles à Git. Dock doit donc laisser ces modifications apparaître normalement.

`wtr` n’appelle pas directement l’API de WezTerm et ne crée pas de workspace ou de pane. La logique de certains raccourcis WezTerm l’entoure séparément pour lancer Claude dans un split.

## Fonction rmwt

Signature : `rmwt [Slug] [-KeepBranch]`.

1. Détermine le dépôt principal à partir du répertoire Git commun.
2. Cible un worktree par slug, ou le worktree courant si le slug est absent. Dans ce second mode, refuse la racine principale.
3. Lit la branche et les informations de base associées au worktree et au dépôt principal.
4. Si le shell se trouve sous le chemin cible selon le test actuel, revient au dépôt principal.
5. Exécute `git worktree remove --force` ; si le dossier existe toujours, tente une suppression récursive résiduelle puis un prune.
6. Tente de supprimer la base répliquée lorsque sa configuration permet de la distinguer de celle du dépôt principal.
7. Supprime la branche avec `git branch -D`, sauf `-KeepBranch` ou HEAD détachée.

La fonction n’arrête pas automatiquement les programmes qui verrouillent le dossier et indique de les fermer si la suppression échoue. Elle n’appelle pas directement l’API de WezTerm.

Points à garder à l’esprit pour des tests ultérieurs : les opérations sont forcées et peuvent toucher fichiers, branche et base. Tester dans un dépôt jetable avec base de test ou sans base. Les contrôles par préfixe de chemin et la construction du chemin à partir du slug ne constituent pas un contrat suffisant pour une nouvelle API native de suppression. Aucune réécriture de ces fonctions n’est effectuée ici. Depuis le 29 septembre 2026, Dock en propose une version native (`Dock.Core/Worktrees`, section 11 de la spécification) : cette inspection sert désormais de référence de comportement, Dock faisant foi en cas de divergence future avec le profil.

## Contrat de synchronisation pour Dock

Ce contrat s’applique aux `wtr` et `rmwt` tapés au terminal, qui restent utilisables à côté de la gestion native (mêmes chemins, même base par défaut).

- Charger les fonctions du profil ; la création et la suppression depuis l’interface passent par la version native de Dock, pas par ces fonctions.
- Après `wtr`, suivre le nouveau dossier du même pane et actualiser son nom automatique, son contexte Git et les actions de dossier.
- Après `rmwt`, refléter le dossier dans lequel le shell est réellement revenu. Si un autre pane pointe vers un dossier supprimé, afficher l’indisponibilité et proposer un dossier de repli ; ne pas fermer arbitrairement cet autre pane.
- Conserver les noms manuels et l’appartenance des onglets aux workspaces.
- Ne pas déduire une réussite complète de la seule présence du texte final : certaines étapes utilisent des avertissements et peuvent échouer partiellement.
- Ne pas relancer ces commandes pendant la restauration d’une session.
- Ne pas créer, supprimer ou renommer automatiquement des workspaces à cause de ces fonctions.

## Sélecteur WezTerm Leader + F

Sources : `%USERPROFILE%\.config\wezterm\modules\sessionizer.lua` et `workspaces.lua`.

- Énumère les entrées de premier niveau de `C:\Files\Projects`, sauf `worktrees`.
- Ajoute séparément les entrées de premier niveau de `C:\Files\Projects\worktrees`.
- Présente des préfixes `[proj]` et `[wt]`, trie les libellés et active la recherche approximative.
- Crée **toujours un nouveau workspace**. En cas de nom déjà pris, ajoute `-2`, `-3`, etc.
- Lance Windows PowerShell puis change le dossier vers le chemin sélectionné.

La configuration Lua ne vérifie pas explicitement que chaque entrée énumérée est un dossier. Dock devra filtrer les fichiers conformément au besoin de sélection de dossiers. La profondeur de premier niveau est la référence existante ; certains raccourcis du profil pointent ensuite vers des dépôts plus profondément imbriqués.

Aucune détection « projet déjà ouvert » n’est requise dans Dock, conformément à la dernière décision utilisateur. Un suffixe pour distinguer deux noms ne constitue pas une détection de projet déjà ouvert.

## Raccourcis et fonctions liés à WezTerm

Le Leader existant est Ctrl + B avec délai de 1 500 ms. Dock retient désormais Ctrl + Espace, décision utilisateur plus récente ; ne pas réimporter Ctrl + B par défaut.

Les raccourcis WezTerm `Leader + n/N` exécutent `wtr`, puis appellent `wezterm cli split-pane` pour ouvrir Claude. Cette seconde partie ne fait pas partie de `wtr` et ne sera pas transférée automatiquement.

La fonction `pnps` du profil utilise aussi `wezterm cli split-pane` pour ouvrir un serveur dotnet à droite lorsque `WEZTERM_PANE` existe. Hors WezTerm, elle se replie sur `pnpm start`. C’est une différence de comportement à prendre en compte si une compatibilité complète des fonctions de développement est souhaitée ultérieurement.

Dock ne doit pas se faire passer pour WezTerm avec de fausses variables d’environnement pour activer des branches de code qui envoient des commandes au mauvais terminal. Adapter ces fonctions au besoin relève d’un travail distinct, à expliciter.

## Agents retenus

Les deux agents à suivre dans Dock sont **Claude Code** et **Codex CLI**. Ils sont tous les deux installés sur la machine au moment de l’inspection. Dock doit les traiter comme deux adaptateurs possibles rattachés à un pane, et non comme deux catégories de workspace.

Claude Code est lancé par la commande `claude`. Codex est lancé par la commande `codex` et correspond au Codex CLI installé. Les noms de processus, les sorties terminal et les fichiers d’état ne suffisent pas toujours à déterminer un état fiable ; l’adaptateur doit donc prévoir l’état « inconnu ».

## États d’agents existants

Le module Lua `claude_status.lua` sait lire des fichiers `%USERPROFILE%\.claude\state\state-<workspace>.txt` avec des états working, done ou waiting et une ancienneté maximale. Il nomme ces fichiers d’après le nom du workspace, ce qui ne suffit pas pour distinguer plusieurs agents/panes dans un même workspace et rend les renommages fragiles. Ce format doit être considéré comme une source Claude Code exploratoire, pas comme l’identifiant final de Dock.

La configuration globale Claude inspectée ne contient pas de clé `hooks`. L’existence d’un module de lecture Lua ne prouve donc pas que les fichiers d’état sont produits aujourd’hui ; une configuration locale ou un autre producteur peut exister. Aucune session privée d’agent n’a été ouverte pour le vérifier.

Pour Dock, rattacher les événements Claude Code et Codex CLI à des identifiants de pane/session stables. Les sources d’événements Codex et les capacités de notification devront être étudiées à l’implémentation. En l’absence de preuve fiable, afficher « inconnu ».
