import { contextBridge, ipcRenderer, webUtils } from 'electron'

const SEND_CHANNEL = 'tily:send'
const MESSAGE_CHANNEL = 'tily:message'

contextBridge.exposeInMainWorld('tily', {
  send: (message: unknown): void => ipcRenderer.send(SEND_CHANNEL, message),
  onMessage: (listener: (message: unknown) => void): void => {
    ipcRenderer.on(MESSAGE_CHANNEL, (_event, json: string) => listener(JSON.parse(json)))
  },
  pathForFile: (file: File): string => webUtils.getPathForFile(file),
})
