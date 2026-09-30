interface EditableNameProps {
  name: string
  className: string
  onClick: (event: React.MouseEvent) => void
}

export function EditableName({ name, className, onClick }: EditableNameProps) {
  return (
    <button type="button" className={`group flex min-w-0 cursor-pointer items-center gap-1.5 rounded px-1.5 text-left hover:bg-tily-line hover:text-tily-ink ${className}`} data-tip="Renommer le workspace" onClick={onClick}>
      <span className="truncate">{name}</span>
      <span className="shrink-0 text-[0.9em] text-tily-muted opacity-60 transition-opacity group-hover:text-tily-green-deep group-hover:opacity-100" aria-hidden="true">
        ✎
      </span>
    </button>
  )
}
