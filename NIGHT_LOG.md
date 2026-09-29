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
- **Correction apportée par l'itération 32** : un vrai glisser depuis une autre application n'atteignait pas du tout la page tant que `AllowDrop` n'était pas activé sur la WebView2. En usage réel, le défaut décrit ci-dessus ne se produisait donc pas avant cette nuit, ma simulation court-circuitant cette couche. Depuis l'itération 32, les vrais dépôts arrivent jusqu'à la page, et ces protections deviennent réellement nécessaires. Vérifié avec un vrai glisser : un fichier lâché hors d'un terminal n'ouvre rien.

### 13. Déposer un fichier sur un terminal insère son chemin

- **Quoi** : glisser un ou plusieurs fichiers ou dossiers depuis l'Explorateur Windows sur un terminal y colle leurs chemins, comme dans Windows Terminal, et le pane devient actif. Le web joint les fichiers au message `terminal.drop` (`postMessageWithAdditionalObjects`, seul moyen d'obtenir leur vrai chemin dans une WebView2). L'hôte les met en forme selon le shell avec `DroppedPaths` dans `Dock.Core` : guillemets simples pour PowerShell (apostrophe doublée), doubles pour CMD, simples pour Git Bash, rien si ce n'est pas nécessaire, une espace après chaque chemin. Il renvoie `terminal.dropped`, et le texte est collé par xterm.js, donc entre crochets si le programme le demande, comme Claude Code. Huit tests xUnit ont été ajoutés. Spécification (« Convention proposée »), README et documents d'architecture sont à jour.
- **Pourquoi** : c'est un geste courant dans les terminaux Windows, et pratique avec Claude Code pour lui passer un fichier ou une image. L'itération 12 bloquait déjà les dépôts ; ceux qui visent un terminal deviennent maintenant utiles.
- **Vérifié** : dans l'instance de test, un fichier dont le chemin contient des espaces déposé après `echo ` donne `echo 'C:\...\exemple fichier.txt'`, que PowerShell affiche. Un dossier déposé sur un autre pane s'insère sans guillemets et rend ce pane actif. 184 tests passent.
- **Correction apportée par l'itération 32** : ces essais passaient par le protocole de débogage. Avec un vrai glisser depuis l'Explorateur, rien n'arrivait à la page, faute d'`AllowDrop` sur la WebView2 : la fonction ne marchait donc pas en usage réel avant l'itération 32, qui l'a vérifiée avec un vrai glisser OLE.

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

### 27. Fondu sur les bords de la barre d'onglets

- **Quoi** : quand des onglets sont masqués à gauche ou à droite de la bande qui défile, son bord s'estompe sur 24 px (`mask-image`), d'un côté ou des deux. L'état est recalculé au défilement et au redimensionnement, avec une tolérance de 8 px pour ignorer le repère de dépôt invisible des extrémités.
- **Pourquoi** : depuis l'itération 10, la bande défile, mais rien n'indiquait qu'il restait des onglets hors de vue.
- **Vérifié** : avec 14 onglets, le masque vaut « fondu à droite » au début, « des deux côtés » au milieu et « à gauche » à la fin. Un premier essai sans tolérance affichait un fondu à droite alors que le dernier onglet était visible ; c'est corrigé.

### 28. Leader actif hors des terminaux

- **Quoi** : Ctrl + Espace active aussi le Leader quand le focus est dans le panneau des workspaces, le graphe Git ou l'explorateur. La lettre suivante lance la commande (même table que dans les terminaux), Échap l'annule, et une touche sans commande l'abandonne. Tout passe par `handleDocumentShortcut`, qui ne fait rien dans un champ de saisie, un menu ou une boîte de dialogue.
- **Pourquoi** : depuis l'itération 18, les raccourcis directs marchent partout, mais le Leader, présenté comme le moyen de tout faire au clavier, ne répondait que dans un terminal.
- **Vérifié** : depuis une ligne du panneau, Ctrl + Espace affiche le rappel du Leader. T crée un onglet et le focus passe au nouveau terminal ; Ctrl + Espace puis Échap annule et le focus reste sur la ligne.

### 29. Corrections issues d'une relecture indépendante

Un sous-agent a relu tout le diff de la nuit (`main..night-session`) sans rien modifier. J'ai vérifié chacun de ses constats avant de le corriger.

- **Focus sous le graphe (gravité moyenne)** : depuis l'itération 18, un raccourci de pane lancé depuis le graphe Git (Ctrl + Maj + D / H / X, Alt + flèche) activait un pane caché sous le graphe et lui donnait le focus. La frappe suivante, par exemple ↑ puis Entrée, pouvait relancer une commande de l'historique sans qu'on la voie. `AppShell` reprend maintenant le focus pour le graphe à chaque changement de pane actif, ainsi qu'à la fermeture de la palette (défaut antérieur à la nuit). Vérifié : après Ctrl + Maj + D puis Alt + ← depuis le graphe, le focus reste sur le graphe.
- **Menus ouverts au clavier** : dans les menus des onglets et des terminaux, le focus revenait sur la première entrée à chaque rendu. Il suffisait d'un changement d'état d'agent pour qu'Entrée lance la mauvaise action, « Coller » par exemple. Les deux gestionnaires de fermeture sont stabilisés par `useCallback`, comme dans les autres menus. Vérifié : le focus reste sur « Fermer l'onglet » pendant qu'un agent change d'état.
- **Chemins déposés avec apostrophe typographique** (« Lettre d’information.pdf ») : PowerShell traite ’ comme une apostrophe, et la commande était cassée (confirmé dans PowerShell 5.1). Les apostrophes typographiques sont maintenant doublées, les guillemets typographiques et les blancs Unicode (espaces insécables) déclenchent la mise entre guillemets, et deux tests ont été ajoutés. L'appel à `postMessageWithAdditionalObjects` est aussi protégé pour un runtime WebView2 trop ancien.
- **Onglet actif après un déplacement** : Ctrl + Maj + PageDown ou « Déplacer à droite » pouvaient le pousser hors de la bande visible. Il est maintenant ramené en vue.
- **Explorateur et virgules** : `explorer /select,` ouvrait « Ce PC » pour un chemin contenant une virgule sans espace (`a,b\f.txt`, confirmé). `RevealInExplorer` utilise désormais l'API `SHOpenFolderAndSelectItems`, sans ligne de commande. Vérifié : `f.txt` est bien sélectionné dans `a,b`, et le fichier avec espaces fonctionne toujours.

### 30. Glisser-déposer des workspaces

