<!-- src/shared/components/ui/Table/DataTable.vue -->
<template>
  <div class="data-table-container">
    <!-- Loading Overlay -->
    <div v-if="loading" class="table-loading-overlay">
      <AppSpinner size="large" />
    </div>

    <!-- Table -->
    <div class="table-wrapper">
      <table class="data-table" :class="tableClasses">
        <!-- Table Header -->
        <thead class="table-header">
          <tr>
            <!-- Selection Column -->
            <th v-if="selectable" class="table-cell table-cell-checkbox">
              <input
                type="checkbox"
                :checked="allSelected"
                :indeterminate="someSelected"
                @change="toggleSelectAll"
                class="table-checkbox"
              />
            </th>

            <!-- Data Columns -->
            <th
              v-for="column in visibleColumns"
              :key="column.key"
              :class="getHeaderClass(column)"
              :style="{ width: column.width }"
              @click="column.sortable && handleSort(column.key)"
            >
              <div class="table-header-content">
                <span>{{ column.label }}</span>
                <span v-if="column.sortable" class="sort-icon">
                  <svg
                    v-if="sortState.column === column.key"
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                    class="icon"
                    :class="{ 'icon-rotate': sortState.direction === 'desc' }"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M5 15l7-7 7 7"
                    />
                  </svg>
                  <svg
                    v-else
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                    class="icon icon-inactive"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l4-4m-4 4l-4-4"
                    />
                  </svg>
                </span>
              </div>
            </th>

            <!-- Actions Column -->
            <th v-if="hasActions" class="table-cell table-cell-actions">Actions</th>
          </tr>
        </thead>

        <!-- Table Body -->
        <tbody class="table-body">
          <template v-if="!loading && data.length > 0">
            <tr
              v-for="(row, index) in data"
              :key="getRowKey(row, index)"
              class="table-row"
              :class="{ 'row-selected': isRowSelected(row) }"
            >
              <!-- Selection Cell -->
              <td v-if="selectable" class="table-cell table-cell-checkbox">
                <input
                  type="checkbox"
                  :checked="isRowSelected(row)"
                  @change="toggleRowSelection(row)"
                  class="table-checkbox"
                />
              </td>

              <!-- Data Cells -->
              <td
                v-for="column in visibleColumns"
                :key="column.key"
                class="table-cell"
                :class="`table-cell-${column.align || 'left'}`"
              >
                <component
                  v-if="column.component"
                  :is="column.component"
                  :value="getCellValue(row, column.key)"
                  :row="row"
                />
                <span v-else>
                  {{ formatCellValue(row, column) }}
                </span>
              </td>

              <!-- Actions Cell -->
              <td v-if="hasActions" class="table-cell table-cell-actions">
                <div class="table-actions">
                  <button
                    v-for="action in getVisibleActions(row)"
                    :key="action.label"
                    :class="['action-button', `action-${action.variant || 'secondary'}`]"
                    :disabled="isActionDisabled(action, row)"
                    @click="action.onClick(row)"
                    :title="action.label"
                  >
                    <span v-if="action.icon" v-html="action.icon" class="action-icon"></span>
                    <span class="action-label">{{ action.label }}</span>
                  </button>
                </div>
              </td>
            </tr>
          </template>

          <!-- Empty State -->
          <tr v-else-if="!loading">
            <td :colspan="totalColumns" class="table-empty">
              <div class="empty-state">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  class="empty-icon"
                >
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"
                  />
                </svg>
                <p>{{ emptyMessage || 'No data available' }}</p>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts" generic="T extends Record<string, any>">
import { ref, computed } from 'vue'
import AppSpinner from '../Spinner/AppSpinner.vue'
import type { TableColumn, TableAction, SortState } from './DataTable.types'

interface Props {
  columns: TableColumn<T>[]
  data: T[]
  loading?: boolean
  sortable?: boolean
  selectable?: boolean
  actions?: TableAction<T>[]
  emptyMessage?: string
  rowKey?: string
  striped?: boolean
  hoverable?: boolean
  bordered?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
  sortable: true,
  selectable: false,
  striped: true,
  hoverable: true,
  bordered: false,
  rowKey: 'id',
})

const emit = defineEmits<{
  sort: [{ column: string; direction: 'asc' | 'desc' }]
  select: [rows: T[]]
}>()

// State
const sortState = ref<SortState>({
  column: null,
  direction: null,
})

const selectedRows = ref<Set<unknown>>(new Set())

// Computed
const visibleColumns = computed(() => props.columns.filter((col) => !col.hidden))

const hasActions = computed(() => props.actions && props.actions.length > 0)

const totalColumns = computed(() => {
  let count = visibleColumns.value.length
  if (props.selectable) count++
  if (hasActions.value) count++
  return count
})

const tableClasses = computed(() => ({
  'table-striped': props.striped,
  'table-hoverable': props.hoverable,
  'table-bordered': props.bordered,
}))

const allSelected = computed(
  () => props.data.length > 0 && selectedRows.value.size === props.data.length,
)

const someSelected = computed(
  () => selectedRows.value.size > 0 && selectedRows.value.size < props.data.length,
)

// Methods
function getRowKey(row: T, index: number): string | number {
  return row[props.rowKey] ?? index
}

function getCellValue(row: T, key: string): unknown {
  return key
    .split('.')
    .reduce(
      (obj: unknown, k) =>
        typeof obj === 'object' && obj !== null ? (obj as Record<string, unknown>)[k] : undefined,
      row,
    )
}

function formatCellValue(row: T, column: TableColumn<T>): string | number {
  const value = getCellValue(row, column.key)
  if (column.formatter) {
    return column.formatter(value, row)
  }
  if (typeof value === 'string' || typeof value === 'number') {
    return value
  }
  if (value == null) {
    return '-'
  }
  return JSON.stringify(value)
}

