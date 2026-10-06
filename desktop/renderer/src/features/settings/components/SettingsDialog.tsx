import { useEffect, useRef, useState, type ChangeEvent, type KeyboardEvent, type MouseEvent, type PointerEvent } from 'react'
import { PickTarget, type AttentionKind, type ImportedPreferences, type NotificationSettings, type PersistenceSettings, type PickedPath, type Settings, type SettingsSnapshot, type TerminalFont } from '../../../bridge/messages'
import type { WorktreeProjectFolder, WorktreeSettings } from '../../../bridge/worktreeMessages'
import { revealInExplorer } from '../../explorer/fileExplorerActions'
import { useHostStore } from '../../../stores/hostStore'
import { keepTabInside } from '../../../components/focusTrap'
import { InfoTip } from '../../../components/InfoTip'
import { AgentSettingsSection } from './AgentSettingsSection'
import { AppearanceSettingsSection } from './AppearanceSettingsSection'
import { BrowseButton } from './BrowseButton'
import { McpSettingsSection } from './McpSettingsSection'
import { NotificationSettingsSection } from './NotificationSettingsSection'
import { NUMBER_FIELDS, numberTextOf, valueWithin, type NumberTexts } from './persistenceFields'
import { PersistenceSettingsSection } from './PersistenceSettingsSection'
import { ProjectSettingsSection } from './ProjectSettingsSection'
import { SETTINGS_HINT, SETTINGS_INPUT, SETTINGS_LABEL, SETTINGS_PRIMARY, SETTINGS_ROW, SETTINGS_SECONDARY } from './settingsStyles'
import { ShellSettingsSection } from './ShellSettingsSection'
import { UpdateSettingsSection } from './UpdateSettingsSection'

interface SettingsDialogProps {
  snapshot: SettingsSnapshot | null
  pickedPath: PickedPath | null
  imported: ImportedPreferences | null
  onClose: () => void
  onSave: (settings: Settings) => void
  onPick: (field: string, target: PickTarget) => void
  onExport: () => void
  onImport: () => void
  onInstallHooks: () => void
  onRemoveHooks: () => void
  onTestNotification: (notifications: NotificationSettings, kind: AttentionKind) => void
}

const SHELL_FIELD_PREFIX = 'shell:'
const EDITOR_FIELD = 'editor'
const PROJECTS_ROOT_FIELD = 'projectsRoot'
const WORKTREE_FOLDER_FIELD = 'worktreeFolder'
const SOUND_FIELD = 'notificationSound'
const DONE_SOUND_FIELD = 'notificationDoneSound'
const EDITOR_ID = 'settings-editor'

const SECTION = 'text-[11px] font-semibold tracking-wide text-tily-muted uppercase'

const comparable = (settings: Settings): Settings => ({ ...settings, shells: Object.fromEntries(Object.entries(settings.shells).filter(([, path]) => path.trim().length > 0)) })