- **Quoi** : dans le panneau, la ligne d'un workspace se glisse à la souris. On la dépose devant un autre workspace, ou dans l'espace libre sous le dernier pour l'envoyer en fin de liste. Pendant le glisser, le workspace est estompé et un repère vert montre l'emplacement. `workspaceDrag.ts` reprend le mécanisme des onglets (événements pointeur, seuil de 4 px, `elementFromPoint`), puis `sessionStore.moveWorkspaceBefore` fait le déplacement. Le glisser part de la ligne du workspace seulement : celui des onglets n'est pas touché.
- **Pourquoi** : c'est la suite de l'itération 14. Les onglets se glissaient déjà, les workspaces non.
- **Vérifié** : glisser réel à la souris dans l'instance de test. LZGChallenge déposé sur PlannerATM passe en tête, puis déposé sous le dernier workspace passe en fin de liste ; une capture pendant le glisser montre le repère. Au passage, mon script de test ne produisait aucun `pointermove` : `SetCursorPos` suivi d'un mouvement relatif nul n'en génère pas. Il utilise maintenant des mouvements absolus, comme une vraie souris.

### 31. Ouvrir un dossier de l'arbre dans l'éditeur

- **Quoi** : le menu contextuel d'un dossier de l'explorateur propose « Ouvrir dans l'éditeur », juste après « Ouvrir un terminal ici ». Le dossier s'ouvre avec la commande d'éditeur configurée, VS Code par défaut, par la commande `context.open` déjà utilisée par l'en-tête des panes.
- **Pourquoi** : seuls les fichiers pouvaient s'ouvrir dans l'éditeur depuis l'arbre. Pour ouvrir un sous-dossier, dans un monorepo par exemple, il fallait passer par un terminal.
- **Vérifié** : pour ne pas ouvrir VS Code sur ton bureau en pleine nuit, l'instance de test utilisait un faux éditeur (un `.cmd` qui écrit son argument). Il a reçu `D:\Projects\Perso\LZGChallenge\bin` et la barre de statut l'annonce. La configuration d'éditeur de l'instance de test a ensuite été remise.

### 32. Le dépôt de fichiers fonctionne avec un vrai glisser depuis l'Explorateur

- **Quoi** : l'hôte active `AllowDrop` sur la WebView2 (`MainWindow`). C'est la seule modification de code : le traitement dans la page (itération 13) et les protections (itération 12) étaient déjà en place.
- **Pourquoi** : en voulant confirmer l'itération 13 avec un vrai glisser OLE, plutôt qu'avec le protocole de débogage, j'ai constaté que rien n'arrivait à la page. Le contrôle WebView2 de WinUI ne transmet les glisser venus d'autres applications que si `AllowDrop` est activé, et il ne l'est pas par défaut. Déposer un fichier de l'Explorateur sur un terminal ne faisait donc rien en usage réel. J'ai d'abord essayé de gérer le dépôt en XAML (`DragOver` / `Drop` sur l'élément), mais le contrôle consomme ces événements ; j'ai abandonné cette voie au profit du simple réglage.
- **Vérifié** : banc de test sûr. Une petite fenêtre WinForms sert de source et démarre un vrai `DoDragDrop` de fichier. Avant d'agir, le script vérifie que le point de départ appartient à cette source et le point d'arrivée à l'instance de test. Une cible OLE témoin a d'abord confirmé que la simulation fonctionne. Résultats :
  - lâché sur un terminal après `echo `, le fichier arrive dans la page (`drop`, 1 fichier) et l'hôte renvoie le chemin mis entre guillemets ; le terminal affiche `echo 'C:\…\exemple fichier.txt'` (capture) ;
  - lâché sur le panneau des workspaces, il est refusé : aucun message, aucune navigation, aucune nouvelle fenêtre.

  Les entrées 12 et 13 ont été complétées en conséquence.

### 33. Les messages de pane restent joignables au clavier, deux faux signalements corrigés

- **Quoi** :
  - quand un message recouvre un pane (shell terminé, échec de démarrage, dossier disparu), son bouton par défaut reçoit le focus chaque fois que le pane devient actif, et plus seulement à l'apparition du message. `focusPane` remplace les appels directs au terminal (activation d'un pane, retour de la palette ou d'un renommage, repli du panneau, `joinPane`) et vise d'abord ce bouton. Pour un dossier disparu, c'est « Ignorer » : le shell y tourne encore et Entrée ne doit pas l'arrêter ; Maj + Tab mène à « Relancer dans le dossier de repli ». La ligne « Repli : … » passe au-dessus des boutons, où l'infobulle ne la cache plus ;
  - à la restauration, le message « dossier disparu » ne restait pas une seconde : le shell, démarré dans le dossier personnel, signalait ce dossier, et tout changement de dossier effaçait l'état du pane. `markAlive` garde maintenant ce message jusqu'au choix de l'utilisateur ;
  - l'hôte n'associe plus un pane qu'aux erreurs des commandes `terminal.*`. Avant, « Ouvrir dans l'explorateur » sur un dossier disparu, ou un éditeur introuvable, affichait « Le shell n'a pas pu démarrer » sur un shell vivant, avec « Relancer » comme action par défaut.
- **Pourquoi** : en revenant sur un pane dont le shell était terminé (Alt + flèche), le focus retombait dans le terminal masqué : Entrée ne relançait rien et Tab restait capturé. C'était le piège noté dans le reste à faire. En le corrigeant, j'ai trouvé les deux autres défauts ; le premier rendait la recette R22 (restauration avec dossier disparu) fausse en pratique.
- **Vérifié** : dans l'instance de test,
  - shell terminé : Alt + ← puis Alt + → rendent le focus à « Relancer » ; Entrée relance et `echo` s'exécute sans clic ;
  - dossier supprimé depuis l'autre pane : Alt + → donne le focus à « Ignorer » ; Entrée masque le message et le shell répond toujours ;
  - restauration avec un dossier disparu : le message reste, focus sur « Ignorer » ; Maj + Tab puis Entrée relancent le shell dans le dossier de repli ;
  - « Ouvrir dans l'explorateur » sur un dossier disparu : l'erreur s'affiche dans la barre de statut seulement ;
  - palette puis Échap : le focus revient au terminal.

### 34. Le pane qui a le focus est toujours le pane actif

- **Quoi** :
  - le focus qui entre dans un pane inactif, que ce soit son terminal, un bouton de son en-tête ou de son message, rend ce pane actif (`PaneView`) ;
  - un clic sur le fond d'un message de pane, hors boutons, donne le focus à son bouton par défaut et active donc le pane. Avant, ce clic envoyait le focus nulle part ;
  - fermer le menu « Choisir un shell » du message (Échap, Tab) rend le focus à son bouton, comme le menu « + » de la barre d'onglets. Avant, il tombait sur la page.
