# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Nature du dépôt

Dock Terminal est un terminal Windows organisé en **workspaces → onglets → panes** (splits imbriqués). Le dépôt contient les spécifications (`docs/specifications-terminal.md`) et **l'application**, livrée en version 1.0.0 : hôte C# .NET 10 (WinUI 3 + WebView2 unique) dans `src/`, interface React + TypeScript + Vite + Tailwind + Zustand dans `web/`, tests xUnit dans `tests/`, installeur Inno Setup dans `installer/`.

La pile a été validée par le spike T01 ; les maquettes, le POC et le spike ont été retirés du dépôt et restent consultables dans l'historique Git (commit `b1c6484`). Le backlog vit uniquement dans les issues GitHub de `MaximeRazafinjato/dock-terminal` ; les anciens scripts de génération du backlog et du HTML des spécifications restent dans l'historique Git (commit `3fe18a5`). Toute évolution de l'application suit `docs/BACKEND_ARCHITECTURE.md`, `docs/FRONTEND_ARCHITECTURE.md` et `docs/TESTING.md`.

Tout le contenu est rédigé en **français**.

## Commandes

Application (racine du dépôt) :

```bash
dotnet build Dock.slnx                 # construit Core, Host (avec pnpm build du web si dist absent) et les tests
dotnet test Dock.slnx                  # tests xUnit, dont deux lancent un vrai PowerShell 5.1
dotnet run --project src/Dock.Host     # lance Dock ; DOCK_WEB_DEV_URL=http://localhost:5173 pour le serveur Vite
cd web && pnpm dev | pnpm build | pnpm lint
```

Toujours lancer `pnpm lint`, `pnpm build` (qui exécute `tsc -b`) et `dotnet test` avant de committer.

`DOCK_DATA_DIR` remplace le dossier de données `%LOCALAPPDATA%\Dock` (session, préférences, états d'agents, profil WebView2) : l'utiliser pour lancer une instance isolée sans toucher à la session de l'utilisateur.

Distribution :

```bash
scripts/build-installer.cmd            # publication autonome win-x64 puis installer/output/Dock-x.y.z-setup.exe (Inno Setup 6 requis)
node scripts/generate-icon.js          # régénère src/Dock.Host/Assets/Dock.ico et Dock.png
```

La version vient de la propriété `Version` de `src/Dock.Host/Dock.Host.csproj` ; chaque version est publiée en release GitHub avec son installeur. Les captures du README sont dans `docs/images/`.

Les issues GitHub `[Fxx]` référencent les **numéros de sections** de la spec et les **identifiants de recette `Rxx`** (section 17). Renuméroter une section de la spec impose de revoir ces références.

## Statut des exigences

La spécification distingue trois statuts qu'il faut respecter dans toute rédaction : **Retenu** (décision utilisateur), **Convention proposée** (détail d'implémentation non validé), **À décider**. Ne jamais promouvoir une convention proposée en exigence validée ; les décisions restantes sont consignées en section 18 de la spec et dans l'issue GitHub `[D01]` (#22).

Décisions déjà tranchées à ne pas rouvrir : Leader = Ctrl + Espace (délai 5 s), palette = Ctrl + P, shell par défaut = Windows PowerShell 5.1 (pour retrouver le profil existant, `pwsh` n'est pas supposé équivalent), agents suivis = Claude Code (`claude`) et Codex CLI (`codex`), projets = premier niveau du dossier des projets (`C:\Files\Projects` par défaut, réglage `ProjectsRoot`) hors `worktrees`, recherche dans les terminaux **hors périmètre**, F13 retirée.

## Application (`src/`, `web/`, `tests/`)

- `src/Dock.Core` : terminaux ConPTY et Job Objects (`Terminal`, `Native`), shells et intégration OSC 7 (`Shell`), session et texte des panes (`Session`, `%LOCALAPPDATA%\Dock\session.json`), préférences (`Settings`), sélecteur de projets (`Projects`), actions contextuelles et éditeur (`Context`), explorateur de fichiers (`Files`), vue Git avec annulation (`Git`), worktrees natifs (`Worktrees`), états d'agents et hooks Claude Code (`Agents`). L'hôte valide toute session avant de l'écrire.
- `src/Dock.Host` : fenêtre WinUI 3 non empaquetée, une seule WebView2, aucun `KeyboardAccelerator`, pont JSON `HostBridge` et flux associés dans `Bridge/` (contrat dans `docs/BACKEND_ARCHITECTURE.md`, types miroir dans `web/src/bridge/messages.ts`), script de hook `hooks/dock-agent-state.ps1`.
- `web/` : modèle pur dans `src/model`, stores Zustand, une instance xterm.js par pane conservée hors React (`terminalRegistry`), raccourcis interceptés dans xterm.js (Leader Ctrl + Espace, Ctrl + P), composants un par fichier, tokens Tailwind `dock-*`. Enums TypeScript autorisés (`erasableSyntaxOnly` désactivé).

## Environnement local documenté

`docs/inspection-environnement.md` décrit le profil PowerShell de l'utilisateur, les fonctions worktree `wtr`/`rmwt`, le sélecteur WezTerm et les états d'agents existants. Depuis le 29 septembre 2026 (issue #95), Dock gère les worktrees en natif (`src/Dock.Core/Worktrees` : liste, création avec ports aléatoires, `pnpm install` et base répliquée, suppression) avec le comportement de `wtr`/`rmwt` ; ces fonctions restent chargées par le profil, utilisables au terminal avec les mêmes chemins, et l'interface suit leurs effets. Ne pas simuler WezTerm avec de fausses variables d'environnement.
