# Dock — Spécifications complètes

Version 2.0 · 18 septembre 2026 · Application Windows · Aucune priorité définie

Document de référence pour l’implémentation. Cette version remplace les spécifications initiales et consolide les décisions prises pendant le cadrage et le développement de l’application.

## 1. Statut des exigences

- **Retenu** : besoin explicitement demandé ou direction visuelle sélectionnée par l’utilisateur.
- **Convention proposée** : détail nécessaire à l’implémentation, proposé ici lorsque la conversation ne le tranche pas. Il ne constitue pas une validation supplémentaire de l’utilisateur.
- **À décider** : information manquante ou choix technique qui doit être établi avant l’implémentation concernée.

## 2. Objectif et périmètre

**Retenu.** Créer un véritable terminal Windows, organisé en workspaces libres, avec une interface légère permettant de naviguer rapidement entre plusieurs activités. L’utilisateur garde la liberté d’exécuter ses commandes, scripts et outils interactifs habituels.

Le panneau de gauche présente les workspaces et leurs onglets. Les états d’agents fournissent des indications d’attention dans cette organisation. L’application ne distribue pas de tâches aux agents.

### Hors périmètre actuel

- Couche « Projet » obligatoire au-dessus des workspaces.
- Maintien des agents ou serveurs en arrière-plan après fermeture de l’application ; reprise des processus après réouverture.
- Orchestration de tâches entre agents ou interface de conversation dédiée aux agents.
- Création ou fermeture automatique de workspaces liée au cycle de vie des worktrees : seules les actions explicites de l’utilisateur en ouvrent ou en ferment (voir la gestion des worktrees en section 11).
- Modèles de workspace et mode focus dédié.
- Recherche globale dans le contenu des fichiers du projet.
- Recherche dans la sortie des terminaux : ce besoin a été retiré du périmètre.
- Catalogue permanent de thèmes : la direction retenue est Dock vert avec panneau en arborescence.

## 3. Modèle fonctionnel

Hiérarchie : **Workspace → Onglet → Pane de terminal**.

| Entité | Définition | Invariants |
| --- | --- | --- |
| Workspace | Groupe nommé d’onglets, sans dossier unique imposé. | Chaque onglet appartient à un workspace. Un workspace par défaut est disponible. |
| Onglet | Activité contenant un ou plusieurs panes, avec sa disposition de splits. | Chaque pane appartient à un onglet. L’onglet conserve un pane actif. |
| Pane | Instance de terminal avec shell, dossier courant, sortie et état de processus. | Le dossier réel est suivi pendant la session. |
| Split | Division d’une zone en deux sous-zones, éventuellement subdivisées. | Orientation et proportions sont conservées. |
| Activité d’agent | Information rattachée à un pane lorsqu’une intégration le permet. | Elle ne devient pas une couche de navigation obligatoire. |

Exemple : « Perso » peut contenir un terminal dans Documents, un autre dans un dépôt Git et un troisième dans un dossier de scripts. Aucun projet commun n’est requis.

**Convention proposée.** Utiliser des identifiants stables indépendants des noms. Deux workspaces ou onglets peuvent porter le même nom sans collision. Renommer un élément ne modifie ni son dossier ni sa branche.

## 4. Direction visuelle et disposition

**Retenu.** Dock vert en thème sombre (décision du 21 septembre 2026, remplaçant l’interface claire initiale) : fonds gris anthracite, accent vert sauge désaturé, terminaux sombres, en-tête compact, panneau gauche en arborescence. Maximiser l’espace disponible pour les terminaux.

| Zone | Contenu attendu |
| --- | --- |
| En-tête compact | Identité de l’application, nom du workspace éditable inline, accès à la palette, bouton de visibilité du panneau. |
| Barre d’onglets | Onglets du workspace actif, bouton « + », actions compactes de split et actions du dossier. |
| Zone de travail | Panes et séparateurs, occupant la hauteur restante. |
| En-tête d’un pane | Shell, dossier courant et action de fermeture ; chemin tronqué si nécessaire et consultable intégralement. |
| Panneau gauche | Workspaces, chevrons de dépliage, onglets enfants, accès aux projets et indications d’attention. |

**Convention proposée.** Le titre de la fenêtre, visible dans la barre des tâches et Alt + Tab, reprend le workspace et l’onglet actifs : « workspace › onglet - Dock », ou « Dock » sans workspace.

### Règles de présentation

- Pas de bordure ou barre colorée pour signaler un onglet sélectionné, un workspace sélectionné ou un résultat de palette sélectionné. Utiliser un fond discret.
- Pas de contour permanent autour du champ de recherche de la palette. Le focus clavier reste perceptible, notamment par le fond.
- Conserver les lignes fines de l’arborescence : elles représentent la hiérarchie, pas une sélection.
- Éviter titres de section redondants, slogan, texte d’aide permanent, chemin global répété, barre d’état sans utilité immédiate et gros blocs de présentation.
- Préférer les infobulles et noms accessibles pour les boutons compacts.
- Réserver les fenêtres modales aux interactions qui en bénéficient réellement, notamment la palette. Les actions courantes sont directes, inline ou contextuelles.

### Repères visuels

Ces valeurs sont des références de réalisation, pas des contraintes de taille absolues : en-tête d’environ 42 px, fond de l’application #17191b, panneaux #1e2123, fond de terminal #121416, accent #7a9f8b, fond de sélection à peine plus clair que le panneau, interface en Segoe UI et terminal en police monospace. Préserver la lisibilité avec la mise à l’échelle Windows.

**À décider.** Personnalisation de la police terminal, taille du texte, zoom et comportement aux très petites dimensions. La cible principale reste une fenêtre d’application de bureau.

### Explorateur de fichiers

**Retenu.** Un explorateur de fichiers intégré s’affiche dans un panneau à droite de la zone de travail, du côté opposé à l’arborescence des workspaces. Il est fermé par défaut et s’ouvre ou se ferme par un bouton. Son état ouvert ou fermé est mémorisé par onglet. Il affiche le dossier courant du pane actif et le suit : un changement de dossier dans le shell ou un changement de pane met l’explorateur à jour. Un fichier s’ouvre dans l’éditeur configuré ; l’explorateur permet de créer, renommer et supprimer des fichiers et des dossiers.

**Retenu (25 septembre 2026).** Bouton à droite de la barre d’onglets, commande « Afficher / masquer les fichiers » dans la palette, Leader puis E ou Ctrl + Maj + E : ouvre avec le focus dans l’arbre, ou ferme et rend le focus au terminal. Arbre dépliable dont la racine est le dossier du pane actif, dossiers d’abord puis fichiers, triés sans casse ; tout est affiché, `.git` et fichiers cachés compris ; au-delà de 2 000 éléments dans un dossier, le reste est seulement annoncé. Les dossiers affichés sont surveillés en direct, un bouton « Actualiser » relit le tout. Un clic déplie ou replie un dossier ; double-clic ou Entrée ouvre un fichier dans l’éditeur, Entrée déplie ou replie aussi un dossier. Boutons « Nouveau fichier » et « Nouveau dossier » en tête du panneau, menu contextuel (clic droit ou Maj + F10 : ouvrir dans l’éditeur, ouvrir un terminal ici, nouveau fichier, nouveau dossier, renommer, supprimer, copier le chemin), noms saisis en place ; F2 renomme, Suppr place l’élément dans la corbeille de Windows après confirmation. Navigation ↑ / ↓ / ← / → / Début / Fin, Échap rend le focus au terminal. Panneau redimensionnable par glisser ou flèches ; état ouvert par onglet et largeur mémorisés dans la session. La recherche dans le contenu des fichiers reste hors périmètre.

**Convention proposée.** Dans l’arbre des fichiers, taper les premières lettres d’un nom sélectionne l’élément visible suivant qui commence ainsi, sans tenir compte de la casse ni des accents, comme dans l’Explorateur Windows ; répéter la même lettre passe d’un élément à l’autre, et la saisie repart de zéro après 0,7 seconde sans frappe.

**Convention proposée.** Le menu contextuel d’un fichier ou d’un dossier propose aussi « Afficher dans l’Explorateur Windows », qui ouvre son dossier parent avec l’élément sélectionné ; celui d’un dossier propose en plus « Ouvrir dans l’éditeur ». « Insérer le chemin dans le terminal » colle son chemin complet dans le terminal actif, comme un glisser de la ligne sur ce terminal ; si un message recouvre ce terminal, rien n’est inséré et la barre de statut le dit.