- **Pourquoi** : un clic sur « Relancer » dans le message d'un pane inactif relançait son shell et y envoyait la saisie, mais l'autre pane restait actif (bordure verte, cible des commandes). Leader puis X aurait fermé le pane voisin, pas celui où l'on tapait. Le même décalage apparaissait avec « Ignorer », « Relancer dans le dossier de repli » ou Tab vers les boutons d'un autre pane. La spécification demande d'activer un pane en cliquant dedans (PANE-04).
- **Vérifié** : dans l'instance de test, avec le pane de gauche actif et le shell du pane de droite terminé :
  - un vrai clic sur « Relancer » à droite rend ce pane actif et y met le focus ;
  - un clic sur le fond du message à droite rend ce pane actif, focus sur « Relancer » ;
  - Tab, Entrée sur « Choisir un shell », puis Échap : le menu se ferme et le focus revient sur son bouton.

### 35. Tab ne quitte plus une fenêtre ouverte

- **Quoi** : un utilitaire `keepTabInside` (`focusTrap.ts`) fait boucler Tab et Maj + Tab à l'intérieur des fenêtres modales : palette et sélecteur de projets (`SearchDialog`), paramètres, et confirmations de fermeture, de suppression et Git. Tab sur le dernier élément revient au premier, Maj + Tab sur le premier va au dernier ; la palette, qui n'a qu'un champ, le garde.
- **Pourquoi** : dans Paramètres, Maj + Tab depuis le premier champ envoyait le focus dans le terminal masqué derrière la fenêtre. xterm.js garde Tab, donc le focus y restait bloqué : la saisie partait au shell alors que la fenêtre restait affichée. Même chose depuis la palette ou une confirmation. La spécification demande d'éviter les pièges de focus (section 16).
- **Vérifié** : dans l'instance de test,
  - Paramètres : Maj + Tab depuis le premier champ va sur « Enregistrer », puis Tab revient au premier champ ;
  - palette : Tab et Maj + Tab laissent le focus dans le champ de recherche ;
  - confirmation de fermeture avec un `ping` actif : Tab et Maj + Tab alternent entre « Annuler » et « Arrêter et fermer » ;
  - Échap ferme chaque fenêtre et rend le focus au terminal.

### 36. Champs numériques des paramètres qu'on peut vider et corriger

- **Quoi** : dans Paramètres, les trois champs de persistance (sauvegarde du texte, lignes par pane, historique global) gardent le texte saisi, même vide. Un champ vide ou hors bornes passe en erreur : bordure, contour de focus et indication « Entre x et y » en rouge, avec `aria-invalid`. Enregistrer ou Ctrl + Entrée ramène alors le focus sur ce champ sans rien envoyer, et l'infobulle d'Enregistrer indique les bornes attendues. Une règle CSS globale colore le contour de focus de tout champ `aria-invalid`.
- **Pourquoi** : effacer un champ remettait aussitôt l'ancienne valeur. Pour passer de 30 à 60 secondes, Ctrl + A, Retour arrière puis « 60 » donnait « 3060 ». L'hôte ramenait ensuite en silence la valeur à 600 et annonçait « Réglages enregistrés » : le réglage obtenu n'était pas celui voulu, sans aucun signal.
- **Vérifié** : dans l'instance de test,
  - Ctrl + A puis Retour arrière laisse le champ vide et en erreur (contour et texte rouges, capture) ;
  - Ctrl + Entrée n'enregistre pas et garde le focus sur le champ ;
  - saisir 45 puis Ctrl + Entrée enregistre : `persistence.json` contient 45.

### 37. Renommer un fichier garde son extension

- **Quoi** : au renommage d'un fichier dans l'arbre (F2 ou menu « Renommer »), seul le nom avant la dernière extension est sélectionné, comme dans l'Explorateur Windows et VS Code. Un dossier ou un nom qui commence par un point (`.gitignore`) reste sélectionné en entier. `InlineNameEditor` accepte pour cela une fin de sélection (`selectionEnd`), calculée par `renameSelectionEnd`.
- **Pourquoi** : F2 sélectionnait tout le nom. Taper le nouveau nom effaçait l'extension : `alpha.txt` renommé en « renamed » devenait `renamed`, sans extension, et perdait son association dans Windows.
- **Vérifié** : dans l'instance de test, sur un dossier de test :
  - F2 sur `sub` sélectionne « sub » ; sur `.gitignore`, « .gitignore » ; sur `alpha.txt`, « alpha » ;
  - taper « omega » puis Entrée renomme le fichier en `omega.txt`, qui reste sélectionné dans l'arbre.

### 38. La branche courante reste lisible dans l'en-tête Git

- **Quoi** : dans l'en-tête du panneau Git, le nom du dépôt et la branche se partagent la place libre de la ligne : une part pour le dépôt, deux pour la branche, chacun plafonné à sa largeur utile. Un nom court comme `main` n'est donc jamais tronqué, un nom long l'est avec une infobulle qui le donne en entier, et le bouton « Graphe » reste toujours visible à droite.
- **Pourquoi** : dans le panneau à sa largeur par défaut (280 px), `main` s'affichait « ma… », rogné au même rythme que le nom du dépôt alors que c'est l'information la plus utile. La branche tronquée n'avait pas d'infobulle. Un premier essai, qui empêchait la branche de rétrécir, faisait déborder la ligne avec une branche longue et coupait « Graphe » ; il a été remplacé par ce partage.
- **Vérifié** : dans l'instance de test, sur un dépôt de test :
  - à 280 px, `main` est entier (44 px) et le dépôt tronqué (« explorer… », infobulle du chemin) ;
  - la branche `feature/une-branche-au-nom-vraiment-tres-long` prend 67 px contre 33 pour le dépôt, avec son infobulle, et la ligne ne déborde pas (260 px de contenu pour 260 px de ligne) ;
  - à 430 px, tout est entier et « Graphe » est calé à droite.

### 39. Le message des commits garde de la place dans un graphe Git étroit

- **Quoi** : quand le message aurait moins de 200 px, une fois Auteur et Date masqués, `fitGraphColumns` resserre d'abord la colonne Branche / Tag, jusqu'à 88 px, assez pour une étiquette comme `main`. Il resserre ensuite la colonne Graphe jusqu'à la largeur utile des voies chargées : 20 px par voie plus une marge, 48 px au moins. Aucune colonne n'est élargie et le réglage enregistré ne change pas ; la largeur de voie est désormais une constante partagée (`GRAPH_LANE_WIDTH`).
- **Pourquoi** : c'était dans le reste à faire. Dans une fenêtre de 1 100 px avec les trois panneaux ouverts, la table ne faisait que 293 px et le message n'avait plus que 53 px : « Premier com… ». La colonne Graphe gardait 100 px pour deux voies seulement.
- **Vérifié** : dans l'instance de test, sur un dépôt de test :
  - à 1 300 px, rien ne change (140, 100 et 253 px) ;
  - à 1 100 px, Branche / Tag passe à 88 px, `main` restant entier, Graphe à 48 px et le message à 157 px : « Premier commit de test » est entier ;
  - avec trois branches divergentes, Graphe passe à 68 px et les trois voies restent entières (capture).

