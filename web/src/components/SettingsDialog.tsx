import { useEffect, useRef, useState, type ChangeEvent, type KeyboardEvent, type PointerEvent } from 'react'
import { NotificationSound, PickTarget, type ImportedPreferences, type NotificationSettings, type PersistenceSettings, type PickedPath, type Settings, type SettingsSnapshot } from '../bridge/messages'
import { keepTabInside } from './focusTrap'

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
  onTestNotification: (notifications: NotificationSettings) => void
}

const SHELL_FIELD_PREFIX = 'shell:'
const EDITOR_FIELD = 'editor'
const PROJECTS_ROOT_FIELD = 'projectsRoot'
const SOUND_FIELD = 'notificationSound'
const CUSTOM_SOUND = 'custom'

const isCustomSound = (sound: string): boolean => !Object.values<string>(NotificationSound).includes(sound)

interface NumberField {
  key: keyof PersistenceSettings
  label: string
  hint: string
  min: number
  max: number
}

type NumberTexts = Partial<Record<keyof PersistenceSettings, string>>

const INTEGER_PATTERN = /^\d+$/

const valueWithin = (text: string, field: NumberField): number | null => {
  const value = INTEGER_PATTERN.test(text) ? Number.parseInt(text, 10) : Number.NaN
  return value >= field.min && value <= field.max ? value : null
}

const NUMBER_FIELDS: NumberField[] = [
  { key: 'textIntervalSeconds', label: 'Sauvegarde du texte (secondes)', hint: 'Intervalle entre deux écritures du texte des terminaux.', min: 5, max: 600 },
  { key: 'linesPerPane', label: 'Lignes conservées par pane', hint: 'Historique xterm.js ; s’applique aux terminaux lancés après l’enregistrement.', min: 500, max: 100000 },
  { key: 'maxTextMebibytes', label: 'Historique global maximal (Mio)', hint: 'Au-delà, les panes les plus récents ne sont plus sauvegardés.', min: 16, max: 2048 },
]

const SOUND_LABELS: Record<NotificationSound, string> = {
  [NotificationSound.None]: 'Aucun',
  [NotificationSound.Default]: 'Notification par défaut',
  [NotificationSound.InstantMessage]: 'Message instantané',
  [NotificationSound.Mail]: 'Courrier',
  [NotificationSound.Reminder]: 'Rappel',
  [NotificationSound.Sms]: 'SMS',
}

const SECTION = 'text-[11px] font-semibold tracking-wide text-dock-muted uppercase'
const LABEL = 'text-[12px] text-dock-ink'
const HINT = 'text-[11px] text-dock-muted'
const INPUT_BASE = 'w-full rounded border bg-dock-paper px-2 py-1.5 font-mono text-[12px] text-dock-ink outline-none placeholder:text-dock-muted'
const INPUT = `${INPUT_BASE} border-dock-line focus:border-dock-focus`
const INVALID_INPUT = `${INPUT_BASE} border-dock-error focus:border-dock-error`
const INVALID_HINT = 'text-[11px] text-dock-error'
const BUTTON = 'cursor-pointer rounded border px-3 py-1.5 text-[12px]'
const PRIMARY = `${BUTTON} border-dock-green text-dock-green-deep hover:bg-dock-green-soft`
const SECONDARY = `${BUTTON} border-dock-line text-dock-ink hover:bg-dock-green-hover`
const BROWSE = 'shrink-0 rounded border border-dock-line px-2 text-[12px] text-dock-muted hover:bg-dock-green-hover hover:text-dock-ink'