**Convention proposée.** Au renommage d’un fichier, seul son nom avant la dernière extension est sélectionné, comme dans l’Explorateur Windows : taper un nouveau nom garde l’extension. Un dossier ou un nom qui commence par un point est sélectionné en entier.

## 5. Workspaces et panneau en arborescence

| ID | Exigence retenue |
| --- | --- |
| WS-01 | Créer plusieurs workspaces et naviguer entre eux sans association obligatoire à un projet. |
| WS-02 | Disposer d’un workspace par défaut ; aucun terminal autonome hors workspace. |
| WS-03 | Renommer le workspace directement dans le titre de l’en-tête, sans popup. |
| WS-04 | Afficher les workspaces à gauche et leurs onglets sous forme d’arborescence. |
| WS-05 | Chaque chevron déplie ou replie les onglets de son workspace, indépendamment des autres. |
| WS-06 | Cliquer sur un onglet de l’arborescence active son workspace et cet onglet. |
| WS-07 | Mémoriser les états déplié/replié, la largeur et la visibilité du panneau. |
| WS-08 | Permettre de masquer entièrement le panneau et de le réafficher via le bouton toujours accessible dans l’en-tête ou la palette. |
| WS-09 | Redimensionner le panneau en faisant glisser son séparateur ; conserver sa largeur après masquage. |

### Notes du workspace