### 40. La barre d'outils Git tient sur une ligne

- **Quoi** : la barre Fetch / Pull / Push / Annuler / Actualiser de l'en-tête Git s'étend de 4 px de chaque côté dans la marge du panneau. Elle gagne 8 px et ses icônes s'alignent sur le nom du dépôt au-dessus. Les boutons eux-mêmes, utilisés à 13 endroits, ne changent pas.
- **Pourquoi** : à la largeur par défaut du panneau (280 px), il manquait 4 px (264 px de boutons pour 260 px de ligne). Le bouton « Actualiser » passait donc seul sur une deuxième ligne, ce qui ressemblait à une erreur de mise en page et faisait descendre tout le panneau d'une ligne.
- **Vérifié** : dans l'instance de test, la barre fait 268 px sur une seule ligne de 24 px, avec les cinq boutons sur la même ligne, et l'icône « Fetch » est alignée sur le nom du dépôt (capture).

### 41. L'import de préférences signale les valeurs ramenées dans leurs bornes

- **Quoi** :
  - `PersistenceSettingsModel.OutOfRangeWarnings` décrit chaque valeur de persistance hors bornes, par exemple « Sauvegarde du texte (secondes) : 3000 ramené à 600 (entre 5 et 600). » ;
  - `SettingsService.Import` renvoie ces avertissements avec les réglages (`PreferencesImportResultModel.Warnings`, `Failed` pour les refus), et l'hôte les ajoute à `settings.imported` ;
  - Paramètres les affiche sous l'avis d'import, en couleur d'avertissement, avant tout enregistrement.
- **Pourquoi** : c'était dans le reste à faire. Un fichier de préférences avec 3 000 secondes était importé en 600 sans aucun signal. Le formulaire, qui ne montre que des valeurs valides, rendait la correction invisible jusqu'à l'enregistrement.
- **Vérifié** :
  - trois tests xUnit : valeurs ramenées, un avertissement par valeur corrigée, aucun avertissement si tout est dans les bornes ; 190 tests au vert ;
  - dans l'instance de test, un `settings.imported` simulé par le protocole de débogage affiche les deux avertissements sous l'avis d'import (capture). Le parcours avec la vraie boîte de dialogue de fichiers n'a pas été automatisé : la partie hôte se limite à transmettre la liste.

### 42. Les textes coupés montrent leur texte complet au survol

- **Quoi** : quand aucun `data-tip` n'est trouvé sous le pointeur, `Tooltip` cherche un texte coupé par des points de suspension, sur l'élément survolé ou jusqu'à deux parents au-dessus, et affiche son texte complet. Les infobulles explicites restent prioritaires. La bulle coupe désormais les mots trop longs, comme les chemins, au lieu de déborder de ses 360 px.
- **Pourquoi** : de nombreux textes tronqués (`truncate`) n'avaient aucune infobulle, par exemple : barre de statut, noms de fichiers de l'arbre et du panneau Git, libellés de la palette, étiquettes de branches… Un long message d'erreur avec un chemin restait illisible dans la barre de statut. Un mécanisme unique évite d'ajouter un `data-tip` à chaque endroit, et ne s'active que si le texte est vraiment coupé.
- **Vérifié** : dans l'instance de test, avec de vrais mouvements de souris :
  - un long message d'erreur dans la barre de statut montre son texte complet, chemin coupé proprement dans la bulle (capture) ;
  - un nom de fichier tronqué du panneau Git affiche son nom complet ;
  - `nouveau.txt`, non tronqué, n'affiche rien ;
  - le bouton « Graphe » garde son infobulle explicite.

### 43. L'arbre des fichiers donne le focus à sa première ligne dès qu'elle arrive

- **Quoi** : quand l'arbre reçoit le focus avant que son dossier soit lu, le focus se pose sur le conteneur de l'arbre, faute de ligne. Dès que les lignes arrivent, `FileTree` le passe à la ligne focusable, la sélection ou à défaut la première ligne.
- **Pourquoi** : à la première ouverture de l'explorateur sur un dossier, tout le panneau restait encadré par le contour de focus et aucune ligne n'était visée. La première flèche sélectionnait bien une ligne, mais l'état initial avait l'air d'un défaut. Depuis un dossier déjà lu, le focus allait déjà sur une ligne.
- **Vérifié** : dans l'instance de test relancée, l'ouverture des fichiers par Ctrl + Maj + E, y compris sur un sous-dossier jamais lu, laisse le focus sur la première ligne de l'arbre (`gamma.txt`, capture). À l'itération 37, dans le même cas, il restait sur le conteneur. L'état intermédiaire n'a pas pu être observé : sur ces petits dossiers, la liste arrive en moins de 250 ms.

### 44. Deuxième relecture indépendante, premier lot : focus des panes et robustesse de l'hôte

Un sous-agent a relu les itérations 30 à 42 (`86c8152..HEAD`) sans rien modifier. Il a rapporté 12 constats, dont 5 confirmés par une reproduction en navigateur isolé. Ce premier lot traite ceux qui touchent au focus des panes et à l'hôte ; je les ai tous reproduits ou vérifiés dans l'instance de test.

- **Régression de l'itération 34** : Tab ou un focus posé sur un bouton d'en-tête d'un pane inactif rendait bien ce pane actif, mais son terminal reprenait aussitôt le focus. Les boutons d'en-tête d'un pane inactif n'étaient donc plus joignables au clavier. `TerminalPane` ne focalise plus son terminal si le focus est déjà dans le pane. Vérifié : le focus reste sur « Copier le chemin » du pane de gauche, devenu actif.
- **Maj + Tab sous un message** : depuis « Relancer », Maj + Tab atteignait la zone de saisie de xterm.js (`tabIndex` 0), puis sa zone défilante, masquées sous le message ; pour un dossier disparu, la frappe partait au shell sans être vue. Tant qu'un message recouvre le pane, `setPaneTerminalTabbable` les sort de l'ordre de Tab, puis rétablit les valeurs de xterm.js. Vérifié : Maj + Tab va sur « Fermer le pane » de l'en-tête, et après la relance la zone de saisie repasse à 0 et la zone défilante perd son attribut.
- **Dépôt de fichier sur un pane recouvert** : le chemin était tapé dans le terminal masqué. `externalDrop` refuse désormais le dépôt sur un tel pane. Vérifié avec un vrai glisser simulé par CDP : rien sur le pane recouvert, chemin inséré dans le pane libre.
- **Hôte et message sans `type`** : `"type": null` passait la désérialisation, et `FailedTerminalPane` levait alors une exception dans le `catch` de `Handle`, ce qui pouvait arrêter l'hôte. Le cas est désormais géré. Vérifié : après un tel message envoyé depuis la page, l'hôte reste vivant et affiche l'erreur dans la barre de statut.

