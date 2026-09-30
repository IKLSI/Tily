import { useEffect, useMemo, useRef, useState, type KeyboardEvent, type MouseEvent, type SyntheticEvent } from 'react'
import { useShallow } from 'zustand/react/shallow'
import { PreviewKind, type FilePreview } from '../bridge/previewMessages'
import { focusFileTree } from '../explorer/fileExplorerActions'
import { folderName } from '../model/session'
import { closePreview, followPreviewLink, openPreviewInEditor } from '../preview/previewActions'
import { anchorTarget, highlightText, renderMarkdown } from '../preview/renderPreview'
import { usePreviewStore } from '../store/previewStore'
import { GitToolButton } from './GitToolButton'
import { Icon } from './Icon'
import { IconName } from './iconName'
import { PANEL_HEADER_BUTTON } from './rightPanelStyles'

const KIND_LABELS: Record<PreviewKind, string> = {
  [PreviewKind.Markdown]: 'Markdown',
  [PreviewKind.Text]: 'Texte',
  [PreviewKind.Image]: 'Image',
}

const handleClose = () => {
  closePreview()
  focusFileTree()
}

const handleKeyDown = (event: KeyboardEvent<HTMLElement>) => {
  if (event.key === 'Escape') {
    event.preventDefault()
    event.stopPropagation()
    handleClose()
  }
}

const handleContentClick = (event: MouseEvent<HTMLElement>) => {
  const link = event.target instanceof Element ? event.target.closest('a[href]') : null
  if (link) {
    event.preventDefault()
    followPreviewLink(link.getAttribute('href') ?? '')
  }
}

const preventAuxiliaryOpen = (event: MouseEvent<HTMLElement>) => {
  if (event.target instanceof Element && event.target.closest('a[href]')) {
    event.preventDefault()
  }
}

const MARKDOWN_LANGUAGE = 'markdown'

const renderedHtml = (preview: FilePreview | null, source: boolean): string | null => {
  if (!preview || preview.error || preview.kind === PreviewKind.Image) {
    return null
  }
  if (preview.kind === PreviewKind.Markdown) {
    return source ? highlightText(preview.content, MARKDOWN_LANGUAGE) : renderMarkdown(preview.content, preview.baseUrl)
  }
  return highlightText(preview.content, preview.language)
}

export function FilePreviewDrawer() {
  const { path, preview, anchor, anchorRequest } = usePreviewStore(useShallow((store) => ({ path: store.path, preview: store.preview, anchor: store.anchor, anchorRequest: store.anchorRequest })))
  const bodyRef = useRef<HTMLDivElement>(null)
  const [sourcePath, setSourcePath] = useState<string | null>(null)
  const showSource = preview?.kind === PreviewKind.Markdown && sourcePath === preview.path
  const html = useMemo(() => renderedHtml(preview, showSource), [preview, showSource])
  const handleToggleSource = () => setSourcePath(showSource ? null : (preview?.path ?? null))
  const [image, setImage] = useState<{ src: string; size: string } | null>(null)
  const [actualSource, setActualSource] = useState<string | null>(null)
  const handleImageLoad = (event: SyntheticEvent<HTMLImageElement>) =>
    setImage({ src: event.currentTarget.getAttribute('src') ?? '', size: `${event.currentTarget.naturalWidth} × ${event.currentTarget.naturalHeight}` })
  const actualSize = actualSource !== null && actualSource === preview?.content
  const handleToggleActualSize = () => setActualSource(actualSize ? null : (preview?.content ?? null))
  const imageSize = image && image.src === preview?.content ? image.size : null

  useEffect(() => {
    const body = bodyRef.current
    if (!body) {
      return
    }
    const target = anchor ? anchorTarget(body, anchor) : null
    if (target) {
      target.scrollIntoView({ block: 'start' })
    } else {
      body.scrollTop = 0
    }
  }, [anchorRequest, anchor])

  if (!path) {
    return null
  }
  const name = preview?.name ?? folderName(path)

  return (
    <aside aria-label="Aperçu du fichier" data-preview-drawer="" className="absolute inset-y-0 right-0 z-20 flex w-[min(920px,100%)] flex-col border-l border-dock-line bg-dock-panel shadow-2xl" onKeyDown={handleKeyDown}>
      <header className="flex h-[36px] shrink-0 items-center gap-[8px] border-b border-dock-line pr-[6px] pl-[12px]">
        {preview && !preview.error && <span className="shrink-0 rounded bg-dock-paper px-[6px] py-[1px] text-[11px] text-dock-muted">{preview.kind === PreviewKind.Image && imageSize ? `${KIND_LABELS[preview.kind]} · ${imageSize}` : KIND_LABELS[preview.kind]}</span>}
        <span className="min-w-0 flex-1 truncate text-[12px] font-semibold text-dock-ink" data-tip={path}>
          {name}
        </span>
        {preview?.kind === PreviewKind.Markdown && !preview.error && (
          <GitToolButton icon={IconName.File} tip={showSource ? 'Revenir au rendu Markdown' : 'Afficher le texte source du Markdown'} label="Source" pressed={showSource} onClick={handleToggleSource} />
        )}
        <GitToolButton icon={IconName.Editor} tip="Ouvrir dans l’éditeur" onClick={openPreviewInEditor} />
        <button type="button" className={PANEL_HEADER_BUTTON} aria-label="Fermer" data-tip="Fermer (Échap)" onClick={handleClose}>
          <Icon name={IconName.Close} />
        </button>
      </header>
      {preview?.truncated && <p className="shrink-0 border-b border-dock-line px-[16px] py-[6px] text-[12px] text-dock-warning">Fichier volumineux : seuls les 2 premiers Mo sont affichés.</p>}
      <div ref={bodyRef} tabIndex={0} aria-label={`Contenu de ${name}`} className="min-h-0 flex-1 overflow-auto px-[24px] py-[18px] select-text" onClick={handleContentClick} onAuxClick={preventAuxiliaryOpen}>
        {!preview ? (
          <p className="text-[12px] text-dock-muted">Chargement de l’aperçu…</p>
        ) : preview.error ? (
          <p className="text-[12px] text-dock-error">{preview.error}</p>
        ) : preview.kind === PreviewKind.Image ? (
          <img
            src={preview.content}
            alt={name}
            data-tip={actualSize ? 'Clic : ajuster à la place disponible' : 'Clic : taille réelle'}
            className={`mx-auto block object-contain ${actualSize ? 'max-w-none cursor-zoom-out' : 'max-h-full max-w-full cursor-zoom-in'}`}
            onLoad={handleImageLoad}
            onClick={handleToggleActualSize}
          />
        ) : preview.kind === PreviewKind.Markdown && !showSource && html !== null ? (
          <div className="dock-markdown text-dock-ink" dangerouslySetInnerHTML={{ __html: html }} />
        ) : html !== null ? (
          <pre className="font-mono text-[12.5px] leading-[1.5] whitespace-pre-wrap text-dock-ink" dangerouslySetInnerHTML={{ __html: html }} />
        ) : (
          <pre className="font-mono text-[12.5px] leading-[1.5] whitespace-pre-wrap text-dock-ink">{preview.content}</pre>
        )}
      </div>
    </aside>
  )
}
