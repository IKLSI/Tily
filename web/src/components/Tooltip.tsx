import { useEffect, useLayoutEffect, useRef, useState } from 'react'

const TIP_ATTRIBUTE = 'data-tip'
const SHOW_DELAY_MS = 250
const GAP_PX = 6
const EDGE_MARGIN_PX = 8

interface TooltipState {
  text: string
  x: number
  y: number
  above: boolean
}

const tipTargetOf = (target: EventTarget | null): HTMLElement | null => (target instanceof Element ? target.closest<HTMLElement>(`[${TIP_ATTRIBUTE}]`) : null)

const placeFor = (element: HTMLElement, text: string): TooltipState => {
  const rect = element.getBoundingClientRect()
  const above = rect.bottom + GAP_PX > window.innerHeight - 40
  return { text, x: rect.left + rect.width / 2, y: above ? rect.top - GAP_PX : rect.bottom + GAP_PX, above }
}

export function Tooltip() {
  const [state, setState] = useState<TooltipState | null>(null)
  const bubbleRef = useRef<HTMLDivElement>(null)

  useLayoutEffect(() => {
    const bubble = bubbleRef.current
    if (!bubble || !state) {
      return
    }
    const width = bubble.offsetWidth
    const left = Math.min(Math.max(state.x - width / 2, EDGE_MARGIN_PX), window.innerWidth - EDGE_MARGIN_PX - width)
    bubble.style.left = `${left}px`
    bubble.style.visibility = 'visible'
  }, [state])

  useEffect(() => {
    let timer: ReturnType<typeof setTimeout> | undefined
    let current: HTMLElement | null = null
    const hide = () => {
      clearTimeout(timer)
      current = null
      setState(null)
    }
    const show = (element: HTMLElement) => {
      const text = element.getAttribute(TIP_ATTRIBUTE)
      if (!text || element === current) {
        return
      }
      clearTimeout(timer)
      current = element
      timer = setTimeout(() => setState(placeFor(element, text)), SHOW_DELAY_MS)
    }
    const handlePointerOver = (event: PointerEvent) => {
      const element = event.buttons === 0 ? tipTargetOf(event.target) : null
      if (element) {
        show(element)
      } else {
        hide()
      }
    }
    const handleFocusIn = (event: FocusEvent) => {
      const element = tipTargetOf(event.target)
      if (element && element.matches(':focus-visible')) {
        show(element)
      }
    }
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        hide()
      }
    }
    document.addEventListener('pointerover', handlePointerOver)
    document.addEventListener('pointerdown', hide)
    document.addEventListener('focusin', handleFocusIn)
    document.addEventListener('focusout', hide)
    document.addEventListener('keydown', handleKeyDown)
    window.addEventListener('scroll', hide, true)
    return () => {
      clearTimeout(timer)
      document.removeEventListener('pointerover', handlePointerOver)
      document.removeEventListener('pointerdown', hide)
      document.removeEventListener('focusin', handleFocusIn)
      document.removeEventListener('focusout', hide)
      document.removeEventListener('keydown', handleKeyDown)
      window.removeEventListener('scroll', hide, true)
    }
  }, [])

  if (!state) {
    return null
  }
  return (
    <div
      ref={bubbleRef}
      role="tooltip"
      className="pointer-events-none fixed z-50 max-w-[360px] rounded border border-dock-line bg-dock-panel px-2 py-1 text-[11px] leading-snug w-max text-dock-ink shadow-lg"
      style={{ left: 0, top: state.y, visibility: 'hidden', transform: state.above ? 'translateY(-100%)' : undefined }}
    >
      {state.text}
    </div>
  )
}
