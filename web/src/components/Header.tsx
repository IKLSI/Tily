import type { ReactNode } from 'react'
import { EditableName } from './EditableName'
import { InlineNameEditor } from './InlineNameEditor'
import { LeaderHints } from './LeaderHints'
import { UpdateButton } from './UpdateButton'

interface HeaderProps {
  workspaceName: string | null
  renaming: boolean
  sidebarCollapsed: boolean
  leaderActive: boolean
  navigation: ReactNode
  onToggleSidebar: () => void
  onOpenSettings: () => void
  onStartRename: () => void
  onCommitRename: (name: string) => void
  onCancelRename: () => void
}

export function Header({ workspaceName, renaming, sidebarCollapsed, leaderActive, navigation, onToggleSidebar, onOpenSettings, onStartRename, onCommitRename, onCancelRename }: HeaderProps) {
  return (
    <header className="relative flex h-[42px] shrink-0 items-center gap-4 border-b border-dock-line bg-dock-panel px-3 text-dock-ink">
      <button
        type="button"
        className="cursor-pointer rounded border border-dock-line px-2 text-lg leading-tight text-dock-muted hover:bg-dock-green-hover hover:text-dock-ink"
        data-tip={sidebarCollapsed ? 'Afficher les workspaces (Ctrl + Maj + B)' : 'Masquer les workspaces (Ctrl + Maj + B)'}
        onClick={onToggleSidebar}
      >
        ☰
      </button>
      <div className="flex min-w-0 items-baseline gap-4">
        <span className="text-[17px] font-semibold text-dock-green">Dock</span>
        {!navigation &&
          (workspaceName === null ? (
            <span className="text-[16px] text-dock-muted">Aucun workspace</span>
          ) : renaming ? (
            <InlineNameEditor value={workspaceName} label="Nom du workspace" className="w-64 text-[16px]" onCommit={onCommitRename} onCancel={onCancelRename} />
          ) : (
            <EditableName name={workspaceName} className="text-[16px]" onClick={onStartRename} />
          ))}
      </div>
      {navigation && <div className={leaderActive ? 'hidden' : 'contents'}>{navigation}</div>}
      {leaderActive && <LeaderHints />}
      <div className="ml-auto flex shrink-0 items-center gap-2">
        <UpdateButton />
        <button
          type="button"
          className="flex cursor-pointer items-center gap-1.5 rounded border border-dock-line px-2 text-dock-muted hover:bg-dock-green-hover hover:text-dock-ink"
          data-tip="Paramètres (Leader puis ,)"
          onClick={onOpenSettings}
        >
          <span className="text-lg leading-tight">⚙</span>
          <span className="text-xs">Paramètres</span>
        </button>
      </div>
    </header>
  )
}
