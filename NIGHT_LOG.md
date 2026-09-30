# Journal de la nuit du 29 au 30 septembre 2026

Session autonome sur la branche `night-session`. À chaque itération : une amélioration, vérifiée (`pnpm lint`, `pnpm build`, `dotnet build`, `dotnet test` et essai dans une instance de développement isolée), puis un commit.

Les essais se font dans un Dock de développement lancé avec son propre dossier de données (`DOCK_DATA_DIR` dans le scratchpad) et un port de débogage dédié (9247). Frappes, clics et captures passent uniquement par le protocole de débogage de la WebView2, après avoir vérifié que la page chargée est bien le bundle de ce build. Aucune action globale de souris ou de clavier, donc rien ne peut atteindre ton Dock. Les opérations Git se font dans un clone jetable du dépôt, dans le scratchpad.

Version HTML de ce journal, avec un sommaire des itérations : `file:///C:/Users/maxim/Desktop/nuit-dock-2026-09-30.html` (régénérée à la fin de la nuit).

## Itérations

### 1. Égaliser les panes d'un onglet

Un split coupe toujours le pane actif en deux. Trois splits côte à côte successifs donnent donc 50 %, 25 % et 25 %, et il fallait ensuite ajuster chaque séparateur à la souris.

- Nouvelle commande « Égaliser les panes de l'onglet », dans la palette (seulement si l'onglet a plusieurs panes) et par Leader puis `=`. Pas de raccourci direct, pour ne pas prendre une combinaison de plus au shell.
- Chaque split est réparti selon le nombre de panes alignés de part et d'autre dans sa direction (`equalizeNode` dans le modèle). Un split vertical placé dans une colonne compte pour une seule colonne. Les bornes de 15 % et 85 % d'un séparateur restent appliquées.
- Vérifié dans l'instance de dev : trois colonnes, dont la dernière coupée en deux, passent de 653, 319 et 319 px à 432, 429 et 429 px. Les deux panes du bas gardent leur moitié chacun.
- **Convention proposée** en section 7 de la spec. README et architecture front mis à jour.

### 2. Sortir un pane dans un nouvel onglet