export function SettingsDialog({ snapshot, pickedPath, imported, onClose, onSave, onPick, onExport, onImport, onInstallHooks, onRemoveHooks, onTestNotification }: SettingsDialogProps) {
  const [draft, setDraft] = useState<Settings | null>(null)
  const [seenSnapshot, setSeenSnapshot] = useState<SettingsSnapshot | null>(null)
  const [seenPick, setSeenPick] = useState<PickedPath | null>(pickedPath)
  const [seenImport, setSeenImport] = useState<ImportedPreferences | null>(imported)
  const [importSource, setImportSource] = useState<string | null>(null)
  const [importWarnings, setImportWarnings] = useState<string[]>([])
  const [numberTexts, setNumberTexts] = useState<NumberTexts>({})
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

  const handleBackdropPointerDown = (event: PointerEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) {
      onClose()
    }
  }
  const numberTextOf = (settings: Settings, field: NumberField): string => numberTexts[field.key] ?? String(settings.persistence[field.key])
  const invalidNumber = draft ? NUMBER_FIELDS.find((field) => valueWithin(numberTextOf(draft, field), field) === null) : undefined
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
  if (pickedPath !== seenPick) {
    setSeenPick(pickedPath)
    if (pickedPath && draft) {
      if (pickedPath.field === EDITOR_FIELD) {
        updateDraft({ editor: pickedPath.path })
      } else if (pickedPath.field === PROJECTS_ROOT_FIELD) {
        updateDraft({ projectsRoot: pickedPath.path })
      } else if (pickedPath.field === SOUND_FIELD) {
        updateDraft({ notifications: { ...draft.notifications, sound: pickedPath.path } })
      } else if (pickedPath.field.startsWith(SHELL_FIELD_PREFIX)) {
        updateDraft({ shells: { ...draft.shells, [pickedPath.field.slice(SHELL_FIELD_PREFIX.length)]: pickedPath.path } })
      }
    }
  }
  const handlePickEditor = () => onPick(EDITOR_FIELD, PickTarget.File)
  const handlePickProjectsRoot = () => onPick(PROJECTS_ROOT_FIELD, PickTarget.Folder)
  const renderBrowse = (onClick: () => void, tip: string) => (
    <button type="button" className={BROWSE} aria-label={tip} data-tip={tip} onClick={onClick}>
      …
    </button>
  )
  const handleEditorChange = (event: ChangeEvent<HTMLInputElement>) => updateDraft({ editor: event.target.value })
  const handleProjectsRootChange = (event: ChangeEvent<HTMLInputElement>) => updateDraft({ projectsRoot: event.target.value })
  const updateNotifications = (patch: Partial<NotificationSettings>) => setDraft((current) => (current ? { ...current, notifications: { ...current.notifications, ...patch } } : current))
  const handleToastChange = (event: ChangeEvent<HTMLInputElement>) => updateNotifications({ windowsToast: event.target.checked })
  const handleFlashChange = (event: ChangeEvent<HTMLInputElement>) => updateNotifications({ taskbarFlash: event.target.checked })
  const handleSoundChange = (event: ChangeEvent<HTMLSelectElement>) => updateNotifications({ sound: event.target.value === CUSTOM_SOUND ? '' : event.target.value })
  const handleSoundPathChange = (event: ChangeEvent<HTMLInputElement>) => updateNotifications({ sound: event.target.value })
  const handlePickSound = () => onPick(SOUND_FIELD, PickTarget.Sound)
  const handleTestNotification = () => {
    if (draft) {
      onTestNotification(draft.notifications)
    }
  }

  const renderBody = (settings: Settings, current: SettingsSnapshot) => (
    <>
      {importSource && <p className="rounded border border-dock-green/50 bg-dock-paper px-3 py-2 text-[12px] text-dock-green">Préférences lues depuis {importSource}. Rien n’est écrit tant que vous n’enregistrez pas ; Enregistrer remplace la configuration actuelle.</p>}
      {importWarnings.length > 0 && (
        <ul className="rounded border border-dock-warning/50 bg-dock-paper px-3 py-2 text-[12px] text-dock-warning">
          {importWarnings.map((warning) => (
            <li key={warning}>{warning}</li>
          ))}
        </ul>
      )}
      {current.warnings.length > 0 && (
        <ul className="rounded border border-dock-warning/50 bg-dock-paper px-3 py-2 text-[12px] text-dock-warning">
          {current.warnings.map((warning) => (
            <li key={warning}>{warning}</li>
          ))}
        </ul>
      )}
      <section className="flex flex-col gap-2">
        <h3 className={SECTION}>Shells</h3>
        <p className={HINT}>Vide = chemin par défaut. Un chemin introuvable est enregistré mais le shell reste indisponible.</p>
        {current.shellSettings.map((shell) => {
          const handleChange = (event: ChangeEvent<HTMLInputElement>) => updateDraft({ shells: { ...settings.shells, [shell.id]: event.target.value } })
          const handlePick = () => onPick(`${SHELL_FIELD_PREFIX}${shell.id}`, PickTarget.File)
          return (
            <label key={shell.id} className="flex flex-col gap-1">
              <span className="flex items-baseline justify-between">
                <span className={LABEL}>{shell.name}</span>
                <span className={`text-[11px] ${shell.available ? 'text-dock-green' : 'text-dock-warning'}`}>{shell.available ? 'Disponible' : 'Introuvable'}</span>
              </span>
              <span className="flex gap-1">
                <input type="text" className={INPUT} value={settings.shells[shell.id] ?? ''} placeholder={shell.defaultExecutable} spellCheck={false} onChange={handleChange} />
                {renderBrowse(handlePick, `Choisir l’exécutable de ${shell.name}`)}
              </span>
            </label>
          )
        })}
        <p className={`${HINT} font-mono`}>{current.files.shells}</p>
      </section>
      <section className="flex flex-col gap-2">
        <h3 className={SECTION}>Éditeur</h3>
        <label className="flex flex-col gap-1">
          <span className={LABEL}>Commande d’ouverture d’un dossier</span>
          <span className="flex gap-1">
            <input type="text" className={INPUT} value={settings.editor} placeholder="code.cmd" spellCheck={false} onChange={handleEditorChange} />
            {renderBrowse(handlePickEditor, 'Choisir l’exécutable de l’éditeur')}
          </span>
        </label>
        <p className={`${HINT} font-mono`}>{current.files.editor}</p>
      </section>
      <section className="flex flex-col gap-2">
        <h3 className={SECTION}>Persistance</h3>
        {NUMBER_FIELDS.map((field) => {
          const text = numberTextOf(settings, field)
          const invalid = valueWithin(text, field) === null
          const keepInput = (input: HTMLInputElement | null) => {
            numberInputsRef.current[field.key] = input
          }
          const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
            const next = event.target.value
            setNumberTexts((current) => ({ ...current, [field.key]: next }))
            const value = valueWithin(next, field)
            if (value !== null) {
              updateDraft({ persistence: { ...settings.persistence, [field.key]: value } })
            }
          }
          return (
            <label key={field.key} className="flex flex-col gap-1">
              <span className={LABEL}>{field.label}</span>
              <input type="number" className={invalid ? INVALID_INPUT : INPUT} value={text} min={field.min} max={field.max} aria-invalid={invalid} ref={keepInput} onChange={handleChange} />
              <span className={invalid ? INVALID_HINT : HINT}>{`${field.hint} Entre ${field.min} et ${field.max}.`}</span>
            </label>
          )
        })}
        <p className={`${HINT} font-mono`}>{current.files.persistence}</p>
      </section>
      <section className="flex flex-col gap-2">
        <h3 className={SECTION}>Projets</h3>
        <label className="flex flex-col gap-1">
          <span className={LABEL}>Dossier racine des projets</span>
          <span className="flex gap-1">
            <input type="text" className={INPUT} value={settings.projectsRoot} spellCheck={false} onChange={handleProjectsRootChange} />
            {renderBrowse(handlePickProjectsRoot, 'Choisir le dossier des projets')}
          </span>
          <span className={HINT}>Dossiers de premier niveau listés par le sélecteur de projets, hors « worktrees » et dossiers cachés.</span>
        </label>
        <p className={`${HINT} font-mono`}>{current.files.projects}</p>
      </section>
      <section className="flex flex-col gap-2">
        <h3 className={SECTION}>Notifications</h3>
        <p className={HINT}>Quand un agent a besoin de vous et que Dock n’est pas la fenêtre active. Les cartes dans Dock restent toujours affichées.</p>
        <label className="flex items-center gap-2">
          <input type="checkbox" checked={settings.notifications.windowsToast} disabled={!current.notifications.toastAvailable} onChange={handleToastChange} />
          <span className={LABEL}>Notification Windows (cliquer rejoint le terminal)</span>
        </label>
        {!current.notifications.toastAvailable && <p className="text-[11px] text-dock-warning">{`Notification Windows indisponible dans cette version du Windows App SDK (bug connu des applications autonomes). Le son et le clignotement restent actifs. ${current.notifications.toastError ?? ''}`}</p>}
        <label className="flex items-center gap-2">
          <input type="checkbox" checked={settings.notifications.taskbarFlash} onChange={handleFlashChange} />
          <span className={LABEL}>Faire clignoter Dock dans la barre des tâches</span>
        </label>
        <label className="flex flex-col gap-1">
          <span className={LABEL}>Son joué à chaque nouvelle attente</span>
          <span className="flex gap-2">
            <select className={INPUT} value={isCustomSound(settings.notifications.sound) ? CUSTOM_SOUND : settings.notifications.sound} onChange={handleSoundChange}>
              {Object.values(NotificationSound).map((sound) => (
                <option key={sound} value={sound}>
                  {SOUND_LABELS[sound]}
                </option>
              ))}
              <option value={CUSTOM_SOUND}>Fichier .wav de mon ordinateur…</option>
            </select>
            <button type="button" className={SECONDARY} data-tip="Joue le son, fait clignoter la barre des tâches et affiche la notification Windows si elle est disponible, avec les réglages ci-dessus, sans enregistrer" onClick={handleTestNotification}>
              Tester
            </button>
          </span>
          {isCustomSound(settings.notifications.sound) && (
            <span className="flex gap-1">
              <input type="text" className={INPUT} value={settings.notifications.sound} placeholder="C:\Sons\attention.wav" spellCheck={false} onChange={handleSoundPathChange} />
              {renderBrowse(handlePickSound, 'Choisir un fichier .wav')}
            </span>
          )}
          {isCustomSound(settings.notifications.sound) && <span className={HINT}>Fichier .wav uniquement (chemin absolu). Un chemin invalide revient au son par défaut à l’enregistrement.</span>}
        </label>
        <p className={`${HINT} font-mono`}>{current.files.notifications}</p>
      </section>
      <section className="flex flex-col gap-2">
        <h3 className={SECTION}>Agents</h3>
        <p className={HINT}>Claude Code signale ses états (en cours, en attente, terminé, en erreur) par des hooks qui exécutent le script ci-dessous. Sans hooks, un processus claude ou codex est affiché « État inconnu ».</p>
        <p className={`${HINT} font-mono`}>{current.agents.script}</p>
        <p className={HINT}>{`Les états sont écrits dans ${current.agents.stateDirectory} et purgés à chaque nouveau terminal. Les hooks ne sont ajoutés ou retirés de ${current.agents.settingsFile} que sur votre clic ; les autres réglages et hooks de ce fichier sont conservés.`}</p>
        <span className="flex items-center gap-3">
          <span className={`text-[11px] ${current.agents.hooksInstalled ? 'text-dock-green' : 'text-dock-warning'}`}>{current.agents.hooksInstalled ? 'Hooks installés' : 'Hooks non installés'}</span>
          {current.agents.hooksInstalled ? (
            <button type="button" className={SECONDARY} data-tip="Retire les hooks Dock de settings.json de Claude Code, sans toucher au reste" onClick={onRemoveHooks}>
              Retirer les hooks
            </button>
          ) : (
            <button type="button" className={PRIMARY} data-tip="Ajoute les hooks Dock dans settings.json de Claude Code (fusion, effet aux prochaines sessions claude)" onClick={onInstallHooks}>
              Installer les hooks
            </button>
          )}
        </span>
      </section>
    </>
  )

  return (
    <div className="absolute inset-0 z-30 flex items-start justify-center bg-dock-paper/60 pt-[6vh]" onPointerDown={handleBackdropPointerDown}>
      <div ref={dialogRef} role="dialog" aria-label="Paramètres" className="flex max-h-[86vh] w-[640px] max-w-[94vw] flex-col rounded-lg border border-dock-line bg-dock-panel shadow-xl" onKeyDown={handleKeyDown}>
        <div className="flex items-center justify-between border-b border-dock-line px-4 py-3">
          <h2 className="text-[15px] font-semibold text-dock-ink">Paramètres</h2>
          <span className={HINT}>Ctrl + Entrée enregistre · Échap ferme</span>
        </div>
        <div className="flex min-h-0 flex-1 flex-col gap-5 overflow-y-auto px-4 py-4">
          {draft && snapshot ? renderBody(draft, snapshot) : <p className={HINT}>Chargement des réglages…</p>}
        </div>
        <div className="flex items-center gap-2 border-t border-dock-line px-4 py-3">
          <button type="button" className={SECONDARY} data-tip="Lit un fichier de préférences JSON et remplit le formulaire sans rien écrire" onClick={onImport}>
            Importer…
          </button>
          <button type="button" className={SECONDARY} data-tip="Écrit la configuration enregistrée (sans les modifications en cours) dans un fichier JSON versionné" onClick={onExport}>
            Exporter…
          </button>
          <button type="button" className={`${SECONDARY} ml-auto`} onClick={onClose}>
            Annuler
          </button>
          <button type="button" className={PRIMARY} aria-disabled={!draft || invalidNumber !== undefined} data-tip={invalidNumber ? `${invalidNumber.label} : entre ${invalidNumber.min} et ${invalidNumber.max}` : 'Écrit les quatre fichiers et applique immédiatement'} onClick={handleSave}>
            Enregistrer
          </button>
        </div>
      </div>
    </div>
  )
}
