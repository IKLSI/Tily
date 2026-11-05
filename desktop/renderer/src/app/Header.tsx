import { memo } from 'react'
import { EditableName } from '../components/EditableName'
import { Icon } from '../components/Icon'
import { IconName } from '../components/iconName'
import { InlineNameEditor } from '../components/InlineNameEditor'
import { useFullscreen } from '../components/useFullscreen'
import { LeaderHints } from '../features/keyboard/components/LeaderHints'
import { Command } from '../features/keyboard/commands'
import { useShortcutTip } from '../features/keyboard/shortcutLabels'
import { UpdateButton } from '../features/updates/components/UpdateButton'

interface HeaderProps {
  workspaceName: string | null
  renaming: boolean
  sidebarCollapsed: boolean
  leaderActive: boolean
  onToggleSidebar: () => void
  onOpenSettings: () => void
  onStartRename: () => void
  onCommitRename: (name: string) => void
  onCancelRename: () => void
}

export const Header = memo(function Header({ workspaceName, renaming, sidebarCollapsed, leaderActive, onToggleSidebar, onOpenSettings, onStartRename, onCommitRename, onCancelRename }: HeaderProps) {
  const sidebarTip = useShortcutTip('Afficher les workspaces', Command.ToggleSidebar)
  const fullscreen = useFullscreen()
  const settingsTip = useShortcutTip('Paramètres', Command.Settings)
  return (
    <header className={`relative flex h-[44px] shrink-0 items-center gap-3 border-b border-tily-line bg-tily-panel pr-3 text-tily-ink [-webkit-app-region:drag] ${fullscreen ? 'pl-3' : 'pl-[84px]'}`}>
      {sidebarCollapsed && (
        <button
          type="button"
          aria-label="Afficher les workspaces"
          className="flex h-[28px] cursor-pointer items-center rounded-tily-sm px-2 text-tily-muted [-webkit-app-region:no-drag] hover:bg-tily-green-hover hover:text-tily-ink"
          data-tip={sidebarTip}
          onClick={onToggleSidebar}
        >
          <Icon name={IconName.Sidebar} size={14} />
        </button>
      )}
      <div className="flex min-w-0 items-baseline gap-4 [-webkit-app-region:no-drag]">
        {(sidebarCollapsed || renaming) &&
          (workspaceName === null ? (
            <span className="text-[15px] text-tily-muted">Aucun workspace</span>
          ) : renaming ? (
            <InlineNameEditor value={workspaceName} label="Nom du workspace" className="w-64 text-[15px]" onCommit={onCommitRename} onCancel={onCancelRename} />
          ) : (
            <EditableName name={workspaceName} className="text-[15px]" onClick={onStartRename} />
          ))}
      </div>
      {leaderActive && <LeaderHints />}
      <div className="ml-auto flex shrink-0 items-center gap-2 [-webkit-app-region:no-drag]">
        <UpdateButton />
        <button
          type="button"
          aria-label="Paramètres"
          className="flex h-[28px] cursor-pointer items-center rounded-tily-sm px-2 text-tily-muted [-webkit-app-region:no-drag] hover:bg-tily-green-hover hover:text-tily-ink"
          data-tip={settingsTip}
          onClick={onOpenSettings}
        >
          <Icon name={IconName.Settings} size={14} />
        </button>
      </div>
    </header>
  )
})
