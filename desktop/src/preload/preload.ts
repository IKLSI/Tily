import { contextBridge, ipcRenderer, webUtils, type IpcRendererEvent } from 'electron'

const SEND_CHANNEL = 'tily:send'
const MESSAGE_CHANNEL = 'tily:message'

contextBridge.exposeInMainWorld('tily', {
  send: (message: unknown): void => ipcRenderer.send(SEND_CHANNEL, message),
  onMessage: (listener: (message: unknown) => void): (() => void) => {
    const receive = (_event: IpcRendererEvent, json: string): void => listener(JSON.parse(json))
    ipcRenderer.on(MESSAGE_CHANNEL, receive)
    return () => {
      ipcRenderer.removeListener(MESSAGE_CHANNEL, receive)
    }
  },
  pathForFile: (file: File): string => webUtils.getPathForFile(file),
})
