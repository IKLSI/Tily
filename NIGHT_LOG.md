# Journal de la nuit du 28 au 29 septembre 2026

Travail autonome sur la branche `night-session`. Chaque itération apporte une amélioration, vérifiée par `pnpm lint`, `pnpm build`, `dotnet build` et `dotnet test`, puis testée dans une instance isolée de Dock (`DOCK_DATA_DIR`) quand elle se voit à l'écran, avant d'être commitée.

## Cadre

- Les issues GitHub ouvertes (#48, #80, #82, #83) sont laissées de côté à ta demande.
- Le SDK .NET 10 n'était pas installé sur la machine (seulement 8 et 9) : je l'ai installé pour mon usage dans `C:\Users\maxim\.dotnet10`, hors PATH et sans droits administrateur, afin de pouvoir compiler et lancer les tests. Tu peux supprimer ce dossier si tu n'en as pas besoin.
- Les dépendances web (`web/node_modules`) ont été installées avec `pnpm install --frozen-lockfile`.
- Ta session Dock (installée dans `C:\Program Files\Dock`) n'a pas été touchée : les essais tournent sur la version de développement avec un dossier de données séparé.

## Itérations

### 1. Ctrl + Tab et Ctrl + Maj + Tab pour changer d'onglet

- **Quoi** : Ctrl + Tab passe à l'onglet suivant du workspace actif, Ctrl + Maj + Tab au précédent, en boucle. Cela marche depuis un terminal comme depuis le graphe Git, l'explorateur ou le panneau, mais pas dans un champ de saisie ni sous une boîte modale. Les deux commandes existent aussi dans la palette (« Onglet suivant », « Onglet précédent »). README, spécification (en « Convention proposée ») et `docs/FRONTEND_ARCHITECTURE.md` sont à jour.
- **Pourquoi** : on ne pouvait changer d'onglet qu'à la souris ou par la palette. Ctrl + Tab est la convention de Windows Terminal et des navigateurs. Il ne prive le shell de rien, car xterm.js envoie la même chose que Tab (ou Maj + Tab) quand Ctrl est enfoncé.
- **Vérifié** : dans l'instance isolée, depuis un terminal et depuis le graphe Git ; le retour au dernier onglet en boucle fonctionne.

### 2. La saisie va au terminal dès le démarrage, sans clic

- **Quoi** : au lancement de Dock, la fenêtre était au premier plan, mais ce qu'on tapait n'arrivait nulle part tant qu'on n'avait pas cliqué dans un terminal. L'hôte donne maintenant le focus clavier à la WebView2 dès qu'elle est créée (`MainWindow.InitializeWebViewAsync`).
- **Pourquoi** : le seul `View.Focus` était appelé à l'activation de la fenêtre, avant la création asynchrone de la WebView2 : il restait sans effet et aucun autre appel ne suivait.
- **Vérifié** : reproduit sur Dock 1.0.0 installé (instance isolée, texte tapé 7 s après le lancement : rien n'apparaît), puis corrigé sur la version de développement (le même texte s'affiche et s'exécute).

### 3. Barres de défilement fines et discrètes

- **Quoi** : toutes les barres de défilement (terminaux, graphe Git, explorateur, listes) sont désormais fines (10 px dont 6 px visibles), sans flèches, avec un curseur gris sombre qui s'éclaircit au survol (tokens `--color-dock-scrollbar` et `--color-dock-scrollbar-hover` dans `index.css`).
- **Pourquoi** : c'étaient les barres natives de Chromium, larges, à flèches et au curseur gris clair, qui tranchaient avec le thème sombre compact et prenaient de la largeur aux terminaux.
- **Vérifié** : captures d'un terminal avec 300 lignes d'historique, du graphe Git et de la liste des fichiers d'un commit.

### 4. Menu contextuel dans les terminaux

- **Quoi** : un clic droit dans un terminal ouvre un menu avec Copier (grisé sans sélection), Coller, Tout sélectionner, Split côte à côte, Split haut / bas et Fermer le pane, chacun avec son raccourci. La touche Menu du clavier l'ouvre à la position du curseur. Quand un programme suit la souris (vim, htop…), le clic droit lui revient et Maj + clic droit ouvre le menu. Copier et coller passent désormais par `terminal/terminalActions.ts`, partagé par le clavier et le menu ; un échec du presse-papiers s'affiche dans la barre de statut au lieu d'être silencieux. Le petit rendu de raccourci des menus est devenu un composant `MenuShortcut`, repris dans le menu des fichiers.
- **Pourquoi** : les menus natifs de la WebView2 sont désactivés, donc un clic droit dans un terminal ne faisait rien, alors que le reste de l'interface (panneau, fichiers, Git) a ses menus contextuels.
- **Vérifié** : clic droit sans sélection (Copier grisé), double-clic sur un mot puis clic droit (Copier actif, la sélection reste visible), Copier puis Coller dans l'invite, touche Menu (menu au curseur), Échap (le focus revient au terminal).

### 5. Les noms restent visibles dans la palette et le sélecteur de projets

- **Quoi** : dans `SearchDialog` (palette et sélecteur de projets), le libellé garde maintenant sa largeur. L'indication (chemin, raccourci) prend la place restante et se tronque en fin, alignée à droite.
- **Pourquoi** : l'indication ne rétrécissait jamais (`shrink-0`). Avec une racine de projets un peu longue, le chemin prenait toute la ligne : le nom du projet disparaissait complètement et la liste débordait avec une barre de défilement horizontale. Je l'ai découvert en testant l'itération suivante.
- **Vérifié** : captures du sélecteur avec des chemins de plus de 100 caractères et de la palette filtrée sur « pane ».

### 6. Worktrees dans le sélecteur de projets

- **Quoi** : Leader puis F liste d'abord les projets, puis les dossiers de `<racine>\worktrees` avec l'indication « worktree · chemin ». En choisir un ouvre un workspace comme pour un projet. Côté hôte, `ProjectCatalog` ajoute ces dossiers marqués `Worktree` : un sous-dossier absent ou illisible ne gêne pas la liste des projets. Deux tests xUnit ont été ajoutés. La spécification (en « Convention proposée »), le README et les deux documents d'architecture sont à jour.
- **Pourquoi** : ton sélecteur WezTerm listait déjà ces worktrees avec un préfixe `[wt]`, et la spécification prévoyait de « l'exposer séparément si nécessaire ». Pour revenir sur un worktree créé par `wtr`, il fallait jusqu'ici passer par un terminal.
- **Vérifié** : sur une racine de démonstration avec deux projets et deux worktrees, la liste est triée puis filtrée par « wor » ; 176 tests passent.

### 7. Le graphe Git garde le focus quand on revient sur son onglet

- **Quoi** : quand l'onglet actif affiche le graphe Git (retour sur l'onglet, démarrage, graphe réaffiché après la lecture du dépôt), le focus va au graphe. Avant, il restait au terminal que le graphe recouvre. `AppShell` appelle `takeFocusFromCoveredTerminals` (dans `gitFocus.ts`), qui n'agit que si le focus était sur la page ou sur un terminal : un champ du panneau Git garde son focus.
- **Pourquoi** : les flèches et la frappe partaient dans un shell invisible au lieu de parcourir les commits affichés.
- **Vérifié** : l'instance de test tourne désormais avec le débogage distant de la WebView2 (`WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=9223`), ce qui permet de lire `document.activeElement` après de vraies frappes. Avant la correction, le focus était sur le `textarea` de xterm. Après, il est sur la liste du graphe au démarrage comme après Ctrl + Tab, ↓ ↓ passe bien au deuxième commit et Échap ferme le graphe en rendant le focus au terminal.

## Reste à faire et idées

- Taille de police et zoom du terminal : absents (police fixe à 14 px). La spécification les classe « À décider » (section 4), donc je n'y ai pas touché ; c'est à trancher.