Les constats restants (glisser des workspaces, piège de Tab et zones défilantes, infobulle des onglets, redimensionnement des colonnes du graphe, chaîne magique) font l'objet des itérations suivantes.

### 45. Deuxième relecture, deuxième lot : glisser des workspaces et des onglets

- **Geste commun** : un utilitaire `pointerDrag.ts` (`trackPointerDrag`) porte désormais le geste des deux glisser. Ses écouteurs sont posés sur `window` et retirés d'un coup par un `AbortController`, et le geste s'arrête sans dépôt si le bouton n'est plus enfoncé. Avant, ils étaient posés sur la ligne : si le premier mouvement sortait de la ligne, ils restaient orphelins, et un simple survol plus tard lançait un glisser fantôme (curseur « grabbing », ligne estompée). Le défaut existait aussi pour les onglets.
- **Dépôt dans un interstice** : lâcher un workspace dans l'espace de 4 px entre deux lignes l'envoyait en fin de liste, car cet espace appartenait à la liste. La cible dépend maintenant de la hauteur du pointeur : devant le premier workspace dont le milieu est plus bas, sinon en fin de liste.
- **Clic après le glisser** : glisser un workspace vers le haut le rendait courant, vers le bas non. Le clic qui suit un glisser de workspace est désormais ignoré. Les onglets gardent leur comportement : l'onglet glissé devient actif.
- **Sélection dans le champ de renommage** : sélectionner le nom à la souris lançait un glisser du workspace. Le geste ne démarre plus depuis un champ de saisie.
- **Vérifié** : dans l'instance de test, avec de vrais gestes de souris :
  - « GameSolver » lâché dans l'interstice entre « PlannerATM » et « Dock » arrive devant « Dock », et le workspace actif reste « LZGChallenge » ;
  - une sélection à la souris dans le champ de renommage ne déplace rien ;
  - un appui au bord d'une ligne, une sortie brusque, un relâchement ailleurs puis un survol ne laissent ni glisser fantôme ni curseur « grabbing » ;
  - le glisser d'un onglet dans la barre fonctionne toujours.
- **Test instable** : une exécution de `dotnet test` a échoué une fois sur 190 tests, sans que je sache lequel, puis quatre exécutions ont passé. Ce lot ne touche que le web ; le script de vérification garde désormais le journal complet pour identifier ce test s'il échoue de nouveau.

### 46. Deuxième relecture, dernier lot : clavier des fenêtres, infobulles et colonnes du graphe

- **Zones défilantes des confirmations** : le piège de Tab de l'itération 35 ne connaissait que les éléments focalisables. Chromium, lui, fait aussi une étape de Tab d'une zone qui défile sans rien contenir de focalisable, comme une longue liste de processus. Cette liste ne se défilait donc plus au clavier. `keepTabInside` la compte désormais comme étape. Vérifié avec une fenêtre de 280 px de haut : la liste (121 px de contenu pour 88 px) est atteinte par Tab et par Maj + Tab, et le focus reste dans la confirmation.
- **Nom des onglets tronqués** : l'infobulle générique de la barre d'onglets passait avant l'infobulle automatique de l'itération 42, si bien que le nom complet n'apparaissait jamais. Elle commence maintenant par le nom : « LZGChallenge2 · Double-clic pour renommer, glisser pour déplacer ».
- **Colonnes resserrées du graphe Git** : un glisser partait de la largeur affichée. Il écrasait la largeur enregistrée (un tremblement de 1 px enregistrait 89 au lieu de 140) sans rien changer à l'écran. Tant qu'une colonne est resserrée, son séparateur est désactivé et l'infobulle explique pourquoi. Vérifié : à 1 100 px, un glisser de 40 px ne change rien et la largeur enregistrée reste 140 ; à 1 300 px, les séparateurs redeviennent actifs.
- **Chaîne magique** : dans Paramètres, le focus du champ numérique invalide passe par une référence React au lieu d'un sélecteur écrit en toutes lettres. Vérifié : le champ vidé reçoit bien le focus.

Les douze constats de la relecture sont traités (itérations 44 à 46).

### 47. Navigation par flèches dans le panneau des workspaces

- **Quoi** : dans le panneau des workspaces, ↑ / ↓ / Début / Fin passent d'une ligne visible à l'autre, noms de workspaces et d'onglets confondus. → déplie un workspace replié, ← le replie ; depuis un onglet, ← remonte à son workspace. Tab parcourt toujours chaque bouton, comme avant. Les lignes portent `data-panel-row` et la liste délègue les flèches à `handlePanelRowKeys`.
- **Pourquoi** : au clavier, atteindre un onglet du panneau obligeait à traverser par Tab le chevron, le nom, les états et la croix de chaque ligne au-dessus, soit plusieurs dizaines de Tab avec une vingtaine d'onglets. Les flèches sont le geste attendu dans une arborescence, et Alt + ↑ / ↓ reste réservé au déplacement.
- **Vérifié** : dans l'instance de test, depuis le premier workspace :
  - ↓ ↓ passent à ses deux onglets, ← remonte au workspace ;
  - ← replie le workspace (de 23 à 21 lignes visibles), → le déplie ;
  - Fin va au dernier workspace, Début revient au premier.

### 48. Le README suit les raccourcis et les gestes ajoutés

- **Quoi** : le tableau des raccourcis du README gagne trois lignes, vérifiées dans `shortcuts.ts` : rouvrir le dernier onglet fermé (Leader puis Z, ou Ctrl + Maj + Z), Paramètres (Leader puis « , ») et déplacer l'onglet (Leader puis Pg préc. / Pg suiv., ou Ctrl + Maj + Pg préc. / Pg suiv.). Une phrase y signale les menus contextuels (clic droit ou touche Menu sur un terminal, un onglet, un workspace ou un fichier) et le clavier du panneau des workspaces. La liste des fonctionnalités mentionne la duplication d'onglet.
- **Pourquoi** : ces raccourcis existaient, affichés dans les indications du Leader, mais le README ne les citait pas, pas plus que plusieurs gestes ajoutés cette nuit. C'est la première page qu'on lit.
- **Vérifié** : chaque raccourci ajouté a été relu dans `LEADER_KEYS`, `DIRECT_LETTER_KEYS`, `DIRECT_PAGE_KEYS` et `LEADER_HINTS`. Plus tôt dans l'itération, l'exploration de la vue Git (checkout de `autre` par Entrée dans le panneau des références, bascule de Ctrl + Maj + G quatre fois de suite) n'a montré aucun défaut.

