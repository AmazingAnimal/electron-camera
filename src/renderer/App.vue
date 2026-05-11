<template>
  <div class="page-shell">
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1>工厂扫码工作台</h1>
          <p>先完成 P0：扫码结果卡片、历史表格、上传状态栏。</p>
        </div>
        <el-tag type="primary" size="large">Electron {{ electronVersion }}</el-tag>
      </div>

      <el-row :gutter="16" class="summary-row">
        <el-col :xs="24" :sm="12" :lg="8">
          <el-card shadow="hover" class="summary-card result-card">
            <template #header>
              <div class="card-title-row">
                <span>最近扫码结果</span>
                <el-tag type="success">最新</el-tag>
              </div>
            </template>

            <div class="code-value">{{ latestScan.code }}</div>
            <div class="material-name">{{ latestScan.materialName }}</div>

            <div class="meta-list">
              <div class="meta-item">
                <span class="meta-label">物料编码</span>
                <span class="meta-value">{{ latestScan.materialCode }}</span>
              </div>
              <div class="meta-item">
                <span class="meta-label">批次号</span>
                <span class="meta-value">{{ latestScan.batchNo }}</span>
              </div>
              <div class="meta-item">
                <span class="meta-label">扫码时间</span>
                <span class="meta-value">{{ latestScan.scanTime }}</span>
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
                <div class="info-value">{{ latestScan.materialName }}</div>
              </div>
              <div class="info-cell">
                <div class="info-label">物料编码</div>
                <div class="info-value">{{ latestScan.materialCode }}</div>
              </div>
              <div class="info-cell">
                <div class="info-label">批次号</div>
                <div class="info-value">{{ latestScan.batchNo }}</div>
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
                <span>上传状态栏</span>
                <el-tag type="info">P0</el-tag>
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
          <el-table-column prop="code" label="码值" min-width="180" />
          <el-table-column prop="materialCode" label="物料编码" min-width="140" />
          <el-table-column prop="materialName" label="物料名称" min-width="160" />
          <el-table-column prop="batchNo" label="批次号" min-width="140" />
          <el-table-column prop="scanTime" label="扫码时间" min-width="180" />
          <el-table-column label="上传状态" width="120">
            <template #default="scope">
              <el-tag :type="statusTagType(scope.row.uploadStatus)">
                {{ statusText(scope.row.uploadStatus) }}
              </el-tag>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </div>
  </div>
</template>

<script setup>
const electronVersion = window.electronAPI?.version ?? 'unavailable'
const workstationName = '一号工位'

const latestScan = {
  code: 'MAT-20260510-0008',
  materialCode: 'MAT-0008',
  materialName: '高温胶带',
  batchNo: 'BATCH-250510-A',
  scanTime: '2026-05-10 18:20:36'
}

const uploadSummary = {
  pending: 8,
  success: 24,
  failed: 2,
  lastUploadTime: '2026-05-10 18:18:00'
}

const scanHistory = [
  {
    code: 'MAT-20260510-0008',
    materialCode: 'MAT-0008',
    materialName: '高温胶带',
    batchNo: 'BATCH-250510-A',
    scanTime: '2026-05-10 18:20:36',
    uploadStatus: 'pending'
  },
  {
    code: 'MAT-20260510-0007',
    materialCode: 'MAT-0007',
    materialName: '金属支架',
    batchNo: 'BATCH-250510-A',
    scanTime: '2026-05-10 18:17:52',
    uploadStatus: 'success'
  },
  {
    code: 'MAT-20260510-0006',
    materialCode: 'MAT-0006',
    materialName: '电机组件',
    batchNo: 'BATCH-250510-A',
    scanTime: '2026-05-10 18:15:19',
    uploadStatus: 'failed'
  },
  {
    code: 'MAT-20260510-0005',
    materialCode: 'MAT-0005',
    materialName: '控制面板',
    batchNo: 'BATCH-250510-A',
    scanTime: '2026-05-10 18:11:40',
    uploadStatus: 'success'
  }
]

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
}
</style>