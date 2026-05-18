<template>
  <div class="page-shell">
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1>工厂扫码工作台</h1>
          <p>已接入本地 DeviceService 事件流，当前支持 PLC 状态与 mock 扫码结果展示。</p>
        </div>
        <div class="header-tags">
          <el-tag :type="serviceRunning ? 'success' : 'danger'" size="large">
            {{ serviceRunning ? '服务运行中' : '服务未连接' }}
          </el-tag>
          <el-tag type="primary" size="large">Electron {{ electronVersion }}</el-tag>
        </div>
      </div>

      <el-card shadow="hover" class="action-card">
        <div class="action-row">
          <el-button type="primary" @click="handleRestartService">重启服务</el-button>
          <el-button type="success" @click="handleSendMockTrigger">发送 Mock 命令</el-button>
          <el-button @click="handleClearHistory">清空历史</el-button>
          <el-button @click="handleClearLogs">清空日志</el-button>
        </div>
      </el-card>

      <el-row :gutter="16" class="summary-row">
        <el-col :xs="24" :sm="12" :lg="8">
          <el-card shadow="hover" class="summary-card result-card">
            <template #header>
              <div class="card-title-row">
                <span>最近扫码结果</span>
                <el-tag :type="latestScan.code ? 'success' : 'info'">
                  {{ latestScan.code ? '最新' : '等待中' }}
                </el-tag>
              </div>
            </template>

            <div class="code-value">{{ latestScan.code || '--' }}</div>
            <div class="material-name">{{ latestScan.materialName || '暂无物料信息' }}</div>

            <div class="meta-list">
              <div class="meta-item">
                <span class="meta-label">物料编码</span>
                <span class="meta-value">{{ latestScan.materialCode || '--' }}</span>
              </div>
              <div class="meta-item">
                <span class="meta-label">批次号</span>
                <span class="meta-value">{{ latestScan.batchNo || '--' }}</span>
              </div>
              <div class="meta-item">
                <span class="meta-label">扫码时间</span>
                <span class="meta-value">{{ latestScan.scanTime || '--' }}</span>
              </div>
            </div>
          </el-card>
        </el-col>

        <el-col :xs="24" :sm="12" :lg="8">
          <el-card shadow="hover" class="summary-card">
            <template #header>
              <div class="card-title-row">
                <span>当前物料信息</span>
                <el-tag type="warning">展示</el-tag>
              </div>
            </template>

            <div class="info-grid">
              <div class="info-cell">
                <div class="info-label">物料名称</div>
                <div class="info-value">{{ latestScan.materialName || '--' }}</div>
              </div>
              <div class="info-cell">
                <div class="info-label">物料编码</div>
                <div class="info-value">{{ latestScan.materialCode || '--' }}</div>
              </div>
              <div class="info-cell">
                <div class="info-label">批次号</div>
                <div class="info-value">{{ latestScan.batchNo || '--' }}</div>
              </div>
              <div class="info-cell">
                <div class="info-label">工位</div>
                <div class="info-value">{{ workstationName }}</div>
              </div>
            </div>
          </el-card>
        </el-col>

        <el-col :xs="24" :sm="24" :lg="8">
          <el-card shadow="hover" class="summary-card upload-card">
            <template #header>
              <div class="card-title-row">
                <span>PLC / 上传状态栏</span>
                <el-tag type="info">实时</el-tag>
              </div>
            </template>

            <div class="status-list">
              <div class="status-item">
                <span>待上传</span>
                <el-tag type="warning">{{ uploadSummary.pending }}</el-tag>
              </div>
              <div class="status-item">
                <span>上传成功</span>
                <el-tag type="success">{{ uploadSummary.success }}</el-tag>
              </div>
              <div class="status-item">
                <span>上传失败</span>
                <el-tag type="danger">{{ uploadSummary.failed }}</el-tag>
              </div>
              <div class="status-item">
                <span>PLC 触发位</span>
                <el-tag :type="plcStatus.scanTrigger ? 'danger' : 'info'">
                  {{ plcStatus.scanTrigger ? '触发' : '未触发' }}
                </el-tag>
              </div>
              <div class="status-item">
                <span>设备状态码</span>
                <span class="status-text">{{ plcStatus.deviceStatus }}</span>
              </div>
              <div class="status-item">
                <span>允许放行</span>
                <el-tag :type="plcStatus.allowPass ? 'success' : 'warning'">
                  {{ plcStatus.allowPass ? 'ON' : 'OFF' }}
                </el-tag>
              </div>
              <div class="status-item">
                <span>最后上传时间</span>
                <span class="status-text">{{ uploadSummary.lastUploadTime }}</span>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>

      <el-card shadow="hover" class="history-card">
        <template #header>
          <div class="card-title-row">
            <span>扫码历史记录</span>
            <el-tag type="primary">{{ scanHistory.length }} 条</el-tag>
          </div>
        </template>

        <el-table :data="scanHistory" stripe border>
          <el-table-column type="index" label="#" width="60" />
          <el-table-column prop="code" label="码值" min-width="220" />
          <el-table-column prop="materialCode" label="物料编码" min-width="140" />
          <el-table-column prop="materialName" label="物料名称" min-width="160" />
          <el-table-column prop="batchNo" label="批次号" min-width="140" />
          <el-table-column prop="scanTime" label="扫码时间" min-width="180" />
          <el-table-column prop="source" label="来源" width="120" />
          <el-table-column label="上传状态" width="120">
            <template #default="scope">
              <el-tag :type="statusTagType(scope.row.uploadStatus)">
                {{ statusText(scope.row.uploadStatus) }}
              </el-tag>
            </template>
          </el-table-column>
        </el-table>
      </el-card>

      <el-card shadow="hover" class="history-card log-card">
        <template #header>
          <div class="card-title-row">
            <span>服务事件日志</span>
            <el-tag type="info">{{ eventLogs.length }} 条</el-tag>
          </div>
        </template>

        <div class="log-list">
          <div v-for="(log, index) in eventLogs" :key="`${index}-${log.time}`" class="log-item">
            <span class="log-time">{{ log.time }}</span>
            <span class="log-type">{{ log.type }}</span>
            <span class="log-message">{{ log.message }}</span>
          </div>
        </div>
      </el-card>
    </div>
  </div>
