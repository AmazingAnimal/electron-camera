const { contextBridge, ipcRenderer } = require('electron')

contextBridge.exposeInMainWorld('electronAPI', {
  version: process.versions.electron,
  pingDeviceService: () => ipcRenderer.invoke('device-service:ping'),
  restartDeviceService: () => ipcRenderer.invoke('device-service:restart'),
  sendDeviceServiceCommand: (command) => ipcRenderer.invoke('device-service:send-command', command),
  onDeviceServiceEvent: (callback) => {
    const listener = (_event, payload) => callback(payload)
    ipcRenderer.on('device-service:event', listener)
    return () => ipcRenderer.removeListener('device-service:event', listener)
  },
})