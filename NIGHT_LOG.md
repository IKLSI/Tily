# Journal de la nuit du 29 au 30 septembre 2026

Session autonome sur la branche `night-session`. À chaque itération : une amélioration, vérifiée (`pnpm lint`, `pnpm build`, `dotnet build`, `dotnet test` et essai dans une instance de développement isolée), puis un commit.

Les essais se font dans un Dock de développement lancé avec son propre dossier de données (`DOCK_DATA_DIR` dans le scratchpad) et un port de débogage dédié (9247). Frappes, clics et captures passent uniquement par le protocole de débogage de la WebView2, après avoir vérifié que la page chargée est bien le bundle de ce build. Aucune action globale de souris ou de clavier, donc rien ne peut atteindre ton Dock. Les opérations Git se font dans un clone jetable du dépôt, dans le scratchpad.

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

## Reste à faire et idées

- Échanger deux panes, ou déplacer un pane vers un onglet existant.
- Après un redimensionnement (sortie d'un pane, panneau masqué…), la ligne d'invite PowerShell repliée par ConPTY peut se redessiner de travers jusqu'à la commande suivante. C'est un comportement de ConPTY au redimensionnement, pas propre à ces actions.
- Fins de commandes longues : CMD et Git Bash n'ont pas de wrapper de prompt, donc pas d'indication ; on pourrait aussi faire clignoter la barre des tâches quand la fenêtre de Dock n'a pas le focus, comme pour les agents.
- Sortie de la dernière commande : sous Windows 10, une ligne longue repliée par ConPTY est copiée en plusieurs lignes (pas d'indicateur de repli). Une invite qui s'écrit elle-même par `Write-Host` au lieu de renvoyer son texte fausserait la hauteur annoncée.