### 49. Agrandir temporairement un pane

- **Quoi** : Leader puis M, Ctrl + Maj + M, la palette (« Agrandir / réduire le pane actif ») ou un double-clic sur l'en-tête d'un pane l'agrandit à toute la zone de l'onglet, comme le zoom de tmux. La disposition enregistrée ne change pas et les autres terminaux continuent de tourner dans `terminalRegistry`. Un bouton « Réduire » apparaît dans l'en-tête du pane agrandi. Le même geste, ce bouton, un split, le passage à un autre pane ou à un autre onglet le réduisent. Un onglet d'un seul pane affiche « Un seul pane dans cet onglet : rien à agrandir. ». L'état (`zoomedPaneId`) n'est pas persisté. Le tout est noté « Convention proposée » dans la spécification, hors du tableau retenu des raccourcis.
- **Pourquoi** : avec trois ou quatre splits, lire une longue sortie ou travailler avec un agent dans un pane étroit est pénible, et refaire la disposition ensuite l'est encore plus. C'est une fonction courante des multiplexeurs (tmux, Windows Terminal).
- **Vérifié** : dans l'instance de test,
  - un seul pane : message ;
  - après un split, Ctrl + Maj + M n'affiche que le pane actif, focus dans son terminal (capture) ;
  - « Réduire » rétablit les deux panes ;
  - un double-clic sur l'en-tête de l'autre pane l'agrandit ;
  - un split pendant le zoom le termine et montre les trois panes ;
  - un aller-retour d'onglet le termine aussi ;
  - graphe Git ouvert par-dessus les terminaux : le zoom laisse le focus au graphe.

### 50. Bilan de nuit en HTML

- **Quoi** : un bilan lisible d'un coup d'œil, `NIGHT_LOG.html`, à côté de ce journal. On y trouve les chiffres de la nuit, ce qu'il faut lire d'abord (incident de 0 h 40, SDK .NET 10 installé), ce qui reste à décider (nouveaux raccourcis proposés, collage de plusieurs lignes, taille de police, tests web), les changements classés par thème avec leur numéro d'itération, puis le reste à faire. Il n'est pas versionné (`.git/info/exclude`) : ce journal reste la référence.
- **Pourquoi** : près de cinquante entrées détaillées sont longues à relire au réveil. Tes consignes demandent un fichier HTML pour tout récapitulatif à relire.
- **Vérifié** : HTML bien formé (analyse automatique des balises) et rendu contrôlé avec Edge en mode headless, dans un profil temporaire isolé.

### 51. Toutes les indications du Leader restent visibles

- **Quoi** : la barre des indications affichée pendant le Leader passe sur deux lignes, dans les 42 px de l'en-tête. Toutes les séquences tiennent dès 1 100 px de large ; au-delà d'une troisième ligne, la fin serait rognée plutôt que de déborder.
- **Pourquoi** : sur une seule ligne, 13 des 17 indications seulement étaient visibles à 1 300 px, et celles de la fin étaient masquées : pane voisin, déplacer l'onglet, « Échap annuler », « Ctrl + Espace envoyer au terminal ». Ce sont justement celles qu'on cherche quand on ne sait plus comment sortir du Leader. Le défaut existait avant l'ajout de « M » à l'itération 49, qui l'aggravait.
- **Vérifié** : dans l'instance de test, 17 indications sur 17 visibles à 1 300 et à 1 100 px (capture).
- **Aussi exploré** :
  - **Suivi des agents** : j'ai simulé un Claude Code en attente dans un onglet d'arrière-plan, avec un processus dans le pane et un fichier `agents\<pane>.json` comme l'écrivent les hooks. La carte d'attention, l'icône de l'onglet, la pastille du workspace et « Rejoindre le terminal » fonctionnent : le terminal visé prend le focus et la carte disparaît.
  - **Test instable** : six exécutions consécutives sont vertes, sans reproduction.

### 52. Le zoom d'un pane s'annonce dans la barre de statut

- **Quoi** : à l'agrandissement d'un pane, la barre de statut indique « Pane agrandi, les autres tournent toujours : Ctrl + Maj + M ou « Réduire » dans son en-tête pour les revoir. » ; à la réduction, « Tous les panes de l'onglet sont de nouveau affichés. ». Quand le zoom prend fin de lui-même (split, autre pane, autre onglet), `endPaneZoom` remplace l'annonce si elle est encore affichée, pour qu'elle ne reste pas périmée.
- **Pourquoi** : en relisant la fonction de l'itération 49, un pane agrandi ne se signalait que par une petite icône dans son en-tête. On pouvait croire les autres panes fermés.
- **Vérifié** : dans l'instance de test :
  - le zoom affiche l'annonce, puis la réduction le message de retour ;
  - un split pendant le zoom fait disparaître l'annonce devenue fausse ;
  - un onglet d'un seul pane garde « rien à agrandir ».
- **Aussi exploré** : erreurs JavaScript. J'ai posé une écoute de `console.error`, `console.warn`, des exceptions et des promesses rejetées, puis parcouru l'application au clavier : palette, paramètres, explorateur, vue Git, nouvel onglet, split, zoom, fermetures, Ctrl + Tab, panneau masqué puis réaffiché, onglet rouvert. Aucune erreur n'a été relevée.

### 53. Le titre de la fenêtre suit le workspace et l'onglet actifs

- **Quoi** : le titre de la fenêtre, donc de la barre des tâches et d'Alt + Tab, devient « workspace › onglet - Dock », ou « Dock » sans workspace, comme Windows Terminal qui reprend l'onglet actif. `AppShell` envoie le contexte à chaque changement par une nouvelle commande `window.title`. Dans l'hôte, `WindowTitle.For` retire les caractères de contrôle, raccourcit au-delà de 160 caractères et ajoute « - Dock », puis `MainWindow` l'applique. L'ajout est noté « Convention proposée » en section 4 de la spécification.
- **Pourquoi** : le titre restait « Dock » quel que soit le travail en cours. Dans Alt + Tab ou la barre des tâches, rien n'indiquait le projet ouvert.
- **Vérifié** :
  - quatre tests xUnit (contexte vide, contexte normal, caractères de contrôle, contexte trop long) ; 194 tests au vert ;
  - dans l'instance de test, titre lu par le système : « LZGChallenge › LZGChallenge2 - Dock », puis « … › LZGChallenge - Dock » après Ctrl + Tab, et « GameSolver › GameSolver - Dock » après un changement de workspace.

### 54. Tests des gardes contre l'injection d'arguments Git

