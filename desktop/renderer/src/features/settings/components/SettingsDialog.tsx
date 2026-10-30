import { useEffect, useRef, useState, type KeyboardEvent, type MouseEvent, type PointerEvent } from 'react'
import { PickTarget, type AttentionKind, type ImportedPreferences, type NotificationSettings, type PersistenceSettings, type PickedPath, type Settings, type SettingsSnapshot, type ShortcutSettings, type TerminalFont } from '../../../bridge/messages'
import type { WorktreeProjectFolder, WorktreeSettings } from '../../../bridge/worktreeMessages'
import { revealInExplorer } from '../../explorer/fileExplorerActions'
import { useHostStore } from '../../../stores/hostStore'
import { keepTabInside } from '../../../components/focusTrap'
import { Icon } from '../../../components/Icon'
import { IconName } from '../../../components/iconName'
import { AgentSettingsSection } from './AgentSettingsSection'
import { NotificationSettingsSection } from './NotificationSettingsSection'
import { NUMBER_FIELDS, numberFieldId, numberTextOf, valueWithin, type NumberField, type NumberTexts } from './persistenceFields'
import { ProjectSettingsSection } from './ProjectSettingsSection'
import { SettingsNav } from './SettingsNav'
import { SETTINGS_SECTION_LABELS, SettingsSection, settingsTabId } from './settingsSections'
import { SETTINGS_HINT, SETTINGS_ICON_BUTTON, SETTINGS_PRIMARY, SETTINGS_SECONDARY } from './settingsStyles'
import { ShellSettingsSection } from './ShellSettingsSection'
import { ShortcutSettingsSection } from './ShortcutSettingsSection'
import { TerminalSettingsSection } from './TerminalSettingsSection'
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
const PANEL_ID = 'settings-panel'

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
  const [section, setSection] = useState(SettingsSection.Shells)
  const dialogRef = useRef<HTMLDivElement>(null)
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
      dialogRef.current?.querySelector<HTMLInputElement>('[role="tabpanel"] input')?.focus()
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
      setSection(SettingsSection.Terminals)
      const fieldId = numberFieldId(invalidNumber.key)
      requestAnimationFrame(() => document.getElementById(fieldId)?.focus())
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
  const handleEditorChange = (editor: string) => updateDraft({ editor })
  const handleProjectsRootChange = (projectsRoot: string) => updateDraft({ projectsRoot })
  const handleWorktreeFoldersChange = (worktreeFolders: WorktreeProjectFolder[]) => updateDraft({ worktreeFolders })
  const handleAutoFetchChange = (autoFetch: boolean) => updateDraft({ git: { autoFetch } })
  const handleNumberTextChange = (key: keyof PersistenceSettings, text: string) => setNumberTexts((current) => ({ ...current, [key]: text }))
  const handleNumberChange = (field: NumberField, text: string) => {
    handleNumberTextChange(field.key, text)
    const value = valueWithin(text, field)
    if (draft && value !== null) {
      updateDraft({ persistence: { ...draft.persistence, [field.key]: value } })
    }
  }
  const updateNotifications = (patch: Partial<NotificationSettings>) => setDraft((current) => (current ? { ...current, notifications: { ...current.notifications, ...patch } } : current))
  const handlePickSound = () => onPick(SOUND_FIELD, PickTarget.Sound)
  const handlePickDoneSound = () => onPick(DONE_SOUND_FIELD, PickTarget.Sound)
  const handleTestNotification = (kind: AttentionKind) => {
    if (draft) {
      onTestNotification(draft.notifications, kind)
    }
  }
  const handleShortcutsChange = (shortcuts: ShortcutSettings) => updateDraft({ shortcuts })
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

  const renderSection = (settings: Settings, current: SettingsSnapshot) => {
    const title = SETTINGS_SECTION_LABELS[section]
    switch (section) {
      case SettingsSection.Shells:
        return <ShellSettingsSection title={title} shellSettings={current.shellSettings} shells={settings.shells} editor={settings.editor} onShellChange={handleShellChange} onEditorChange={handleEditorChange} onPickShell={handlePickShell} onPickEditor={handlePickEditor} />
      case SettingsSection.Terminals:
        return <TerminalSettingsSection title={title} fontSize={settings.appearance.fontSize} fontFamily={settings.appearance.fontFamily} numberText={(field) => numberTextOf(numberTexts, settings.persistence, field)} onFontSizeChange={handleFontSizeChange} onFontFamilyChange={handleFontFamilyChange} onNumberChange={handleNumberChange} />
      case SettingsSection.Shortcuts:
        return <ShortcutSettingsSection title={title} shortcuts={settings.shortcuts} defaults={current.shortcutDefaults} onChange={handleShortcutsChange} />
      case SettingsSection.Projects:
        return <ProjectSettingsSection title={title} projectsRoot={settings.projectsRoot} worktrees={settings.worktrees} worktreeFolders={settings.worktreeFolders} autoFetch={settings.git.autoFetch} onProjectsRootChange={handleProjectsRootChange} onWorktreesChange={updateWorktrees} onWorktreeFoldersChange={handleWorktreeFoldersChange} onAutoFetchChange={handleAutoFetchChange} onPickProjectsRoot={handlePickProjectsRoot} onPickWorktreeFolder={handlePickWorktreeFolder} />
      case SettingsSection.Notifications:
        return <NotificationSettingsSection title={title} notifications={settings.notifications} onChange={updateNotifications} onPickSound={handlePickSound} onPickDoneSound={handlePickDoneSound} onTest={handleTestNotification} />
      case SettingsSection.Agents:
        return <AgentSettingsSection title={title} agents={current.agents} mcp={current.mcp} onInstallHooks={onInstallHooks} onRemoveHooks={onRemoveHooks} />
      case SettingsSection.Updates:
        return <UpdateSettingsSection title={title} autoCheck={settings.updates.autoCheck} onAutoCheckChange={handleAutoCheckChange} />
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
      <div key={section}>{renderSection(settings, current)}</div>
    </>
  )

  return (
    <div className="absolute inset-0 z-30 flex items-start justify-center bg-tily-paper/60 pt-[6vh]" onPointerDown={handleBackdropPointerDown} onMouseDown={handleBackdropMouseDown}>
      <div ref={dialogRef} role="dialog" aria-label="Paramètres" className="flex h-[min(86vh,680px)] w-[780px] max-w-[94vw] flex-col rounded-lg border border-tily-line bg-tily-panel shadow-xl" onKeyDown={handleKeyDown}>
        <div className="flex items-baseline justify-between border-b border-tily-line px-4 py-3">
          <h2 className="text-[15px] font-semibold text-tily-ink">
            Paramètres
            {version && <span className="ml-2 text-[12px] font-normal text-tily-muted">{`· Tily ${version}`}</span>}
          </h2>
          <span className={SETTINGS_HINT}>Ctrl + Entrée enregistre · Échap ferme</span>
        </div>
        <div className="flex min-h-0 flex-1">
          <SettingsNav section={section} panelId={PANEL_ID} onSelect={setSection} />
          <div id={PANEL_ID} role="tabpanel" aria-labelledby={settingsTabId(section)} className="flex min-w-0 flex-1 flex-col gap-4 overflow-y-auto px-5 py-4">
            {draft && snapshot ? renderBody(draft, snapshot) : <p className={SETTINGS_HINT}>Chargement des réglages…</p>}
          </div>
        </div>
        <div className="flex items-center gap-2 border-t border-tily-line px-4 py-3">
          <button type="button" className={SETTINGS_ICON_BUTTON} aria-label="Importer" data-tip="Importer : lit un fichier de préférences JSON et remplit le formulaire sans rien écrire" onClick={onImport}>
            <Icon name={IconName.Pull} />
          </button>
          <button type="button" className={SETTINGS_ICON_BUTTON} aria-label="Exporter" data-tip="Exporter : écrit la configuration enregistrée (sans les modifications en cours) dans un fichier JSON versionné" onClick={onExport}>
            <Icon name={IconName.Push} />
          </button>
          <button type="button" className={SETTINGS_ICON_BUTTON} aria-label="Afficher les fichiers" aria-disabled={!snapshot} data-tip="Afficher les fichiers : ouvre le Finder sur le dossier des fichiers de réglages, pour les sauvegarder ou les modifier à la main" onClick={handleRevealFiles}>
            <Icon name={IconName.Folder} />
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