</template>

<script setup>
import { onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'

const electronVersion = window.electronAPI?.version ?? 'unavailable'
const workstationName = '一号工位'
const serviceRunning = ref(false)

const latestScan = reactive({
  code: '',
  materialCode: '',
  materialName: '',
  batchNo: '',
  scanTime: '',
  source: '',
})

const uploadSummary = reactive({
  pending: 0,
  success: 0,
  failed: 0,
  lastUploadTime: '--',
})

const plcStatus = reactive({
  scanTrigger: false,
  deviceStatus: 0,
  allowPass: false,
  time: '--',
})

const scanHistory = ref([])
const eventLogs = ref([])

const addLog = (type, message) => {
  eventLogs.value.unshift({
    time: new Date().toLocaleTimeString(),
    type,
    message,
  })

  if (eventLogs.value.length > 50) {
    eventLogs.value = eventLogs.value.slice(0, 50)
  }
}

const mapScanToMaterial = (scanEvent) => {
  const suffix = scanEvent.code.split('-').pop() || '0000'
  return {
    materialCode: suffix.startsWith('000') ? `MAT-${suffix}` : suffix,
    materialName: '模拟物料',
    batchNo: `BATCH-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}`,
  }
}

const statusTagType = (status) => {
  if (status === 'success') return 'success'
  if (status === 'failed') return 'danger'
  return 'warning'
}

const statusText = (status) => {
  if (status === 'success') return '已上传'
  if (status === 'failed') return '上传失败'
  return '待上传'
}

const handleRestartService = async () => {
  await window.electronAPI.restartDeviceService()
  ElMessage.success('已请求重启本地服务')
}

const handleSendMockTrigger = async () => {
  const result = await window.electronAPI.sendDeviceServiceCommand({ type: 'mock-trigger' })
  if (result?.ok) {
    ElMessage.success('已发送 mock-trigger 命令')
  } else {
    ElMessage.error('发送命令失败')
  }
}

const handleClearHistory = () => {
  scanHistory.value = []
  uploadSummary.pending = 0
  uploadSummary.success = 0
  uploadSummary.failed = 0
  uploadSummary.lastUploadTime = '--'
  ElMessage.success('已清空历史记录')
}

const handleClearLogs = () => {
  eventLogs.value = []
  ElMessage.success('已清空日志')
}

let removeDeviceListener = null

onMounted(async () => {
  try {
    const ping = await window.electronAPI.pingDeviceService()
    serviceRunning.value = !!ping?.running
    addLog('service', `device service ${serviceRunning.value ? 'connected' : 'not running'}`)
  } catch (error) {
    addLog('service-error', error.message)
  }

  removeDeviceListener = window.electronAPI.onDeviceServiceEvent((payload) => {
    if (!payload || !payload.type) return

    if (payload.type === 'service-status') {
      serviceRunning.value = true
      addLog(payload.type, payload.status)
      return
    }

    if (payload.type === 'service-exit' || payload.type === 'service-error') {
      serviceRunning.value = false
      addLog(payload.type, payload.message || `exit code: ${payload.code}`)
      return
    }

    if (payload.type === 'stderr' || payload.type === 'log') {
      addLog(payload.type, payload.message)
      return
    }

    if (payload.type === 'plc-status') {
      plcStatus.scanTrigger = !!payload.scanTrigger
      plcStatus.deviceStatus = payload.deviceStatus ?? 0
      plcStatus.allowPass = !!payload.allowPass
      plcStatus.time = payload.time || '--'
      addLog(payload.type, `trigger=${plcStatus.scanTrigger}, status=${plcStatus.deviceStatus}, allowPass=${plcStatus.allowPass}`)
      return
    }

    if (payload.type === 'scan') {
      const material = mapScanToMaterial(payload)
      latestScan.code = payload.code
      latestScan.materialCode = material.materialCode
      latestScan.materialName = material.materialName
      latestScan.batchNo = material.batchNo
      latestScan.scanTime = payload.scanTime
      latestScan.source = payload.source || 'device'

      scanHistory.value.unshift({
        code: payload.code,
        materialCode: material.materialCode,
        materialName: material.materialName,
        batchNo: material.batchNo,
        scanTime: payload.scanTime,
        source: payload.source || 'device',
        uploadStatus: 'pending',
      })

      if (scanHistory.value.length > 100) {
        scanHistory.value = scanHistory.value.slice(0, 100)
      }

      uploadSummary.pending += 1
      uploadSummary.lastUploadTime = payload.scanTime
      addLog(payload.type, payload.code)
    }
  })
})

onBeforeUnmount(() => {
  if (typeof removeDeviceListener === 'function') {
    removeDeviceListener()
  }
})
</script>

<style scoped>
.page-shell {
  min-height: 100vh;
  background: linear-gradient(180deg, #eef4ff 0%, #f8fafc 100%);
}

.page-container {
  max-width: 1400px;
  margin: 0 auto;
  padding: 24px;
}

.page-header {
  margin-bottom: 16px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.header-tags {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.action-card {
  margin-bottom: 16px;
  border-radius: 18px;
}

.action-row {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.page-header h1 {
  margin: 0;
  font-size: 28px;
  color: #111827;
}

.page-header p {
  margin: 8px 0 0;
  color: #6b7280;
}

.summary-row {
  margin-bottom: 16px;
}

.summary-card,
.history-card {
  border-radius: 18px;
}

.card-title-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  font-weight: 600;
  color: #1f2937;
}

.result-card {
  background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
  color: #f8fafc;
}

.result-card :deep(.el-card__header) {
  border-bottom-color: rgba(255, 255, 255, 0.08);
}

.result-card .card-title-row {
  color: #f8fafc;
}

.code-value {
  font-size: 24px;
  font-weight: 700;
  line-height: 1.4;
  color: #93c5fd;
  word-break: break-all;
}

.material-name {
  margin-top: 6px;
  font-size: 18px;
  color: #f8fafc;
}

.meta-list {
  margin-top: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.meta-item,
.status-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.meta-label,
.info-label {
  color: #94a3b8;
  font-size: 13px;
}

.meta-value,
.info-value,
.status-text {
  color: #e5e7eb;
  font-size: 14px;
  font-weight: 500;
  text-align: right;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.info-cell {
  padding: 14px;
  border-radius: 14px;
  background: #f8fafc;
  border: 1px solid #e5e7eb;
}

.info-value {
  margin-top: 8px;
  color: #111827;
  text-align: left;
}

.status-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.upload-card .status-text {
  color: #111827;
}

.history-card :deep(.el-table) {
  border-radius: 12px;
  overflow: hidden;
}

.log-card {
  margin-top: 16px;
}

.log-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 260px;
  overflow: auto;
}

.log-item {
  display: grid;
  grid-template-columns: 90px 130px 1fr;
  gap: 12px;
  padding: 10px 12px;
  border-radius: 12px;
  background: #f8fafc;
  border: 1px solid #e5e7eb;
  font-size: 13px;
  color: #334155;
}

.log-time {
  color: #64748b;
}

.log-type {
  color: #2563eb;
  font-weight: 600;
}

@media (max-width: 768px) {
  .page-container {
    padding: 16px;
  }

  .page-header {
    flex-direction: column;
  }

  .info-grid {
    grid-template-columns: 1fr;
  }

  .log-item {
    grid-template-columns: 1fr;
    gap: 6px;
  }
}
</style>