- **Quoi** : `GitNamesTests` couvre `GitNames.RequireRevision` et `GitNames.RequireRelativePath` en 20 cas.
  - Révisions acceptées : `main`, `origin/feature/git`, `HEAD~2`, un SHA.
  - Révisions refusées : vide, blanche, `--upload-pack=calc`, `-n`, avec espace ou saut de ligne.
  - Chemins acceptés : `src/app.ts`, un chemin avec espaces, `notes..bak.txt`, où « .. » ne forme pas un segment.
  - Chemins refusés : vide, `C:\Windows\win.ini`, `/etc/passwd`, `../secret.txt`, `src\..\..\secret.txt`.
- **Pourquoi** : ces deux fonctions sont la seule barrière entre les données reçues du web et la ligne de commande de `git`. Une révision qui commence par « - » serait lue comme une option, et un chemin avec « .. » sortirait du dépôt. Aucun test ne les visait directement.
- **Vérifié** : 214 tests au vert. Pendant ce temps, une troisième relecture indépendante des itérations 47 à 53 tourne en arrière-plan ; le bilan HTML a été mis à jour (itérations 51 à 53, nouveaux chiffres).

### 55. Troisième relecture indépendante : corrections du zoom et des derniers ajouts

Un sous-agent a relu les itérations 47 à 53 (`6e125b7..HEAD`) sans rien modifier. Il a trouvé 1 point important et 6 mineurs ; je les ai tous traités.

- **Important, fin de zoom** : à la réduction d'un pane agrandi, les panes cachés réapparaissent pendant que le focus est sur `body`. Un autre pane affichant un message (shell terminé, dossier disparu) prenait alors le focus et devenait actif : l'Entrée suivante relançait son shell. Au montage, `PaneOverlay` ne prend plus le focus depuis `body` que si son pane est actif. Vérifié : A terminé, B actif, zoom puis réduction ; B reste actif, le focus est dans son terminal. Avant la correction, A devenait actif avec le focus sur « Relancer ».
- **Alt + flèche pendant un zoom** : ne faisait rien. Le zoom est maintenant réduit et le pane voisin rejoint à l'image suivante. Vérifié : Alt + ← réduit et passe au pane de gauche.
- **Double-clic sur « Réduire »** : le second clic tombait sur « Split haut / bas », revenu au même endroit, et créait un split. Les boutons de split et de fermeture de l'en-tête ignorent désormais les clics répétés d'un double-clic. Vérifié : deux panes après le double-clic, pas trois.
- **Indications du Leader** : au-delà de deux lignes, le centrage coupait la première ligne. Elles sont maintenant alignées en haut. Vérifié à 850 px : première ligne entière, 13 indications sur 17.
- **Accessibilité** : le nom d'un workspace, que → et ← déplient ou replient, expose `aria-expanded`.
- **Titre de fenêtre** : `WindowTitle.For` ne coupe plus une paire de substitution (émoji) en deux ; nouveau test, et le test du contexte blanc est renommé.
- **Détails** :
  - `AppShell` repasse sous les 400 lignes, avec les hooks `useWindowTitle` et `useEndZoomWhenPaneChanges` ;
  - quand on quitte l'onglet d'un pane agrandi, l'annonce du zoom est effacée au lieu de dire que les panes sont réaffichés ;
  - l'indication du Leader dit « agrandir / réduire le pane » ;
  - la liste des touches du panneau n'est plus écrite deux fois.
- **Vérifié** : lint, builds et 215 tests au vert ; chaque correction a été rejouée dans l'instance de test, graphe Git fermé.

### 56. Rouvrir n'importe lequel des onglets fermés depuis la palette

Dock garde les cinq derniers onglets fermés, mais seul le dernier pouvait être rouvert (Ctrl + Maj + Z, état vide, palette). Pour retrouver l'avant-dernier, il fallait d'abord rouvrir le dernier puis le refermer.

- La palette propose maintenant une entrée « Rouvrir l’onglet fermé · nom » par onglet encore restaurable, de la plus récente à la plus ancienne, avec le workspace d'origine en indice. Taper « fermé » ou le nom de l'onglet suffit à les trouver.
- `restoreTab` accepte une position dans `session.closed` ; sans position, il rouvre toujours le dernier. Le texte, les dossiers et le séparateur de restauration passent par le même chemin qu'avant (`restoreClosedTabAt` dans `tabLifecycle`).
- Ces entrées n'ont pas d'étoile de favori : leur identifiant disparaît avec l'onglet.
- Vérifié dans l'instance de dev : trois onglets (`Temp`, `Windows`, `Users`) ouverts puis fermés, la palette les liste dans l'ordre inverse de fermeture. J'ai rouvert `Windows`, celui du milieu : il reprend sa place dans le workspace, avec un terminal dans `C:\Windows`, et la liste ne contient plus que `Users`, `Temp` et les plus anciens. Ctrl + Maj + Z a ensuite rouvert `Users`, le dernier fermé.
- Documenté comme **convention proposée** en section 6 de la spec.

### 57. Plus d'étoile sur « Rejoindre », et pas plus de 50 favoris

