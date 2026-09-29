import { useEffect, useRef, type KeyboardEvent } from 'react'

const NAME_MAX_LENGTH = 100

interface InlineNameEditorProps {
  value: string
  label: string
  className: string
  selectionEnd?: number
  onCommit: (name: string) => void
  onCancel: () => void
}

export function InlineNameEditor({ value, label, className, selectionEnd, onCommit, onCancel }: InlineNameEditorProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const settledRef = useRef(false)
  const initialSelectionEndRef = useRef(selectionEnd ?? value.length)

  useEffect(() => {
    inputRef.current?.focus()
    inputRef.current?.setSelectionRange(0, initialSelectionEndRef.current)
  }, [])

  const settle = (action: () => void) => {
    if (settledRef.current) {
      return
    }
    settledRef.current = true
    action()
  }
  const handleBlur = () => settle(() => onCommit(inputRef.current?.value ?? ''))
  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    event.stopPropagation()
    if (event.nativeEvent.isComposing) {
      return
    }
    if (event.key === 'Enter') {
      event.preventDefault()
      settle(() => onCommit(event.currentTarget.value))
    } else if (event.key === 'Escape') {
      event.preventDefault()
      settle(onCancel)
    }
  }

  return (
    <input
      ref={inputRef}
      type="text"
      defaultValue={value}
      maxLength={NAME_MAX_LENGTH}
      aria-label={label}
      className={`rounded border border-dock-focus bg-dock-paper px-1 text-dock-ink outline-none ${className}`}
      onBlur={handleBlur}
      onKeyDown={handleKeyDown}
    />
  )
}
