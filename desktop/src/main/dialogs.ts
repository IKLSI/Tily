import { dialog, type BrowserWindow, type OpenDialogOptions } from 'electron'

const JSON_FILTER = { name: 'JSON', extensions: ['json'] }
const SOUND_FILTER = { name: 'Sons', extensions: ['aiff', 'aif', 'wav', 'mp3'] }
const PREFERENCES_NAME = 'tily-preferences.json'

const pickOptions = (target: string | undefined): OpenDialogOptions => {
  switch (target) {
    case 'folder':
      return { properties: ['openDirectory', 'createDirectory'] }
    case 'sound':
      return { properties: ['openFile'], filters: [SOUND_FILTER] }
    default:
      return { properties: ['openFile', 'treatPackageAsDirectory'] }
  }
}

export const pickPath = async (window: BrowserWindow, target: string | undefined): Promise<string | null> => {
  const result = await dialog.showOpenDialog(window, pickOptions(target))
  return result.canceled ? null : (result.filePaths[0] ?? null)
}

export const pickPreferencesToImport = async (window: BrowserWindow): Promise<string | null> => {
  const result = await dialog.showOpenDialog(window, { properties: ['openFile'], filters: [JSON_FILTER] })
  return result.canceled ? null : (result.filePaths[0] ?? null)
}

export const pickPreferencesToExport = async (window: BrowserWindow): Promise<string | null> => {
  const result = await dialog.showSaveDialog(window, { defaultPath: PREFERENCES_NAME, filters: [JSON_FILTER] })
  return result.canceled ? null : (result.filePath ?? null)
}