Quand un split devient trop encombré (un agent qui tourne longtemps à côté d'un shell, par exemple), il n'y avait aucun moyen de donner à un pane son propre onglet sans fermer son terminal et en perdre le processus.

- Nouvelle action, comme le `break-pane` de tmux : Leader puis `!` (la même touche que dans tmux), « Déplacer le pane actif dans un nouvel onglet » dans la palette, ou « Déplacer dans un nouvel onglet » dans le menu contextuel du terminal.
- Le pane devient seul dans un onglet inséré juste après le sien, qui devient actif et reçoit le focus. Le terminal xterm.js, le processus, le texte et l'état d'agent restent les mêmes : le registre des terminaux est indexé par identifiant de pane, qui ne change pas.
- L'onglet d'origine se referme sur les panes restants et reprend le nom du dossier de son nouveau pane actif s'il n'a pas été renommé à la main. Un pane déjà seul ne bouge pas et la barre de statut l'indique ; un pane agrandi est d'abord réduit.
- Vérifié dans l'instance de dev : après `$env:MARK='processus-conserve'`, le pane sorti répond toujours `processus-conserve` dans son nouvel onglet, l'onglet d'origine garde ses 3 autres panes, et le menu contextuel affiche l'entrée.
- **Convention proposée** en section 7 de la spec. README et architecture front mis à jour.

### 3. L'arbre des fichiers montre l'état Git

Dans l'explorateur, rien ne distinguait un fichier modifié ou non suivi d'un autre : il fallait basculer sur la vue Git pour savoir ce qui avait changé, alors que c'est le premier repère d'un explorateur d'IDE.

- Dans un dépôt, le nom de chaque fichier est coloré et suivi de sa lettre (M, A, D, R, U, ! pour un conflit), avec les mêmes lettres, couleurs et libellés que la vue Git. Un dossier qui contient des modifications porte un point de la couleur de la plus marquante.
- Côté hôte, `ExplorerGitMarks` suit le dépôt de la racine de l'arbre avec le même `GitWatcher` que la vue Git, relit `git status` à chaque changement et n'envoie `files.gitMarks` que si les marques ont changé. `GitPathMarks` (Core) transforme le statut en chemins complets. Rien ne tourne quand l'explorateur est fermé ou hors dépôt, et la vue Git n'est pas sollicitée (pas de fetch automatique, pas d'historique).
- 5 tests ajoutés (`GitPathMarksTests`), dont un sur un vrai dépôt `GitSandbox`.
- Vérifié dans l'instance de dev, sur le clone jetable : `README.md` M, `notes.md` U, `docs` et `docs\sous` avec un point, `nouveau.txt` U ; une ligne ajoutée à `installer\Dock.iss` depuis l'extérieur fait apparaître le point sur `installer` sans action.
- **Convention proposée** en section 4 de la spec (explorateur). Architectures, contrat du pont et doc des tests mis à jour.

### 4. Le prompt d'origine voit de nouveau l'échec de la dernière commande

Le wrapper de prompt de Dock (qui annonce le dossier courant par OSC 7) exécutait ses propres instructions avant d'appeler le prompt de l'utilisateur. PowerShell remettait donc `$?` à vrai : un prompt qui signale l'échec de la commande précédente, comme les segments de statut d'oh-my-posh ou de starship, affichait toujours un succès dans Dock, alors qu'il marche dans WezTerm.

- Le wrapper lit `$?` en toute première instruction, puis le rétablit juste avant d'appeler le prompt d'origine, par un `Write-Error '' -ErrorAction Ignore` qui ne laisse rien dans `$Error`. C'est la technique de l'intégration shell de VS Code. `$LASTEXITCODE` n'était pas touché.
- 3 tests ajoutés (`PowerShellIntegrationTests`), dans un vrai Windows PowerShell 5.1 sans profil : statut False après une commande en échec (ce test échoue sans la correction, vérifié), True après un succès, séquence OSC 7 toujours émise.
- Vérifié dans l'instance de dev avec un prompt d'origine `"[statut $?] PS> "` : `[statut False]` après `Get-Item` sur un chemin absent, `[statut True]` après `Get-Date`, et la branche Git du pane reste affichée (OSC 7 intact).
- Spec (section 15), architecture back et doc des tests mises à jour.

### 5. Signaler la fin d'une commande longue dans un onglet en arrière-plan

On lance un build, des tests ou un `pnpm install` dans un onglet, puis on va travailler ailleurs : rien ne signalait la fin, ni son échec. Seuls les agents Claude Code avaient cette indication.

- Le wrapper de prompt de Dock (itération 4), passé à Windows PowerShell comme à PowerShell 7, annonce désormais, à chaque nouvelle entrée de l'historique PowerShell, la durée de la commande et son succès (`$?`), par une séquence OSC privée (`6973;done;<ms>;<1|0>`), interceptée par xterm.js et jamais affichée. Une simple Entrée sur une ligne vide n'annonce rien (même entrée d'historique).
- Si la commande a duré au moins 10 s et que son onglet n'est pas celui affiché, l'onglet porte, dans la barre d'onglets et le panneau des workspaces, une coche ou une croix rouge (les icônes « terminé » et « erreur » des agents), avec la durée en infobulle. La barre de statut l'annonce (en rouge si échec), donc le journal des messages aussi. Afficher l'onglet efface l'indication. Un état d'agent garde la priorité sur l'icône.
- 3 tests PowerShell réels de plus (`Add-History` simule une commande de 12 s, puis deux prompts : une seule annonce ; échec ; historique vide).
- Vérifié dans l'instance de dev : `Start-Sleep 12; Get-Item C:
existe-pas` puis Ctrl + Tab : croix rouge et « Commande en échec après 12 s dans l'onglet « repo ». » ; retour sur l'onglet : croix effacée ; `Start-Sleep 11` réussi : coche « Commande terminée après 11 s » ; `Start-Sleep 2` : rien ; commande finie alors que l'onglet est affiché : rien.
- **Convention proposée** en section 12 de la spec. README, architectures et doc des tests mis à jour.

### 6. Copier la sortie de la dernière commande

Pour coller une erreur de build ou de tests dans Claude Code, il fallait sélectionner la sortie à la souris, souvent sur plusieurs écrans.

- Nouvelle entrée « Copier la sortie de la dernière commande » dans le menu contextuel du terminal, et « … du pane actif » dans la palette. Elle copie le texte entre la ligne de la commande et l'invite suivante, sans l'une ni l'autre, et la barre de statut donne le nombre de lignes.
- Le repérage s'appuie sur le wrapper de prompt : un repère xterm.js à l'Entrée, l'invite suivante (OSC 7) et l'annonce `done` de l'itération 5, qui valide la plage (une Entrée sur une ligne vide garde donc la sortie précédente).
- **Difficulté rencontrée** : sous Windows 10, ConPTY transmet les séquences OSC dès qu'il les lit, mais ne dessine la sortie qu'à la trame suivante. Le repère de fin posé à l'OSC 7 tombait donc avant la fin de la sortie (premier essai : sortie vide, puis `avant` sans `apres`). Le repère de fin suit maintenant le curseur à chaque écriture jusqu'à la première frappe.
- **Seconde difficulté** : ConPTY écrit les lignes repliées comme des lignes distinctes, sans indicateur de repli, et l'invite (ici sur deux lignes) se retrouvait au bout de la copie. Le wrapper annonce donc aussi la hauteur de l'invite qu'il vient de produire (`OSC 6973;prompt;<lignes>`), séquences d'échappement exclues et repli à la largeur du tampon compris, ce qui couvre aussi les invites oh-my-posh sur deux lignes.
- 3 tests PowerShell réels de plus (invite sur deux lignes, invite colorée avec lien OSC 8, invite plus large que le tampon).
- Vérifié dans l'instance de dev, presse-papiers intercepté dans la page pour ne pas toucher au tien : `avant` + `apres` (400 ms d'écart) copiés, Entrée à vide qui garde la sortie précédente, `cd ..` annoncé « n'a rien affiché », une erreur PowerShell de 12 lignes copiée en entier.
- **Conventions proposées** en sections 8 et 15 de la spec. Architectures et doc des tests mises à jour.

### 7. Échanger un pane avec son voisin

Après quelques splits, un terminal se retrouve souvent du mauvais côté (l'agent à gauche alors qu'on le veut à droite) ; il fallait tout refaire.

- Leader puis Maj + flèche échange le pane actif avec son voisin dans cette direction, comme le `swap-pane` de tmux. Quatre entrées de palette font de même (seulement si l'onglet a plusieurs panes). Pas de raccourci direct, pour ne pas prendre une combinaison de plus au shell.
- Le voisin est choisi comme pour la navigation (Alt + flèche). Les ratios ne changent pas, le pane actif reste actif et garde le focus à sa nouvelle place, les deux terminaux gardent processus et texte : `PaneView` rattache chaque terminal par identifiant de pane, les éléments xterm.js sont simplement déplacés dans le DOM. Un pane agrandi est d'abord réduit.
- Vérifié dans l'instance de dev : après `echo PANE-UN` dans le pane de gauche, Leader puis Maj + → le place en haut à droite avec son texte, le focus le suit, l'ancien voisin passe à gauche.
- **Convention proposée** en section 7 de la spec. README et architecture front mis à jour.

### 8. Déplacer un pane vers un autre onglet

Complément de l'itération 2 : ramener un terminal dans un onglet existant (le `join-pane` de tmux), par exemple pour regrouper un serveur et son agent.

- La palette propose « Déplacer le pane actif vers l'onglet · <nom> » pour chaque autre onglet du workspace. Le pane arrive à droite du pane actif de cet onglet, qui devient actif, avec son terminal, son processus et son texte.
- Si l'onglet d'origine se vide, il disparaît sans entrer dans les onglets fermés : aucun terminal n'a été arrêté, il n'y a rien à rouvrir. Une étoile posée sur une de ces entrées devient orpheline quand l'onglet cible n'existe plus, comme pour « Déplacer l'onglet vers ».
- Vérifié dans l'instance de dev : le pane actif d'un onglet de 3 panes rejoint l'onglet « repo » (deux panes de 796 px côte à côte, l'onglet d'origine garde les 2 autres) ; en déplaçant ensuite les deux panes de « repo », l'onglet disparaît et l'autre en compte 4.
- **Convention proposée** en section 7 de la spec, architecture front mise à jour.

### 9. Alléger le store de session et les raccourcis

Les itérations 1, 2, 7 et 8 avaient fait grossir `sessionStore.ts` (486 → 563 lignes) et `shortcuts.ts` (394 → 447), au-delà de la limite de 400 lignes.

- Les deux déplacements de pane passent dans `store/paneMoves.ts` (`movePaneOut`, `movePaneInto`, avec un `keepRemaining` commun), `tabNameFor` rejoint le modèle.
- Navigation, échange et égalisation au clavier passent dans `keyboard/paneCommands.ts` ; les huit `case` de direction de `runCommand` deviennent deux tables `Command → Direction`.
- Résultat : `shortcuts.ts` 416 lignes, `sessionStore.ts` 513. Le store reste au-dessus de la limite, comme avant la nuit : le découper davantage serait une refonte.
- Vérifié dans l'instance de dev, sans changement de comportement : Alt + → , Leader puis Maj + ←, Leader puis =, Leader puis ! et « Déplacer le pane actif vers l'onglet » donnent les mêmes dispositions qu'avant.

### 10. Relecture indépendante des itérations 1 à 7 et corrections

Un sous-agent a relu le diff de la nuit sans rien modifier, en reproduisant ses constats dans un vrai PowerShell 5.1 et avec git. Il a relevé 1 défaut important et 9 mineurs. J'ai corrigé ceux-ci :

- **Important, `git status` en rafale dans l'explorateur** : pendant un build ou un `pnpm install`, chaque événement du watcher ajoutait un `git status` à la file, celle des listings de l'explorateur, qui paraissait alors figé. Désormais un seul `git status` attend à la fois (comme `GitFeed`), dans une file propre aux marques.
- **`Set-StrictMode`** : le wrapper lisait une variable globale jamais initialisée. Sous StrictMode, plus aucune fin de commande n'était annoncée et une erreur s'ajoutait à `$Error` à chaque invite. Les variables sont maintenant initialisées, et l'OSC 7 n'est émis que dans le système de fichiers (dans `HKCU:`, `[Uri]` levait une erreur). 2 tests PowerShell réels de plus.
- **Commande sur plusieurs lignes, invite transitoire d'oh-my-posh, commande lancée par Dock** (3 constats) : le début de la sortie reposait sur la première Entrée tapée. Le wrapper enveloppe maintenant `PSConsoleHostReadLine` de PSReadLine, comme l'intégration shell de VS Code, et annonce `OSC 6973;exec` quand la ligne part vraiment au shell. L'Entrée reste le repli sans PSReadLine.
- **Jonctions et lecteurs `subst`** : git renvoie le chemin réel, l'arbre affiche le chemin logique, donc aucune marque ne correspondait. Les chemins partent maintenant du dossier affiché, grâce à `git rev-parse --show-prefix` (`GitPathMarks.DisplayRoot`, 3 tests).
- **Sous-module modifié ou dépôt imbriqué non suivi** : ces dossiers, eux-mêmes entrées du statut, n'avaient pas de marque. Leur ligne consulte aussi les marques de fichiers. Le statut n'est plus tronqué à 1 000 fichiers pour l'explorateur.
- **« Actualiser »** relocalise le dépôt, par exemple après un `git init` dans le dossier affiché.
- **Dépôt supprimé pendant le suivi** : les erreurs `Win32Exception`, `IOException` et `InvalidOperationException` sont interceptées, et `git status` n'est lancé que si le dossier existe.
- **Taille de `shortcuts.ts`** : déjà traitée à l'itération 9.
- **Non modifié** : `CLAUDE.md` dit encore que deux tests lancent un vrai PowerShell 5.1, alors qu'ils sont maintenant 13 (les 11 de `PowerShellIntegrationTests` en plus). Je n'ai pas touché ton `CLAUDE.md` : c'est à mettre à jour si tu le souhaites.
- Vérifié dans l'instance de dev :
  - `if ($true) {` / `'dans le bloc'` / `}` : la copie donne seulement `dans le bloc`, sans les lignes `>>` ;
  - PSReadLine intact : coloration, continuation `>>`, rappel de la commande précédente par ↑ ;
  - marques Git de l'explorateur toujours affichées ;
  - 406 tests au vert.

### 11. Fin de commande visible au niveau du workspace

L'indication de l'itération 5 n'apparaissait que sur les lignes d'onglets : invisible pour un workspace replié dans le panneau, ou quand le panneau est masqué (les workspaces sont alors listés dans l'en-tête).

- Un workspace replié porte la coche ou la croix de ses onglets (échec d'abord), à côté de son résumé d'agents ; dans l'en-tête, chaque workspace la porte aussi.
- Vérifié dans l'instance de dev : `Start-Sleep 11` dans « Général », passage à « Workspace 2 » ; la coche apparaît sur l'onglet « repo », puis sur la ligne de « Général » une fois replié, puis dans l'en-tête après Ctrl + Maj + B.
- Spec (convention de la section 12) et architecture front mises à jour.

### 12. Ctrl + clic sur un chemin de fichier dans le terminal

Les erreurs de compilation, de tests ou de lint affichent `src/app.ts:12:5` ou `Program.cs(42,17)`. Seules les URL étaient cliquables : il fallait retrouver le fichier à la main.

- Les chemins de fichiers deviennent des liens, soulignés au survol : absolus ou relatifs avec séparateur, suivis ou non de `:ligne`, `:ligne:colonne` ou `(ligne,colonne)`. Un nom seul n'est lié que s'il porte une ligne, pour ne pas souligner chaque `package.json`.
- Ctrl + clic l'ouvre dans l'éditeur configuré. Pour VS Code et ses dérivés (`code`, `cursor`, `windsurf`…), on passe `-g chemin:ligne:colonne` pour arriver directement à la bonne ligne ; un autre éditeur reçoit juste le fichier. Le clic simple n'ouvre toujours rien, comme le veut la décision sur les liens.
- Un chemin relatif part du dossier du pane (côté hôte, `EditorLocation.Resolve`). Un fichier absent est signalé (« Fichier introuvable : … »).
- Détection vérifiée sur des sorties types : tsc, dotnet, pytest, pile Node et `git status`. Les URL, versions et noms isolés ne sont pas liés.
- 8 tests (`EditorLocationTests`).
- Vérifié dans l'instance de dev, avec un faux `code.cmd` qui journalise ses arguments pour ne pas ouvrir ton VS Code :
  - Ctrl + clic sur `src\Dock.Core\Git\GitRunner.cs(42,17): error CS1002` lance `-g …\repo\src\Dock.Core\Git\GitRunner.cs:42:17` ;
  - un clic simple ne lance rien ;
  - un fichier absent affiche le message.
- **Limites** : un chemin qui contient des espaces n'est pas lié. Un lien sur deux lignes repliées non plus.
- **Convention proposée** en section 9 de la spec. Architectures mises à jour.

### 13. Charger l'aperçu des fichiers à la demande

Tout le code web tenait dans un seul fichier JavaScript de 1,3 Mo, lu et compilé à chaque démarrage de Dock, y compris marked, highlight.js et DOMPurify, qui ne servent qu'à l'aperçu Markdown et texte.

- Le tiroir d'aperçu est chargé par `React.lazy` à sa première ouverture (`LazyFilePreview`) : le fichier principal passe de 1 304 à 1 057 Ko, l'aperçu part dans un fichier de 248 Ko.
- Vérifié dans l'instance de dev : au démarrage, aucun fichier d'aperçu n'est demandé ; au clic sur `notes.md`, `FilePreviewDrawer-*.js` est servi par l'hôte comme les autres fichiers de `wwwroot`, le titre et la coloration du bloc de code sont identiques. La publication de l'installeur copie tout `dist`, le fichier supplémentaire compris.
- Architecture front mise à jour.

### 14. Revert d'un commit depuis la vue Git

La vue Git proposait cherry-pick, merge, rebase et reset, mais pas le revert, pourtant le moyen sûr de défaire un commit déjà publié.

- Le menu d'un commit propose « Revert sur « branche » ». Il lance `git revert --no-edit`, par rapport au premier parent pour un merge, et fonctionne aussi sur le commit HEAD.
- Tout le reste existait déjà et sert tel quel : la détection d'un revert en cours (`REVERT_HEAD`), « Terminer » et « Abandonner » en cas de conflit. « Annuler » (nouveau type `Revert` dans le journal d'annulation) retire le commit de revert par `reset --keep` tant qu'il n'est pas publié.
- 1 test d'intégration Git : revert d'un commit passé, fichier retiré, puis « Annuler » rend HEAD.
- Vérifié dans l'instance de dev, sur le clone jetable :
  - revert de « Suppression de l'ancien night log » depuis le graphe : commit `Revert "…"` créé, `NIGHT_LOG.md` restauré, message « Revert de 2009a21 sur « night-session » terminé. » ;
  - « Annuler » ramène HEAD sur `2009a21`.
- **Convention proposée** en section 11 de la spec, README mis à jour.
- **Remarque** : l'en-tête du panneau Git manquait de place à 280 px (corrigé à l'itération 15).

### 15. Rangée d'outils du panneau Git sur une seule ligne

Remarque de l'itération 14 : à la largeur par défaut du panneau (280 px), avec « Pull 16 » en retard, le bouton « Actualiser » passait seul sur une deuxième ligne.

- Sous 300 px de rangée, « Annuler » ne montre plus que son icône, par une requête de conteneur comme pour la branche dans l'en-tête des panes. L'infobulle et le libellé accessible restent.
- Vérifié dans l'instance de dev : à 268 px, Fetch, « Pull 16 », Push, Annuler et Actualiser tiennent sur la même ligne.

### 16. Copier le chemin relatif depuis l'arbre des fichiers

Pour citer un fichier à Claude Code ou dans une commande, le chemin complet (`C:\Users\…\docs\TESTING.md`) est trop long : on veut celui depuis le dossier du terminal.

- Le menu contextuel d'un fichier ou d'un dossier de l'arbre propose « Copier le chemin relatif », depuis la racine de l'arbre, qui est le dossier du pane actif.
- Vérifié dans l'instance de dev : sur `docs\TESTING.md`, le presse-papiers (intercepté dans la page) reçoit `docs\TESTING.md` et la barre de statut l'annonce.
- **Convention proposée** en section 4 de la spec.

### 17. Aide du Leader lisible à toute largeur

L'aide affichée après Ctrl + Espace tenait sur deux lignes dans les 42 px de l'en-tête et rognait la fin au-delà. Avec les trois entrées ajoutées cette nuit (=, !, Maj + flèche), elle ne tenait plus : à 1 264 px de large, Maj + flèche, PgUp / PgDn, Échap et Ctrl + Espace disparaissaient.

- Le badge « Leader… » reste dans l'en-tête, et les séquences s'affichent dans un panneau flottant juste en dessous, en grille (colonnes d'au moins 200 px), pendant les 5 s du Leader. Le panneau ne prend pas le focus et disparaît avec le Leader.
- Vérifié dans l'instance de dev, fenêtre redimensionnée par son handle : à 1 084 px, les 24 séquences sont visibles en trois colonnes, aucune rognée.
- Architecture front mise à jour.

### 18. Aperçu des images

L'aperçu de l'explorateur ne connaissait que le Markdown et le texte. Une capture ou un logo s'ouvrait dans l'éditeur, alors que l'hôte sait déjà servir les images locales, pour celles d'un Markdown.

- Les images courantes (`.png`, `.jpg`, `.gif`, `.webp`, `.svg`, `.bmp`, `.ico`, `.avif`) s'ouvrent par un simple clic dans le tiroir d'aperçu, ajustées à la place disponible, avec le badge « Image · largeur × hauteur ».
- Côté hôte, `FilePreview.Read` ne lit pas le fichier. Il renvoie son adresse `https://dock.files/…`, déjà servie avec `Cache-Control: no-cache`, suivie de `?v=` et de la date de modification, pour que le rechargement à la modification affiche la nouvelle version. Un SVG est affiché par une balise `img`, donc sans exécuter de script.
- 4 tests de plus : type image, adresse avec version, adresse résolue vers le même fichier par l'hôte, image absente.
- Vérifié dans l'instance de dev : `docs\images\files.png` du clone s'affiche avec « Image · 1586 × 922 », puis `git.png` au clic suivant.
- **Convention proposée** en section 4 de la spec. README et architecture back mis à jour.

### 19. Deuxième relecture indépendante (itérations 11 à 17) et corrections

Un sous-agent a relu le diff des itérations 11 à 17 sans rien modifier, en reproduisant ses constats avec node, git et .NET dans un dossier temporaire. Il a relevé 1 défaut important et 6 mineurs, tous corrigés :

- **Important, sécurité : injection de commande par l'éditeur**. L'éditeur par défaut, `code.cmd`, passe par cmd.exe. .NET ne mettait entre guillemets que les arguments contenant un espace : un `&` dans un nom de dossier sans espace (`R&D`, ou un `docs&calc` piégé dans un dépôt cloné) laissait cmd.exe exécuter la suite. Le défaut existait déjà pour « Ouvrir dans l'éditeur » de l'explorateur, et l'itération 12 lui ajoutait une voie d'accès. Chaque argument est maintenant toujours entre guillemets (`EditorLocation.CommandLine`). Un test d'intégration lance un vrai `.cmd` sur un dossier `R&md,injecte^%x` : il échoue sans la correction (chemin coupé au `&`) et passe avec.
- **Liens de fichiers** :
  - les lettres accentuées font partie du chemin (`D:\Projets\dépôt\…` n'est plus coupé en `t\src\…`) ;
  - la suite d'un chemin replié sur deux lignes n'est plus liée ;
  - les domaines (`www.example.com/…`, `hote.com:8080`) ne sont plus pris pour des fichiers ;
  - les chemins `a/…` et `b/…` de `git diff` sont cherchés sans ce préfixe côté hôte (`EditorLocation.ResolveExisting`, 3 tests) ;
  - la zone cliquable suit les cellules du terminal, donc un emoji ou un caractère CJK devant le chemin ne la décale plus ;
  - un numéro de ligne énorme n'empêche plus l'ouverture.
- **Débordement de l'en-tête** : un workspace passé dans le menu « +N » montre sa coche ou sa croix dans ce menu et sur le bouton.
- **Revert sans effet** : si les modifications du commit sont déjà absentes, le message dit « Rien à défaire… » au lieu de « Le revert a échoué. » (1 test).
- Vérifié dans l'instance de dev : Ctrl + clic sur le dernier caractère de `✅ b/src/Dock.Core/Git/GitRunner.cs:7:2` ouvre `…\repo\src\Dock.Core\Git\GitRunner.cs:7:2`, arguments entre guillemets. Un décalage d'une cellule aurait raté le lien. 427 tests au vert.
- Spec (conventions des liens de fichiers) mise à jour.

### 20. Naviguer de commande en commande, et copie de sortie plus fiable

Dans un terminal qui a beaucoup défilé (plusieurs builds ou tests à la suite), retrouver le début d'une commande obligeait à faire défiler à la main.

- Alt + PgUp / Alt + PgDn font défiler le terminal actif jusqu'à la commande précédente ou suivante, avec une ligne de contexte au-dessus. Au-delà de la dernière, il revient en bas. Deux entrées de palette font de même. Ce raccourci direct sort de la règle « Ctrl + Maj + lettre ou Alt + flèche » : c'est à confirmer.
- **En testant, j'ai trouvé deux défauts de l'itération 6 (copie de la sortie), corrigés :**
  - l'annonce `exec` peut arriver après la première ligne de sortie déjà dessinée, et cette ligne (`COMMANDE-3`) disparaissait de la copie. Le début repose maintenant sur l'Entrée tapée (la dernière avant l'exécution, pour une saisie sur plusieurs lignes). `exec` la confirme, et sa position ne sert plus que pour une commande envoyée par Dock ;
  - quand la sortie fait défiler l'écran, ConPTY sous Windows 10 redessine parfois la zone visible au lieu de la faire défiler, et le contenu glisse d'une ligne par rapport aux repères xterm.js. Le wrapper transmet donc le texte de la commande dans l'annonce `exec` (base64 UTF-8). La première ligne de sortie et le repère de navigation sont recalés sur la ligne, à ±3 près, dont la fin (avec les deux précédentes, espaces ignorés, pour une commande repliée) correspond à ce texte.
- 1 test PowerShell de plus (annonce `exec` avec le texte de la commande).
- Vérifié dans l'instance de dev, avec une instrumentation temporaire retirée ensuite :
  - quatre commandes de 41 lignes : chaque copie commence bien par `LONGUE-n` ;
  - le bloc `if` multiligne donne `dans le bloc` ;
  - Alt + PgUp affiche la ligne de contexte puis l'invite de la commande précédente, et un second appui celle d'avant.
- **Conventions proposées** en section 8 de la spec. README et architecture back mis à jour.

### 21. Sortir les commandes et les tables de touches des raccourcis

Avec Alt + PgUp / PgDn, `shortcuts.ts` était repassé à 427 lignes, au-dessus de la limite de 400.

- L'enum `Command` et les tables de touches (Leader, Leader + Maj + flèche, Ctrl + Maj + lettre, Alt + flèche ou page) passent dans `keyboard/commands.ts`. `shortcuts.ts` réexporte `Command`, donc aucun import ne change. Il revient à 328 lignes.
- Vérifié dans l'instance de dev : Ctrl + Maj + D ouvre un split, Leader puis X le ferme, Ctrl + P ouvre la palette.

### 22. Nommer la commande terminée

« Commande terminée après 11 s dans l'onglet « repo » » ne disait pas laquelle. Avec plusieurs builds en parallèle, il fallait aller voir.

- L'annonce `done` du wrapper transmet aussi le texte de la commande (base64 UTF-8, tiré de l'historique PowerShell). La barre de statut, le journal et l'infobulle de l'onglet citent sa première ligne, tronquée à 48 caractères : « « Start-Sleep 11; Write-Output 'fini' » terminée après 11 s dans l'onglet « repo ». ».
- Le décodage base64 est partagé avec la copie de sortie (`decodeCommandText`).
- 1 test PowerShell de plus (texte accentué dans l'annonce). L'extraction des annonces dans les tests suit le nouveau format.
- Vérifié dans l'instance de dev avec `Start-Sleep 11; Write-Output 'fini'` lancé avant Ctrl + Maj + T.
- Spec et architecture back mises à jour.

### 23. Graphe Git utilisable dans une petite fenêtre

À 900 px de large, avec le panneau des workspaces et le panneau Git ouverts, le graphe n'avait plus qu'une centaine de pixels, mangés par la colonne des branches et tags. C'était une limite notée la nuit dernière : il fallait replier un panneau à la main.

- Comme Auteur et Date, la colonne des branches, tags et stash se replie à l'affichage quand il resterait moins de 400 px au graphe, sans changer le réglage mémorisé. Elle revient quand la place revient.
- Le bouton de la barre du graphe suit l'affichage réel : enfoncé seulement si la colonne est visible. Quand elle est repliée faute de place, il la force à s'afficher, sans rien mémoriser.
- Vérifié dans l'instance de dev, fenêtre de 884 px : la colonne se replie et le graphe s'affiche avec ses voies. Le bouton la réaffiche, puis la fenêtre agrandie la garde.
- Convention de la section 11 complétée.

### 24. Distinguer les panes d'un même onglet dans la palette

J'ai revu l'interface à 900 px de large (palette, paramètres, graphe Git) : rien ne déborde. En revanche, la palette listait quatre fois « Pane · Général / repo / repo (powershell) » pour les quatre panes d'un onglet, avec des indices tronqués identiques.

- Quand un onglet a plusieurs panes, chaque entrée « Pane » porte sa position dans l'ordre de la disposition : « · 1/4 », « · 2/4 »…
- Vérifié dans l'instance de dev : taper « pane » affiche « … (powershell) · 1/4 » à « · 4/4 », puis le pane seul de « Workspace 2 » sans numéro.

### 25. Aperçu d'image : taille réelle et dimensions justes

Suite de l'itération 18.

- Un clic sur l'image bascule entre la taille ajustée et la taille réelle, avec défilement. Le curseur loupe et l'infobulle l'indiquent.
- Les dimensions du badge correspondent toujours à l'image affichée : celles de l'image précédente ne restent plus visibles le temps du chargement suivant.
- Vérifié dans l'instance de dev : `files.png` passe de 871 px (ajustée) à 1 586 px (taille réelle, zone défilante de 1 634 px) après un clic, puis `git.png` annonce ses propres dimensions.
- Convention de la section 4 complétée.

Bilan HTML de la nuit généré sur ton Bureau (lien en tête de ce journal), à partir de ce fichier.

### 26. Déplacer un pane vers un onglet d'un autre workspace

Idée laissée en suspens à l'itération 8 : ramener un terminal dans un autre workspace, par exemple un serveur lancé au mauvais endroit.

- La palette propose aussi « Déplacer le pane actif vers l'onglet · <workspace> / <onglet> » pour chaque onglet des autres workspaces. Le pane arrive à droite du pane actif de cet onglet, et ce workspace devient actif. Si l'onglet d'origine se vide, il disparaît et son workspace se replace sur l'onglet voisin.
- **Garde-fou** : le dernier pane d'un workspace ne peut pas le quitter. Sinon le workspace disparaîtrait, avec sa note, sans confirmation. La barre de statut l'explique.
- Vérifié dans l'instance de dev : le pane seul d'un onglet de « Workspace 2 » rejoint « Général / repo » (5 panes), « Workspace 2 » garde son autre onglet ; le dernier pane de « Workspace 2 » est ensuite refusé avec le message.
- Convention de la section 7 complétée.

### 27. Copier un chemin au clavier dans l'arbre des fichiers

Suite de l'itération 16 : les deux copies de chemin n'étaient accessibles qu'au menu contextuel.

- Sur la ligne sélectionnée de l'arbre, Ctrl + C copie le chemin complet et Ctrl + Maj + C le chemin relatif. Le menu contextuel affiche ces raccourcis.
- Vérifié dans l'instance de dev (presse-papiers intercepté dans la page) sur `README.md` : chemin complet, puis `README.md` avec « Chemin copié : README.md ».
- Convention de la section 4 complétée.

### 28. Nouvel onglet par double-clic dans la barre vide

Geste attendu des navigateurs et de Windows Terminal : double-cliquer dans l'espace vide de la barre d'onglets n'avait aucun effet.

- Un double-clic dans l'espace vide de la barre (hors onglets et boutons) ouvre un nouvel onglet, comme « + ». Le double-clic sur un onglet renomme toujours l'onglet. L'infobulle de « + » le mentionne.
- Vérifié dans l'instance de dev : un double-clic sur la barre, puis sur l'espace vide après les onglets, ouvre à chaque fois un onglet (1 → 2 → 3).
- Une troisième relecture indépendante (itérations 20 à 27) tourne en parallèle ; ses constats seront traités à l'itération suivante.
- **Convention proposée** en section 6 de la spec.

### 29. Troisième relecture indépendante (itérations 20 à 27) et corrections

Le sous-agent a rejoué `commandOutput.ts` avec xterm.js sans interface dans un dossier temporaire. Il a relevé 1 défaut important et 5 mineurs, tous corrigés :

- **Important : invite qui affiche du texte à droite** (l'heure d'un thème oh-my-posh, par exemple). La ligne de commande devient `PS> ls      10:32:05` et ne se termine plus par la commande. Le recalage de l'itération 20 cherchait alors ailleurs et prenait `Directory: …`, qui se termine par `ls` : la copie perdait deux lignes, et Alt + PgUp s'arrêtait trop bas. Désormais :
  - la ligne attendue est testée en premier, par « contient » plutôt que « se termine par » ;
  - ailleurs, la recherche exige que la ligne se termine par la commande quand sa fin est courte (moins de 6 caractères, comme `}` ou `ls`) ;
  - dans tous les cas, la ligne candidate doit contenir les 3 derniers caractères de la commande, sans quoi une ligne de sortie glissée juste en dessous passait pour la commande.
- **`cls`** : il effaçait le repère de départ, et la navigation gardait un repère fantôme par `cls`. Les repères effacés sont maintenant ignorés, y compris celui de l'Entrée à l'annonce `exec`.
- **Première commande de chaque terminal** : elle n'utilisait pas encore le repère posé à l'Entrée. C'est corrigé.
- **Écran alternatif** (`less`, vim) : Alt + PgUp / PgDn est laissé à l'application au lieu d'être avalé.
- **Graphe Git dans une fenêtre étroite** : si l'utilisateur avait fermé la colonne des branches, le premier clic sur son bouton semblait sans effet. Il l'affiche maintenant tout de suite.
- **Aperçu d'image** : la taille réelle ne s'applique plus à l'image suivante.
- Vérifié dans l'instance de dev avec un scénario rejouable (`scen.sh` du scratchpad), avec l'invite normale puis avec une invite qui affiche `10:32:05` à droite par positionnement du curseur. Les 10 copies sont justes : trois sorties longues qui font défiler l'écran (`LONGUE-n` en tête), `ls` (en tête `Directory: …`), et le bloc `if` multiligne (`dans le bloc`).

### 30. Copier le diff d'un fichier

Pour demander à Claude Code de relire une modification, ou pour la reporter ailleurs par `git apply`, il fallait sélectionner le diff à la main dans le volet.

- L'en-tête du volet de diff porte un bouton « Copier le diff ». Il copie le diff du fichier au format unifié, reconstruit depuis les chunks déjà reçus : en-têtes `--- a/…` et `+++ b/…`, lignes `@@`, préfixes ` `, `+`, `-`, et note git standard `\ No newline at end of file`. Il marche pour un fichier Unstaged, Staged, d'un commit ou d'un stash, et reste grisé pour un fichier binaire.
- Nouvelle icône « copier » dans le jeu d'icônes (celle de l'en-tête des panes).
- Vérifié dans l'instance de dev, sur `README.md` du clone : le presse-papiers (intercepté) reçoit un diff unifié complet, et la barre de statut affiche « Diff de README.md copié (1 ligne modifiée). ».
- **Convention proposée** en section 11 de la spec.

### 31. Base de worktree par défaut absente du dépôt

J'ai essayé la création de worktree (Leader puis N) sur le clone jetable, sans la valider. Le formulaire proposait « depuis origin/develop », la base réglée par défaut, alors que ce dépôt n'a pas de branche `develop` ; sans changer la liste, la création aurait échoué au fetch (« Échec du fetch de origin/develop »).

- Si la base réglée n'existe pas dans le dépôt, localement ni sur le dépôt distant, Dock propose `main` puis `master`, sur le dépôt distant de préférence. Le réglage lui-même ne change pas, et une base choisie explicitement est toujours respectée.
- 2 tests `WorktreeCreatorTests` : le plan propose `origin/main`, et la création part bien de `origin/main`.
- Vérifié dans l'instance de dev : le formulaire propose maintenant `origin/main`, et l'option `origin/develop` inexistante a disparu de la liste. Aucun worktree n'a été créé.
- Convention ajoutée au tableau de la section 11 de la spec.

### 32. Sélecteur de projets vide : dire quoi faire

Dans l'instance de dev, le sélecteur de projets (Leader puis F) affichait seulement « Aucun dossier trouvé. ». Le dossier des projets par défaut, `C:\Files\Projects`, ne contient qu'un sous-dossier `worktrees`, exclu de la liste. Le même message servait aussi quand la recherche ne trouvait rien.

- Sans aucun projet : « Aucun projet dans C:\Files\Projects : le dossier des projets se change dans Paramètres (Leader puis ,). ». Si la recherche ne trouve rien : « Aucun dossier ne correspond à la recherche. ». Un dossier introuvable garde son message d'erreur existant.
- Vérifié dans l'instance de dev pour le premier cas. Pour le second, il aurait fallu créer un dossier dans ton `C:\Files\Projects`, ce que je n'ai pas fait.

### 33. Fermer les onglets à droite

Le menu d'un onglet proposait « Fermer les autres onglets », mais pas « Fermer les onglets à droite », courant dans les navigateurs et VS Code pour nettoyer une série d'onglets ouverts le temps d'une tâche.

- Nouvelle entrée « Fermer les onglets à droite », dans le menu de l'onglet (grisée pour le dernier) et dans la palette (seulement s'il y a des onglets à droite). Même confirmation quand un programme tourne, et même texte conservé pour « Rouvrir » que « Fermer les autres onglets ».
- Vérifié dans l'instance de dev : sur le premier de trois onglets, l'entrée ferme les deux suivants.
- Convention de la section 6 complétée.

### 34. Mêmes fermetures d'onglets dans le panneau des workspaces

Le menu d'une ligne d'onglet du panneau ne proposait que « Fermer l'onglet », alors que celui de la barre d'onglets offre aussi « Fermer les autres onglets » et, depuis l'itération 33, « Fermer les onglets à droite ».

- Le menu du panneau propose « Fermer les autres onglets » et « Fermer les onglets en dessous », l'équivalent de « à droite » dans une liste verticale. Ce sont les mêmes actions, avec la même confirmation.
- Vérifié dans l'instance de dev : les deux entrées apparaissent, grisées pour un workspace d'un seul onglet.
- Convention de la section 6 complétée.

### 35. README à jour des nouveautés de la nuit

Le README ne mentionnait que certaines nouveautés, au fil des itérations.

- La liste des fonctionnalités couvre maintenant :
  - les opérations sur les panes : égaliser, échanger, sortir, ramener ;
  - la sortie des commandes : copie et navigation ;
  - la fin de commande qui cite la commande ;
  - les liens de fichiers du terminal ;
  - la copie du diff Git.
- Le paragraphe des raccourcis ajoute Ctrl + C / Ctrl + Maj + C dans l'arbre des fichiers et le double-clic dans la barre d'onglets.

### 36. Aller à l'onglet N par Leader puis un chiffre

Pour sauter directement à un onglet, il fallait enchaîner Ctrl + Tab ou passer par la palette.

- Leader puis 1 à 8 affiche l'onglet correspondant du workspace actif, et 9 le dernier, comme dans les navigateurs et avec le préfixe de tmux. La touche est reconnue par sa position (`Digit1`…) : en AZERTY, la rangée du haut marche sans Maj. Un onglet absent est signalé (« Pas d'onglet 5 dans ce workspace. »). L'aide du Leader et le README le mentionnent.
- Vérifié dans l'instance de dev, avec trois onglets : Leader puis 1, 9 et 2 affichent le 1ᵉʳ, le 3ᵉ puis le 2ᵉ onglet, et 5 affiche le message.
- **Convention proposée** en section 9 de la spec. Point 15 de la section 18 complété (pas de raccourci direct).

### 37. Envoyer une ligne des notes au terminal

Les notes servent souvent à garder des commandes (ports, scripts, requêtes) : il fallait les sélectionner, copier, cliquer dans le terminal et coller.

- Dans la note, Ctrl + Entrée colle la ligne du curseur, ou la sélection, dans le terminal actif du workspace (collage xterm.js), sans l'exécuter, et donne le focus au terminal : il ne reste qu'à relire et taper Entrée. Ligne vide ou terminal sans shell : message dans la barre de statut, rien n'est envoyé. Le texte d'aide de la note le mentionne. *Correction (itération 42) : une sélection de plusieurs lignes, elle, était exécutée ligne à ligne sous Windows PowerShell 5.1 ; elle passe désormais par la confirmation du collage multi-ligne.*
- Vérifié dans l'instance de dev : note « git status --short / echo depuis-les-notes », Ctrl + Entrée sur la 2ᵉ ligne → `echo depuis-les-notes` apparaît après le prompt, non exécuté, focus dans le terminal.
- **Convention proposée** en section 5 de la spec (« Notes du workspace ») et ligne ajoutée au README.

### 38. Clignotement de la barre des tâches à la fin d'une commande longue

Lancer un build ou des tests puis passer à une autre fenêtre : rien ne signalait la fin, sauf à revenir voir Dock.

- Quand une commande Windows PowerShell ou PowerShell 7 d'au moins 10 secondes se termine et que Dock n'est pas la fenêtre active, Dock clignote dans la barre des tâches, même si la commande tournait dans l'onglet affiché. Pas de réglage en plus : c'est la case « Faire clignoter Dock dans la barre des tâches » des notifications d'agents, dont le libellé le précise désormais. Pas de son ni de notification Windows, pour rester discret.
- Nouvelle commande de pont `attention.flash` ; l'hôte vérifie lui-même que la fenêtre est inactive et le réglage coché (`AttentionNotifier.FlashWhenInactive`).
- Vérifié : build et 431 tests verts ; dans l'instance de dev, `attention.flash` est acceptée (une commande inconnue affiche bien « Commande inconnue »). Le clignotement lui-même ne s'observe pas par le débogage distant : à confirmer à l'usage.
- **Convention proposée** complétée en section 12 de la spec (fin d'une commande longue).

### 39. Corrections de la quatrième relecture (clavier, sortie, diff copié)

Une relecture indépendante des itérations 28 à 36 a relevé 8 défauts ; cette itération corrige les six premiers.

- **Leader puis `!` cassé en QWERTY, QWERTZ et AZERTY belge** (important) : `!` y est Maj + 1 ou la touche 8, que le test des chiffres interceptait avant les touches Leader, affichant un onglet au lieu de sortir le pane. Les touches Leader sont maintenant cherchées d'abord, les chiffres ensuite. Rien ne change en AZERTY français.
- **Pavé numérique** : Leader puis 1 … 9 du pavé numérique (Verr Num actif) affiche aussi l'onglet, au lieu de taper le chiffre dans le shell. Sans Verr Num, ces touches restent des flèches.
- **Copie de sortie** : quand la commande repliée ne laisse qu'un ou deux caractères sur sa dernière ligne (`rg Leader src/…/shortcuts.ts` sur 40 colonnes), la première ligne de sortie était perdue. La ligne de la commande est maintenant celle où finit la dernière occurrence de la fin de la commande : la tolérance pour le texte affiché à droite de l'invite reste, et la ligne de sortie suivante n'est plus prise pour la commande.
- **Diff copié** : l'hôte fournit le diff brut de Git (`patch`), copié tel quel. Un renommage, une suppression et un fichier CRLF s'appliquent désormais par `git apply` (avant : « No such file », fichier vidé au lieu de supprimé, « patch does not apply »). Un diff tronqué à l'affichage n'est plus copiable (bouton grisé, infobulle explicite) au lieu de donner un patch corrompu.
- Vérifié : 433 tests (dont deux nouveaux : renommage + suppression appliqués par `git apply`, diff tronqué sans `patch`) ; simulation xterm headless du cas de la relecture (première ligne retrouvée) et des anciennes simulations (identiques) ; les 10 scénarios réels de copie de sortie dans l'instance de dev ; Leader puis 1 et 3 du pavé numérique, Leader puis Maj + 1 (« ! » en QWERTY) qui sort bien le pane.
- Spec (section 11, « Copier le diff ») et architecture backend mises à jour.

### 40. Fin des corrections de la quatrième relecture

Les deux derniers défauts et les deux remarques secondaires de la relecture des itérations 28 à 36.

- **Base des worktrees** : le repli vers `main` / `master` (itération 31) s'appliquait aussi quand la base réglée venait d'être poussée sans avoir encore été récupérée, et la rendait inaccessible ; il remplaçait aussi un tag ou un commit réglé. Désormais une base qui désigne un tag ou un commit est gardée, et la base réglée reste toujours dans la liste « Base par défaut » du formulaire : la choisir la récupère par le fetch de la création.
- **Double-clic sur le × du dernier onglet** : le premier clic fermait l'onglet, le « + » glissait à gauche et le second clic, tombé sur l'espace vide, formait un double-clic qui rouvrait un onglet. Le double-clic n'ouvre un onglet que si son premier clic visait déjà l'espace vide.
- **Sélecteur de projets et de worktrees** : à la première ouverture, avant la réponse de l'hôte, ils affichaient « Aucun projet dans  : … » ; ils affichent « Chargement des projets… ».
- **« Fermer les onglets en dessous »** (panneau des workspaces) demandait confirmation pour « les N onglets de droite » ; le texte dit maintenant « en dessous ».
- Vérifié : 434 tests (dont base en tag sans dépôt distant gardée, base absente toujours proposée) ; dans l'instance de dev, double-clic sur le × du dernier onglet → l'onglet est fermé et aucun autre n'est ouvert, double-clic dans l'espace vide → nouvel onglet.
- Spec (section 11, réglages des worktrees) mise à jour.

### 41. Taille du texte des terminaux

La taille du texte était figée à 14 px dans le code : impossible de l'agrandir sur un grand écran ou de la réduire pour plus de colonnes. La spec la laissait « À décider » (section 4), sans issue ouverte.

- Nouveau réglage « Taille du texte (px) » dans une section « Terminaux » des Paramètres : liste de 8 à 32 px, 14 par défaut, avec un aperçu d'une ligne de prompt à la taille choisie.
- À l'enregistrement, tous les terminaux ouverts changent de taille à chaud et recalculent colonnes et lignes, transmises au shell ; un onglet masqué se réajuste en s'affichant. La taille voyage dans `app.hello` : les terminaux naissent directement à la bonne taille au lancement, sans redimensionnement visible.
- Côté hôte : huitième fichier de réglages `appearance.json` (`AppearanceSettingsRepository`), taille ramenée entre 8 et 32 à la lecture, à l'enregistrement et à l'import, clé `appearance` facultative dans les préférences exportées.
- Vérifié : 438 tests (dont 4 nouveaux : bornes, fichier vide, export / import, import sans la clé) ; dans l'instance de dev, passage à 18 px → texte agrandi à chaud, `$Host.UI.RawUI.WindowSize` donne 131 × 42 et le prompt se replie bien à 131 colonnes dans un onglet qui était masqué.
- **Convention proposée** en section 4 de la spec (la police et le zoom restent à décider) ; README et architecture backend mis à jour.

### 42. Confirmation avant un collage de plusieurs lignes

Coller plusieurs lignes dans Windows PowerShell 5.1 les exécutait aussitôt, une par une : ni PSReadLine 2.0 ni ConPTY sous Windows 10 n'activent le collage délimité (bracketed paste). Vérifié dans l'instance de dev : `echo un` / `echo deux` collés → la première ligne s'exécute immédiatement. Un script copié depuis une page ou un message partait donc sans relecture ; Windows Terminal demande confirmation dans ce cas.

- Ctrl + V, Ctrl + Maj + V, « Coller » du menu du terminal et Ctrl + Entrée des notes demandent confirmation quand le texte compte plusieurs lignes et que le programme n'a pas activé le collage délimité : dialogue « Coller N lignes ? » avec l'avertissement, les 8 premières lignes et « … et N autres lignes » ; Entrée colle et exécute, Échap annule (« Collage annulé : rien n'a été envoyé au terminal. ») et rend le focus au terminal.
- Un seul saut de ligne final ne déclenche rien (une commande copiée avec son retour à la ligne se colle comme avant). Claude Code, Codex et les éditeurs plein écran, qui activent le collage délimité, reçoivent le texte sans confirmation.
- Vérifié dans l'instance de dev : deux lignes → dialogue ; Échap → rien dans le terminal, focus rendu ; Entrée → les deux commandes s'exécutent ; une ligne avec saut de ligne final → collée directement.
- **Convention proposée** en section 8 de la spec ; la convention des notes (section 5) y renvoie. Journal de l'itération 37 corrigé : il affirmait à tort que le collage des notes était protégé par le bracketed paste.

### 43. Dossier du terminal dans l'infobulle des onglets

Plusieurs onglets portent souvent le même nom (le dossier du projet, « repo » trois fois dans l'instance de dev) : rien ne permettait de les distinguer sans les afficher.

- L'infobulle d'un onglet, dans la barre d'onglets comme dans le panneau des workspaces, donne maintenant le chemin complet du dossier de son terminal actif, suivi du nombre de panes (la barre d'onglets ne l'avait pas). Le chemin suit les `cd` (OSC 7).
- Vérifié dans l'instance de dev : infobulles des trois onglets, puis `cd ..` dans l'un d'eux → son infobulle donne le dossier parent.
- Polish d'affichage, pas de changement de spec.

### 44. Liens vers les chemins absolus contenant des espaces

Les erreurs de `dotnet build`, `tsc` ou PowerShell citent des chemins absolus comme `D:\Projects\Perso\Projet T\dock-terminal\src\X.cs(12,5)` : ton propre dépôt a une espace dans son chemin, et ces liens ne marchaient pas (limite connue de la section « Reste à faire »).

- Web (`fileLinks.ts`) : un second motif reconnaît un chemin absolu `X:\` dont les dossiers peuvent contenir des espaces (et des parenthèses, pour `Program Files (x86)`), le nom de fichier restant sans espace ; il n'est retenu que s'il contient une espace, et remplace alors les liens plus courts qui le recouvrent.
- Hôte (`EditorLocation.ResolveExisting`) : si ce chemin n'existe pas, parce qu'une phrase a été prise pour un chemin (« Build D:\Projects then src\file.cs »), la partie qui suit chaque espace est essayée à son tour, depuis le dossier du pane : `src\file.cs` s'ouvre comme avant.
- Vérifié : 11 cas passés à `findFileLinks` (erreur `dotnet build` avec deux chemins, `At C:\…\My Scripts\build.ps1:12`, `Program Files (x86)`, URL, prompt PowerShell non lié, phrase ambiguë…) ; 440 tests dont deux nouveaux pour le repli ; dans l'instance de dev, Ctrl + clic sur `…\Temp\dock essai\cible.txt:2:1` affiché par PowerShell → l'éditeur reçoit `-g "…\dock essai\cible.txt:2:1"`.
- **Convention proposée** de la section 8 de la spec mise à jour ; limite connue du journal réduite.

### 45. Corrections de la cinquième relecture (collage, notes, palette, diff)

Une relecture indépendante des itérations 37 à 43 a relevé 2 défauts moyens et 3 mineurs, tous corrigés ici.

- **Notes, Ctrl + Entrée sur une ligne sélectionnée entière** (moyen) : la sélection finissait par un saut de ligne, que le garde-fou du collage ne compte pas comme une ligne de plus ; la commande s'exécutait donc aussitôt. Les sauts de ligne finaux de la sélection sont retirés : la commande attend Entrée, comme promis.
- **Maj + Inser contournait la confirmation du collage multi-ligne** (moyen) : xterm.js laisse passer Maj + Inser, et le collage natif du navigateur allait droit au shell. Un écouteur `paste` posé en capture sur chaque terminal fait maintenant passer tout collage natif (Maj + Inser, menu Édition du système) par le même garde-fou. Ctrl + V annule déjà le collage natif : pas de double collage (vérifié).
- **Ctrl + P sous un dialogue de confirmation** (mineur) : la palette s'ouvrait cachée sous « Coller N lignes ? », les confirmations Git ou de suppression, et Entrée y lançait une commande invisible. Ctrl + P est ignoré tant qu'un de ces dialogues est ouvert (`confirmationOpen`, réutilisé par `modalOpen`).
- **Diff copié d'un fichier hors UTF-8** (mineur) : un `.ps1` en Windows-1252 donnait un patch aux caractères remplacés, refusé par `git apply`. L'hôte ne fournit plus de `patch` quand le décodage a perdu des caractères ; le bouton est grisé avec « Fichier hors UTF-8 : le diff copié ne s'appliquerait pas ».
- **Contrat du pont** (mineur) : `appearance{fontSize}` d'`app.hello`, `configuredBase?` du plan de worktree et les clés `updates` / `appearance` de l'export ajoutés à `docs/BACKEND_ARCHITECTURE.md`.
- Non retenu : Leader puis 8 en AZERTY belge sort le pane (8 y donne `!`), compromis voulu à l'itération 39 ; bornes 8 / 14 / 32 recopiées côté web pour la liste des tailles, l'hôte restant l'arbitre.
- Vérifié : 441 tests (dont un fichier Windows-1252 sans `patch`) ; dans l'instance de dev, événement `paste` natif de deux lignes → dialogue ; Ctrl + P pendant le dialogue → pas de palette, focus sur « Coller et exécuter » ; sélection « echo note-selection⏎ » puis Ctrl + Entrée → collée sans être exécutée ; Ctrl + V et Ctrl + Maj + V → un seul collage chacun.

### 46. Ouvrir un projet dans un onglet du workspace actif

Le sélecteur de projets (Leader puis F) créait toujours un nouveau workspace ; pour travailler sur deux projets liés (un front et son API) dans le même workspace, il fallait ouvrir un onglet puis faire `cd`.

- Maj + Entrée, ou Maj + clic, ouvre le projet (ou le worktree) choisi dans un nouvel onglet du workspace actif, nommé d'après le dossier. Entrée garde son comportement. Une ligne discrète en bas du sélecteur rappelle les deux gestes.
- `SearchDialog` accepte une action secondaire (`onRunAlternate`) et un pied de page (`footer`) ; la palette n'en utilise pas et ne change pas.
- Vérifié dans l'instance de dev, dossier des projets pointé temporairement sur le scratchpad : Maj + Entrée sur « xt » → onglet « xt » ajouté et affiché dans le workspace actif ; Entrée sur « gd » → nouveau workspace « gd ».
- **Convention proposée** en section 10 de la spec ; README mis à jour.

### 47. Même geste dans « Ouvrir un worktree »

Suite de l'itération 46, pour la cohérence : le sélecteur « Ouvrir un worktree » de la palette n'avait pas Maj + Entrée.

- Maj + Entrée, ou Maj + clic, ouvre le worktree choisi dans un nouvel onglet du workspace actif ; un worktree déjà ouvert dans un terminal reste simplement rejoint, comme avec Entrée. Même ligne d'aide en bas du sélecteur. Le sélecteur « Créer un worktree depuis… » n'est pas concerné.
- Au passage, le script de fermeture de l'instance de dev force l'arrêt de la seule instance lancée depuis le dépôt quand une confirmation de fermeture la bloque (elle verrouillait `Dock.Core.dll` et faisait échouer le build) ; ton Dock installé n'est jamais visé.
- Vérifié dans l'instance de dev (dossier des projets pointé temporairement sur le scratchpad, avec un faux `worktrees\essai-wt`) : Maj + Entrée sur « essai-wt » → onglet « essai-wt » ajouté au workspace actif.
- **Convention proposée** de la section 10 complétée.

### 48. Revenir au nom automatique d'un onglet

Un onglet renommé à la main ne suivait plus jamais son dossier (TAB-06a), et aucun geste ne permettait d'y revenir : il fallait le fermer et en rouvrir un.

- « Reprendre le nom du dossier » dans le menu d'un onglet (barre d'onglets et panneau des workspaces), et « Reprendre le nom du dossier pour l'onglet » dans la palette pour l'onglet actif : l'onglet reprend le nom du dossier de son terminal actif et le suit de nouveau aux `cd` suivants. Grisé (menus) ou absent (palette) quand le nom est déjà automatique.
- Vérifié dans l'instance de dev : onglet « essai-wt » renommé « mon-nom », puis menu → « Reprendre le nom du dossier » → « essai-wt » ; l'entrée est grisée sur un onglet au nom automatique.
- **Convention proposée** en section 6 de la spec, après TAB-06a.

### 49. Heure du message dans la barre de statut

La barre de statut garde son dernier message jusqu'au suivant, parfois des heures : « Collage annulé » ou « Réglages enregistrés » semblaient toujours récents.

- L'heure du message affiché (format du journal : `03:41:58`, précédée du jour si ce n'est pas aujourd'hui) s'affiche en gris à droite de la barre de statut, avec la date complète en infobulle. Masquée pendant une opération Git ou worktree, dont le libellé remplace le message.
- Vérifié dans l'instance de dev : « Session restaurée… » suivi de `03:41:58`, infobulle « Message du mercredi 30 septembre 2026 à 03:41:58 ».
- **Convention proposée** en section 4 de la spec (journal des messages).

### 50. Chemin relatif depuis la vue Git, README à jour

- Le menu d'un fichier modifié de la vue Git (un ou plusieurs sélectionnés) propose « Copier le chemin relatif » à côté de « Copier le chemin » : chemins relatifs à la racine du dépôt, un par ligne, à coller dans un agent (`@src/app.ts`) ou une commande. *Nuance relevée à l'itération 54 : la vue Git donne le format de Git (relatif à la racine du dépôt, barres obliques), l'arbre des fichiers le format Windows (relatif à sa racine, antislashs).*
- README complété des nouveautés 36 à 49 qui n'y figuraient pas : clignotement de la barre des tâches à la fin d'une commande longue, retour au nom automatique d'un onglet, diff copié applicable par `git apply`, chemin relatif depuis la vue Git.
- Vérifié : lint et build ; dans l'instance de dev, menu de `docs/TESTING.md` dans la vue Git → « Copier le chemin relatif » copie `docs/TESTING.md` et annonce « Chemin relatif copié. ».

### 51. Insérer le chemin d'un fichier modifié dans le terminal

Suite de l'itération 50 : l'arbre des fichiers sait insérer un chemin dans le terminal actif, pas la vue Git.

- Le menu d'un ou plusieurs fichiers modifiés de la vue Git propose « Insérer le chemin dans le terminal » : chemin complet de chaque fichier, protégé par l'hôte selon le shell (guillemets simples pour PowerShell et Git Bash, doubles pour CMD) et suivi d'une espace, exactement comme depuis l'arbre des fichiers ou par glisser-déposer. Pratique pour taper `git diff`, `code` ou une commande d'agent puis y ajouter les fichiers.
- Vérifié dans l'instance de dev : menu de `dossier avec espace/cible.txt` → `'C:\…\repo\dossier avec espace\cible.txt'` inséré après le prompt, non exécuté.

### 52. Ouvrir un terminal dans le dossier d'un fichier

Dans l'arbre des fichiers, seuls les dossiers proposaient « Ouvrir un terminal ici » : pour un fichier repéré au fond d'une arborescence, il fallait remonter à son dossier.

- Le menu d'un fichier propose « Ouvrir un terminal dans son dossier » (nouvel onglet dans le dossier parent), juste après « Ouvrir dans l'éditeur ».
- Vérifié dans l'instance de dev : menu de `README.md` → nouvel onglet « repo » ouvert et affiché.
- **Convention proposée** en section 4 de la spec (la liste « Retenu » du menu n'est pas modifiée).
- Une sixième relecture indépendante (itérations 44 à 51) tourne en parallèle.

### 53. « Tout replier » dans l'arbre des fichiers

Après avoir fouillé plusieurs sous-dossiers, l'arbre des fichiers restait déplié en profondeur ; il fallait replier chaque dossier à la main.

- Bouton « Tout replier » (deux chevrons qui se rejoignent) dans l'en-tête de l'arbre, entre « Nouveau dossier » et « Actualiser », comme dans VS Code : replie tous les dossiers dépliés sous la racine affichée, sans toucher à ceux d'un autre onglet. Grisé quand rien n'est déplié.
- Vérifié dans l'instance de dev : 15 lignes, dossier `web` déplié → 27 lignes, « Tout replier » → 15 lignes et bouton grisé.
- **Convention proposée** en section 4 de la spec.

### 54. Corrections de la sixième relecture (liens, vue Git, sélecteurs, barre de statut)

Une relecture indépendante des itérations 44 à 51 a relevé un défaut moyen et trois mineurs, tous corrigés.

- **Liens, régression de l'itération 44** (moyen) : dans « Modified C:\repo\a.ts and src\b.ts » (sortie courante des agents), le motif à espaces prenait `a.ts and src` pour un dossier et ne faisait plus qu'un lien, `C:\repo\a.ts` n'étant plus ouvrable. Un chemin à espaces est désormais écarté dès qu'un de ses « dossiers » contient une extension suivie d'une espace : les deux liens courts reviennent. Banc de 13 cas rejoué : les deux phrases de la relecture donnent de nouveau deux liens, les 11 anciens cas sont inchangés.
- **« Insérer le chemin dans le terminal » sur un fichier supprimé** : l'hôte refuse un chemin qui n'existe pas, avec un message parlant de dépôt. L'entrée ne porte plus que sur les fichiers existants (comme « Ouvrir dans l'éditeur ») et disparaît s'il n'y en a aucun.
- **Maj + Entrée sans workspace ouvert** (sélecteur de projets, « Ouvrir un worktree ») : le sélecteur se fermait sans rien ouvrir. Sans workspace actif, Maj + Entrée ouvre un workspace comme Entrée.
- **Heure de la barre de statut après minuit** : sans nouveau message, un message de 23:58 restait affiché sans le jour le lendemain. La barre se redessine à chaque minuit.
- Remarques traitées : le caractère de remplacement U+FFFD est écrit `'\uFFFD'` dans `GitDiffReader` au lieu du caractère littéral ; espace rétablie dans `URL_BEFORE =` ; nuance de format du chemin relatif ajoutée à l'itération 50. Non retenu : distinguer un vrai U+FFFD d'un décodage raté (cas rarissime).
- Vérifié : lint, build, 441 tests.

### 55. Point d'étape : « Reste à faire » et bilan HTML à jour

- Section « Reste à faire et idées » complétée des points ouverts depuis l'itération 38 : réglage éventuel pour couper la confirmation de collage, Leader puis 8 en AZERTY belge, format du chemin relatif copié, taille du texte en convention ; limites connues (liens avec extension dans un dossier, clignotement non observé, U+FFFD) ; idée de tests web étendue.
- Bilan HTML de la nuit régénéré sur le Bureau.

### 56. Raccourcis sur l'écran vide

L'écran « Aucun workspace ouvert » était le seul endroit sans raccourcis indiqués : un nouvel utilisateur n'y apprenait ni la palette ni le Leader.

- Infobulles des boutons complétées (« Nouveau workspace » : Ctrl + Maj + W ; « Ouvrir un projet… » : Leader puis F) et ligne d'aide discrète sous les boutons : « Ctrl + P ouvre la palette · Ctrl + Espace puis une lettre lance une commande au clavier ».
- Vérifié dans l'instance de dev : tous les workspaces fermés → écran vide avec la ligne d'aide ; Ctrl + P ouvre la palette et Leader puis F le sélecteur de projets depuis cet écran ; session rouverte ensuite par Ctrl + Maj + Z.

### 57. Longueur de la première ligne du message de commit

Rien n'indiquait qu'un message de commit dépassait la longueur usuelle de la première ligne (72 caractères), au-delà de laquelle GitHub, `git log --oneline` et la plupart des outils la coupent.

- Sous la zone de message de la vue Git, à droite d'« Amend du dernier commit », le nombre de caractères de la première ligne s'affiche en gris dès qu'elle n'est pas vide, en orange au-delà de 72, avec une infobulle qui explique la limite. Rien n'est bloqué.
- Vérifié dans l'instance de dev : « Corriger le lien » → `16` en gris ; message de 90 caractères → `90` en orange, infobulle « Première ligne de 90 caractères : au-delà de 72, elle est coupée par la plupart des outils Git ».
- Point à décider ajouté au « Reste à faire » : fermer un workspace de plus de cinq onglets en perd une partie (limite des cinq onglets fermés conservés), constaté pendant le test de l'itération 56.

### 58. Brouillon du message de commit gardé par dépôt

Passer sur un onglet d'un autre dépôt (ou hors dépôt) vidait le message de commit en cours de rédaction, sans retour possible : un message soigné était perdu au moindre changement d'onglet.

- Chaque dépôt garde son brouillon (en mémoire, pour la session de Dock) : en quittant un dépôt, le message non vide est mis de côté ; en y revenant, il est restauré. Un message vide efface le brouillon, donc un commit réussi ne laisse rien derrière lui. Le mode Amend n'est pas mis de côté (il repart décoché, comme avant).
- Vérifié dans l'instance de dev : « Brouillon à garder » saisi dans `repo`, Ctrl + Tab vers `xt` (hors dépôt), Ctrl + Tab retour → message restauré.

### 59. Paramètres : un clic à côté ne jette plus les modifications

Dans Paramètres, un clic hors du dialogue le fermait et abandonnait en silence toutes les modifications en cours (plusieurs chemins de shells, sons, dossiers…).

- Si des modifications sont en cours (formulaire différent des réglages enregistrés, ou préférences importées), le clic à côté ne ferme plus : le pied du dialogue affiche « Modifications non enregistrées : Enregistrer, ou Annuler pour les abandonner. ». Sans modification, le clic à côté ferme comme avant ; Échap et Annuler ferment toujours.
- Vérifié dans l'instance de dev : clic à côté sans modification → fermé ; taille du texte passée à 16 puis clic à côté → dialogue gardé avec le message ; Échap → fermé, `appearance.json` toujours à 14.
- **Convention proposée** en section 14 de la spec.

### 60. Formulaire de worktree : un clic à côté ne jette plus le nom saisi

Même défaut que l'itération 59 dans le formulaire « Créer un worktree » : un clic hors du dialogue fermait le formulaire et perdait le nom de la nouvelle branche.

- Une fois un nom de nouvelle branche saisi, le clic à côté ne ferme plus le formulaire ; Échap et Annuler le ferment toujours. Formulaire vide, ou mode « branche existante » (simple choix dans une liste) : comportement inchangé.
- Vérifié dans l'instance de dev : Leader puis N, clic à côté → fermé ; Leader puis N, « feat/essai », clic à côté → formulaire gardé ; Échap → fermé.
- Convention de la section 14 de la spec étendue à ce formulaire.

### 61. Afficher un fichier modifié dans l'arbre des fichiers

Depuis la vue Git, rien ne permettait de retrouver un fichier modifié dans l'arbre des fichiers (pour voir ses voisins, le renommer, en créer un à côté) : il fallait déplier les dossiers à la main.

- Le menu d'un fichier modifié (sélection unique, fichier non supprimé) propose « Afficher dans l'arbre des fichiers » : la vue Fichiers s'ouvre, les dossiers parents se déplient, le fichier est sélectionné et reçoit le focus dès que sa ligne est chargée. Hors du dossier affiché par l'arbre, un message l'explique.
- Au passage, deux espaces manquants après `=` rétablis (`openWorkspaceNotes`, `renewPaneIds`), perdus lors d'éditions de cette nuit.
- Vérifié dans l'instance de dev : menu de `docs/TESTING.md` → vue Fichiers, `docs` déplié, `TESTING.md` sélectionné et focalisé (le premier essai laissait le focus sur `.git`, corrigé par une attente de la ligne).
- La barre d'invite Git (nom de branche, tag, stash) a été vérifiée : elle ne se ferme que par Annuler ou Échap, pas de perte de saisie. Une septième relecture (itérations 52 à 60) tourne en parallèle.
- **Convention proposée** en section 4 de la spec.

### 62. Voir les modifications d'un fichier depuis l'arbre

L'inverse de l'itération 61 : l'arbre des fichiers marque les fichiers modifiés (M, A, U…) mais n'ouvrait pas leur diff.

- Le menu d'un fichier marqué propose « Voir les modifications » : la vue Git s'ouvre et affiche son diff, Unstaged s'il en a, sinon Staged. L'état Git n'étant chargé qu'à l'ouverture de la vue Git, le diff s'affiche dès que cet état arrive (3 secondes au plus, sinon un message l'explique).
- Vérifié dans l'instance de dev : menu de `README.md` (M) → vue Git ouverte sur le diff Unstaged de `README.md`. Le premier essai affichait « Aucune modification » parce que l'état Git n'était pas encore chargé ; corrigé par l'attente.
- **Convention proposée** complétée en section 4 de la spec.

### 63. Corrections de la septième relecture (brouillons, dialogues, liens)

Une relecture indépendante des itérations 52 à 60 a relevé un défaut moyen et cinq mineurs, tous corrigés.

- **Brouillon de commit pendant un commit** (moyen) : changer d'onglet pendant un « Commit et push » effaçait à la fin le message affiché, celui d'un autre dépôt, et restaurait plus tard le message déjà commité. Le dépôt du commit est retenu : à la fin, seul son message (affiché ou mis de côté) est vidé.
- **Brouillon et mode Amend** : un message saisi puis Amend coché était perdu au changement de dépôt, et un brouillon restauré puis commité pouvait revenir. Le brouillon d'un dépôt est retiré dès qu'il est restauré, et en mode Amend le message est mis de côté s'il diffère du dernier message de commit.
- **Focus après un clic à côté gardé** (Paramètres, formulaire de worktree) : le clic sur le fond retirait le focus du dialogue, et Ctrl + Entrée, Entrée ou Tab ne marchaient plus. Le clic gardé n'enlève plus le focus.
- **Paramètres « modifiés » à tort** après avoir tapé puis effacé un chemin de shell : la comparaison ignore les chemins vides (l'hôte ne les enregistre pas) ; le message « non enregistrées » disparaît quand on revient à l'état enregistré.
- **Liens de l'itération 54 trop stricts** : `C:\Tools\Node.js Apps\index.ts` ou `D:\Dev\ASP.NET Core\Program.cs(12,5)` n'ouvraient plus que le dossier tronqué. Le lien court porte désormais le chemin long en alternative (`files.openAt` : `alternative`, `alternativeLine`, `alternativeColumn`) ; l'hôte ouvre ce chemin long s'il existe exactement comme fichier (`EditorLocation.ExistingExactly`, sans repli après les espaces), sinon le chemin court. « Modified C:\repo\a.ts and src\b.ts » garde ses deux liens corrects.
- **« Insérer le chemin » et « Ouvrir dans l'éditeur »** proposés pour un fichier stagé puis supprimé du disque : exclus aussi.
- Cosmétique : compteur de la première ligne en caractères réels (un gitmoji compte pour un) ; double ligne vide retirée ; exemple faux de la limite connue (`v1.2 beta`) corrigé ; repli Maj + Entrée sans workspace ajouté à la spec.
- Vérifié : 443 tests (dont deux pour `ExistingExactly`) ; banc de liens (chemins `Node.js Apps`, `ASP.NET Core`, phrase à deux chemins, `Projet T`) ; dans l'instance de dev, aller-retour dans un chemin de shell puis clic à côté → Paramètres fermés, modification puis clic à côté → gardés avec le focus dedans, Échap → fermés.

### 64. Ouvrir la vue Git d'un clic sur la branche d'un terminal

L'en-tête de chaque terminal affiche sa branche Git, mais ce n'était qu'un texte : pour voir l'état du dépôt, il fallait passer par Ctrl + Maj + G ou le bouton du panneau.

- La branche de l'en-tête est maintenant un bouton : un clic sélectionne ce terminal et ouvre la vue Git sur son dépôt, en ouvrant le panneau de droite s'il était fermé ou en quittant la vue Fichiers / Notes. L'infobulle garde le résumé Git (branche, avance / retard) et indique le geste.
- Vérifié dans l'instance de dev : panneau fermé → clic sur « night-session » → panneau ouvert sur la vue Git ; vue Fichiers → clic → vue Git.
- Vérifié aussi de bout en bout le correctif de liens de l'itération 63 : `…\Temp\dock.js essai\cible.txt:3:1` affiché par PowerShell, Ctrl + clic sur la partie soulignée → l'éditeur reçoit `-g "…\dock.js essai\cible.txt:3:1"`.
- **Convention proposée** en section 4 de la spec.

### 65. README à jour des itérations 52 à 64, bilan HTML régénéré

- README complété : diff d'un fichier depuis l'arbre et bouton « Tout replier » (explorateur), clic sur la branche d'un terminal pour ouvrir la vue Git, affichage d'un fichier modifié dans l'arbre et brouillon de commit gardé par dépôt (vue Git).
- Bilan HTML de la nuit régénéré sur le Bureau.

### 66. F5 actualise l'arbre des fichiers

L'arbre des fichiers se met à jour tout seul, mais quand un dossier réseau ou un outil externe échappe à la surveillance, il fallait viser le petit bouton « Actualiser ».

- F5, dans l'arbre des fichiers, relit les dossiers affichés, comme dans l'Explorateur Windows ; le raccourci est indiqué dans le menu (« Actualiser F5 ») et l'infobulle du bouton. Les raccourcis du navigateur étant désactivés dans la WebView, F5 ne recharge jamais l'interface.
- Vérifié dans l'instance de dev : fichier créé sur le disque, focus dans l'arbre, F5 → l'interface n'est pas rechargée (une variable posée avant survit) et le fichier apparaît.
- Banc des 10 scénarios réels de copie de sortie rejoué avant cette itération : toujours justes.
- Erreur de ma part : le commit `9477342` porte le gitmoji ⌨️, absent de ta liste autorisée (il aurait dû être ✨). Déjà poussé, je ne l'ai pas réécrit pour ne pas forcer le push ; c'est le seul de la nuit hors liste (vérifié sur tout l'historique de la branche).

### 67. F5 actualise aussi la vue Git

Suite de l'itération 66, pour la cohérence : dans la vue Git, seul le bouton « Actualiser » relisait l'état du dépôt.

- F5, quand le focus est dans la vue Git (liste des modifications, branches, message de commit…), relit l'état du dépôt comme le bouton « Actualiser », dont l'infobulle indique désormais F5.
- Vérifié dans l'instance de dev : fichier créé sur le disque, focus sur une ligne de la vue Git, F5 → l'interface n'est pas rechargée et le fichier apparaît dans les modifications.

### 68. Messages de la barre de statut annoncés aux lecteurs d'écran

Le message de la barre de statut vit dans le bouton qui ouvre le journal : un lecteur d'écran (Narrateur, NVDA) ne l'annonçait pas quand il changeait, alors que c'est le seul retour de nombreuses actions (« Chemin copié », erreurs Git, collage annulé…).

- Une région `status` invisible (classe `sr-only`) reprend le message courant, ou le libellé d'une opération Git en cours, pour qu'il soit annoncé à chaque changement. Rien ne change à l'écran.
- Vérifié dans l'instance de dev : la région existe, est masquée (1 px, `overflow: hidden`) et passe de « Session restaurée… » à « Chemin copié : … » après un clic sur « Copier le chemin ».

## Reste à faire et idées

### À décider par toi

- **Raccourcis** (point 15 de la section 18 de la spec) : Alt + PgUp / PgDn sort de la règle « Ctrl + Maj + lettre ou Alt + flèche ». Leader puis `=`, `!` et Maj + flèche n'ont pas de raccourci direct.
- **`CLAUDE.md`** dit que deux tests lancent un vrai PowerShell 5.1 : ils sont maintenant 15 (les 13 de `PowerShellIntegrationTests` en plus). Je n'ai pas touché ton `CLAUDE.md`.
- **Le wrapper de prompt en fait plus** : il enveloppe `PSConsoleHostReadLine` de PSReadLine (comme VS Code) et annonce durée, succès et texte de chaque commande. PSReadLine est resté intact dans mes essais (coloration, continuation, historique). Si un module de ton profil redéfinit aussi `PSConsoleHostReadLine`, à surveiller.
- **Confirmation du collage multi-ligne** (itération 42) : toujours active dans un shell sans collage délimité. Windows Terminal propose un réglage pour la couper ; je n'en ai pas ajouté. À toi de dire si tu en veux un.
- **Leader puis un chiffre en AZERTY belge** : la touche 8 y donne `!`, donc Leader puis 8 sort le pane (Leader puis Maj + 8 affiche l'onglet 8). Compromis voulu pour que `!` marche sur tous les claviers.
- **Format du chemin relatif copié** : `src/app.ts` depuis la vue Git (format Git), `src\app.ts` depuis l'arbre des fichiers (format Windows). Harmoniser ou non ?
- **Fermer un workspace de plus de cinq onglets** : la décision « conserver les cinq derniers onglets fermés » fait que les onglets au-delà sont perdus sans retour (constaté en fermant trois workspaces de test : « Général » n'était plus restaurable). Une confirmation, ou une limite plus haute pour ce cas, serait à décider.
- **Taille du texte** (itération 41) : réglée en « Convention proposée » ; la police et un zoom rapide restent à décider (section 4 de la spec).

### Limites connues

- **ConPTY sous Windows 10** :
  - une ligne longue repliée est copiée en plusieurs lignes, faute d'indicateur de repli ;
  - un chemin de fichier replié sur deux lignes n'est pas cliquable ;
  - après un redimensionnement, l'invite repliée peut se redessiner de travers jusqu'à la commande suivante.
- **Copie de sortie et navigation entre commandes** : Windows PowerShell et PowerShell 7 seulement, pas CMD ni Git Bash. Une invite qui s'écrit elle-même par `Write-Host` au lieu de renvoyer son texte fausserait la hauteur annoncée.
- **Liens de fichiers** : depuis l'itération 44, un chemin absolu avec des espaces dans ses dossiers est lié ; un nom de fichier ou un chemin relatif avec espaces ne l'est toujours pas, ni un chemin replié sur deux lignes, et un chemin dont un dossier contient une extension suivie d'une espace (`Node.js Apps`, `ASP.NET Core`) n'est souligné que jusqu'à ce point (le Ctrl + clic ouvre quand même le fichier complet s'il existe, itération 63).
- **Clignotement de la barre des tâches** (itération 38) : la commande est bien reçue par l'hôte, mais le clignotement lui-même ne s'observe pas par le débogage distant ; à confirmer à l'usage.
- **Diff copié** : un fichier UTF-8 qui contient réellement le caractère de remplacement U+FFFD est traité comme mal décodé et n'est pas copiable (rarissime).

### Idées

- Zoom rapide du texte (Ctrl + molette, comme Windows Terminal) en plus du réglage de l'itération 41 : raccourci à décider (point 15 de la section 18).
- Tests web : aucun encore (décision du 21 septembre). Plusieurs fonctions pures ajoutées cette nuit s'y prêteraient : `findFileLinks` (le banc de 13 cas des itérations 44 et 54 en serait le point de départ), `equalizeNode`, `swapPanes`, `folderMarksOf`, `relativeEntryPath`, `formatCommandDuration`.