function getHeaderClass(column: TableColumn<T>): string[] {
  const classes = ['table-cell', 'table-header-cell']
  if (column.sortable) classes.push('sortable')
  if (column.align) classes.push(`table-cell-${column.align}`)
  return classes
}

function handleSort(columnKey: string): void {
  if (!props.sortable) return

  if (sortState.value.column === columnKey) {
    // Toggle direction or clear
    if (sortState.value.direction === 'asc') {
      sortState.value.direction = 'desc'
    } else {
      sortState.value = { column: null, direction: null }
    }
  } else {
    sortState.value = { column: columnKey, direction: 'asc' }
  }

  if (sortState.value.column && sortState.value.direction) {
    emit('sort', {
      column: sortState.value.column,
      direction: sortState.value.direction,
    })
  }
}

function isRowSelected(row: T): boolean {
  return selectedRows.value.has(getRowKey(row, -1))
}

function toggleRowSelection(row: T): void {
  const key = getRowKey(row, -1)
  if (selectedRows.value.has(key)) {
    selectedRows.value.delete(key)
  } else {
    selectedRows.value.add(key)
  }
  emitSelectedRows()
}

function toggleSelectAll(): void {
  if (allSelected.value) {
    selectedRows.value.clear()
  } else {
    props.data.forEach((row, index) => {
      selectedRows.value.add(getRowKey(row, index))
    })
  }
  emitSelectedRows()
}

function emitSelectedRows(): void {
  const selected = props.data.filter((row, index) => selectedRows.value.has(getRowKey(row, index)))
  emit('select', selected)
}

function getVisibleActions(row: T): TableAction<T>[] {
  if (!props.actions) return []
  return props.actions.filter((action) => !action.visible || action.visible(row))
}

function isActionDisabled(action: TableAction<T>, row: T): boolean {
  return action.disabled ? action.disabled(row) : false
}

// Expose methods
defineExpose({
  clearSelection: () => selectedRows.value.clear(),
  getSelectedRows: () =>
    props.data.filter((row, index) => selectedRows.value.has(getRowKey(row, index))),
})
</script>

<style scoped lang="scss">
.data-table-container {
  position: relative;
  width: 100%;
}

.table-loading-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(255, 255, 255, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10;
  border-radius: 8px;
}

.table-wrapper {
  overflow-x: auto;
  border-radius: 8px;
  border: 1px solid #e5e7eb;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  background: white;

  &.table-bordered {
    .table-cell {
      border: 1px solid #e5e7eb;
    }
  }

  &.table-striped {
    .table-body .table-row:nth-child(even) {
      background-color: #f9fafb;
    }
  }

  &.table-hoverable {
    .table-body .table-row:hover {
      background-color: #f3f4f6;
    }
  }
}

.table-header {
  background-color: #f9fafb;
  border-bottom: 2px solid #e5e7eb;

  .table-cell {
    padding: 12px 16px;
    font-weight: 600;
    font-size: 14px;
    color: #374151;
    text-align: left;
    white-space: nowrap;

    &.sortable {
      cursor: pointer;
      user-select: none;
      transition: background-color 0.2s;

      &:hover {
        background-color: #f3f4f6;
      }
    }

    &.table-cell-center {
      text-align: center;
    }

    &.table-cell-right {
      text-align: right;
    }
  }
}

.table-header-content {
  display: flex;
  align-items: center;
  gap: 8px;
}

.sort-icon {
  display: inline-flex;
  align-items: center;

  .icon {
    width: 16px;
    height: 16px;
    transition: transform 0.2s;

    &.icon-rotate {
      transform: rotate(180deg);
    }

    &.icon-inactive {
      opacity: 0.3;
    }
  }
}

.table-body {
  .table-row {
    border-bottom: 1px solid #e5e7eb;
    transition: background-color 0.15s;

    &.row-selected {
      background-color: #eff6ff;
    }

    &:last-child {
      border-bottom: none;
    }
  }

  .table-cell {
    padding: 12px 16px;
    font-size: 14px;
    color: #1f2937;
    vertical-align: middle;

    &.table-cell-center {
      text-align: center;
    }

    &.table-cell-right {
      text-align: right;
    }
  }
}

.table-cell-checkbox {
  width: 48px;
  padding: 12px 16px;

  .table-checkbox {
    cursor: pointer;
    width: 16px;
    height: 16px;
  }
}

.table-cell-actions {
  width: 120px;
  text-align: right;
}

.table-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  align-items: center;
}

.action-button {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 6px 12px;
  font-size: 13px;
  font-weight: 500;
  border-radius: 6px;
  border: 1px solid transparent;
  cursor: pointer;
  transition: all 0.2s;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  .action-icon {
    width: 16px;
    height: 16px;
    display: flex;
    align-items: center;
    justify-content: center;

    :deep(svg) {
      width: 100%;
      height: 100%;
    }
  }

  &.action-primary {
    background: #667eea;
    color: white;

    &:hover:not(:disabled) {
      background: #5a67d8;
    }
  }

  &.action-secondary {
    background: #e5e7eb;
    color: #374151;

    &:hover:not(:disabled) {
      background: #d1d5db;
    }
  }

  &.action-danger {
    background: #f87171;
    color: white;

    &:hover:not(:disabled) {
      background: #ef4444;
    }
  }

  &.action-success {
    background: #34d399;
    color: white;

    &:hover:not(:disabled) {
      background: #10b981;
    }
  }
}

.table-empty {
  padding: 48px 16px;
  text-align: center;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  color: #9ca3af;

  .empty-icon {
    width: 48px;
    height: 48px;
    stroke: currentColor;
  }

  p {
    margin: 0;
    font-size: 14px;
  }
}
</style>