export function SettingsDialog({ snapshot, pickedPath, imported, onClose, onSave, onPick, onExport, onImport, onInstallHooks, onRemoveHooks, onTestNotification }: SettingsDialogProps) {
  const version = useHostStore((state) => state.version)
  const [draft, setDraft] = useState<Settings | null>(null)
  const [seenSnapshot, setSeenSnapshot] = useState<SettingsSnapshot | null>(null)
  const [seenPick, setSeenPick] = useState<PickedPath | null>(pickedPath)
  const [seenImport, setSeenImport] = useState<ImportedPreferences | null>(imported)
  const [importSource, setImportSource] = useState<string | null>(null)
  const [importWarnings, setImportWarnings] = useState<string[]>([])
  const [numberTexts, setNumberTexts] = useState<NumberTexts>({})
  const [closeHeld, setCloseHeld] = useState(false)
  const dialogRef = useRef<HTMLDivElement>(null)
  const numberInputsRef = useRef<Partial<Record<keyof PersistenceSettings, HTMLInputElement | null>>>({})
  if (snapshot !== seenSnapshot) {
    setSeenSnapshot(snapshot)
    setDraft(snapshot ? structuredClone(snapshot.settings) : null)
    setImportSource(null)
    setImportWarnings([])
    setNumberTexts({})
  }
  if (imported !== seenImport) {
    setSeenImport(imported)
    if (imported && snapshot) {
      setDraft(structuredClone(imported.settings))
      setImportSource(imported.path)
      setImportWarnings(imported.warnings)
      setNumberTexts({})
    }
  }

  useEffect(() => {
    const handleDocumentKeyDown = (event: globalThis.KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault()
        onClose()
      }
    }
    document.addEventListener('keydown', handleDocumentKeyDown)
    return () => document.removeEventListener('keydown', handleDocumentKeyDown)
  }, [onClose])

  const loaded = draft !== null
  useEffect(() => {
    if (loaded) {
      dialogRef.current?.querySelector<HTMLInputElement>('input')?.focus()
    }
  }, [loaded])

  const unsaved = Boolean(draft && snapshot && (importSource !== null || JSON.stringify(comparable(draft)) !== JSON.stringify(comparable(snapshot.settings))))
  if (closeHeld && !unsaved) {
    setCloseHeld(false)
  }
  const handleBackdropMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget && unsaved) {
      event.preventDefault()
    }
  }
  const handleBackdropPointerDown = (event: PointerEvent<HTMLDivElement>) => {
    if (event.target !== event.currentTarget) {
      return
    }
    if (unsaved) {
      setCloseHeld(true)
    } else {
      onClose()
    }
  }
  const invalidNumber = draft ? NUMBER_FIELDS.find((field) => valueWithin(numberTextOf(numberTexts, draft.persistence, field), field) === null) : undefined
  const handleSave = () => {
    if (invalidNumber) {
      numberInputsRef.current[invalidNumber.key]?.focus()
    } else if (draft) {
      onSave(draft)
    }
  }
  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === 'Enter' && event.ctrlKey) {
      event.preventDefault()
      handleSave()
    } else {
      keepTabInside(event)
    }
  }
  const updateDraft = (patch: Partial<Settings>) => setDraft((current) => (current ? { ...current, ...patch } : current))
  const updateWorktrees = (patch: Partial<WorktreeSettings>) => setDraft((current) => (current ? { ...current, worktrees: { ...current.worktrees, ...patch } } : current))
  if (pickedPath !== seenPick) {
    setSeenPick(pickedPath)
    if (pickedPath && draft) {
      if (pickedPath.field === EDITOR_FIELD) {
        updateDraft({ editor: pickedPath.path })
      } else if (pickedPath.field === PROJECTS_ROOT_FIELD) {
        updateDraft({ projectsRoot: pickedPath.path })
      } else if (pickedPath.field === WORKTREE_FOLDER_FIELD) {
        updateWorktrees({ folder: pickedPath.path })
      } else if (pickedPath.field === SOUND_FIELD) {
        updateDraft({ notifications: { ...draft.notifications, sound: pickedPath.path } })
      } else if (pickedPath.field === DONE_SOUND_FIELD) {
        updateDraft({ notifications: { ...draft.notifications, doneSound: pickedPath.path } })
      } else if (pickedPath.field.startsWith(SHELL_FIELD_PREFIX)) {
        updateDraft({ shells: { ...draft.shells, [pickedPath.field.slice(SHELL_FIELD_PREFIX.length)]: pickedPath.path } })
      }
    }
  }
  const handlePickEditor = () => onPick(EDITOR_FIELD, PickTarget.File)
  const handlePickProjectsRoot = () => onPick(PROJECTS_ROOT_FIELD, PickTarget.Folder)
  const handlePickWorktreeFolder = () => onPick(WORKTREE_FOLDER_FIELD, PickTarget.Folder)
  const handlePickShell = (shellId: string) => onPick(`${SHELL_FIELD_PREFIX}${shellId}`, PickTarget.File)
  const handleShellChange = (shellId: string, path: string) => setDraft((current) => (current ? { ...current, shells: { ...current.shells, [shellId]: path } } : current))
  const handleEditorChange = (event: ChangeEvent<HTMLInputElement>) => updateDraft({ editor: event.target.value })
  const handleProjectsRootChange = (projectsRoot: string) => updateDraft({ projectsRoot })
  const handleWorktreeFoldersChange = (worktreeFolders: WorktreeProjectFolder[]) => updateDraft({ worktreeFolders })
  const handleAutoFetchChange = (event: ChangeEvent<HTMLInputElement>) => updateDraft({ git: { autoFetch: event.target.checked } })
  const handleNumberTextChange = (key: keyof PersistenceSettings, text: string) => setNumberTexts((current) => ({ ...current, [key]: text }))
  const handlePersistenceChange = (persistence: PersistenceSettings) => updateDraft({ persistence })
  const updateNotifications = (patch: Partial<NotificationSettings>) => setDraft((current) => (current ? { ...current, notifications: { ...current.notifications, ...patch } } : current))
  const handlePickSound = () => onPick(SOUND_FIELD, PickTarget.Sound)
  const handlePickDoneSound = () => onPick(DONE_SOUND_FIELD, PickTarget.Sound)
  const handleTestNotification = (kind: AttentionKind) => {
    if (draft) {
      onTestNotification(draft.notifications, kind)
    }
  }
  const handleAutoCheckChange = (autoCheck: boolean) => updateDraft({ updates: { autoCheck } })
  const updateAppearance = (patch: Partial<Settings['appearance']>) => setDraft((current) => (current ? { ...current, appearance: { ...current.appearance, ...patch } } : current))
  const handleFontSizeChange = (fontSize: number) => updateAppearance({ fontSize })
  const handleFontFamilyChange = (fontFamily: TerminalFont) => updateAppearance({ fontFamily })
  const handleRevealFiles = () => {
    const shellsFile = snapshot?.files.shells
    if (shellsFile) {
      revealInExplorer(shellsFile)
    }
  }

  const renderBody = (settings: Settings, current: SettingsSnapshot) => (
    <>
      {importSource && <p className="rounded border border-tily-green/50 bg-tily-paper px-3 py-2 text-[12px] text-tily-green">Préférences lues depuis {importSource}. Rien n’est écrit tant que vous n’enregistrez pas ; Enregistrer remplace la configuration actuelle.</p>}
      {importWarnings.length > 0 && (
        <ul className="rounded border border-tily-warning/50 bg-tily-paper px-3 py-2 text-[12px] text-tily-warning">
          {importWarnings.map((warning) => (
            <li key={warning}>{warning}</li>
          ))}
        </ul>
      )}
      {current.warnings.length > 0 && (
        <ul className="rounded border border-tily-warning/50 bg-tily-paper px-3 py-2 text-[12px] text-tily-warning">
          {current.warnings.map((warning) => (
            <li key={warning}>{warning}</li>
          ))}
        </ul>
      )}
      <ShellSettingsSection sectionClassName={SECTION} shellSettings={current.shellSettings} shells={settings.shells} onShellChange={handleShellChange} onPickShell={handlePickShell} />
      <AppearanceSettingsSection sectionClassName={SECTION} fontSize={settings.appearance.fontSize} fontFamily={settings.appearance.fontFamily} onFontSizeChange={handleFontSizeChange} onFontFamilyChange={handleFontFamilyChange} />
      <section className="flex flex-col gap-2">
        <h3 className={SECTION}>Éditeur</h3>
        <div className="flex flex-col gap-1">
          <label htmlFor={EDITOR_ID} className={SETTINGS_LABEL}>
            Commande d’ouverture d’un dossier
          </label>
          <span className="flex gap-1">
            <input id={EDITOR_ID} type="text" className={SETTINGS_INPUT} value={settings.editor} placeholder="code" spellCheck={false} onChange={handleEditorChange} />
            <BrowseButton tip="Choisir l’exécutable de l’éditeur" onClick={handlePickEditor} />
          </span>
        </div>
      </section>
      <PersistenceSettingsSection sectionClassName={SECTION} persistence={settings.persistence} numberTexts={numberTexts} inputs={numberInputsRef} onNumberTextChange={handleNumberTextChange} onPersistenceChange={handlePersistenceChange} />
      <ProjectSettingsSection sectionClassName={SECTION} settings={settings} onProjectsRootChange={handleProjectsRootChange} onPickProjectsRoot={handlePickProjectsRoot} onWorktreesChange={updateWorktrees} onWorktreeFoldersChange={handleWorktreeFoldersChange} onPickWorktreeFolder={handlePickWorktreeFolder} />
      <section className="flex flex-col gap-2">
        <h3 className={SECTION}>Git</h3>
        <span className={SETTINGS_ROW}>
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={settings.git.autoFetch} onChange={handleAutoFetchChange} />
            <span className={SETTINGS_LABEL}>Fetch automatique à l’ouverture de la vue Git</span>
          </label>
          <InfoTip text="Lance git fetch --all à l’ouverture de la vue Git et quand le pane actif passe à un autre dépôt, au plus une fois toutes les 5 minutes par dépôt. Un échec (hors ligne, authentification) reste silencieux." />
        </span>
      </section>
      <NotificationSettingsSection sectionClassName={SECTION} notifications={settings.notifications} availability={current.notifications} onChange={updateNotifications} onPickSound={handlePickSound} onPickDoneSound={handlePickDoneSound} onTest={handleTestNotification} />
      <AgentSettingsSection sectionClassName={SECTION} agents={current.agents} onInstallHooks={onInstallHooks} onRemoveHooks={onRemoveHooks} />
      <McpSettingsSection sectionClassName={SECTION} mcp={current.mcp} />
      <UpdateSettingsSection sectionClassName={SECTION} autoCheck={settings.updates.autoCheck} onAutoCheckChange={handleAutoCheckChange} />
    </>
  )

  return (
    <div className="absolute inset-0 z-30 flex items-start justify-center bg-tily-paper/60 pt-[6vh]" onPointerDown={handleBackdropPointerDown} onMouseDown={handleBackdropMouseDown}>
      <div ref={dialogRef} role="dialog" aria-label="Paramètres" className="flex max-h-[86vh] w-[640px] max-w-[94vw] flex-col rounded-lg border border-tily-line bg-tily-panel shadow-xl" onKeyDown={handleKeyDown}>
        <div className="flex items-center justify-between border-b border-tily-line px-4 py-3">
          <h2 className="text-[15px] font-semibold text-tily-ink">
            Paramètres
            {version && <span className="ml-2 text-[12px] font-normal text-tily-muted">{`Tily ${version}`}</span>}
          </h2>
          <span className={SETTINGS_HINT}>Ctrl + Entrée enregistre · Échap ferme</span>
        </div>
        <div className="flex min-h-0 flex-1 flex-col gap-5 overflow-y-auto px-4 py-4">
          {draft && snapshot ? renderBody(draft, snapshot) : <p className={SETTINGS_HINT}>Chargement des réglages…</p>}
        </div>
        <div className="flex items-center gap-2 border-t border-tily-line px-4 py-3">
          <button type="button" className={SETTINGS_SECONDARY} data-tip="Lit un fichier de préférences JSON et remplit le formulaire sans rien écrire" onClick={onImport}>
            Importer…
          </button>
          <button type="button" className={SETTINGS_SECONDARY} data-tip="Écrit la configuration enregistrée (sans les modifications en cours) dans un fichier JSON versionné" onClick={onExport}>
            Exporter…
          </button>
          <button type="button" className={SETTINGS_SECONDARY} aria-disabled={!snapshot} data-tip="Ouvre le Finder sur le dossier des fichiers de réglages, pour les sauvegarder ou les modifier à la main" onClick={handleRevealFiles}>
            Afficher les fichiers
          </button>
          <span role="status" className="ml-auto text-[11px] text-tily-warning">
            {closeHeld && unsaved ? 'Modifications non enregistrées : Enregistrer, ou Annuler pour les abandonner.' : ''}
          </span>
          <button type="button" className={SETTINGS_SECONDARY} onClick={onClose}>
            Annuler
          </button>
          <button type="button" className={SETTINGS_PRIMARY} aria-disabled={!draft || invalidNumber !== undefined} data-tip={invalidNumber ? `${invalidNumber.label} : entre ${invalidNumber.min} et ${invalidNumber.max}` : 'Écrit les fichiers de réglages et applique immédiatement'} onClick={handleSave}>
            Enregistrer
          </button>
        </div>
      </div>
    </div>
  )
}
