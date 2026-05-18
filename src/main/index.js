const { app, BrowserWindow, ipcMain } = require('electron')
const { spawn } = require('node:child_process')
const path = require('node:path')

let mainWindow = null
let deviceServiceProcess = null
let mockIntervals = []

function createMainWindow() {
  const win = new BrowserWindow({
    width: 1000,
    height: 700,
    webPreferences: {
      preload: path.join(__dirname, '../preload/index.js'),
    },
  })

  if (process.env.ELECTRON_RENDERER_URL) {
    win.loadURL(process.env.ELECTRON_RENDERER_URL)
  } else {
    win.loadFile(path.join(__dirname, '../renderer/index.html'))
  }

  return win
}

function emitDeviceEvent(payload) {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.webContents.send('device-service:event', payload)
  }
}

function clearMockIntervals() {
  for (const timer of mockIntervals) {
    clearInterval(timer)
  }
  mockIntervals = []
}

function startMockDeviceEventStream() {
  clearMockIntervals()
  emitDeviceEvent({ type: 'service-status', status: 'mock-started' })

  mockIntervals.push(setInterval(() => {
    emitDeviceEvent({
      type: 'plc-status',
      scanTrigger: false,
      deviceStatus: 2,
      allowPass: true,
      time: new Date().toISOString(),
    })
  }, 3000))

  mockIntervals.push(setInterval(() => {
    emitDeviceEvent({
      type: 'scan',
      deviceId: 'mock-camera-01',
      code: `MOCK-${Date.now()}`,
      symbology: 'QR',
      scanTime: new Date().toISOString(),
      source: 'mock-main',
    })
  }, 8000))
}

function startDeviceService() {
  const servicePath = path.join(process.cwd(), 'mvp', 'DeviceService', 'bin', 'Debug', 'DeviceService.exe')

  try {
    deviceServiceProcess = spawn(servicePath, [], {
      windowsHide: true,
      stdio: ['pipe', 'pipe', 'pipe'],
    })

    let stdoutBuffer = ''

    deviceServiceProcess.stdout.on('data', (chunk) => {
      stdoutBuffer += chunk.toString()
      const lines = stdoutBuffer.split(/\r?\n/)
      stdoutBuffer = lines.pop() || ''

      for (const line of lines) {
        const trimmed = line.trim()
        if (!trimmed) continue

        try {
          const payload = JSON.parse(trimmed)
          emitDeviceEvent(payload)
        } catch {
          emitDeviceEvent({ type: 'log', message: trimmed })
        }
      }
    })

    deviceServiceProcess.stderr.on('data', (chunk) => {
      emitDeviceEvent({ type: 'stderr', message: chunk.toString().trim() })
    })

    deviceServiceProcess.on('exit', (code) => {
      emitDeviceEvent({ type: 'service-exit', code })
      deviceServiceProcess = null
    })
  } catch (error) {
    emitDeviceEvent({ type: 'service-error', message: error.message })
    startMockDeviceEventStream()
  }
}

function stopDeviceService() {
  if (deviceServiceProcess) {
    deviceServiceProcess.kill()
    deviceServiceProcess = null
  }
  clearMockIntervals()
}

function sendCommandToDeviceService(command) {
  if (!deviceServiceProcess || !deviceServiceProcess.stdin.writable) {
    emitDeviceEvent({ type: 'service-error', message: 'device service is not writable' })
    return false
  }

  deviceServiceProcess.stdin.write(`${JSON.stringify(command)}\n`)
  return true
}

app.whenReady().then(() => {
  mainWindow = createMainWindow()
  startDeviceService()

  ipcMain.handle('device-service:ping', async () => {
    return { ok: true, running: !!deviceServiceProcess }
  })

  ipcMain.handle('device-service:restart', async () => {
    stopDeviceService()
    startDeviceService()
    return { ok: true }
  })

  ipcMain.handle('device-service:send-command', async (_event, command) => {
    const ok = sendCommandToDeviceService(command)
    return { ok }
  })

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      mainWindow = createMainWindow()
    }
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    stopDeviceService()
    app.quit()
  }
})