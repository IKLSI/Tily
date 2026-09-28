# Journal de la nuit du 28 au 29 septembre 2026

Travail autonome sur la branche `night-session`. Chaque itération apporte une amélioration, vérifiée par `pnpm lint`, `pnpm build`, `dotnet build` et `dotnet test`, puis testée dans une instance isolée de Dock (`DOCK_DATA_DIR`) quand elle se voit à l'écran, avant d'être commitée.

## Cadre

- Les issues GitHub ouvertes (#48, #80, #82, #83) sont laissées de côté à ta demande.
- Le SDK .NET 10 n'était pas installé sur la machine (seulement 8 et 9) : je l'ai installé pour mon usage dans `C:\Users\maxim\.dotnet10`, hors PATH et sans droits administrateur, afin de pouvoir compiler et lancer les tests. Tu peux supprimer ce dossier si tu n'en as pas besoin.
- Les dépendances web (`web/node_modules`) ont été installées avec `pnpm install --frozen-lockfile`.
- Les essais tournent sur la version de développement, avec un dossier de données séparé.
- **Incident vers 00 h 40** : un test de glisser-déposer a cliqué par erreur dans ton Dock, celui où tourne cette session Claude Code. La fenêtre source du test ne s'était pas affichée, donc l'appui souris est tombé sur ta fenêtre. Effet : une sélection de texte et un défilement vers le haut dans le terminal de la session, rien d'autre. Aucune frappe n'est partie vers ta fenêtre. Le message « Onglet fermé » visible dans ta barre de statut ne vient pas de mes tests : aucun n'envoie de commande qui ferme un onglet (Ctrl + Maj + X, clic milieu ou croix). Depuis, les scripts de test refusent toute frappe ou tout clic tant que la fenêtre de test n'est pas au premier plan et que le point visé n'est pas à l'intérieur, et refusent de cibler un autre processus que la version de développement.

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

### 8. « Rejoindre » révèle le terminal même sous le graphe Git

- **Quoi** : toutes les façons de rejoindre un agent (carte d'attention, pastille d'état du panneau ou de l'en-tête, entrée « Rejoindre » de la palette, clic sur une notification Windows) passent par une seule fonction, `joinPane`. Elle acquitte l'attente, sélectionne le pane, masque le graphe Git s'il recouvre les terminaux de l'onglet et donne le focus au terminal. L'entrée de la palette acquitte désormais l'attente comme les autres, ce qu'elle ne faisait pas.
- **Pourquoi** : la logique était recopiée à trois endroits. Surtout, depuis l'itération 7, rejoindre un pane dont l'onglet affiche le graphe aurait donné le focus au graphe au lieu du terminal de l'agent.
- **Vérifié** : Claude Code simulé en attente (un `ping -t` dans le pane et un fichier `agents\<pane>.json` écrit dans le dossier de données de test), onglet sous le graphe Git, clic sur « Rejoindre le terminal » depuis un autre onglet. Le graphe se ferme, le focus arrive dans le bon pane et la carte disparaît.

### 9. Les boutons des panes étroits restent accessibles

- **Quoi** : l'en-tête d'un pane étroit tronque son chemin au lieu de faire sortir ses boutons du pane. La grille du pane a désormais une seule colonne bornée (`grid-cols-1`, soit `minmax(0, 1fr)`).
- **Pourquoi** : la colonne implicite de la grille prenait la largeur du chemin complet. Dès qu'un pane était un peu étroit (splits, fenêtre non maximisée, panneau de droite ouvert), les boutons Split et Fermer disparaissaient hors du pane, sans aucun moyen de les atteindre.
- **Vérifié** : fenêtre de 1300 px avec trois panes. Avant, des en-têtes de 479 et 505 px pour des panes de 366 et 300 px, et une croix à 107 px hors du pane. Après, des en-têtes de 364 et 298 px, tous les boutons visibles et le chemin terminé par des points de suspension (capture).

### 10. Barre d'onglets utilisable avec beaucoup d'onglets

- **Quoi** : quand les onglets ne tiennent plus, ils défilent horizontalement dans une bande sans barre visible (molette verticale convertie en défilement horizontal). L'onglet actif est ramené en vue à chaque changement, création comprise, et quand la bande change de taille. Le « + » et le bouton du panneau de droite sont sortis de la bande : ils restent toujours visibles. Seuls les onglets portent désormais `role="tablist"`, ce qui est aussi plus juste pour l'accessibilité.
- **Pourquoi** : avec 13 onglets dans une fenêtre de 1300 px, la barre débordait. L'onglet actif, le « + » et le bouton du panneau de droite étaient poussés hors de la fenêtre et impossibles à atteindre à la souris.
- **Vérifié** : mesures avant et après dans l'instance de test (bouton « + » à 1694 px dans une fenêtre de 1284 px avant, à 1250 px après), onglet actif ramené en vue après un rétrécissement de la fenêtre et après Ctrl + Tab, défilement par une vraie molette.

### 11. Build des tests sans avertissement

- **Quoi** : `TerminalManagerTests` utilise `Assert.DoesNotContain(processIds, IsAlive)` au lieu de `Assert.Empty(processIds.Where(IsAlive))`.
- **Pourquoi** : l'analyseur xUnit (xUnit2029) levait un avertissement à chaque compilation des tests ; l'assertion est la même, et en cas d'échec le message indique le processus encore vivant.
- **Vérifié** : `dotnet build` affiche 0 avertissement et les 176 tests passent.

### 12. Un fichier déposé sur Dock n'ouvre plus de fenêtre de navigateur

- **Quoi** : côté web, `terminal/externalDrop.ts` refuse le dépôt de fichiers et de liens venus de l'extérieur (curseur « interdit »). Côté hôte, `MainWindow` annule toute navigation hors de l'origine de l'application et toute demande de nouvelle fenêtre, par défense en profondeur.
- **Pourquoi** : glisser un fichier depuis l'Explorateur Windows sur Dock ouvrait une fenêtre WebView2 brute, hors de Dock, qui affichait le fichier ; un lien déposé pouvait aussi remplacer l'interface. Rien n'interceptait ces cas.
- **Vérifié** : dépôt simulé par le protocole de débogage (`Input.dispatchDragEvent`, le même chemin qu'un vrai glisser-déposer dans Chromium). Avant, une nouvelle fenêtre « exemple fichier.txt » s'ouvrait (processus `msedgewebview2` séparé). Après, rien ne s'ouvre et la page reste en place. Côté hôte, `window.open` renvoie `null` et une navigation forcée vers `file:///` est annulée.

### 13. Déposer un fichier sur un terminal insère son chemin

- **Quoi** : glisser un ou plusieurs fichiers ou dossiers depuis l'Explorateur Windows sur un terminal y colle leurs chemins, comme dans Windows Terminal, et le pane devient actif. Le web joint les fichiers au message `terminal.drop` (`postMessageWithAdditionalObjects`, seul moyen d'obtenir leur vrai chemin dans une WebView2). L'hôte les met en forme selon le shell avec `DroppedPaths` dans `Dock.Core` : guillemets simples pour PowerShell (apostrophe doublée), doubles pour CMD, simples pour Git Bash, rien si ce n'est pas nécessaire, une espace après chaque chemin. Il renvoie `terminal.dropped`, et le texte est collé par xterm.js, donc entre crochets si le programme le demande, comme Claude Code. Huit tests xUnit ont été ajoutés. Spécification (« Convention proposée »), README et documents d'architecture sont à jour.
- **Pourquoi** : c'est un geste courant dans les terminaux Windows, et pratique avec Claude Code pour lui passer un fichier ou une image. L'itération 12 bloquait déjà les dépôts ; ceux qui visent un terminal deviennent maintenant utiles.
- **Vérifié** : dans l'instance de test, un fichier dont le chemin contient des espaces déposé après `echo ` donne `echo 'C:\...\exemple fichier.txt'`, que PowerShell affiche. Un dossier déposé sur un autre pane s'insère sans guillemets et rend ce pane actif. 184 tests passent.

### 14. Réordonner les workspaces

- **Quoi** : « Monter » et « Descendre » dans le menu contextuel d'un workspace (désactivés aux extrémités, avec leur raccourci affiché), Alt + ↑ / ↓ sur un workspace sélectionné au clavier dans le panneau, et « Monter / Descendre le workspace » dans la palette pour le workspace actif. `sessionStore.moveWorkspace` fait le déplacement ; l'ordre est enregistré avec la session, et le bandeau de l'en-tête (panneau replié) le suit. Spécification (« Convention proposée »), README et `docs/FRONTEND_ARCHITECTURE.md` sont à jour.
- **Pourquoi** : l'ordre des workspaces était figé à celui de leur création, sans aucun moyen de le changer, alors qu'on peut déplacer les onglets.
- **Vérifié** : dans l'instance de test, les entrées du menu sont justes ; « Monter » passe PlannerATM en tête, Alt + ↑ remonte LZGChallenge d'un rang et le focus reste sur sa ligne.

### 15. Relancer un shell terminé au clavier

- **Quoi** : quand le shell du pane s'arrête (`exit`) ou n'a pas pu démarrer, le bouton « Relancer » du message prend le focus, s'il était dans ce pane ou nulle part. Entrée relance donc le shell, comme dans Windows Terminal, et Tab parcourt « Choisir un shell » et « Fermer le pane ». Après une relance, un changement de shell ou « Ignorer », le focus revient au terminal. Un dossier disparu ne prend pas le focus, car le shell y est toujours vivant.
- **Pourquoi** : le terminal mort gardait le focus et capturait même Tab. Les boutons du message étaient inaccessibles au clavier, et après un clic sur « Relancer » il fallait encore cliquer dans le terminal pour taper.
- **Vérifié** : dans l'instance de test, `exit` affiche le message avec le focus sur « Relancer ». Entrée relance le shell, le message disparaît et `echo relance-ok` s'exécute sans clic.

### 16. Messages de commit lisibles dans un graphe étroit

- **Quoi** : le graphe Git mesure sa largeur et masque à l'affichage la colonne Date, puis Auteur, tant que la colonne Message aurait moins de 200 px (`fitGraphColumns` dans `gitGraphStyles.ts`). Le réglage enregistré n'est pas modifié : les colonnes réapparaissent dès que la place revient, et le menu de l'en-tête garde la main.
- **Pourquoi** : Auteur (130 px) et Date (120 px) gardaient leur largeur fixe quoi qu'il arrive. Dans une fenêtre de 1300 px avec le panneau de droite ouvert, le message des commits tombait à environ 60 px (« Ren… », « Réor… »), illisible.
- **Vérifié** : mesures dans l'instance de test. Fenêtre maximisée, toutes les colonnes sont là (Message 709 px). À 1300 px, Auteur et Date sont masqués et Message passe à 323 px (capture). À 1100 px, Message fait 123 px avec les seules colonnes fixes restantes.

### 17. Écran vide plus accueillant et utilisable au clavier

- **Quoi** : quand aucun workspace n'est ouvert, l'écran propose une courte ligne d'accueil, « Nouveau workspace » et « Ouvrir un projet… » (le sélecteur de projets), en plus de « Rouvrir le dernier onglet fermé » quand c'est possible. « Nouveau workspace » prend le focus si rien d'autre ne l'a : Entrée suffit pour repartir.
- **Pourquoi** : la spécification demande « un message accueillant et une action pour en créer un ». Surtout, sans terminal, aucun raccourci direct ne répondait et rien n'avait le focus : au clavier, on restait bloqué sur un écran vide.
- **Vérifié** : instance de test partant d'une session sans workspace. Le focus est sur « Nouveau workspace » ; Entrée crée le workspace, son terminal et l'éditeur de nom (capture).

### 18. Raccourcis directs actifs hors des terminaux

- **Quoi** : Ctrl + Maj + T, W, D, H, X, Z, E, G, Ctrl + Maj + PageUp / PageDown, Alt + flèche et Ctrl + Tab fonctionnent maintenant aussi quand le focus est dans le panneau des workspaces, le graphe Git ou l'explorateur. `handleDocumentShortcut` (dans `shortcuts.ts`, écouté par `AppShell`) réutilise la même table de raccourcis que les terminaux. Il ne fait rien dans un champ de saisie, un menu, une boîte de dialogue ou sous une boîte modale, ni pour les touches qu'un panneau a déjà traitées. Il remplace le traitement de Ctrl + Tab ajouté à l'itération 1.
- **Pourquoi** : ces raccourcis n'étaient interceptés que dans xterm.js. Après un clic dans le graphe ou le panneau, Ctrl + Maj + T ou Ctrl + Maj + D ne faisaient plus rien, et il fallait d'abord recliquer dans un terminal.
- **Vérifié** : dans l'instance de test, Ctrl + Maj + T depuis le graphe Git crée un onglet (13 → 14) et Ctrl + Maj + D depuis une ligne du panneau découpe le pane actif (1 → 2). Dans le message de commit, Ctrl + Maj + T ne fait rien et le focus reste dans le champ.

### 19. Double-clic sur un séparateur : taille par défaut

- **Quoi** : un double-clic sur un séparateur rétablit sa taille par défaut. Un split revient à parts égales ; le panneau des workspaces à 292 px, le panneau de droite à 280 px ; la colonne des branches et les colonnes Branche / Tag, Graphe, Auteur et Date du graphe Git reprennent leur largeur initiale. Les infobulles des séparateurs le signalent. `SidebarResizer` et `GitColumnResizer` reçoivent pour cela leur largeur par défaut (`defaultWidth`), et `SPLIT_RATIO_DEFAULT` et `SIDEBAR_DEFAULT` sont exportés par le modèle.
- **Pourquoi** : c'est la convention de VS Code et de la plupart des éditeurs. Sans elle, retrouver un partage égal ou une largeur d'origine demandait un réglage à l'œil.
- **Vérifié** : dans l'instance de test, chaque séparateur est d'abord modifié au clavier puis double-cliqué avec une vraie souris : panneau des workspaces 337 → 292, colonne Graphe 170 → 100, panneau de droite 200 → 280, split 65 % → 50 %.

### 20. Panes distincts dans la palette

- **Quoi** : dans la palette, un pane est désormais libellé « Pane · workspace / onglet / dossier (shell) » au lieu de « Pane · workspace / onglet / shell ».
- **Pourquoi** : les panes d'un même onglet avaient des libellés identiques (« Pane · Dock / dock-terminal / powershell » trois fois), et seul le chemin, en petit et tronqué, les distinguait. Le nom du dossier dans le libellé les rend reconnaissables et fait remonter la recherche par nom de dossier.
- **Vérifié** : la recherche « src » donne directement le pane `src`, et « pane dock » liste les trois panes de l'onglet dock-terminal sous des noms différents (capture).

### 21. Déplacer un onglet depuis le panneau

- **Quoi** : dans le panneau des workspaces, Alt + ↑ / ↓ sur un onglet sélectionné au clavier le déplace d'un rang dans son workspace. Son menu contextuel propose aussi « Monter » et « Descendre », désactivés aux extrémités. `sessionStore.shiftTab` fait le déplacement. Le calcul de position est partagé avec le menu des workspaces (`menuPlaceOf`, `MOVE_KEYS` dans `workspacePanel.ts`), qui est ainsi simplifié.
- **Pourquoi** : c'est la suite naturelle de l'itération 14. Jusqu'ici, on ne pouvait déplacer un onglet au clavier qu'avec Ctrl + Maj + PageUp / PageDown depuis un terminal, et seulement l'onglet actif.
- **Vérifié** : Alt + ↑ sur « tests » le fait passer devant « web », dans le panneau comme dans la barre d'onglets, et le focus reste sur sa ligne. Maj + F10 sur le premier onglet ouvre son menu avec « Monter » désactivé, et Échap rend le focus à la ligne.

### 22. Menu contextuel des onglets de la barre et « Fermer les autres onglets »

- **Quoi** : un clic droit, Maj + F10 ou la touche Menu sur un onglet de la barre du haut ouvre son menu : Renommer, Déplacer à gauche / à droite (désactivés aux extrémités), Fermer l'onglet, Fermer les autres onglets. Ce dernier (`closeOtherTabsKeepingText` dans `tabLifecycle.ts`) passe par le garde-fou habituel : une seule confirmation si des programmes tournent, et le texte des onglets est conservé pour Ctrl + Maj + Z, dans la limite des cinq derniers onglets fermés.
- **Pourquoi** : un clic droit sur un onglet de la barre ne faisait rien, alors que le panneau des workspaces a son menu. Et après avoir ouvert beaucoup d'onglets, il fallait les fermer un par un.
- **Vérifié** : dans l'instance de test, le menu du dernier onglet a « Déplacer à droite » grisé. « Fermer les autres onglets » ne laisse que cet onglet (13 fermés, sans programme actif donc sans confirmation), et Ctrl + Maj + Z rouvre le dernier fermé, actif, avec un nouveau terminal.

### 23. « Afficher dans l'Explorateur Windows » depuis l'arbre des fichiers

- **Quoi** : le menu contextuel d'un fichier ou d'un dossier de l'explorateur propose « Afficher dans l'Explorateur Windows ». L'Explorateur s'ouvre sur le dossier parent avec l'élément sélectionné, comme « Reveal in File Explorer » dans VS Code. La chaîne complète est en place : message `files.reveal`, traité par `FileExplorerFeed`, puis `LocalActions.RevealInExplorer` dans `Dock.Core` (élément absent : erreur en français). Deux tests xUnit ont été ajoutés.
- **Pourquoi** : on pouvait ouvrir un fichier dans l'éditeur ou copier son chemin, mais pas le retrouver dans l'Explorateur Windows (pour le glisser dans un mail, voir ses propriétés, etc.). Le bouton de l'en-tête du pane n'ouvre que le dossier courant.
- **Vérifié** : pour respecter la règle `ArgumentList`, j'ai d'abord essayé `"/select,C:\…\exemple fichier.txt"`, que .NET met entièrement entre guillemets : l'Explorateur ne sait pas le lire et ouvre « Documents ». La bonne forme passe `/select,` et le chemin en deux arguments : fichier avec ou sans espaces bien sélectionné. De bout en bout dans l'instance de test, `package.json` apparaît sélectionné dans `web`. Les fenêtres de l'Explorateur ouvertes par ces essais ont été refermées. 186 tests passent.

### 24. Fermer l'onglet ou les autres onglets depuis la palette

- **Quoi** : la palette propose « Fermer l'onglet » et, s'il y en a plusieurs, « Fermer les autres onglets », pour l'onglet actif. Ces commandes passent par le même garde-fou que la croix et le menu contextuel.
- **Pourquoi** : la palette ne proposait que « Fermer le pane actif ». Fermer un onglet de plusieurs panes au clavier demandait de fermer ses panes un par un.
- **Vérifié** : dans l'instance de test, taper « fermer les autres » puis Entrée ne laisse que l'onglet actif.

### 25. Dupliquer un onglet

- **Quoi** : « Dupliquer l'onglet », dans le menu d'un onglet (barre ou panneau) comme dans la palette, insère juste après lui une copie avec la même disposition de splits, les mêmes dossiers et les mêmes shells. Les terminaux sont neufs et aucune commande n'est rejouée. La copie devient active et la barre de statut confirme. `sessionStore.duplicateTab` réutilise `cloneTabWithNewIds`, déjà utilisé pour rouvrir un onglet fermé.
- **Pourquoi** : recréer à la main un onglet à plusieurs panes (par exemple serveur, tests et agent dans le même projet) demandait autant de splits et de `cd` que de panes.
- **Vérifié** : dans l'instance de test, un onglet à deux panes dupliqué depuis la palette donne un nouvel onglet actif avec le même split et deux shells neufs dans le même dossier (capture). Depuis le menu de la barre, le message « Onglet « web » dupliqué… » s'affiche.

### 26. Raccourci pour masquer ou afficher le panneau des workspaces

- **Quoi** : Leader puis B, ou Ctrl + Maj + B (B comme la barre latérale de VS Code), masque ou affiche le panneau des workspaces. Si le focus était dans le panneau au moment de le masquer, il revient au terminal actif. Le rappel des séquences Leader, la palette, l'infobulle du bouton de l'en-tête et le tableau des raccourcis du README mentionnent le raccourci.
- **Pourquoi** : c'est le geste le plus direct pour gagner de la largeur, et il fallait passer par le bouton ou par la palette. La lettre B était libre dans les deux tables de raccourcis.
- **Vérifié** : dans l'instance de test, Ctrl + Maj + B puis Leader puis B basculent le panneau. Depuis une ligne du panneau, Ctrl + Maj + B le masque et le focus arrive dans le terminal.

## Reste à faire et idées

- Taille de police et zoom du terminal : absents (police fixe à 14 px). La spécification les classe « À décider » (section 4), donc je n'y ai pas touché ; c'est à trancher.
