# Dock Terminal

Projet de terminal Windows organisé en **workspaces → onglets → panes**, avec une interface Dock verte et un panneau en arborescence.

## État du projet

La pile technique a été validée par le spike T01 ([compte rendu](https://github.com/MaximeRazafinjato/dock-terminal/blob/b1c648454c311b51a731118aea84b98d10bea1ac/spike/README.md), conservé dans l’historique Git). Le socle de l’application est en place : hôte C# .NET 10 (WinUI 3 + WebView2 unique) dans `src/`, interface React + TypeScript dans `web/`, tests dans `tests/`. Il ouvre de vrais terminaux PowerShell 5.1 avec le profil, persiste la session et gère les splits ; les fonctionnalités du backlog restent à implémenter.

```
dotnet build Dock.slnx
dotnet test Dock.slnx
dotnet run --project src/Dock.Host
scripts/build-installer.cmd          # installeur Windows (Inno Setup 6 requis) dans installer/output/
node scripts/generate-icon.js        # régénère src/Dock.Host/Assets/Dock.ico et Dock.png
```

## Documents

- [Spécifications complètes](docs/specifications-terminal.md)
- [Inspection de l’environnement local](docs/inspection-environnement.md)
- [Backlog fonctionnel](https://github.com/MaximeRazafinjato/dock-terminal/issues)
- [Architecture backend](docs/BACKEND_ARCHITECTURE.md), [architecture frontend](docs/FRONTEND_ARCHITECTURE.md), [tests](docs/TESTING.md)

## Direction retenue

- Interface compacte, terminaux sombres, accents verts et sélections par le fond.
- Workspaces libres, onglets déplaçables et splits redimensionnables.
- Édition inline, peu de popups, palette navigable au clavier via Ctrl + P.
- Clic gauche sur le « + » : PowerShell ; clic droit : choix du shell.
- Panneau repliable avec onglets dépliables par workspace.
- Restauration de la disposition et du texte, avec de nouveaux processus.
- Agents suivis : Claude Code (`claude`) et Codex CLI (`codex`).
- Sélecteur de projets basé sur `C:\Files\Projects` ; éditeur configuré : VS Code.

Les issues fonctionnelles décrivent l’application cible et leurs critères d’acceptation. Aucune priorité ni échéance n’est fixée pour l’instant.
