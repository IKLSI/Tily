# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Nature du dépôt

Tily est un terminal **macOS** organisé en **workspaces → onglets → panes** (splits imbriqués). Le dépôt contient les spécifications (`docs/specification.md`) et l'application, en version 3.0.0 :
- `backend/` : solution `Tily.slnx` et `Directory.Build.props` ; `backend/src/` : hôte C# .NET 10 sans interface (processus console piloté par Electron), bibliothèque `Tily.Core`, serveur MCP `tily-mcp`, lanceur de terminal `tily-pty` ;
- `backend/tests/` : tests xUnit ;
- `desktop/` : application Electron, selon la convention `main` / `preload` / `renderer` : `src/main/` (processus principal : fenêtre, menus, dialogues, notifications, protocoles, panes navigateur, lancement de l'hôte et relais de ses messages), `src/preload/` (pont `window.tily`), `renderer/` (interface React + TypeScript + Vite + Tailwind + Zustand, projet pnpm autonome), `resources/` (icône, entitlements).

Tily (de *mitily*, « guetter » en malgache) s'appelait Dock jusqu'au 30 septembre 2026 : l'historique Git, les issues GitHub et les releases antérieures emploient encore ce nom. Jusqu'à la version 2.2.0 incluse, Tily était une application Windows (WinUI 3 + WebView2, ConPTY, PowerShell 5.1, installeur Inno Setup) ; le port macOS du 5 octobre 2026 a supprimé tout le code Windows, qui reste consultable dans l'historique Git.

Le backlog vit uniquement dans les issues GitHub de `MaximeRazafinjato/tily`. Toute évolution de l'application suit `docs/architecture/backend.md`, `docs/architecture/frontend.md` et `docs/testing.md`.

Tout le contenu est rédigé en **français**.

## Commandes

Application (racine du dépôt) :

```bash
dotnet build backend/Tily.slnx         # construit Core, Host, Mcp, Pty et les tests
dotnet test backend/Tily.slnx          # tests xUnit ; sur macOS seulement (PTY, libproc, vrais zsh/bash)
cd desktop/renderer && pnpm dev | pnpm build | pnpm lint
cd desktop && pnpm build && pnpm start # compile le processus principal Electron et lance Tily
```

`pnpm start` utilise l'hôte de `backend/src/Tily.Host/bin/Debug/net10.0/Tily` et l'interface de `desktop/renderer/dist` ; `TILY_BACKEND=<chemin>` remplace l'exécutable de l'hôte, `TILY_RENDERER_DEV_URL=http://localhost:5173` charge le serveur Vite.

Toujours lancer `pnpm lint` et `pnpm build` (qui exécute `tsc -b`) dans `desktop/renderer/`, `pnpm build` dans `desktop/` et `dotnet test` (sur un Mac ou en CI) avant de committer. Sous Windows, le code compile mais la plupart des tests échouent (chemins POSIX, `/bin/zsh`, libc).

`TILY_DATA_DIR` remplace le dossier de données `~/Library/Application Support/Tily` (session, préférences, états d'agents, intégration des shells) : l'utiliser pour lancer une instance isolée sans toucher à la session de l'utilisateur.

Distribution :

```bash
scripts/build-mac.sh                   # publication autonome osx-arm64, web, puis desktop/release/Tily-x.y.z-arm64.dmg (signature ad hoc)
node scripts/generate-icon.js          # régénère desktop/resources/icon.png (1024 px)
```

La version vient de la propriété `Version` de `backend/src/Tily.Host/Tily.Host.csproj` (recopiée dans `desktop/package.json` par le script) ; chaque version est publiée en release GitHub avec son `.dmg`. La CI `.github/workflows/macos.yml` construit, teste et empaquette sur `macos-latest`. Les captures du README sont dans `docs/images/`.

Les issues GitHub `[Fxx]` référencent les **numéros de sections** de la spec et les **identifiants de recette `Rxx`** (section 16). La spec a été renumérotée le 6 octobre 2026 (sections 2 à 17 devenues 1 à 16) : les anciennes issues citent l'ancienne numérotation. Renuméroter une section impose de revoir ces références.

## Choix établis

`docs/specification.md` décrit le comportement actuel de Tily, sans statut ni point en suspens : toute évolution la met à jour pour qu'elle reste factuelle. Choix à ne pas rouvrir : Leader = Cmd + K (délai 5 s ; Ctrl + Espace reste accepté, mais macOS le réserve par défaut au changement de source de saisie), palette = Ctrl + P, shell par défaut = zsh (`/bin/zsh -l`, profil de l'utilisateur chargé ; bash disponible), agents suivis = Claude Code (`claude`) et Codex CLI (`codex`), projets = premier niveau du dossier des projets (`~/Projects` par défaut, réglage `ProjectsRoot`) hors `worktrees`, recherche dans les terminaux **hors périmètre**, F13 retirée.

## Application (`backend/`, `desktop/`, `desktop/renderer/`)

- `backend/src/Tily.Core` : terminaux sur PTY Unix (`Terminal` : `UnixPty`, `ProcessTree`, `ProcessCommandName` ; `Native` : P/Invoke libc et libproc), shells zsh / bash et leur intégration OSC 7 / OSC 6973 (`Shell`), session et texte des panes (`Session`, `session.json`), préférences (`Settings`), sélecteur de projets (`Projects`), actions contextuelles, Finder et éditeur (`Context`), explorateur de fichiers et Corbeille (`Files`), vue Git avec annulation (`Git`), worktrees natifs (`Worktrees`), états d'agents et hook Claude Code (`Agents`), serveur MCP de Tily : socket par instance et déclaration dans `~/.claude.json` (`Mcp`), journal console et réseau des panes navigateur (`Browser`), vérification des mises à jour (`Updates`). L'hôte valide toute session avant de l'écrire.
- `backend/src/Tily.Host` : exécutable console `Tily`, sans interface ; lit et écrit des messages JSON, un par ligne, sur l'entrée et la sortie standard (`StdioChannel`), les traite sur une boucle unique (`HostLoop`), pont `HostBridge` et flux associés dans `Bridge/` (contrat dans `docs/architecture/backend.md`, types miroir dans `desktop/renderer/src/bridge/messages.ts`). Les messages `host.*` s'échangent avec le processus principal d'Electron et ne vont jamais au web.
- `backend/src/Tily.Mcp` : `tily-mcp`, serveur MCP stdio lancé par Claude Code dans un pane, qui relaie ses outils à son instance de Tily ; `tily-mcp hook` est aussi le hook d'état de Claude Code. `backend/src/Tily.Pty` : `tily-pty`, lanceur qui attache le terminal de contrôle avant d'exécuter le shell. Tous deux sont publiés à côté de `Tily` et partagent son runtime.
- `desktop/src/main/` : processus principal Electron en TypeScript (`main.ts` et modules) ; `desktop/src/preload/` : preload qui expose `window.tily` ; configuration `desktop/electron-builder.yml`.
- `desktop/renderer/` : organisé par fonctionnalité (`src/features/<nom>/` : logique et store à la racine, composants dans `components/` ; `src/app/` pour l'application et sa mise en page, `src/stores/` pour les stores globaux, `src/components/` pour l'interface commune), modèle pur dans `src/model`, stores Zustand, une instance xterm.js par pane conservée hors React (`terminalRegistry`), raccourcis interceptés dans xterm.js (Leader Cmd + K ou Ctrl + Espace, Ctrl + P), composants un par fichier, tokens Tailwind `tily-*`. Enums TypeScript autorisés (`erasableSyntaxOnly` désactivé).

## Worktrees

Tily gère les worktrees en natif (`backend/src/Tily.Core/Worktrees` : liste, création avec ports aléatoires, `pnpm install` et base répliquée, suppression) avec le comportement des anciennes fonctions `wtr`/`rmwt` du profil PowerShell ; un worktree créé ou supprimé au terminal par `git worktree` est suivi par l'interface. Ne pas simuler WezTerm avec de fausses variables d'environnement.
