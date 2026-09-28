# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Nature du dépôt

Dock Terminal est un terminal Windows organisé en **workspaces → onglets → panes** (splits imbriqués). Le dépôt contient les spécifications (Markdown/HTML), les scripts Node du backlog et, depuis le 21 septembre 2026, le **socle de l'application** : hôte C# .NET 10 (WinUI 3 + WebView2 unique) dans `src/`, interface React + TypeScript + Vite + Tailwind + Zustand dans `web/`, tests xUnit dans `tests/`.

La pile a été validée par le spike T01 ; les maquettes, le POC et le spike ont été retirés du dépôt et restent consultables dans l'historique Git (commit `b1c6484`). Toute évolution du socle suit `docs/BACKEND_ARCHITECTURE.md`, `docs/FRONTEND_ARCHITECTURE.md` et `docs/TESTING.md`.

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

Scripts de documentation, depuis la **racine du dépôt** (chemins relatifs `backlog/...` codés en dur) :

```bash
node backlog/create-manifest.js        # régénère backlog/issues.json, backlog/Fxx.md et BACKLOG.md (sans GitHub)
node backlog/publish.js                # crée/met à jour les issues GitHub via `gh`, réécrit published.json et BACKLOG.md
node backlog/render-specifications.js  # régénère specifications-terminal.html depuis specifications-terminal.md
```

`publish.js` nécessite le GitHub CLI authentifié sur `MaximeRazafinjato/dock-terminal`. Il est idempotent par préfixe de titre `[Fxx]`/`[Txx]`/`[Dxx]` et **écrase BACKLOG.md** avec les liens GitHub : lancer `create-manifest.js` puis `publish.js` pour rester cohérent, ne pas éditer BACKLOG.md à la main.

## Chaîne de production des documents

Chaque artefact a une **source unique** ; modifier la source, puis régénérer.

| Source de vérité | Généré | Par |
| --- | --- | --- |
| Tableau `tasks` dans `backlog/create-manifest.js` | `backlog/issues.json`, `backlog/F01..F21.md`, `T01.md`, `D01.md`, `BACKLOG.md` | `create-manifest.js` |
| `backlog/issues.json` + GitHub | `backlog/published.json`, `BACKLOG.md` (liens), corps des issues | `publish.js` |
| `specifications-terminal.md` | `specifications-terminal.html` | `render-specifications.js` |

Contraintes des scripts à connaître :

- `render-specifications.js` réutilise le `<head>` de l'ancien HTML et **exige exactement 18 sections `## `** dans le Markdown, sinon il lève une erreur. Ajouter une section implique de mettre à jour cette constante et le compteur affiché dans l'en-tête généré.
- Le Markdown des spécifications est parsé par un convertisseur maison minimal : titres `##`/`###`, tableaux `|`, listes `-`/`1.`, paragraphes, liens, `code` et `**gras**` uniquement. Éviter toute autre syntaxe Markdown.
- Chaque entrée `tasks` référence les **numéros de sections** de la spec et les **identifiants de recette `Rxx`** (section 17). Renuméroter une section de la spec impose de revoir ces références.

## Statut des exigences

La spécification distingue trois statuts qu'il faut respecter dans toute rédaction : **Retenu** (décision utilisateur), **Convention proposée** (détail d'implémentation non validé), **À décider**. Ne jamais promouvoir une convention proposée en exigence validée ; les décisions restantes sont consignées en section 18 de la spec et dans `backlog/D01.md`.

Décisions déjà tranchées à ne pas rouvrir : Leader = Ctrl + Espace (délai 5 s), palette = Ctrl + P, shell par défaut = Windows PowerShell 5.1 (pour retrouver le profil existant, `pwsh` n'est pas supposé équivalent), agents suivis = Claude Code (`claude`) et Codex CLI (`codex`), projets = premier niveau de `C:\Files\Projects` hors `worktrees`, recherche dans les terminaux **hors périmètre**, F13 retirée.

## Socle de l'application (`src/`, `web/`, `tests/`)

- `src/Dock.Core` : ConPTY, Job Objects, intégration shell (OSC 7), modèle et persistance de session (`%LOCALAPPDATA%\Dock\session.json`). L'hôte valide toute session avant de l'écrire.
- `src/Dock.Host` : fenêtre WinUI 3 non empaquetée, une seule WebView2, aucun `KeyboardAccelerator`, pont JSON `HostBridge` (contrat dans `docs/BACKEND_ARCHITECTURE.md`, types miroir dans `web/src/bridge/messages.ts`).
- `web/` : modèle pur dans `src/model`, stores Zustand, une instance xterm.js par pane conservée hors React (`terminalRegistry`), raccourcis interceptés dans xterm.js (Leader Ctrl + Espace, Ctrl + P), composants un par fichier, tokens Tailwind `dock-*`. Enums TypeScript autorisés (`erasableSyntaxOnly` désactivé).

## Environnement local documenté

`docs/inspection-environnement.md` décrit le profil PowerShell de l'utilisateur, les fonctions worktree `wtr`/`rmwt`, le sélecteur WezTerm et les états d'agents existants. Ces fonctions ne doivent **pas** être réimplémentées dans Dock ; l'application doit les charger via le profil et suivre leurs effets. Ne pas simuler WezTerm avec de fausses variables d'environnement.
