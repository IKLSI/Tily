import { Component, type ErrorInfo, type ReactNode } from 'react'
import { BUTTON_SECONDARY } from './buttonStyles'

interface ErrorBoundaryProps {
  children: ReactNode
  className?: string
  resetKey?: string
  onRecover?: (error: Error) => void
}

interface ErrorBoundaryState {
  error: Error | null
}

const RECOVERY_INTERVAL_MS = 1_000

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { error: null }
  private lastRecoveryAt = 0

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error('Erreur d’affichage interceptée :', error, info.componentStack)
    this.recover(error)
  }

  componentDidUpdate(previous: ErrorBoundaryProps): void {
    if (this.state.error && previous.resetKey !== this.props.resetKey) {
      this.reset()
    }
  }

  reset = (): void => this.setState({ error: null })

  private recover(error: Error): void {
    const { onRecover } = this.props
    const now = Date.now()
    if (!onRecover || now - this.lastRecoveryAt < RECOVERY_INTERVAL_MS) {
      return
    }

    this.lastRecoveryAt = now
    onRecover(error)
    this.reset()
  }

  render(): ReactNode {
    const { error } = this.state
    if (!error) {
      return this.props.children
    }
    return (
      <div role="alert" className={this.props.className ?? 'flex h-full min-h-0 flex-col items-center justify-center gap-3 p-4 text-center'}>
        <p className="text-tily-ink">Une erreur inattendue a interrompu l’affichage de cette zone.</p>
        <p className="max-w-md break-words text-[12px] text-tily-muted">{error.message}</p>
        <button type="button" className={BUTTON_SECONDARY} onClick={this.reset}>
          Réessayer
        </button>
      </div>
    )
  }
}
