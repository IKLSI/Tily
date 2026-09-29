<p align="center">
  <img src="src/Dock.Host/Assets/Dock.png" alt="Logo de Dock" width="96">
</p>

<h1 align="center">Dock Terminal</h1>

<p align="center">
  Terminal Windows organisé en <strong>workspaces → onglets → panes</strong>, avec une interface compacte et un panneau en arborescence.
</p>

<p align="center">
  <a href="https://github.com/MaximeRazafinjato/dock-terminal/releases/latest"><img src="https://img.shields.io/github/v/release/MaximeRazafinjato/dock-terminal" alt="Dernière version"></a>
  <img src="https://img.shields.io/badge/plateforme-Windows%2010%2B-0078d4" alt="Plateforme : Windows 10+">
</p>

![Dock avec plusieurs workspaces et un onglet découpé en trois terminaux](docs/images/overview.png)

## Fonctionnalités

- **Workspaces libres** : un panneau en arborescence, repliable et redimensionnable, regroupe les workspaces et leurs onglets. Tout se renomme directement sur place, les workspaces se réordonnent, les onglets se dupliquent et se déplacent d’un workspace à l’autre, et un onglet fermé par erreur se rouvre.
- **Vrais terminaux** : Windows PowerShell par défaut avec votre profil habituel, ses alias et ses fonctions ; PowerShell 7, CMD et Git Bash au clic droit sur « + ».
- **Splits** : plusieurs terminaux côte à côte ou l’un sous l’autre dans le même onglet, redimensionnables et navigables au clavier.
- **Palette Ctrl + P** : retrouver une commande, un workspace, un onglet ou un terminal en quelques lettres.
- **Touche Leader Ctrl + Espace** : toutes les actions au clavier, sans gêner la saisie dans le terminal.
- **Sélecteur de projets** : ouvrir un nouveau workspace directement dans un dossier de projet ou dans l’un de ses worktrees.
- **Explorateur de fichiers** : parcourir le dossier du terminal actif dans un panneau à droite.
- **Vue Git** : graphe de l’historique, branches et tags, Stage et commit, Push et Pull, Merge, Rebase, Stash, résolution des conflits et bouton « Annuler », sans taper de commande.
- **Suivi de Claude Code** : repérer d’un coup d’œil le workspace et l’onglet où Claude Code travaille, attend une réponse ou a terminé.
- **Liens cliquables** : Ctrl + clic sur un lien affiché dans le terminal l’ouvre dans le navigateur.
- **Glisser-déposer** : déposer un fichier ou un dossier de l’Explorateur Windows sur un terminal y insère son chemin.
- **Session retrouvée** : workspaces, onglets, splits et texte des terminaux sont restaurés à la réouverture ; les préférences s’exportent et s’importent.

## Aperçu

### Palette Ctrl + P

![Palette de commandes ouverte par-dessus les terminaux](docs/images/palette.png)

### Vue Git

![Vue Git avec les branches, le graphe de l’historique et les modifications en cours](docs/images/git.png)

### Explorateur de fichiers

![Explorateur de fichiers ouvert à droite du terminal](docs/images/files.png)

## Installation

1. Télécharger `Dock-x.y.z-setup.exe` depuis la [dernière version](https://github.com/MaximeRazafinjato/dock-terminal/releases/latest).
2. Lancer l’installeur. Il n’est pas signé : si Windows SmartScreen s’affiche, cliquer « Informations complémentaires » puis « Exécuter quand même ».

Aucun droit administrateur n’est nécessaire. Pour mettre à jour, fermer Dock puis lancer le nouvel installeur : workspaces et préférences sont conservés.

## Raccourcis clavier

| Leader (Ctrl + Espace, puis…) | Raccourci direct | Action |
| --- | --- | --- |
| P | Ctrl + P | Palette |
| T | Ctrl + Maj + T | Nouvel onglet |
| V | Ctrl + Maj + D | Split côte à côte |
| H | Ctrl + Maj + H | Split haut/bas |
| F | — | Sélecteur de projets |
| W | Ctrl + Maj + W | Nouveau workspace |
| X | Ctrl + Maj + X | Fermer le terminal actif |
| M | Ctrl + Maj + M | Agrandir / réduire le terminal actif (ou double-clic sur son en-tête) |
| E | Ctrl + Maj + E | Explorateur de fichiers |
| G | Ctrl + Maj + G | Vue Git |
| B | Ctrl + Maj + B | Afficher / masquer les workspaces |
| Z | Ctrl + Maj + Z | Rouvrir le dernier onglet fermé |
| , | — | Paramètres |
| Flèche | Alt + flèche | Passer d’un terminal à l’autre |
| Pg préc. / Pg suiv. | Ctrl + Maj + Pg préc. / Pg suiv. | Déplacer l’onglet vers la gauche / la droite |
| — | Ctrl + Tab / Ctrl + Maj + Tab | Onglet suivant / précédent |

Ctrl + Maj + C et Ctrl + Maj + V copient et collent. Un clic droit, ou la touche Menu, ouvre le menu d’un terminal, d’un onglet, d’un workspace ou d’un fichier. Dans le panneau des workspaces, ↑ et ↓ passent d’une ligne à l’autre, → et ← déplient et replient, F2 renomme et Alt + ↑ / ↓ déplace la ligne.