**Retenu (29 septembre 2026, issue #85).** Chaque workspace porte une note en texte brut, partagée par tous ses onglets, affichée et modifiée dans une troisième vue « Notes » du panneau de droite, à côté de « Fichiers » et « Git ». La note est enregistrée avec la session, sans bouton d’enregistrement. Dans le panneau des workspaces, une petite icône suit le nom d’un workspace dont la note n’est pas vide ; son infobulle montre la première ligne de la note.

**Convention proposée.** Leader puis O, Ctrl + Maj + O, la palette (« Afficher / masquer les notes du workspace ») ou l’onglet « Notes » du panneau ouvrent la vue avec le focus dans la note, ou la ferment ; Échap rend le focus au terminal. Un clic sur l’icône de note, ou « Notes du workspace » dans le menu contextuel du workspace, active ce workspace et ouvre la vue. Police mono, au plus 100 000 caractères. Fermer le dernier onglet d’un workspace garde sa note avec l’onglet fermé : le rouvrir recrée le workspace avec sa note (recette R35).

### Création et renommage

Le « + » du panneau crée directement un workspace et permet de modifier son nom inline. Le clic sur le titre du workspace actif démarre le renommage. Entrée ou perte de focus enregistre ; Échap annule. Un nom vide ne remplace pas le nom existant.

La commande « Renommer le workspace » dans la palette active le même éditeur inline. Le panneau reflète immédiatement le nouveau nom.

**Convention proposée.** Les workspaces se réordonnent par « Monter » et « Descendre » dans leur menu contextuel ou dans la palette (workspace actif), par Alt + ↑ / ↓ sur un workspace sélectionné au clavier dans le panneau, et par glisser-déposer de sa ligne, devant un autre workspace ou en fin de liste. Les onglets se déplacent de même d’un rang dans leur workspace (menu contextuel de l’onglet dans le panneau, Alt + ↑ / ↓ sur l’onglet sélectionné au clavier dans le panneau, Alt + ← / → sur un onglet de la barre qui a le focus), en plus du glisser-déposer et de Ctrl + Maj + PageUp / PageDown. L’ordre est conservé dans la session.

**Convention proposée.** « Fermer le workspace », dans son menu contextuel ou dans la palette pour le workspace actif (nommé dans l’entrée), ferme tous ses onglets avec une seule confirmation si des programmes tournent ; ses derniers onglets restent restaurables un par un.

**Convention proposée.** Au clavier, dans le panneau des workspaces, ↑ / ↓ / Début / Fin passent d’une ligne visible à l’autre, workspaces et onglets confondus ; → déplie un workspace replié, ← le replie ou, depuis un onglet, remonte à son workspace. Tab parcourt toujours chaque bouton. Taper les premières lettres d’un nom donne le focus à la ligne visible suivante qui commence ainsi, comme dans l’arbre des fichiers. Échap rend le focus au terminal.

**Décision prise.** Un workspace créé depuis le sélecteur de projets porte automatiquement le nom du dossier choisi. Un workspace créé sans projet reçoit un nom automatique descriptif ; un nom saisi manuellement reste prioritaire et n’est jamais écrasé. Le premier onglet PowerShell reprend le dossier du pane actif ou, au premier lancement, le dossier utilisateur. Un clic sur un workspace rejoint son dernier onglet et son dernier pane actifs. Replier une branche ne change pas la sélection.

**Décision prise.** Fermer le dernier onglet ou le dernier pane d’un workspace ferme ce workspace et arrête ses processus. S’il ne reste aucun workspace, afficher un état vide avec un message accueillant et une action pour en créer un. La suppression explicite d’un workspace demande une confirmation lorsqu’il contient des onglets ou des processus actifs ; la confirmation arrête alors tous ses processus et supprime le workspace.

## 6. Onglets

| ID | Exigence retenue |
| --- | --- |
| TAB-01 | Ouvrir plusieurs onglets dans un workspace. |
| TAB-02 | Clic gauche sur « + » : créer immédiatement un onglet PowerShell, sans formulaire. |
| TAB-03 | Clic droit sur le même « + » : menu contextuel PowerShell / CMD / Git Bash, placé près du bouton. Supprimer le bouton séparé de choix du shell. |
| TAB-04 | Le nouvel onglet reprend le dossier courant du pane actif au moment de l’action. |
| TAB-05 | Double-clic sur le nom d’un onglet : renommage inline. La palette offre aussi cette action. |
| TAB-06 | Entrée ou clic ailleurs valide le nom ; Échap annule ; le nom manuel prime sur les noms automatiques. |
| TAB-06a | Le nom automatique d’un onglet est le nom du dossier courant au moment de sa création ; il reste synchronisé tant qu’aucun nom manuel n’a été saisi. |
| TAB-07 | Réordonner les onglets et les déplacer entre workspaces, notamment par glisser-déposer. |
| TAB-08 | Fermer un onglet et pouvoir rouvrir un onglet fermé accidentellement. |
| TAB-09 | La barre d’onglets et l’arborescence reflètent la même sélection, le même ordre et les mêmes noms. |

### Menu des shells

Le menu accepte ↑ / ↓, Entrée et Échap. Un clic à l’extérieur le ferme. Maj + F10 ou la touche de menu contextuel sur le « + » offre un accès clavier. L’action reste accessible via la palette. Le menu ne remplace pas le clic gauche direct.

### Déplacement

Le déplacement conserve le shell, le dossier, l’historique, les processus actifs et la disposition de l’onglet. Il ne recrée pas les terminaux. Le glisser-déposer est possible depuis la barre supérieure ou les onglets de l’arborescence. Déposer sur un workspace transfère l’onglet ; déposer sur un onglet permet de choisir sa position.

**Convention proposée.** Insérer avant l’onglet cible ; déposer sur le workspace ajoute en fin de liste et active l’onglet déplacé. Proposer un équivalent clavier dans les commandes pour les déplacements essentiels.

### Cas de fermeture

**Décision prise.** La fermeture du dernier onglet supprime le workspace après arrêt de ses processus. Aucun onglet vide de remplacement n’est créé. Si aucun workspace ne subsiste, afficher l’état vide et son action de création.

**Décision prise.** Rouvrir restaure noms, shells, chemins, splits et texte avec de nouveaux processus et un séparateur de restauration. Ne pas réexécuter les anciennes commandes. Conserver les cinq derniers onglets fermés, y compris après redémarrage.

**Convention proposée.** Un clic droit sur un onglet de la barre ouvre son menu : Renommer, Dupliquer l’onglet (même disposition, mêmes dossiers et shells, terminaux neufs, aucune commande rejouée), Déplacer à gauche ou à droite, Fermer l’onglet et Fermer les autres onglets. Ce dernier ne demande qu’une seule confirmation si des programmes tournent ; les cinq derniers onglets fermés restent restaurables.

**Convention proposée.** Au clavier, dans la barre d’onglets, ← / → passent le focus à l’onglet voisin sans l’afficher, Entrée l’affiche, F2 le renomme et Alt + ← / → le déplacent d’un rang, comme ↑ / ↓, F2 et Alt + ↑ / ↓ dans le panneau des workspaces.

**Convention proposée.** La palette propose une entrée « Rouvrir l’onglet fermé · nom » par onglet encore restaurable, de la plus récente à la plus ancienne, avec le workspace d’origine en indice : on peut rouvrir n’importe lequel des cinq, pas seulement le dernier. L’onglet reprend sa position d’origine dans son workspace, recréé s’il n’existe plus (position exacte quand on rouvre dans l’ordre inverse des fermetures, la plus proche sinon) ; Ctrl + Maj + Z et l’état vide rouvrent toujours le dernier fermé.

## 7. Panes et splits

| ID | Exigence retenue |
| --- | --- |
| PANE-01 | Diviser un pane côte à côte ou haut/bas, rapidement depuis les commandes ou les boutons compacts. |
| PANE-02 | Le nouveau pane reprend le dossier du pane actif. |
| PANE-03 | Redimensionner les sous-zones avec leurs séparateurs. |
| PANE-04 | Naviguer entre panes au clavier et activer un pane en cliquant dedans. |
| PANE-05 | Conserver la disposition et ses proportions entre sessions. |

Les splits peuvent être imbriqués. L’action de fermeture d’un pane retire cette feuille de la disposition et agrandit la zone restante.

**Convention proposée.** Leader puis M, Ctrl + Maj + M, la palette, le menu contextuel du terminal ou un double-clic sur l’en-tête d’un pane l’agrandit temporairement à toute la zone de l’onglet, sans modifier la disposition enregistrée ; les autres terminaux continuent de tourner. Le même geste, le bouton « Réduire » de son en-tête, un split, le passage à un autre pane (Alt + flèche compris, qui réduit puis passe au pane voisin) ou à un autre onglet le réduisent. Un onglet d’un seul pane n’a rien à agrandir.

**Convention proposée.** Un double-clic sur un séparateur rétablit sa taille par défaut : parts égales pour un split, largeur initiale pour le panneau des workspaces, le panneau de droite et les colonnes du graphe Git.

**Conventions proposées.** Le split hérite aussi du shell du pane actif, commence à parts égales et active le nouveau pane. Le pane qui reçoit le focus, y compris par un bouton de son en-tête ou du message qui le recouvre, devient le pane actif : la saisie et les commandes visent toujours le même pane. La fermeture du dernier pane utilise les règles de fermeture d’un onglet. Préserver les saisies, processus et sélections lorsque le panneau latéral est masqué, lorsqu’un groupe est déplié ou lorsqu’une zone est redimensionnée.

**Décision prise.** La navigation au clavier est spatiale : chaque direction choisit le pane dont la position visuelle est la plus proche dans cette direction. En l’absence de cible dans la direction demandée, conserver le pane actif.

## 8. Terminal réel et shells

**Retenu.** L’application finale héberge de vrais terminaux interactifs. Windows PowerShell 5.1 est le shell par défaut afin de charger le profil existant ; CMD, Git Bash et PowerShell 7 sont disponibles en alternative configurée.

- Charger le profil PowerShell habituel avec ses alias, fonctions, modules et prompt.
- Préserver l’utilisation de wtr et rmwt depuis le profil.
- Permettre sélection de texte, copier/coller, défilement et exécution libre des programmes.
- Prendre en charge les interactions des outils utilisés : couleurs, curseur, touches de contrôle, programmes plein écran et redimensionnement.
- Suivre le dossier courant réel après les commandes de navigation, y compris après une fonction du profil qui change le dossier.
- Ne pas remplacer le shell par un interpréteur limité à quelques commandes reconnues par l’interface.

**Convention proposée.** Un clic droit dans un terminal, ou la touche Menu, ouvre un menu contextuel : Copier (si du texte est sélectionné), Coller, Tout sélectionner, Split côte à côte, Split haut / bas, Agrandir le pane (ou Réduire le pane s’il est agrandi) et Fermer le pane. Quand le programme du terminal suit la souris, le clic droit lui revient ; Maj + clic droit ouvre alors le menu.

**Convention proposée.** Déposer des fichiers ou des dossiers depuis l’Explorateur Windows sur un terminal y colle leurs chemins, comme dans Windows Terminal, protégés selon le shell (guillemets simples pour PowerShell et Git Bash, doubles pour CMD) et suivis d’une espace ; le pane devient actif. Glisser une ligne de l’arbre des fichiers de Dock sur un terminal y colle de même le chemin complet du fichier ou du dossier. Ailleurs dans Dock, le dépôt de fichiers ou de liens est refusé et n’ouvre jamais de fenêtre.

**Conventions proposées.** Préserver l’historique et le processus lors des changements de workspace. Si un shell est introuvable ou échoue au démarrage, afficher un état local au pane avec actions de relance ou de choix du shell. Ne pas basculer silencieusement vers un autre shell. Tant qu’un tel message recouvre un pane, il reçoit le focus à la place du terminal chaque fois que le pane devient actif : Entrée déclenche son action par défaut, « Relancer » pour un shell arrêté, « Ignorer » pour un dossier disparu car le shell y tourne encore, et Tab parcourt les autres actions. Quand le message prend le focus alors qu’on tapait dans ce terminal, Entrée et Espace sont ignorés pendant 0,6 seconde : une frappe déjà en cours ne déclenche pas une action que l’on n’a pas eu le temps de lire. Rejoindre volontairement un pane qui affiche déjà un message n’est pas concerné. Le message d’un dossier disparu reste affiché jusqu’à ce choix, y compris à la restauration, où le shell démarre dans le dossier personnel.

**Décision prise.** Le profil contenant `wtr` et `rmwt` est `%USERPROFILE%\\Documents\\WindowsPowerShell\\Microsoft.PowerShell_profile.ps1`. WezTerm utilise actuellement `powershell.exe -NoLogo`. PowerShell 7 est installé mais son profil utilisateur correspondant n’a pas été trouvé dans `Documents\\PowerShell`; il reste une alternative à configurer explicitement. Les chemins de CMD et Git Bash doivent rester configurables. Le dossier courant est obtenu par une intégration shell propre à Dock (variable d’environnement dédiée et séquence OSC émise par le prompt), décrite en section 15 ; aucune variable WezTerm n’est simulée.

**Retenu.** Un lien affiché dans un terminal s’ouvre par Ctrl + clic ; le clic simple reste réservé à la sélection de texte et au placement du curseur, sans jamais ouvrir de lien.

**Conventions proposées.** Reconnaître les URL présentes dans le texte ainsi que les hyperliens explicites émis par les programmes (séquence OSC 8). Ouvrir le lien dans le navigateur par défaut de Windows, jamais dans la fenêtre de Dock. N’ouvrir que les liens `http` et `https` ; un autre schéma est ignoré et signalé dans la barre de statut. Souligner le lien au survol pour montrer qu’il est actif.

**Retenu (29 septembre 2026).** Dans un pane où Dock détecte Claude Code ou Codex CLI, Maj + Entrée et Ctrl + Entrée insèrent un retour à la ligne dans le prompt au lieu de l’envoyer (Dock transmet Échap + Entrée, que les deux agents interprètent ainsi). Dans les autres panes, ces combinaisons restent transmises telles quelles au shell.

## 9. Palette et clavier

### Palette de commandes

**Retenu.** Ctrl + P ouvre la palette « Commandes & navigation ». Elle recherche les commandes et les éléments de navigation. ↑ / ↓ déplace la sélection ; Entrée exécute le résultat sélectionné ; Échap ferme la palette.

- Sélection visible uniquement par un fond discret, sans barre latérale colorée.
- Le résultat sélectionné reste visible lorsque la liste défile.
- Modifier la requête remet la sélection au premier résultat disponible.
- Une recherche sans résultat affiche un message neutre et Entrée ne déclenche aucune action.
- La sélection à la souris et au clavier conduit à la même action.
- La palette rejoint les workspaces, onglets et panes ; elle déclenche les éditeurs inline plutôt que des formulaires modaux supplémentaires.

**Convention proposée.** Conserver Ctrl + Maj + P comme alias. Prévoir retour du focus à l’élément d’origine à la fermeture ; lorsqu’une commande ouvre un terminal ou un éditeur inline, son nouveau champ reçoit le focus.

**Convention proposée.** Tant qu’une fenêtre est ouverte (palette, sélecteur de projets, paramètres, confirmation), Tab et Maj + Tab parcourent ses seuls éléments, en boucle : le focus ne rejoint jamais un terminal masqué derrière elle.

**Convention proposée.** L’étoile d’une commande (clic ou Ctrl + Entrée) la marque comme favorite et la place en tête de la palette. Seules les commandes en portent une : ni les entrées de navigation, ni « Rejoindre », ni « Rouvrir l’onglet fermé ». Au plus 50 favoris ; au-delà, la palette demande d’en retirer un.

### Touche Leader

Le principe d’une touche Leader est retenu. Le raccourci par défaut est **Ctrl + Espace** et son délai d’expiration est de **5 secondes**. Le mapping et le délai sont personnalisables.

**Retenu (21 septembre 2026).** Chaque commande Leader dispose aussi d’un raccourci direct, sans passer par le Leader. Les raccourcis directs utilisent Ctrl + Maj + lettre (jamais Ctrl + lettre seul, réservé au shell) et Alt + flèche pour la navigation (Ctrl + Maj + flèche est utilisé par PSReadLine). Ctrl + Maj + C et Ctrl + Maj + V restent copier et coller ; le split côte à côte direct utilise donc D.

| Séquence Leader | Raccourci direct | Action |
| --- | --- | --- |
| Ctrl + Espace, puis P | Ctrl + P ou Ctrl + Maj + P | Palette |
| Ctrl + Espace, puis T | Ctrl + Maj + T | Nouvel onglet PowerShell |
| Ctrl + Espace, puis V | Ctrl + Maj + D | Split côte à côte |
| Ctrl + Espace, puis H | Ctrl + Maj + H | Split haut/bas |
| Ctrl + Espace, puis F | À définir avec le sélecteur | Sélecteur de projets |
| Ctrl + Espace, puis W | Ctrl + Maj + W | Nouveau workspace |
| Ctrl + Espace, puis X | Ctrl + Maj + X | Fermer le pane actif |
| Ctrl + Espace, puis E | Ctrl + Maj + E | Explorateur de fichiers |
| Ctrl + Espace, puis G | Ctrl + Maj + G | Vue Git |
| Ctrl + Espace, puis flèche | Alt + flèche | Navigation entre panes |

Les séquences Leader sont consommées par l’application uniquement lorsqu’elles correspondent à une commande active. Une commande non reconnue ou expirée rend la saisie au pane actif ; les raccourcis personnalisés peuvent désactiver ou remplacer les valeurs par défaut.

**Convention proposée.** Leader puis N ouvre la création d’un worktree (section 11), comme le Leader + n de WezTerm ; aucun raccourci direct.

**Convention proposée.** Leader puis O, ou Ctrl + Maj + O, ouvre ou ferme la vue Notes du panneau de droite (section 5, « Notes du workspace »).

**Convention proposée.** Ctrl + Tab et Ctrl + Maj + Tab passent à l’onglet suivant ou précédent du workspace actif, en boucle, comme dans Windows Terminal. Ces combinaisons n’envoient au shell que Tab ou Maj + Tab, qui restent disponibles sans Ctrl.

**Convention proposée.** Leader puis B, ou Ctrl + Maj + B, masque ou affiche le panneau des workspaces (WS-08), en rendant le focus au terminal s’il était dans le panneau ; quand il l’affiche, le focus va à l’onglet actif dans le panneau. « Aller au panneau des workspaces », dans la palette, y amène le focus sans le masquer.

**Convention proposée.** Les raccourcis directs fonctionnent aussi quand le focus est hors d’un terminal (panneau des workspaces, graphe Git, explorateur), sauf dans un champ de saisie, un menu ou une boîte de dialogue ; il en va de même pour le Leader, dont la touche suivante n’est jamais interceptée par l’élément qui a le focus. Exception à confirmer : sur un onglet de la barre ou une ligne du panneau des workspaces qui a le focus, Alt + flèche déplace cet élément au lieu de changer de pane.

## 10. Sélecteur de projets

**Retenu.** Chercher rapidement un dossier dans `C:\\Files\\Projects`, puis ouvrir un workspace avec un premier terminal dans ce dossier. Le raccourci WezTerm Leader + F existant sert de référence fonctionnelle.

Ce sélecteur recherche des dossiers, pas du texte dans les fichiers. L’interface doit rester compacte et intégrée. La navigation clavier suit le principe ↑ / ↓ / Entrée / Échap.

**Décision prise.** Le chemin est `C:\\Files\\Projects`. Reprendre la profondeur de premier niveau de WezTerm, exclure `worktrees` de la liste des projets puis l’exposer séparément si nécessaire. Le sélecteur ne recherche que des dossiers et ne détecte pas les workspaces déjà ouverts : chaque sélection peut créer un nouveau workspace. Le nom initial est celui du dossier sélectionné.

**Convention proposée.** Les dossiers de premier niveau de `worktrees` sont listés à part, après les projets, avec la mention « worktree », comme le préfixe `[wt]` du sélecteur WezTerm ; en choisir un ouvre un workspace comme pour un projet. Quand un autre dossier des worktrees est réglé (section 11), c’est lui qui est listé, et il est retiré de la liste des projets s’il se trouve dans le dossier des projets.

## 11. Actions contextuelles et worktrees

Les actions utilisent le **dossier du pane actif**, jamais un hypothétique dossier unique du workspace.

| Action retenue | Comportement attendu |
| --- | --- |
| Copier le chemin | Copier le chemin courant réel dans le presse-papiers. |
| Ouvrir dans l’éditeur | Ouvrir le dossier dans l’éditeur configuré. |
| Ouvrir dans l’explorateur | Ouvrir le dossier dans l’explorateur Windows. |
| Copier la branche | Résoudre et copier la branche Git du contexte courant lorsque disponible. |
| Terminal supplémentaire | Ouvrir un terminal dans le même dossier, notamment dans le même worktree. |

**Décision prise.** Montrer le chemin ciblé dans le menu ou la zone d’actions. Afficher explicitement « Aucun dépôt Git », « Aucune branche » ou « HEAD détachée » selon le contexte. Désactiver ou masquer les actions Git hors dépôt ; ne jamais afficher une branche fictive.

**Convention proposée.** L’en-tête de chaque pane affiche la branche Git de son dossier, ou « HEAD détachée », à côté du chemin ; rien hors d’un dépôt. Elle est relue après chaque commande, pour suivre un `git switch` ou un `wtr`. Dans un pane de moins de 520 px de large, elle est masquée et reste lisible dans l’infobulle de « Copier la branche ». Dans la palette, l’entrée de chaque pane rappelle aussi sa branche, qu’on peut donc taper pour le retrouver.

### Gestion des worktrees

**Retenu (29 septembre 2026).** Dock liste, ouvre, crée et supprime les worktrees Git en natif, avec le comportement de `wtr` et `rmwt` : les étapes Git, les ports de développement aléatoires, `pnpm install` et la base de données répliquée (PostgreSQL sous Docker, SQL Server). La règle précédente, qui interdisait de réimplémenter ces fonctions, est levée. `wtr` et `rmwt` restent utilisables au terminal, avec les mêmes chemins ; l’interface suit aussi leurs effets (contrat ci-dessous). Décisions de l’utilisateur :

| Sujet | Décision |
| --- | --- |
| Points d’entrée | Vue Git, palette, Leader, panneau des workspaces, et une icône d’arbre à côté de « Ouvrir un projet » qui fait choisir le projet source. Sur chaque ligne d’onglet du panneau, un bouton d’arbre affiché au survol, comme la croix, crée un worktree du projet de l’onglet ; hors dépôt Git, il est grisé avec une infobulle. |
| Branche | Nouvelle branche depuis une base, ou branche existante, locale ou distante. |
| Réglages | Dossier des worktrees (par défaut `worktrees` du dossier des projets) et base par défaut (`develop`), modifiables dans Paramètres. |
| Ouverture | Nouveau workspace, ouvert dès que le dossier existe ; `pnpm install` tourne visiblement dans son terminal ; la base est répliquée en parallèle ; progression et avertissements dans la barre de statut. |
| Suppression | La confirmation liste les onglets et panes concernés, avec « Fermer ces onglets » (cochée), « Garder la branche » et « Supprimer la base répliquée ». |
| Cycle de vie | Jamais d’ouverture ni de fermeture automatique : seulement proposer. |

**Convention proposée.** Le formulaire affiche le dossier cible calculé par l’hôte (`<dossier des worktrees>\<projet>-<dernier segment de la branche>`) et refuse un dossier existant, un nom de branche invalide, une branche déjà utilisée par un autre worktree. Une nouvelle branche part de `origin/<base>` après un fetch, sans branche suivie (un push la publiera sous son propre nom) ; une branche distante crée une branche locale qui la suit. Les ports reprennent les fichiers et l’expression de `wtr` ; un port sans remplaçant libre est signalé. Un échec Git arrête la création ; les ports, `pnpm install` et la base ne produisent que des avertissements. « Ouvrir » un worktree rejoint un pane qui s’y trouve déjà, sinon ouvre un workspace. La suppression refuse le dépôt principal, vérifie d’abord qu’aucun programme ne verrouille le dossier (le worktree reste alors intact, avec « Réessayer » et, quand Windows les connaît, les processus en cause), puis supprime le dossier, fait un prune, supprime la base répliquée (jamais une base identique à celle du dépôt principal) et la branche, sauf si elle est gardée ou si HEAD est détachée.

**Contrat de synchronisation.** Après `wtr`, le pane qui exécute la commande devient la source de vérité pour le dossier courant et le contexte Git ; l’interface relit ces valeurs et met à jour le workspace ou l’onglet déjà associé sans créer de doublon automatiquement. Après `rmwt`, elle relit le dossier et Git, marque comme indisponibles les panes dont le chemin n’existe plus et propose de les fermer ou de choisir un dossier de repli. L’interface ne rejoue pas les effets d’un `wtr`/`rmwt` tapé au terminal et ne supprime pas un workspace sans action explicite de l’utilisateur.

### Gestion visuelle de Git

**Retenu.** Une vue Git permet de piloter le dépôt du pane actif sans taper de commande. Elle s’affiche dans le panneau de droite, qui bascule entre Fichiers et Git : même bouton d’ouverture, même état par onglet et même largeur que l’explorateur, les terminaux restant visibles. Elle présente la liste des branches locales et distantes avec leur avance et leur retard, et le graphe de l’historique avec les étiquettes de branches et de tags ; un clic sur un commit montre ses fichiers et son diff. Stage et unstage se font fichier par fichier, avec un diff coloré en lecture seule dans Dock, un message de commit, « Amend du dernier commit » et « Commit et push ». Fonctions incluses : commit, push, pull, fetch, merge, création, checkout, renommage et suppression de branches, stash, tags, rebase non interactif, cherry-pick et reset vers un commit. Un bouton « Annuler » défait la dernière opération faite depuis Dock tant qu’aucun push ne l’a publiée. En cas de conflit, la vue liste les fichiers concernés, les ouvre dans l’éditeur configuré, permet de les marquer résolus puis de terminer ou d’annuler l’opération. Le push forcé n’est proposé qu’après un push refusé, toujours avec `--force-with-lease` et après confirmation.

**Retenu (25 septembre 2026).** Le panneau de droite porte deux onglets, « Fichiers » et « Git » ; le bouton de la barre d’onglets l’ouvre ou le ferme sur sa dernière vue, mémorisée par onglet avec son ouverture. Commande « Afficher / masquer Git » dans la palette, Leader puis G ou Ctrl + Maj + G : ouvre la vue Git, y bascule depuis les fichiers, ou ferme et rend le focus au terminal. La vue suit le dépôt ou le worktree du dossier du pane actif et affiche « Aucun dépôt Git » hors dépôt ; elle se met à jour en direct, y compris après une commande tapée au terminal ou lancée par un agent. Ouvrir la vue Git affiche, à la place des terminaux de l’onglet et sous la barre d’onglets, un graphe qui regroupe l’historique et les branches, avec le focus. Colonnes « Branche / Tag », « Graphe », « Message », « Auteur » et « Date » : leur largeur se règle à la souris ou au clavier, « Auteur » et « Date » se masquent par un clic droit sur l’en-tête, et ces réglages sont mémorisés dans la session. Les étiquettes des branches et des tags sont reliées à leur commit ; une branche locale et sa branche distante sont réunies dans une seule étiquette quand elles pointent sur le même commit, la branche courante porte une coche. Les nœuds portent les initiales de l’auteur (aucune photo, aucun accès réseau), les merges une simple pastille. Une ligne « // WIP » en tête, reliée en pointillés au commit HEAD, annonce les fichiers ajoutés, modifiés, supprimés ou en conflit et l’opération en cours ; chaque stash apparaît en nœud pointillé rattaché à son commit de départ. Le graphe montre toutes les branches par défaut, une bascule « Courante » le limite à la branche courante et à son amont, et les commits se chargent par pages de 200 au défilement. À gauche du graphe, une colonne repliable et redimensionnable liste les branches locales avec leur avance et leur retard, les branches distantes par dépôt distant, les tags et les stash : un clic amène au commit dans le graphe, un double-clic fait le checkout de la branche. Ctrl + clic, Maj + clic, Maj + flèches et Ctrl + A y sélectionnent plusieurs branches, branches distantes, tags ou stash, même de sections différentes : le clic droit propose alors de les supprimer après une seule confirmation, qui signale les branches sans merge et les branches distantes, ou de copier leurs noms, et Suppr supprime la sélection ; « Annuler » restaure ensuite les branches, tags et stash locaux. Glisser une branche sur la branche courante, ou la branche courante sur une autre branche, propose un merge ou un rebase. Menus contextuels : commit (checkout, créer une branche ou un tag, cherry-pick, merge, rebase, reset soft, mixed ou hard), branche (checkout, merge, rebase, aller au commit, renommer, supprimer, supprimer la branche distante), tag (push, supprimer), stash (appliquer, appliquer et supprimer, supprimer), ligne WIP (stage de tout, stash, créer une branche) et fichiers modifiés du panneau Git, sélectionnables à plusieurs par Ctrl + clic, Maj + clic ou Ctrl + A (ouvrir dans l’éditeur, stage, unstage, marquer résolu, stash des seuls fichiers choisis, abandonner, ajouter au `.gitignore`, copier les chemins ; Espace et Suppr agissent sur toute la sélection). Échap ou la croix ferme le graphe et rend les terminaux sans fermer le panneau ; le bouton « Graphe » du panneau le rouvre. Le panneau Git montre le détail de la ligne choisie, sans onglets internes : pour la ligne WIP, les conflits, les fichiers Staged et Unstaged avec leurs actions, le message de commit, « Amend du dernier commit », « Commit » et « Commit et push » ; pour un commit, son message, son auteur, sa date et ses fichiers ; pour un stash, les mêmes informations et ses actions. En tête du panneau : dépôt, branche (un clic la copie) ou HEAD détachée, avance et retard sur la branche distante suivie, bouton « Graphe », puis Fetch, Pull, Push (qui publie une branche sans amont), Annuler et Actualiser. Le diff d’un fichier s’affiche dans un volet large par-dessus le graphe ou les terminaux : il suit le fichier choisi, se recharge quand le fichier change et se ferme par Échap ou la croix. Les branches distantes se suppriment depuis la vue après confirmation ; le reset hard, la suppression d’une branche sans merge, l’abandon de modifications, la suppression d’un stash, l’abandon d’une opération en cours et le push forcé demandent aussi une confirmation. « Annuler » couvre commit, amend, merge, pull, rebase, cherry-pick, reset (les modifications locales effacées par un reset hard sont restaurées), checkout, création, suppression et renommage de branche, tags, stash, suppression d’un stash et abandon de modifications ; il est refusé, raison en infobulle, si le dépôt a changé depuis ou si un push a publié les commits, ne couvre ni ce qui a été publié ni l’application d’un stash, et n’est pas conservé à la fermeture de Dock. Un conflit ramène le panneau sur la ligne WIP avec une bannière « Terminer » / « Abandonner » ; « Marquer résolu » demande confirmation si le fichier contient encore des marqueurs de conflit. Après un push refusé, une bannière propose « Pull » ou « Forcer le push… » avec `--force-with-lease`, refusé si la branche distante a changé depuis le dernier fetch. L’hôte exécute le `git` installé avec des arguments séparés, sans composer de ligne de commande, et respecte la configuration, les hooks et le gestionnaire d’identifiants de l’utilisateur ; la sortie d’une commande refusée, hooks compris, s’affiche dans la vue. La gestion des worktrees reste celle de `wtr`/`rmwt` ; les pull requests, le rebase interactif, le stage d’une partie de fichier et l’éditeur de conflits intégré sont hors périmètre.

**Retenu (25 septembre 2026).** Les libellés et les messages de la vue Git gardent les termes Git anglais, invariables : Push, Pull, Fetch, Stash, Stage et Unstage (fichiers Staged et Unstaged), Merge, Rebase, Amend, Checkout et Cherry-pick. Les phrases restent en français et emploient ces termes comme des noms, par exemple « Push vers origin/main terminé. », « 3 commits à push » ou « Faites un pull pour les intégrer ».

**Convention proposée.** Quand la place manque dans le graphe, Date puis Auteur sont masqués à l’affichage pour laisser au moins 200 px au message, sans changer le réglage mémorisé ; ils réapparaissent dès que la place revient.

## 12. Attention, agents et notifications

**Retenu.** Les workspaces restent les éléments principaux. Les activités de **Claude Code** (`claude`) et du **Codex CLI** (`codex`) s’y rattachent pour aider à trouver où une intervention est nécessaire.

| État | Signification |
| --- | --- |
| En cours | Une intégration indique une activité en cours. |
| En attente | Une réponse utilisateur ou une autorisation est nécessaire. |
| Terminé | L’activité suivie est terminée. |
| En erreur | L’activité a rencontré une erreur identifiée. |
| Inconnu | L’état ne peut pas être déterminé de manière fiable. |

- Résumer les besoins d’attention au niveau du workspace et les rendre repérables sur l’onglet concerné.
- Cliquer sur une activité ou une indication d’attention rejoint le workspace, l’onglet et le pane correspondants.
- Fournir des notifications discrètes et ciblées, notamment lors d’une demande d’intervention.
- Ne pas interrompre la saisie ni changer automatiquement de workspace à l’arrivée d’un état.
- Ne pas considérer un processus existant comme une preuve de travail effectif ou d’attente utilisateur.
- À la restauration d’une session, ne pas restaurer comme vivants les états des anciens processus.

**Conventions proposées.** Agréger le nombre d’attentes par workspace, éviter les notifications répétées pour le même événement et offrir l’accès au pane sans prise de focus forcée. Si plusieurs panes attendent, permettre de choisir la destination.

**Convention proposée.** Dans la palette, chaque entrée « Rejoindre » indique depuis combien de temps l’agent attend (« depuis 3 min »), mesuré depuis que Dock a reçu cet état, et les entrées vont de l’attente la plus ancienne à la plus récente. Les cartes d’attention affichent la même durée, mise à jour toutes les 30 secondes, et suivent le même ordre.

**Convention proposée.** Leader puis A, Ctrl + Maj + A ou « Rejoindre l’agent en attente suivant » dans la palette rejoint le pane en attente qui suit le pane actif dans l’ordre de la palette, de l’attente la plus ancienne à la plus récente (ordre du panneau à égalité), tous workspaces confondus et en boucle ; répété, il passe d’une attente à l’autre. Sans agent en attente, la barre de statut l’indique.

**Décision de périmètre.** L’architecture doit permettre des adaptateurs Claude Code et Codex CLI, mais leur détection fiable et leurs notifications sont reportées à une évolution dédiée. Tant qu’un adaptateur ne peut pas établir un état, afficher « État inconnu » plutôt que d’inférer une activité depuis le seul processus.

## 13. Sauvegarde, fermeture et restauration

### Données à retrouver

- Workspaces et onglets, noms et ordre.
- Note de chaque workspace.
- Dispositions de splits, orientations, proportions et panes actifs.
- Dossiers courants et shells utilisés.
- Texte des anciennes sessions avec distinction visuelle à la réouverture.
- Workspace et onglet actifs, largeur du panneau, panneau visible/replié et groupes dépliés/repliés.

**Retenu.** Sauvegarder automatiquement la disposition pendant l’utilisation, sans attendre une fermeture normale. À la réouverture, restaurer l’environnement visuel et créer des shells neufs. Ne pas réexécuter les anciennes commandes et ne pas conserver volontairement les agents ou serveurs en arrière-plan après fermeture.

Le texte restauré est accompagné d’un séparateur explicite, par exemple « Session restaurée — nouvelle session ». Un ancien affichage de serveur ou d’agent ne doit pas être présenté comme une activité encore en cours.

### Robustesse proposée

- Écritures atomiques et format versionné pour éviter une sauvegarde partiellement écrite.
- Sauvegardes différées et regroupées pour les redimensionnements, avec enregistrement final en fin d’interaction.
- Sauvegarde périodique du texte indépendante de la sauvegarde de disposition.
- En cas d’échec d’écriture, garder la session utilisable et signaler que les changements ne sont pas enregistrés.
- Si les données sont corrompues, conserver le fichier fautif pour récupération et ouvrir une session de secours plutôt que l’écraser silencieusement.
- Si un dossier a disparu, conserver le pane et demander ou proposer un dossier de repli avec indication locale.

**Décisions prises.** La valeur par défaut est de 10 000 lignes conservées par pane et de 256 Mio pour l’historique global ; ces deux limites sont configurables. Sauvegarder le texte toutes les 30 secondes, fréquence configurable. Conserver cinq onglets fermés restaurables et leur historique après redémarrage. À la fermeture d’un pane, d’un onglet, d’un workspace ou de l’application, utiliser l’arrêt forcé ; demander une confirmation si un serveur, un agent ou un programme est encore actif, puis arrêter tous les processus concernés.

## 14. Configuration exportable

**Retenu.** La configuration doit être sauvegardable, exportable et versionnable au format **JSON**. L’import doit permettre de retrouver les préférences sauvegardées.

**Convention proposée.** Séparer trois ensembles :

| Ensemble | Contenu |
| --- | --- |
| Préférences versionnables | Shells, chemins exécutables, paramètres, raccourcis, police, éditeur, dossier Projets et préférences visuelles. |
| État de session local | Workspaces, onglets, chemins courants, sélection et disposition. |
| Historique local | Texte terminal et éventuelles données nécessaires à la réouverture d’un onglet. |

L’export de préférences ne doit pas embarquer implicitement la sortie des terminaux. Un export volontaire de disposition peut être proposé séparément ; il contient alors des chemins locaux. L’import valide l’ensemble avant mutation et **remplace** la configuration courante ; il ne fusionne pas silencieusement les valeurs.

**Convention de fichiers.** Les préférences exportables, l’état de session et l’historique restent séparés, chacun avec une version de schéma. L’emplacement exact peut être choisi par l’implémentation dans les répertoires de données Windows appropriés ; il doit être documenté et stable. Une confirmation est requise avant un import qui remplace une configuration existante.

**Convention proposée.** L’en-tête de l’écran Paramètres affiche la version de Dock (« Dock 1.0.0 »), pour la comparer aux versions publiées ou la citer dans un signalement.

**Retenu (29 septembre 2026).** Le dossier des worktrees et la base par défaut font partie des préférences exportées, sous une clé `worktrees` facultative : un fichier de version 1 qui ne la contient pas reste importable, avec les valeurs par défaut.

**Convention proposée.** À l’import, une valeur de persistance hors bornes (sauvegarde du texte, lignes par pane, historique global) est ramenée dans sa plage, et chaque correction est signalée dans l’écran Paramètres, sous l’avis d’import, avant tout enregistrement.

## 15. Architecture fonctionnelle et choix techniques

Cette section décrit une séparation des responsabilités puis la pile technique retenue.

- **Interface** : navigation, édition inline, palette, arborescence et affichage des états.
- **Modèle de session** : identifiants stables, ownership des onglets/panes, arbre de splits et sélection.
- **Moteur de terminal** : rendu, entrées clavier, sélection et historique.
- **Gestionnaire de processus** : lancement des shells, communication avec les terminaux, redimensionnement et fermeture des processus associés.
- **Intégration shell** : obtention fiable du dossier courant et chargement de la configuration habituelle.
- **Services locaux** : dossiers de projets, presse-papiers, éditeur, explorateur et contexte Git.
- **Adaptateurs d’agents** : conversion des événements disponibles vers les états normalisés du produit.
- **Persistance** : préférences, état de session, historique, migrations et récupération.

Les déplacements et changements de présentation agissent sur le modèle et la visibilité, pas sur le cycle de vie des processus. Les identifiants servent à retrouver les éléments ; les noms servent à les présenter.

### Pile technique retenue (validée par le spike T01)

- **Hôte Windows :** application C# sur .NET 10 LTS avec une seule fenêtre WinUI 3 (Windows App SDK). L’hôte ne porte aucune interface métier : il gère la fenêtre, le gestionnaire de processus, les services locaux, les adaptateurs d’agents et la persistance. WinUI 3 non empaqueté est confirmé par le spike T01 (Windows App SDK 2.5.1, publication autonome depuis la ligne de commande) ; le repli WPF n’est plus nécessaire.
- **Interface :** une WebView2 unique héberge toute l’interface (arborescence, onglets, splits, palette, Leader) et un terminal xterm.js par pane, avec le renderer WebGL et un repli canvas. Les raccourcis sont interceptés dans xterm.js, jamais par des accélérateurs XAML, afin qu’un seul moteur traite le clavier et le focus.
- **Pseudo-terminal :** ConPTY, isolé derrière le gestionnaire de processus en C# avec P/Invoke. Chaque pane est rattaché à un Job Object Windows pour garantir l’arrêt de l’arbre de processus. Le spike T01 a comparé la ConPTY intégrée à Windows et une `conpty.dll` embarquée issue d’OpenConsole.
- **Dossier courant :** ConPTY ne le fournit pas. Dock l’obtient par intégration shell propre (variable d’environnement dédiée et wrapper de prompt non intrusif émettant une séquence OSC), compatible avec Windows PowerShell 5.1 et oh-my-posh, sans imiter WezTerm.
- **Pont hôte / interface :** messages JSON pour les commandes et un canal dédié pour les octets PTY. Mesurer d’abord `PostWebMessage` ; basculer sur un WebSocket local ou un flux binaire si le débit soutenu décroche.
- **Distribution :** build Windows autonome distribuée manuellement dans une release GitHub. Recommandation initiale : installeur Inno Setup pour l’application dépaquetée, sans mise à jour automatique ; l’installeur remplace la version précédente, détecte ou installe le runtime WebView2 Evergreen et embarque le runtime Windows App SDK (build autonome). Une distribution MSIX signée pourra être ajoutée si les contraintes de signature et de sideloading deviennent acceptables.

**Retenu.** Cette pile a été validée le 21 septembre 2026 par le spike T01 : PowerShell 5.1 réel avec le profil et oh-my-posh, ConPTY Windows et OpenConsole comparées, redimensionnement et applications plein écran, Unicode, IME, sélection et clavier français, dossier courant par `DOCK_PANE_ID` et séquence OSC 7, Job Object sans processus survivant, pont hôte / interface mesuré et installeur autonome testé. Les résultats détaillés restent consultables dans l’historique Git : [compte rendu du spike T01](https://github.com/MaximeRazafinjato/dock-terminal/blob/b1c648454c311b51a731118aea84b98d10bea1ac/spike/README.md). Les alternatives écartées sont l’interface hybride XAML + WebView2 par pane (clavier et focus partagés entre deux moteurs), le contrôle de Windows Terminal (aucun paquet officiel WinUI 3), Electron (empreinte) et Tauri 2 (introduit Rust dans une équipe .NET).

### Structure conceptuelle des données

| Objet | Champs conceptuels |
| --- | --- |
| Session | Version, liste ordonnée des workspaces, workspace actif, état du panneau. |
| Workspace | Identifiant, nom, onglets ordonnés, onglet actif, état déplié. |
| Onglet | Identifiant, nom affiché, nom manuel ou automatique, arbre de splits, pane actif. |
| Nœud split | Orientation, proportion, deux enfants ; ou référence à un pane pour une feuille. |
| Pane | Identifiant, profil de shell, dossier courant, référence à l’historique, état de session. |
| Activité | Identifiant, pane concerné, état, source, date de changement ; données de session vivante. |

Mesures de référence du spike : ConPTY livre 7 à 12 Mo/s en flux soutenu ; `PostWebMessage` transmet 20 Mo sur deux panes simultanés sans perte, avec un rendu xterm.js cumulé de 18 Mc/s, et reste le canal retenu. Le dossier courant est reçu environ 100 ms après le prompt. Versions minimales : Windows 10 1809 (build 17763) pour ConPTY, Windows App SDK et WebView2 Evergreen ; seule la configuration Windows 11 build 26200 a été testée. Critères de performance à respecter par l’application : aucune perte ni doublon de frappe, débit de rendu au moins égal au débit ConPTY, dossier courant reçu en moins d’une seconde, aucun processus survivant après fermeture d’un pane.

## 16. Qualité et accessibilité

Ces exigences sont des recommandations d’implémentation pour préserver les interactions retenues.

- Ne pas perdre une saisie lorsqu’on replie le panneau, déplie un workspace, renomme un autre élément ou redimensionne une zone.
- Maintenir une distinction entre sélection de texte dans un terminal, raccourcis de l’application et saisie dans les champs inline.
- Supporter la composition de texte ; Entrée pendant une composition ne doit pas valider prématurément un renommage.
- Exposer noms accessibles, états dépliés, sélection et relation entre menus et boutons.
- Conserver un focus visible et un ordre clavier cohérent ; éviter les pièges de focus.
- Limiter les rendus et écritures pendant les flux de sortie importants. Définir des budgets de performance avec une charge représentative avant validation technique.
- Afficher les noms, chemins et sorties comme des données ; ne pas les interpréter comme du code HTML.
- Valider les données importées avant de modifier la session courante.
- Traiter les chemins comme des arguments structurés lors des appels aux outils locaux, y compris avec espaces et caractères spéciaux.

**Convention proposée.** Un nom, un chemin ou un message coupé par manque de place (points de suspension) montre son texte complet en infobulle au survol, y compris dans la barre de statut.

## 17. Matrice de recette

Ces scénarios définissent les vérifications à effectuer sur l’application finale.

| ID | Scénario | Résultat attendu |
| --- | --- | --- |
| R01 | Créer Perso avec trois dossiers différents. | Aucun dossier ou projet commun imposé. |
| R02 | Créer puis renommer un workspace inline ; tester Entrée, clic extérieur, Échap et nom vide. | Nom synchronisé, annulation correcte, aucun formulaire modal. |
| R02a | Créer un workspace depuis un dossier puis ouvrir un onglet et changer de dossier. | Workspace nommé d’après le dossier choisi ; onglet nommé d’après son dossier initial ; les noms manuels restent prioritaires. |
| R03 | Clic gauche sur « + ». | PowerShell s’ouvre immédiatement dans le dossier actif. |
| R04 | Clic droit ou Maj + F10 sur « + », puis choix CMD/Git Bash au clavier. | Menu contextuel accessible et shell choisi réellement lancé. |
| R05 | Double-cliquer sur un onglet, saisir un nom, puis changer son contexte. | Nom manuel conservé dans la barre et le panneau. |
| R06 | Réordonner/transférer un onglet avec un processus et plusieurs panes. | Ordre mis à jour sans relancer les processus ni perdre la disposition. |
| R07 | Déplier/replier plusieurs workspaces et cliquer sur un onglet enfant. | Groupes indépendants et navigation vers le bon onglet. |
| R08 | Replier le panneau avec une commande terminal non soumise, puis le rouvrir. | Pleine largeur disponible ; saisie et largeur précédente conservées. |
| R09 | Créer des splits imbriqués, les redimensionner et changer de pane au clavier. | Dossiers hérités, navigation spatiale et disposition cohérente. |
| R10 | Ctrl + P, filtrage, flèches, Entrée, recherche sans résultat et Échap. | Navigation correcte, sélection visible sans bordure, aucun déclenchement accidentel. |
| R12 | Charger le vrai profil PowerShell et exécuter ses alias/fonctions. | Comportement conforme à la session PowerShell habituelle. |
| R13 | Changer de dossier avec cd puis une fonction ; ouvrir onglet et split. | Dossier courant réel hérité. |
| R14 | Ouvrir un outil interactif plein écran, redimensionner et utiliser les touches de contrôle. | Rendu et interactions corrects avec le moteur terminal. |
| R15 | Exécuter wtr et rmwt. | Fonctions disponibles et interface cohérente selon le contrat défini après inspection. |
| R16 | Sélectionner un dossier réel dans Projets. | Workspace ouvert avec terminal dans le bon dossier. |
| R17 | Actions contextuelles depuis deux panes dans des dossiers différents. | Éditeur, explorateur, chemin et branche ciblent le pane actif. |
| R18 | Déclencher une attente avec une intégration d’agent réelle. | Workspace/onglet signalés et accès au bon pane sans focus forcé. |
| R19 | Fermer puis rouvrir l’application. | Disposition et texte restaurés, séparation explicite, shells neufs, aucune ancienne commande rejouée. |
| R20 | Fermer l’application avec serveur et agent actifs. | Confirmation ciblée puis arrêt forcé de tous les processus concernés, sans maintien volontaire. |
| R21 | Interrompre l’application après modification de disposition. | Dernière sauvegarde automatique exploitable, fichier non partiellement écrit. |
| R22 | Restaurer avec dossier disparu ou shell indisponible. | État local compréhensible et solution de repli sans perte silencieuse. |
| R23 | Fermer puis rouvrir un onglet. | Données prévues restaurées avec processus neufs. |
| R24 | Export/import et import invalide. | Préférences récupérables ; aucune mutation si validation échoue. |
| R25 | Tester noms longs, espaces, accents, IME, mise à l’échelle Windows. | Interface lisible, saisie fiable, chemins correctement traités. |
| R26 | Inspecter onglets, workspaces et palette. | Pas de bordures de sélection colorées ; fond et focus restent lisibles. |
| R27 | Ouvrir deux panes PowerShell 5.1 réels, utiliser les raccourcis, changer de dossier, produire un flux soutenu, fermer un pane, puis installer Dock sur une machine vierge. | Aucune perte ni doublon de frappe, dossier courant exact, aucun processus survivant, débit au moins égal à celui de ConPTY, installation fonctionnelle. |
| R28 | Dans un terminal, Ctrl + clic sur une URL puis sur un hyperlien OSC 8, clic simple sur un lien, puis Ctrl + clic sur un lien `file:` ou d’un autre schéma. | Les liens `http` et `https` s’ouvrent dans le navigateur par défaut avec Ctrl + clic uniquement ; le clic simple sélectionne sans rien ouvrir ; un autre schéma n’est jamais ouvert et un message l’explique. |
| R29 | Ouvrir l’explorateur par son bouton dans un onglet, changer d’onglet puis revenir, faire `cd` dans le shell, changer de pane, double-cliquer un fichier, créer, renommer puis supprimer un fichier. | Fermé par défaut, l’état est propre à chaque onglet ; l’explorateur suit le dossier du pane actif ; le fichier s’ouvre dans l’éditeur ; les opérations sur les fichiers sont visibles immédiatement et la suppression passe par une confirmation. |
| R30 | Dans un dépôt de test, ouvrir la vue Git puis enchaîner : stage d’un fichier et lecture de son diff, commit, création et merge d’une branche, conflit provoqué, annulation de la dernière opération, stash, tag et cherry-pick, push vers un dépôt distant, réécriture de l’historique et nouveau push. | Branches et graphe reflètent chaque opération, y compris celles faites au terminal ; le conflit liste ses fichiers et se termine ou s’annule ; « Annuler » restaure l’état précédent ; le push refusé ne propose le forçage qu’avec `--force-with-lease` et confirmation. |
| R31 | Dans un dépôt de test avec un worktree créé par `wtr`, ouvrir la vue Git, puis ouvrir ce worktree depuis sa section. | La section « Worktrees » liste le dépôt principal et le worktree avec leur branche ; « Ouvrir » rejoint un pane déjà dans le worktree, sinon ouvre un workspace. |
| R32 | Créer un worktree par Leader puis N (nouvelle branche), par l’icône d’arbre (branche existante) et depuis le menu d’une branche distante, dans un projet qui a des ports, un `package.json` et une base de test. | Workspace ouvert dès la création, `pnpm install` lancé dans son terminal, ports remplacés, base répliquée et chaîne de connexion réécrite ; bilan et avertissements dans la barre de statut. |
| R33 | Supprimer ce worktree avec un pane ouvert dedans, d’abord sans fermer le pane, puis en le fermant. | Premier essai refusé, worktree intact et « Réessayer » ; second essai : pane fermé, dossier, base répliquée et branche supprimés, sauf la branche si « Garder la branche » est cochée. |
| R34 | Dans un pane, lancer Claude Code puis Codex CLI et saisir un prompt sur plusieurs lignes avec Maj + Entrée et Ctrl + Entrée, puis faire Maj + Entrée dans PowerShell. | Chaque combinaison ajoute une ligne au prompt de l’agent sans l’envoyer ; dans PowerShell, Maj + Entrée exécute la commande comme Entrée. |
| R35 | Écrire une note sur plusieurs lignes dans la vue Notes d’un workspace (Ctrl + Maj + O), passer à un autre workspace, fermer puis rouvrir Dock ; fermer ensuite tous les onglets de ce workspace et rouvrir le dernier par Ctrl + Maj + Z. | Chaque workspace garde sa propre note ; l’icône de note et sa première ligne en infobulle apparaissent dans le panneau des workspaces ; la note est retrouvée après redémarrage et après la réouverture de l’onglet ; Échap rend le focus au terminal. |

## 18. Décisions restantes avant développement

1. Inspecter le profil PowerShell, wtr/rmwt et la configuration WezTerm Leader + F. **Fait :** voir `docs/inspection-environnement.md`.
2. Identifier les shells installés, l’éditeur, le dossier Projets et les agents utilisés. **Fait :** Windows PowerShell 5.1 par défaut, PowerShell 7/CMD/Git Bash disponibles, VS Code, `C:\\Files\\Projects`, Claude Code et Codex CLI.
3. **Fait :** pile hôte C# + WebView2 unique (xterm.js) + ConPTY validée par le spike T01 avec l’installeur manuel ; voir le [compte rendu du spike T01](https://github.com/MaximeRazafinjato/dock-terminal/blob/b1c648454c311b51a731118aea84b98d10bea1ac/spike/README.md). **Décidé :** l’interface entière est web dans une seule WebView2 ; l’hôte natif ne porte pas d’interface métier.
4. **Fait :** fermer le dernier onglet/pane supprime le workspace ; confirmer avant suppression d’un workspace actif ; état vide si nécessaire.
5. **Fait :** arrêt forcé avec confirmation si serveur, agent ou programme actif ; limites 10 000 lignes/256 Mio, sauvegarde texte toutes les 30 s, cinq onglets fermés.
6. **Fait :** Leader Ctrl + Espace, délai de 5 s, mapping personnalisable ; navigation spatiale.
7. **Fait :** JSON, préférences/session/historique séparés, import par remplacement.
8. **Fait :** noms automatiques dossier, branche absente/HEAD détachée et actions Git indisponibles hors dépôt ; contrat wtr/rmwt documenté.
9. **Reporté :** implémenter les adaptateurs Claude Code/Codex CLI et définir leurs événements fiables dans une évolution dédiée.
10. **Fait :** critères mesurables de performance et versions minimales de Windows consignés en section 15 à l’issue du spike.
11. **À décider :** sur un onglet de la barre ou une ligne du panneau des workspaces qui a le focus, Alt + flèche déplace cet élément au lieu de changer de pane comme le prévoit le tableau retenu de la section 9 (conventions proposées des sections 5, 6 et 9). Garder cette exception ou la retirer.
12. **À décider :** la « Décision prise » de la section 5 demande une confirmation pour supprimer un workspace « lorsqu’il contient des onglets ou des processus actifs ». Aujourd’hui, « Fermer le workspace » (menu du panneau, palette) ne confirme que si des programmes tournent : un workspace de plus de cinq onglets inactifs se ferme sans confirmation et ses onglets au-delà des cinq derniers ne sont plus restaurables. Préciser si la confirmation doit porter sur tout workspace qui contient des onglets.
13. **Fait (29 septembre 2026) :** gestion native des worktrees (section 11, recettes R31 à R33) ; `wtr` et `rmwt` restent utilisables au terminal avec les mêmes chemins.

Ces décisions ne bloquent pas la compréhension du produit ; elles évitent de traiter un comportement accidentel comme une exigence validée.
