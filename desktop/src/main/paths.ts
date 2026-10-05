import { app } from 'electron'
import path from 'node:path'

const BACKEND_VARIABLE = 'TILY_BACKEND'
const RENDERER_DEV_VARIABLE = 'TILY_RENDERER_DEV_URL'
const BACKEND_EXECUTABLE = 'Tily'
const DESKTOP_ROOT = path.resolve(__dirname, '..', '..')
const REPOSITORY_ROOT = path.resolve(DESKTOP_ROOT, '..')

export const APP_SCHEME = 'tily'
export const APP_HOST = 'app'
const APP_ORIGIN = `${APP_SCHEME}://${APP_HOST}`

export const backendExecutable = (): string =>
  process.env[BACKEND_VARIABLE] ??
  (app.isPackaged
    ? path.join(process.resourcesPath, 'backend', BACKEND_EXECUTABLE)
    : path.join(REPOSITORY_ROOT, 'backend', 'src', 'Tily.Host', 'bin', 'Debug', 'net10.0', BACKEND_EXECUTABLE))

export const rendererRoot = (): string =>
  app.isPackaged ? path.join(process.resourcesPath, 'renderer') : path.join(DESKTOP_ROOT, 'renderer', 'dist')

export const startUrl = (): string => process.env[RENDERER_DEV_VARIABLE] || `${APP_ORIGIN}/index.html`

export const preloadScript = (): string => path.join(__dirname, '..', 'preload', 'preload.js')