En relisant la palette pour l'itération 56, j'ai remarqué que les entrées « Rejoindre · … » (panes en attente d'un agent) affichaient une étoile de favori. Un clic ou Ctrl + Entrée ajoutait `attention-<pane>` aux favoris, mais l'étoile ne se remplissait jamais : seules les commandes sont relues comme favorites. Chaque essai laissait donc un identifiant invisible dans la session. Or l'hôte refuse d'enregistrer une session qui compte plus de 50 favoris : passé ce cap, chaque sauvegarde échouait avec « Favoris invalides. Les changements ne sont pas enregistrés. », sans moyen de comprendre pourquoi.

- Les entrées « Rejoindre » n'ont plus d'étoile, comme celles des onglets fermés.
- `toggleFavoriteCommand` refuse un 51ᵉ favori avec « Pas plus de 50 favoris : retirez une étoile avant d'en ajouter une. » dans la barre de statut. Le message s'efface à la bascule suivante.
- Vérifié dans l'instance de dev :
  - un agent simulé (`ping` dans le pane et fichier d'état `waiting`) : l'entrée « Rejoindre » n'a pas d'étoile, et Ctrl + Entrée ne fait rien ;
  - une session à 50 favoris : ajouter « Split côte à côte » est refusé avec le message, la session enregistrée garde 50 favoris ; retirer une étoile efface le message, puis l'ajout passe et la session est bien enregistrée.
- Les favoris de l'instance de dev ont été remis à zéro après l'essai.
- Les identifiants `attention-…` déjà enregistrés par ce bug restent dans les sessions existantes. Ils sont invisibles et comptent dans les 50 ; je ne les ai pas nettoyés pour ne pas toucher aux données sans nécessité.
- Documenté comme **convention proposée** en section 9 de la spec.

### 58. La barre d'onglets se pilote au clavier comme le panneau

Dans le panneau des workspaces, un onglet qui a le focus se renomme avec F2 et se déplace avec Alt + ↑ / ↓ (itération 47). Dans la barre d'onglets, seule la touche Menu faisait quelque chose. Pour atteindre le cinquième onglet, il fallait enchaîner les Tab en passant par chaque bouton de fermeture.

- Sur un onglet de la barre qui a le focus :
  - ← / → donnent le focus à l'onglet voisin sans l'afficher, et Entrée l'affiche ;
  - F2 ouvre le renommage, comme le double-clic ;
  - Alt + ← / → déplacent l'onglet d'un rang, et le focus le suit.
- Ailleurs, Alt + flèche change toujours de pane. Sur un onglet de la barre, la touche est interceptée avant les raccourcis du document, comme Alt + ↑ / ↓ dans le panneau.
- Vérifié dans l'instance de dev, focus sur le premier onglet :
  - → passe au deuxième sans changer l'onglet affiché, et ← revient ;
  - ← au bord ne fait rien, et Alt + ← au bord ne déplace rien ni ne change de pane ;
  - Alt + → échange les deux premiers onglets, focus conservé, et Alt + ← les remet dans l'ordre ;
  - F2 ouvre l'éditeur avec tout le nom sélectionné, puis Échap l'annule et rend le focus au terminal, comme après un double-clic.
- Documenté comme **convention proposée** en section 6 de la spec, et dans le README.

### 59. Les menus contextuels suivent les ajouts de la nuit

Le zoom d'un pane (itération 49) et le clavier des onglets (itérations 47 et 58) ne figuraient dans aucun menu contextuel. Ils restaient donc à découvrir par hasard ou dans le README.

- Le menu d'un terminal propose « Agrandir le pane », qui devient « Réduire le pane » quand le pane est agrandi, avec Ctrl + Maj + M en rappel. Comme dans la palette, l'entrée reste active avec un seul pane : la barre de statut explique alors qu'il n'y a rien à agrandir.
- Le menu d'un onglet de la barre rappelle Alt + ← et Alt + → sur « Déplacer à gauche / à droite ».
- Le menu du panneau rappelle F2 sur « Renommer », pour un onglet comme pour un workspace.
- Vérifié dans l'instance de dev :
  - onglet partagé en deux, clic droit dans le pane de gauche : « Agrandir le pane » l'agrandit, il devient actif et la barre de statut l'annonce ;
  - un nouveau clic droit propose « Réduire le pane », qui réaffiche les deux panes ;
  - les trois autres menus ouverts au clavier (Maj + F10) affichent les nouveaux rappels ;
  - le pane de test a ensuite été fermé.
- Spec (conventions du menu du terminal et du zoom) et architecture front mises à jour.

### 60. Glisser un fichier de l'arbre sur un terminal y insère son chemin

Depuis l'itération 13, un fichier déposé depuis l'Explorateur Windows sur un terminal y insère son chemin. L'arbre des fichiers de Dock, lui, obligeait à passer par « Copier le chemin » puis à coller, alors qu'il est affiché juste à côté des terminaux. C'est pourtant le geste le plus direct pour désigner un fichier à une commande ou à Claude Code.

- Les lignes de l'arbre (fichiers et dossiers) sont glissables, sauf pendant un renommage. Elles portent leur chemin sous un type propre à Dock et en texte, pour un dépôt dans une autre application.
- Déposée sur un terminal, une ligne envoie `terminal.dropPath {pane, shell, path}` à l'hôte. L'hôte protège le chemin selon le shell avec `DroppedPaths`, comme pour l'Explorateur, et le renvoie dans `terminal.dropped`. La protection reste donc côté hôte. Un chemin qui n'est pas absolu est refusé avec « Chemin déposé invalide. », sans marquer le terminal en échec.
- Le reste ne change pas : un pane couvert par un message refuse le dépôt, et ailleurs dans Dock le curseur indique « interdit ».
- Vérifié dans l'instance de dev avec un vrai glisser à la souris, onglet `web` du workspace Dock, shell PowerShell :
  - `package.json` déposé sur le terminal insère `'D:\Projects\Perso\Projet T\dock-terminal\web\package.json' `, entre guillemets à cause de l'espace, et le focus passe au terminal ;
  - le dossier `src` insère `'…\web\src' ` ;
  - `README.md` lâché sur un onglet n'insère rien et ne change pas d'onglet ;
  - un clic sur une ligne la sélectionne toujours.
- Documenté comme **convention proposée** en section 8 de la spec, avec le dépôt depuis l'Explorateur ; contrat du pont, architectures et README mis à jour.

## Reste à faire et idées

- **Taille de police et zoom du terminal** : police fixe à 14 px. La spécification classe ce point « À décider » (section 4), je n'y ai donc pas touché ; c'est à trancher.
- **Graphe Git dans une fenêtre très étroite** : depuis l'itération 39, Auteur et Date s'effacent, puis la colonne des branches rétrécit jusqu'à 88 px et celle du graphe jusqu'à la largeur de ses voies. En dessous d'environ 400 px pour la table (fenêtre de 900 px avec les trois panneaux ouverts), le message reste coupé : replier le panneau des références ou celui des workspaces reste nécessaire.
- **Message de pane pendant une saisie** : un message qui apparaît sur le pane où l'on tape prend le focus, et la frappe suivante peut le déclencher. Pour un dossier disparu, le bouton par défaut est donc « Ignorer », sans effet sur le shell, mais le signalement peut alors disparaître sans avoir été lu. Le cas est rare : il faut que le dossier du pane actif disparaisse pendant la saisie.
- **Glisser-déposer depuis l'Explorateur** : depuis l'itération 32, il est vérifié avec un vrai glisser OLE de fichier, le même mécanisme que l'Explorateur. Un essai à la main depuis l'Explorateur, avec une image dans Claude Code par exemple, reste conseillé.
- **Collage de plusieurs lignes** : coller un texte de plusieurs lignes dans un shell qui n'a pas activé le collage encadré (bracketed paste) exécute chaque ligne comme une commande. Windows Terminal demande une confirmation dans ce cas. Je ne l'ai pas ajouté : c'est un choix entre sécurité et friction qui te revient. Il faudrait le limiter aux programmes sans collage encadré, pour ne pas gêner les prompts collés dans Claude Code.
- **Tests web** : il n'y en a toujours aucun (décision du 21 septembre). Les fonctions pures ajoutées cette nuit (`fitGraphColumns`, `menuPlaceOf`, `selectAdjacentTab`…) s'y prêteraient bien si tu changes d'avis.
- **Effacer un terminal** : non ajouté au menu contextuel. Sous Windows 10, ConPTY ne permet pas de vider son propre tampon, et un effacement côté xterm.js pourrait réapparaître au premier redimensionnement.

