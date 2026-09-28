const REFUSED_TYPES = ['Files', 'text/uri-list']
const NO_DROP = 'none'

const carriesRefusedData = (event: DragEvent): boolean => REFUSED_TYPES.some((type) => event.dataTransfer?.types.includes(type))

const refuseDrop = (event: DragEvent): void => {
  if (!carriesRefusedData(event)) {
    return
  }
  event.preventDefault()
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = NO_DROP
  }
}

export const startExternalDropGuard = (): (() => void) => {
  window.addEventListener('dragover', refuseDrop)
  window.addEventListener('drop', refuseDrop)
  return () => {
    window.removeEventListener('dragover', refuseDrop)
    window.removeEventListener('drop', refuseDrop)
  }
}
