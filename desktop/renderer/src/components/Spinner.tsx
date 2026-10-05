interface SpinnerProps {
  size?: number
  className?: string
}

export function Spinner({ size = 12, className = '' }: SpinnerProps) {
  return <span aria-hidden="true" className={`inline-block shrink-0 animate-spin rounded-full border-[1.5px] border-current border-t-transparent motion-reduce:animate-none ${className}`} style={{ width: size, height: size }} />
}
