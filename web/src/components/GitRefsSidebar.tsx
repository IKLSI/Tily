import { useMemo, useRef, useState, type KeyboardEvent, type ReactNode } from 'react'
import { GitRefKind, type GitBranch, type GitRemoteBranch, type GitState, type GitStash, type GitTag } from '../bridge/gitMessages'
import { branchTree, visibleBranches, type GitBranchFolder } from '../git/gitBranchTree'
import { shortSha } from '../git/gitLabels'
import { branchMenu, remoteBranchMenu, stashMenu, tagMenu } from '../git/gitMenus'
import { promptNewBranch, promptNewTag, promptStash, switchToBranch, switchToRemote } from '../git/gitRefActions'
import { openGitMenu, revealCommit } from '../git/gitRequests'
import type { ActionMenuItem } from './ActionMenu'
import { GitAheadBehind } from './GitAheadBehind'
import { GitRefFolder } from './GitRefFolder'
import { GitRefRow } from './GitRefRow'
import { GitSection } from './GitSection'
import { Icon } from './Icon'
import { IconName } from './iconName'
import { ROW_ACTION } from './rightPanelStyles'

interface GitRefsSidebarProps {
  state: GitState
  width: number
}

const LOCAL = 'local'
const REMOTE = 'remote'
const TAGS = 'tags'
const STASHES = 'stashes'
const ROW_SELECTOR = '[data-git-row]'

const localKey = (branch: GitBranch) => `${LOCAL}\n${branch.name}`
const remoteKey = (branch: GitRemoteBranch) => `${REMOTE}\n${branch.name}`
const groupKey = (remote: string) => `${REMOTE}:${remote}`
const folderKey = (scope: string, path: string) => `${scope}/${path}`

const branchMeta = (branch: GitBranch): ReactNode => {
  if (branch.gone) {
    return 'supprimée'
  }
  return branch.ahead > 0 || branch.behind > 0 ? <GitAheadBehind ahead={branch.ahead} behind={branch.behind} /> : undefined
}

const branchTip = (branch: GitBranch): string => {
  if (branch.gone) {
    return `${branch.name} : la branche distante suivie ${branch.upstream ?? ''} a été supprimée`
  }
  const tracking = branch.upstream ? `suit ${branch.upstream}` : 'aucune branche distante suivie'
  return `${branch.name} · ${tracking} · clic : aller au commit · double-clic : checkout · glisser sur la branche courante : merge ou rebase`
}

