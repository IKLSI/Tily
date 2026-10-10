<p align="center">
  <img src="desktop/resources/icon.png" alt="Logo de Tily" width="96">
</p>

<h1 align="center">Tily</h1>

<p align="center">
  Terminal macOS organisé en <strong>workspaces, onglets et panes</strong>, avec une interface compacte et un panneau en arborescence.
</p>

<p align="center">
  <a href="https://github.com/IKLSI/Tily/releases/latest"><img src="https://img.shields.io/github/v/release/IKLSI/Tily" alt="Dernière version"></a>
  <img src="https://img.shields.io/badge/plateforme-macOS-000000" alt="Plateforme : macOS">
  <a href="LICENSE"><img src="https://img.shields.io/github/license/IKLSI/Tily" alt="Licence : GPL-3.0"></a>
</p>

![Tily avec plusieurs workspaces et un onglet découpé en trois terminaux](docs/images/overview.png)

## Fonctionnalités

- **Workspaces, onglets et splits** : arborescence repliable, onglets déplaçables d’un workspace à l’autre, terminaux côte à côte sans jamais arrêter leur processus, session restaurée à la réouverture.
- **Vrais terminaux** : zsh avec votre profil (bash disponible), liens et chemins de fichiers cliquables, glisser-déposer depuis le Finder.
- **Clavier d’abord** : palette Ctrl + P et touche Leader Cmd + K pour toutes les actions.
- **Projets et worktrees** : ouvrir un dossier de projet ou créer / supprimer un worktree (ports libres, `pnpm install`, base répliquée).
- **Fichiers et Git** : explorateur avec état Git, aperçu et édition Markdown / HTML / texte, vue Git complète (graphe, commit, push, rebase, stash, conflits, annulation).
- **Claude Code** : état de chaque agent (travaille, attend, terminé), notifications, et serveur MCP qui lui permet de lire les terminaux et de piloter Tily.
- **Navigateur intégré** : afficher l’application en développement dans un pane, avec DevTools et vue mobile.
- **Notes par workspace**, journal des messages, alerte à la fin des commandes longues et mises à jour intégrées.

## Aperçu

| Palette Ctrl + P | Vue Git | Explorateur |
| --- | --- | --- |
| ![Palette de commandes](docs/images/palette.png) | ![Vue Git](docs/images/git.png) | ![Explorateur de fichiers](docs/images/files.png) |

## Installation

1. Télécharger la [dernière version](https://github.com/IKLSI/Tily/releases/latest) : `Tily-x.y.z-arm64.dmg` (puce Apple) ou `Tily-x.y.z-x64.dmg` (Intel).
2. Glisser Tily dans Applications.
3. Tily n’étant pas notarisé, retirer la quarantaine avant le premier lancement :

   ```bash
   xattr -cr /Applications/Tily.app
   ```

Les mises à jour s’installent ensuite depuis Tily. Les données sont dans `~/Library/Application Support/Tily`.

## Construction

Sur un Mac, avec .NET 10, Node 24 et pnpm 12 :

```bash
dotnet test backend/Tily.slnx                       # hôte, bibliothèque et tests
cd desktop/renderer && pnpm install && pnpm build   # interface
cd .. && pnpm install && pnpm start                 # lance Tily en développement
scripts/build-mac.sh                                # paquets .dmg arm64 et x64
```

## Raccourcis essentiels

| Leader (Cmd + K, puis…) | Direct | Action |
| --- | --- | --- |
| P | Ctrl + P | Palette |
| T | Ctrl + Maj + T | Nouvel onglet |
| W | Ctrl + Maj + W | Nouveau workspace |
| V / H | Ctrl + Maj + D / H | Split côte à côte / haut-bas |
| X | Ctrl + Maj + X | Fermer le terminal |
| F | — | Sélecteur de projets |
| E / G / O | Ctrl + Maj + E / G / O | Fichiers / Git / Notes |
| A | Ctrl + Maj + A | Rejoindre l’agent en attente |
| Flèche | Alt + flèche | Changer de terminal |

La liste complète est dans la palette et dans la [spécification](docs/specification.md).

## Licence

Copyright (c) 2026 Maxime Razafinjato  
Copyright (c) 2026 KLS

Tily est un logiciel libre distribué sous licence [GNU GPL v3](LICENSE) : chacun peut l’utiliser, l’étudier, le modifier et le redistribuer, à condition que toute version redistribuée reste sous la même licence.
