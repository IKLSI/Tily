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

## Reste à faire et idées

- Déplacer un pane dans un nouvel onglet (équivalent du `break-pane` de tmux), ou échanger deux panes.
