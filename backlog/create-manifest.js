// Generates reviewable issue drafts. Does not contact GitHub.
const fs=require('fs');
const tasks=[
['F01','Interface Dock verte, compacte et sans contours de sélection','4, 16','R26','Retrouver une interface compacte donnant la priorité aux terminaux.',[
'Reprendre Dock vert et le panneau en arborescence.',
'Conserver en-tête compact, barre d’onglets et terminaux occupant la hauteur restante.',
'Identifier les éléments sélectionnés par un fond discret, sans bordure colorée sur onglets, workspaces et résultats.',
'Éviter les aides permanentes et popups pour les actions courantes ; fournir noms accessibles et focus perceptible.',
'Respecter la mise à l’échelle Windows et les noms/chemins longs.'
],[],'Police, zoom et dimensions minimales restent à préciser.',true],
['F02','Créer, sélectionner et renommer les workspaces inline','3, 5','R01, R02','Organiser des terminaux dans des groupes libres, sans couche projet.',[
'Disposer d’un workspace par défaut et interdire les terminaux sans workspace.',
'Créer un workspace directement depuis le panneau et éditer son nom inline.',
'Permettre des onglets dans des dossiers sans rapport entre eux.',
'Renommer depuis le titre ou la palette ; Entrée/perte de focus valide, Échap annule, nom vide conserve le précédent.',
'Synchroniser le nom et la sélection entre en-tête et panneau.'
],['F01'],'Nom automatique selon le dossier ; le nom manuel reste prioritaire. Fermer le dernier onglet supprime le workspace ; confirmer la suppression explicite si des onglets ou processus sont actifs, puis arrêter les processus.',true],
['F03','Naviguer dans les workspaces et onglets en arborescence','5','R07','Voir les onglets de chaque workspace et rejoindre une activité directement.',[
'Déplier et replier chaque workspace indépendamment avec un chevron accessible.',
'Cliquer sur un onglet enfant active son workspace, son onglet et son pane mémorisé.',
'Synchroniser noms, ordre et sélection avec la barre supérieure.',
'Conserver les états de dépliage ; replier ne change pas la sélection.',
'Afficher les nombres d’onglets et les attentes sans ajouter de bordures de sélection.'
],['F02'],'Le panneau reste centré sur les workspaces et non sur une liste indépendante d’agents.',true],
['F04','Replier et redimensionner le panneau des workspaces','5, 16','R08','Libérer la largeur des terminaux sans perdre le contexte.',[
'Replier/réafficher le panneau depuis un bouton toujours accessible ou la palette.',
'Redimensionner par séparateur et fournir un accès clavier.',
'Conserver la largeur après réouverture et mémoriser la visibilité.',
'Préserver les commandes en cours de saisie, processus et dispositions des panes.',
'Le panneau masqué ne conserve pas d’éléments focusables invisibles.'
],['F03'],'Les états du panneau sont sauvegardés avec la session.',true],
['F05','Ouvrir les onglets et choisir le shell au clic droit sur « + »','6','R03, R04','Ouvrir un terminal sans formulaire et choisir un autre shell au besoin.',[
'Clic gauche sur « + » ouvre directement PowerShell.',
'Clic droit sur le même bouton affiche PowerShell, CMD et Git Bash dans un menu contextuel.',
'Le menu accepte flèches, Entrée, Échap, clic extérieur et Maj + F10.',
'Le nouvel onglet reprend le dossier réel du pane actif.',
'Aucun bouton séparé de choix du shell ; garder une action équivalente dans la palette.'
],['F02','F11'],'Les chemins de shells sont configurés, pas codés avec des valeurs de démonstration.',true],
['F06','Renommer les onglets inline et préserver les noms manuels','6','R05','Nommer une activité directement dans la barre d’onglets.',[
'Double-clic sur le nom ou commande de palette active un éditeur inline.',
'Entrée/perte de focus valide, Échap annule et un nom vide est refusé sans perdre le nom précédent.',
'Synchroniser immédiatement le nom dans l’arborescence.',
'Ne jamais écraser un nom manuel par une mise à jour automatique.',
'Gérer les noms longs et la composition de texte sans validation prématurée.'
],['F05'],'Règles de nommage automatique et retour au nom automatique à décider.',true],
['F07','Réordonner et transférer les onglets entre workspaces','6','R06','Réorganiser une activité sans relancer ses terminaux.',[
'Glisser les onglets depuis la barre supérieure ou l’arborescence.',
'Réordonner dans un workspace ou transférer vers un autre.',
'Conserver processus actifs, dossiers, historique, noms et splits.',
'Synchroniser les deux représentations et sauvegarder le nouvel ordre.',
'Fournir un équivalent clavier pour les déplacements essentiels.'
],['F03','F05'],'Insertion avant la cible et cas du dernier onglet source à trancher ; ne pas imposer silencieusement un comportement non validé.',true],
['F08','Fermer et rouvrir un onglet fermé accidentellement','6, 13','R23','Récupérer un contexte fermé par erreur.',[
'Fermer l’onglet et gérer ses processus selon la politique de fermeture retenue.',
'Proposer la réouverture via les commandes.',
'Restaurer noms, shells, chemins, splits et texte avec de nouveaux processus.',
'Marquer l’ancienne session et ne pas rejouer de commandes.',
'Tester la fermeture du dernier onglet selon la décision documentée.'
],['F05','F17'],'Conserver les cinq derniers onglets fermés, y compris après redémarrage. La fermeture du dernier onglet supprime le workspace et affiche l’état vide si nécessaire.',true],
['F09','Créer, fermer et redimensionner des splits imbriqués','7','R09','Organiser plusieurs terminaux dans un même onglet.',[
'Créer des splits côte à côte et haut/bas depuis le pane actif.',
'Hériter du dossier réel du pane actif.',
'Supporter des divisions imbriquées avec séparateurs redimensionnables.',
'La fermeture retire un pane et agrandit la zone restante.',
'Sauvegarder orientations et proportions sans relancer les processus.'
],['F05'],'Le split hérite du shell et du dossier du pane actif ; fermer le dernier pane applique la politique de fermeture du dernier onglet.',true],
['F10','Naviguer au clavier et gérer le Leader sans perte de saisie','7, 9, 16','R09, R25','Piloter le terminal et ses vues au clavier.',[
'Activer un pane à la souris et naviguer entre panes au clavier.',
'Proposer les actions via une touche Leader et documenter les séquences retenues.',
'Gérer expiration, annulation et collisions avec les applications terminal.',
'Préserver les saisies lors des opérations de présentation.',
'Assurer un focus visible et le fonctionnement avec IME/composition.'
],['F09'],'Leader par défaut Ctrl + Espace, délai 5 secondes, mapping personnalisable ; navigation spatiale selon la position visuelle.',true],
['F11','Exécuter de vrais shells et charger le profil PowerShell','8','R12, R13, R14','Utiliser l’application comme un terminal Windows complet.',[
 'Lancer Windows PowerShell 5.1 par défaut ; proposer PowerShell 7, CMD et Git Bash lorsque les chemins sont configurés.',
 'Charger le vrai profil avec alias, fonctions, modules et prompt.',
 'Charger notamment wtr et rmwt depuis %USERPROFILE%\\Documents\\WindowsPowerShell\\Microsoft.PowerShell_profile.ps1.',
'Supporter commandes libres, sélection, copier/coller, couleurs, Unicode et outils plein écran.',
'Suivre le dossier courant réel après cd et fonctions du profil.',
'Obtenir ce dossier par une intégration shell propre à Dock (variable d’environnement dédiée et séquence OSC émise par le prompt), compatible avec le profil et oh-my-posh, sans simuler WezTerm.',
'Signaler localement un shell introuvable avec relance ou choix alternatif, sans substitution silencieuse.',
'Valider redimensionnement et touches de contrôle avec les outils réels.'
],[],'VS Code est l’éditeur retenu. Le profil observé est celui de Windows PowerShell 5.1 ; le profil PowerShell 7 n’a pas été trouvé dans Documents\\PowerShell et ne doit pas être supposé identique.',true],
['F12','Palette Ctrl + P avec navigation clavier et destinations','9','R10','Trouver une commande ou rejoindre une activité rapidement.',[
'Ouvrir avec Ctrl + P ; conserver Ctrl + Maj + P comme alias proposé.',
'Rechercher les commandes, workspaces, onglets et panes.',
'↑/↓ sélectionne, Entrée valide, Échap ferme ; faire défiler la sélection en vue.',
'Revenir au premier résultat après filtrage et ne rien exécuter sur liste vide.',
'Afficher la sélection par le fond uniquement et déclencher les éditeurs inline appropriés.',
'Restaurer le focus correctement sans perdre la saisie terminal.'
],['F03','F05'],'La navigation directe vers un pane fait partie de la cible.',true],
['F14','Ouvrir un workspace depuis le sélecteur de projets','10','R16','Trouver rapidement un dossier dans Projets.',[
 'Lire le vrai dossier C:\\Files\\Projects, pas une liste fictive.',
'Rechercher des dossiers et naviguer avec flèches, Entrée et Échap.',
'Créer un workspace avec son premier terminal dans le dossier choisi.',
'Conserver une interface compacte intégrée et l’accès par Leader/palette.',
'Traiter dossiers absents/inaccessibles sans modifier le contexte courant par erreur.'
],['F02','F11'],'Le sélecteur reprend le premier niveau de C:\\Files\\Projects, exclut worktrees de la liste des projets et crée toujours un nouveau workspace.',true],
['F15','Actions de dossier et de branche sur le pane actif','11','R17','Agir sur le contexte réel du terminal sélectionné.',[
'Copier le chemin réel et ouvrir le dossier dans l’éditeur configuré ou l’explorateur.',
'Copier la branche du dépôt/worktree courant lorsqu’elle existe.',
'Ouvrir un terminal supplémentaire dans ce dossier.',
'Indiquer le chemin ciblé ; ne pas supposer un dossier unique de workspace.',
'Gérer hors dépôt, HEAD détachée et chemins avec espaces sans valeur fictive.'
],['F11'],'Éditeur et paramètres d’ouverture à configurer ; aucune commande locale ne doit être composée naïvement avec les chemins.',true],
['F16','Préserver wtr/rmwt et synchroniser leur contexte','11','R15','Continuer à utiliser les fonctions worktree existantes depuis le terminal.',[
 'Prendre en compte la lecture du profil et la documentation de wtr/rmwt dans docs/inspection-environnement.md.',
'Pouvoir exécuter wtr et rmwt dans PowerShell.',
'Suivre leurs effets observables sur dossier courant et contexte Git.',
'Documenter puis vérifier le contrat de synchronisation avec l’interface.',
'Ne pas créer de cycle de vie automatique de workspaces non demandé.'
],['F11','F15'],'wtr crée un worktree sous C:\\Files\\Projects\\worktrees, installe les dépendances et peut répliquer une base ; rmwt supprime le worktree, une base répliquée et la branche sauf -KeepBranch. Ne pas reproduire ces opérations dans l’interface sans contrat explicite.',true],
['F17','Sauvegarder et restaurer automatiquement les sessions visuelles','13','R19, R21, R22','Retrouver l’environnement de travail à la réouverture.',[
'Sauvegarder automatiquement workspaces, onglets, ordre, noms, shells, chemins et splits.',
'Conserver sélections, largeur/visibilité du panneau et dépliage des groupes.',
'Restaurer le texte avec un séparateur explicite et démarrer des shells neufs.',
'Ne pas réexécuter les anciennes commandes ou rétablir de faux états actifs.',
'Résister à un arrêt inattendu ; signaler échec de sauvegarde et données corrompues sans écrasement silencieux.',
'Gérer dossiers disparus et shells indisponibles avec un état local compréhensible.'
],['F03','F04','F09','F11'],'Valeurs par défaut configurables : 10 000 lignes par pane, 256 Mio d’historique global, texte sauvegardé toutes les 30 secondes, cinq onglets fermés restaurables et historique conservé après redémarrage.',true],
['F18','Arrêter proprement les processus à la fermeture','8, 13','R20','Fermer l’application sans laisser volontairement agents et serveurs en arrière-plan.',[
'Définir le traitement des shells et de leurs processus enfants.',
'Rattacher chaque pane à un Job Object Windows afin que la fermeture arrête tout l’arbre de processus, y compris les enfants détachés du shell.',
'Appliquer la politique aux fermetures de pane, onglet et application.',
'Ne pas introduire de service persistant qui reprend les anciens agents.',
'Sauvegarder la disposition avant fermeture lorsque possible.',
'Tester avec serveur et agent actifs ainsi qu’un programme qui ne se termine pas immédiatement.'
],['F11','F17'],'Arrêt forcé. Demander une confirmation ciblée si un serveur, un agent ou un programme est actif, puis arrêter tous les processus concernés.',true],
['F19','Sauvegarder, exporter et importer les préférences','14','R24','Versionner la configuration et la réutiliser.',[
'Définir un format versionné pour shells, raccourcis, éditeur, Projets et préférences visuelles.',
'Séparer préférences, disposition et historique terminal.',
'Ne pas inclure implicitement le texte des terminaux dans l’export.',
'Valider l’import intégralement avant toute mutation ; conserver la session si invalide.',
'Définir migrations et politique de fusion/remplacement avec protection des données existantes.'
],['F17'],'JSON ; préférences, session et historique séparés ; import validé puis appliqué par remplacement. L’emplacement Windows exact reste une décision d’implémentation documentée.',true],
['F20','Afficher les états de Claude Code et Codex CLI dans les workspaces','12','R18','Repérer une activité nécessitant une intervention sans quitter la logique de workspaces.',[
'Supporter en cours, attente de réponse/autorisation, terminé, erreur et inconnu.',
'Rattacher les états au pane puis les résumer sur onglet/workspace.',
'Mettre en évidence les attentes et rejoindre le pane correspondant.',
'Ne pas déduire un état fiable de la simple présence d’un processus.',
'Ne pas restaurer des activités anciennes comme vivantes.'
],['F03','F11'],'Préparer des adaptateurs extensibles pour Claude Code (`claude`) et Codex CLI (`codex`) ; leur détection fiable est reportée à une évolution dédiée.',true],
['F21','Notifier les demandes d’attention sans interrompre le travail','12','R18','Être prévenu lorsqu’une intervention devient nécessaire.',[
'Émettre une notification discrète et ciblée pour une nouvelle attente.',
'Éviter les doublons et changements automatiques de workspace ou de focus.',
'Permettre de rejoindre précisément le pane concerné.',
'Définir le choix de destination lorsque plusieurs panes attendent.',
'Définir un comportement utile lorsque le panneau est replié.'
],['F20'],'Les canaux et événements seront définis avec les adaptateurs réels ; afficher « état inconnu » lorsque la source ne permet pas de conclure.',true],
['F22','Ouvrir les liens des terminaux par Ctrl + clic','8','R28','Ouvrir une adresse affichée par un programme sans avoir à la copier.',[
'Reconnaître les URL `http` et `https` présentes dans le texte des terminaux.',
'Reconnaître les hyperliens explicites émis par les programmes (séquence OSC 8).',
'Ouvrir un lien par Ctrl + clic uniquement ; le clic simple reste réservé à la sélection et au placement du curseur.',
'Ouvrir le lien dans le navigateur par défaut de Windows, jamais dans la fenêtre de Dock.',
'Ne jamais ouvrir un autre schéma que `http` et `https` et le signaler dans la barre de statut.',
'Souligner le lien au survol pour montrer qu’il est actif.'
],['F11'],'Ctrl + clic est retenu par l’utilisateur. La détection des URL et des hyperliens OSC 8, l’ouverture dans le navigateur par défaut et la restriction à `http` / `https` sont des conventions proposées.',false],
['F23','Explorer les fichiers du pane actif dans un panneau à droite','4','R29','Parcourir et gérer les fichiers du dossier courant sans quitter Dock.',[
'Afficher un panneau explorateur à droite de la zone de travail, fermé par défaut.',
'Ouvrir et fermer l’explorateur par un bouton ; mémoriser son état par onglet.',
'Afficher le dossier courant du pane actif et le suivre lors d’un `cd` ou d’un changement de pane.',
'Ouvrir un fichier dans l’éditeur configuré.',
'Créer, renommer et supprimer des fichiers et des dossiers ; supprimer vers la corbeille après confirmation.',
'Rendre le panneau redimensionnable et navigable au clavier.'
],['F11','F15'],'Le panneau à droite, fermé par défaut, l’état par onglet, le suivi du pane actif, l’ouverture dans l’éditeur et la gestion des fichiers sont retenus par l’utilisateur. L’emplacement du bouton, le double-clic, la corbeille, le redimensionnement et les touches sont des conventions proposées.',false],
['F24','Gérer Git visuellement depuis le panneau de droite','11','R30','Visualiser les branches et effectuer les opérations Git courantes sans taper de commande.',[
'Ajouter une vue Git au panneau de droite, qui bascule entre Fichiers et Git avec le même état par onglet et la même largeur.',
'Afficher les branches locales et distantes avec avance et retard, et le graphe de l’historique avec étiquettes ; montrer les fichiers et le diff d’un commit.',
'Indexer et retirer les modifications fichier par fichier avec un diff intégré ; committer, modifier le dernier commit, « Commit et push ».',
'Pousser, tirer, récupérer et fusionner ; créer, basculer, renommer et supprimer des branches ; gérer stash et tags.',
'Rebaser sans mode interactif, cherry-picker et faire un reset vers un commit du graphe.',
'Annuler la dernière opération faite depuis Dock tant qu’elle n’a pas été poussée.',
'Lister les fichiers en conflit, les ouvrir dans l’éditeur, les marquer résolus, terminer ou annuler l’opération.',
'Ne proposer le push forcé qu’après un refus, avec `--force-with-lease` et confirmation.'
],['F11','F15','F23'],'Le panneau de droite partagé avec l’explorateur, la liste des branches et le graphe, l’indexation par fichier avec diff intégré, les fonctions incluses, l’annulation de la dernière opération, la gestion des conflits dans l’éditeur et le push forcé encadré sont retenus par l’utilisateur. Le suivi du pane actif, la mise à jour en direct, les raccourcis, l’exécution de `git`, les confirmations et le chargement paginé sont des conventions proposées. Pull requests, rebase interactif, éditeur de conflits intégré et gestion des worktrees restent hors périmètre.',false]
,
['T01','Valider la pile Windows et le pipeline de terminal','8, 15','R27','Valider la pile retenue avant l’implémentation complète : hôte C# .NET 10 (WinUI 3), une seule WebView2 portant toute l’interface avec xterm.js, ConPTY et Job Objects côté hôte.',[
'Créer un spike C# .NET 10 LTS avec une fenêtre WinUI 3 (Windows App SDK) hébergeant une WebView2 unique ; l’hôte ne définit aucun KeyboardAccelerator.',
'Afficher dans cette WebView2 une interface minimale (deux panes, changement de pane) et un terminal xterm.js par pane avec renderer WebGL et repli canvas.',
'Lancer Windows PowerShell 5.1 dans ConPTY avec le profil réel et transmettre entrée, sortie, redimensionnement et fermeture.',
'Vérifier Unicode, couleurs, sélection, copier/coller, IME et clavier français (AltGr, touches mortes) dans xterm.js sous WebView2.',
'Vérifier le clavier de bout en bout : Ctrl + Espace puis touche, Ctrl + P, Ctrl + C vers le shell, Alt + F4, changement de pane à la souris puis frappe immédiate, sans perte ni doublon.',
'Obtenir le dossier courant réel après cd et après wtr par une intégration shell propre à Dock (variable d’environnement dédiée et séquence OSC), avec oh-my-posh chargé et sans variable WezTerm.',
'Rattacher chaque pane à un Job Object et vérifier qu’aucun processus enfant ne survit à la fermeture du pane.',
'Mesurer le pont hôte / WebView2 sur un flux de sortie soutenu de plusieurs Mo avec deux panes actifs ; comparer PostWebMessage à un canal dédié si le débit décroche.',
'Comparer la ConPTY intégrée à Windows et une conpty.dll embarquée (OpenConsole) sur le redimensionnement et les applications plein écran.',
'Produire un installeur Windows autonome à mise à jour manuelle, sans auto-updater, qui détecte ou installe le runtime WebView2 Evergreen et embarque le runtime Windows App SDK ; tester sur une machine vierge.',
'Documenter les versions minimales de Windows, les mesures et les limites découvertes ; confirmer WinUI 3 ou acter le repli WPF pour l’hôte.'
],[],'Décision prise : interface entière dans une seule WebView2, hôte natif sans interface métier. Alternatives écartées : interface hybride XAML + WebView2 par pane (clavier et focus partagés), contrôle Windows Terminal (aucun paquet officiel WinUI 3), Electron, Tauri 2. La distribution recommandée est un installeur Inno Setup pour l’application dépaquetée ; MSIX reste une option ultérieure. Le spike doit confirmer ou invalider ce choix avant le développement produit.\n\n**Résultat (21 septembre 2026) :** pile confirmée, aucun repli WPF. Spike, harnais, mesures et limites dans le [compte rendu du spike](https://github.com/MaximeRazafinjato/dock-terminal/blob/b1c648454c311b51a731118aea84b98d10bea1ac/spike/README.md) : 14 scénarios automatisés passés sur ConPTY Windows et OpenConsole, PostWebMessage retenu (pont non limitant, ConPTY borne à 10 à 12 Mo/s), installeur Inno Setup autonome produit et testé.',true]
];
const manifest=tasks.map(([id,title,sections,tests,goal,checks,deps,open,done])=>({id,title:`[${id}] ${title}`,dependencies:deps,body:`## Besoin utilisateur\n\n${goal}\n\n## Critères d’acceptation\n\n${checks.map(c=>(done?'- [x] ':'- [ ] ')+c).join('\n')}\n\n## Références\n\n- [Spécifications complètes](https://github.com/MaximeRazafinjato/dock-terminal/blob/main/specifications-terminal.md), sections ${sections}.\n- Recette : ${tests}.\n\n## Dépendances fonctionnelles\n\n${deps.length?deps.join(', '):'Aucune dépendance fonctionnelle imposée.'}\n\n## Points à préciser\n\n${open}\n\nAucune priorité ni échéance attribuée.\n`}));
manifest.push({id:'D01',title:'[D01] Trancher les comportements fonctionnels encore ouverts',dependencies:[],body:`## Objectif\n\nDocumenter les décisions restantes sans transformer les conventions proposées en exigences validées.\n\n## Décisions à consigner\n\n- [x] Dernier onglet/pane et fermeture ou suppression des workspaces.\n- [x] Noms automatiques et priorité aux noms manuels.\n- [x] Navigation spatiale et raccourcis Leader : Ctrl + Espace, 5 secondes, personnalisables.\n- [x] Dossier Projets : C:\\Files\\Projects, premier niveau, sans détection de projet déjà ouvert.\n- [x] Limites de conservation du texte, sauvegarde et réouverture d’onglets.\n- [x] Arrêt forcé des processus et confirmations ciblées si une activité est détectée.\n- [x] Recherche dans les terminaux retirée du périmètre.\n- [x] Extensibilité Claude Code/Codex CLI ; intégration réelle reportée à une évolution dédiée.\n- [x] JSON, séparation préférences/session/historique et import par remplacement.\n\n## Critère de fin\n\nChaque décision est reportée dans les spécifications et les issues concernées. Les points nécessitant l’inspection du profil et de WezTerm sont documentés.\n\nRéférence : section 18 des [spécifications](https://github.com/MaximeRazafinjato/dock-terminal/blob/main/specifications-terminal.md). Le spike technique T01 est réalisé et la pile est confirmée (voir le [compte rendu du spike](https://github.com/MaximeRazafinjato/dock-terminal/blob/b1c648454c311b51a731118aea84b98d10bea1ac/spike/README.md)).\n`});
fs.writeFileSync('backlog/issues.json',JSON.stringify(manifest,null,2)+'\n');
for(const issue of manifest)fs.writeFileSync(`backlog/${issue.id}.md`,issue.body);
fs.writeFileSync('BACKLOG.md','# Backlog fonctionnel\n\n'+manifest.length+' tâches sans ordre de priorité : 23 fonctionnalités, une tâche technique et une tâche de clarification. F13 est retirée du périmètre. Les dépendances expriment des relations fonctionnelles, pas un planning.\n\n| ID | Tâche | Dépendances |\n| --- | --- | --- |\n'+manifest.map(i=>`| ${i.id} | [${i.title}](backlog/${i.id}.md) | ${i.dependencies.join(', ')||'—'} |`).join('\n')+'\n\nLes liens vers les issues GitHub seront ajoutés après publication.\n');
console.log(`${manifest.length} issue drafts generated.`);
