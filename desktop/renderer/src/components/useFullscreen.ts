import { useEffect, useState } from 'react'

const isFullscreen = (): boolean => window.innerHeight >= window.screen.height

export const useFullscreen = (): boolean => {
  const [fullscreen, setFullscreen] = useState(isFullscreen)
  useEffect(() => {
    const handleResize = () => setFullscreen(isFullscreen())
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])
  return fullscreen
}