export function GitRefsSidebar({ state, width }: GitRefsSidebarProps) {
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({})
  const [focusKey, setFocusKey] = useState<string | null>(null)
  const containerRef = useRef<HTMLDivElement>(null)
  const localTree = useMemo(() => branchTree(state.branches, (branch) => branch.name), [state.branches])
  const groups = useMemo(
    () =>
      state.remotes.map((remote) => {
        const branches = state.remoteBranches.filter((branch) => branch.remote === remote)
        return { remote, count: branches.length, tree: branchTree(branches, (branch) => branch.branch) }
      }),
    [state.remotes, state.remoteBranches],
  )
  const isCollapsed = (scope: string) => (path: string) => collapsed[folderKey(scope, path)] === true
  const keys = [
    ...(collapsed[LOCAL] ? [] : visibleBranches(localTree, isCollapsed(LOCAL)).map(localKey)),
    ...(collapsed[REMOTE] ? [] : groups.flatMap((group) => (collapsed[groupKey(group.remote)] ? [] : visibleBranches(group.tree, isCollapsed(groupKey(group.remote))).map(remoteKey)))),
    ...(collapsed[TAGS] ? [] : state.tags.map((tag) => `${TAGS}\n${tag.name}`)),
    ...(collapsed[STASHES] ? [] : state.stashes.map((stash) => `${STASHES}\n${stash.sha}`)),
  ]
  const focusable = keys.find((key) => key === focusKey) ?? keys[0]
  const clean = state.stagedTotal + state.unstagedTotal === 0

  const rows = (): HTMLElement[] => Array.from(containerRef.current?.querySelectorAll<HTMLElement>(ROW_SELECTOR) ?? [])
  const toggle = (section: string) => () => setCollapsed((current) => ({ ...current, [section]: !current[section] }))
  const openMenu = (key: string, label: string, items: ActionMenuItem[]) => (x: number, y: number) =>
    openGitMenu({ x, y, label, items, restoreFocus: () => rows().find((row) => row.dataset.gitRow === key)?.focus() })
  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const all = rows()
    const index = all.indexOf(event.target as HTMLElement)
    const moves: Record<string, number> = { ArrowDown: index + 1, ArrowUp: index - 1, Home: 0, End: all.length - 1 }
    if (!(event.key in moves) || index < 0) {
      return
    }
    event.preventDefault()
    event.stopPropagation()
    const target = all[Math.min(Math.max(moves[event.key], 0), all.length - 1)]
    setFocusKey(target?.dataset.gitRow ?? null)
    target?.focus()
  }
  const handleNewBranch = () => promptNewBranch()
  const handleNewTag = () => promptNewTag()

  const renderBranch = (branch: GitBranch, label: string, depth: number) => {
    const key = localKey(branch)
    const handleShow = () => revealCommit(branch.sha)
    const handleActivate = () => {
      if (!branch.current) {
        switchToBranch(branch)
      }
    }
    return (
      <GitRefRow
        key={key}
        rowKey={key}
        icon={IconName.Local}
        name={label}
        depth={depth}
        meta={branchMeta(branch)}
        metaTip={branch.upstream ? `↑ à push, ↓ à pull depuis ${branch.upstream}` : undefined}
        tip={branchTip(branch)}
        current={branch.current}
        focusable={key === focusable}
        handle={{ kind: GitRefKind.Branch, name: branch.name }}
        onFocus={setFocusKey}
        onSelect={handleShow}
        onActivate={handleActivate}
        onMenu={openMenu(key, `Actions de ${branch.name}`, branchMenu(branch, state))}
      />
    )
  }
  const renderRemoteBranch = (branch: GitRemoteBranch, label: string, depth: number) => {
    const key = remoteKey(branch)
    const handleShow = () => revealCommit(branch.sha)
    const handleActivate = () => switchToRemote(branch)
    return (
      <GitRefRow
        key={key}
        rowKey={key}
        icon={IconName.Remote}
        name={label}
        tip={`${branch.name} · clic : aller au commit · double-clic : checkout d’une branche locale qui la suit`}
        depth={depth}
        focusable={key === focusable}
        handle={{ kind: GitRefKind.Remote, name: branch.name }}
        onFocus={setFocusKey}
        onSelect={handleShow}
        onActivate={handleActivate}
        onMenu={openMenu(key, `Actions de ${branch.name}`, remoteBranchMenu(branch, state))}
      />
    )
  }
  const renderTree = <T,>(folder: GitBranchFolder<T>, scope: string, depth: number, renderLeaf: (item: T, label: string, depth: number) => ReactNode): ReactNode[] => [
    ...folder.folders.map((child) => {
      const key = folderKey(scope, child.path)
      return (
        <div key={key} className="flex flex-col">
          <GitRefFolder name={child.name} path={child.path} count={child.count} depth={depth} expanded={!collapsed[key]} onToggle={toggle(key)} />
          {!collapsed[key] && renderTree(child, scope, depth + 1, renderLeaf)}
        </div>
      )
    }),
    ...folder.leaves.map((leaf) => renderLeaf(leaf.item, leaf.label, depth)),
  ]
  const renderTag = (tag: GitTag) => {
    const key = `${TAGS}\n${tag.name}`
    const handleShow = () => revealCommit(tag.sha)
    return (
      <GitRefRow
        key={key}
        rowKey={key}
        icon={IconName.Tag}
        name={tag.name}
        meta={shortSha(tag.sha)}
        tip={`Tag ${tag.name} · clic : aller au commit`}
        focusable={key === focusable}
        onFocus={setFocusKey}
        onSelect={handleShow}
        onActivate={handleShow}
        onMenu={openMenu(key, `Actions du tag ${tag.name}`, tagMenu(tag, state))}
      />
    )
  }
  const renderStash = (stash: GitStash) => {
    const key = `${STASHES}\n${stash.sha}`
    const handleShow = () => revealCommit(stash.sha)
    return (
      <GitRefRow
        key={key}
        rowKey={key}
        icon={IconName.Stash}
        name={stash.message}
        meta={`stash@{${stash.index}}`}
        tip={`${stash.message} · clic : voir les modifications`}
        focusable={key === focusable}
        onFocus={setFocusKey}
        onSelect={handleShow}
        onActivate={handleShow}
        onMenu={openMenu(key, 'Actions du stash', stashMenu(stash, state))}
      />
    )
  }

  const headerButton = (icon: IconName, label: string, onClick: () => void, disabled = false) => {
    const handleClick = () => {
      if (!disabled) {
        onClick()
      }
    }
    return (
      <button type="button" className={`${ROW_ACTION} aria-disabled:cursor-default aria-disabled:opacity-40`} aria-label={label} aria-disabled={disabled} data-tip={label} onClick={handleClick}>
        <Icon name={icon} />
      </button>
    )
  }

  return (
    <div ref={containerRef} role="listbox" aria-label="Branches, tags et stash" className="min-h-0 shrink-0 overflow-auto py-[4px]" style={{ width }} onKeyDown={handleKeyDown}>
      <GitSection title="Locales" count={state.branches.length} expanded={!collapsed[LOCAL]} empty="Aucune branche." onToggle={toggle(LOCAL)} actions={headerButton(IconName.Plus, 'Nouvelle branche depuis HEAD', handleNewBranch, state.head.unborn)}>
        {renderTree(localTree, LOCAL, 0, renderBranch)}
      </GitSection>
      <GitSection title="Distantes" count={state.remoteBranches.length} expanded={!collapsed[REMOTE]} empty={state.remotes.length === 0 ? 'Aucun dépôt distant configuré.' : 'Aucune branche distante.'} onToggle={toggle(REMOTE)}>
        {groups.map((group) => (
          <GitSection key={group.remote} title={group.remote} count={group.count} nested expanded={!collapsed[groupKey(group.remote)]} empty="Aucune branche : faites un fetch." onToggle={toggle(groupKey(group.remote))}>
            {renderTree(group.tree, groupKey(group.remote), 1, renderRemoteBranch)}
          </GitSection>
        ))}
      </GitSection>
      <GitSection title="Tags" count={state.tags.length} expanded={!collapsed[TAGS]} empty="Aucun tag." onToggle={toggle(TAGS)} actions={headerButton(IconName.Plus, 'Nouveau tag sur HEAD', handleNewTag, state.head.unborn)}>
        {state.tags.map(renderTag)}
      </GitSection>
      <GitSection title="Stash" count={state.stashes.length} expanded={!collapsed[STASHES]} empty="Aucun stash." onToggle={toggle(STASHES)} actions={headerButton(IconName.Stash, clean ? 'Aucune modification à stash' : 'Stash des modifications', promptStash, clean)}>
        {state.stashes.map(renderStash)}
      </GitSection>
    </div>
  )
}
