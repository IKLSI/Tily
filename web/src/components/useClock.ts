import { useEffect, useState } from 'react'

export const useClock = (running: boolean, intervalMs: number): number => {
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    if (!running) {
      return
    }
    const timer = setInterval(() => setNow(Date.now()), intervalMs)
    return () => clearInterval(timer)
  }, [running, intervalMs])

  return now
}
