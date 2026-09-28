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

## Reste à faire et idées

- Taille de police et zoom du terminal : absents (police fixe à 14 px). La spécification les classe « À décider » (section 4), donc je n'y ai pas touché ; c'est à trancher.
