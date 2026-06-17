<template>
  <div class="page-container">
    <a-page-header :title="$t('user.management')" :sub-title="$t('user.manageSystemUsers')">
      <template #extra>
        <a-button type="primary" @click="showCreateModal">
          <plus-outlined /> {{ $t('user.createUser') }}
        </a-button>
      </template>
    </a-page-header>

    <div class="filter-section">
      <a-row :gutter="16">
        <a-col :xs="24" :sm="12" :md="8">
          <a-input
            v-model:value="filters.search"
            :placeholder="$t('user.searchPlaceholder')"
            allow-clear
            @change="handleSearch"
          >
            <template #prefix>
              <search-outlined />
            </template>
          </a-input>
        </a-col>

        <a-col :xs="24" :sm="12" :md="6">
          <a-select
            v-model:value="filters.role"
            :placeholder="$t('user.filterByRole')"
            allow-clear
            style="width: 100%"
            @change="loadUsers"
          >
            <a-select-option value="Admin">{{ $t('user.roleAdmin') }}</a-select-option>
            <a-select-option value="Provider">{{ $t('user.roleProvider') }}</a-select-option>
            <a-select-option value="Client">{{ $t('user.roleClient') }}</a-select-option>
          </a-select>
        </a-col>

        <a-col :xs="24" :sm="12" :md="6">
          <a-select
            v-model:value="filters.isActive"
            :placeholder="$t('user.filterByStatus')"
            allow-clear
            style="width: 100%"
            @change="loadUsers"
          >
            <a-select-option :value="true">{{ $t('user.statusActive') }}</a-select-option>
            <a-select-option :value="false">{{ $t('user.statusInactive') }}</a-select-option>
          </a-select>
        </a-col>

        <a-col :xs="24" :sm="12" :md="4">
          <a-button block @click="resetFilters">{{ $t('common.reset') }}</a-button>
        </a-col>
      </a-row>
    </div>

    <a-card>
      <a-table
        :columns="columns"
        :data-source="users"
        :loading="loading"
        :pagination="pagination"
        @change="handleTableChange"
        row-key="id"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'name'">
            <div>
              <div>{{ record.firstName }} {{ record.lastName }}</div>
              <div style="font-size: 12px; color: #999">{{ record.email }}</div>
            </div>
          </template>

          <template v-if="column.key === 'role'">
            <a-tag :color="getRoleColor(record.role)">
              {{ record.role }}
            </a-tag>
          </template>

          <template v-if="column.key === 'status'">
            <a-badge :status="record.isActive ? 'success' : 'default'" />
            <span>{{ record.isActive ? $t('user.statusActive') : $t('user.statusInactive') }}</span>
          </template>

          <template v-if="column.key === 'lastLogin'">
            {{ formatDate(record.lastLoginAt) }}
          </template>

          <template v-if="column.key === 'actions'">
            <div class="table-actions">
              <a-button type="link" size="small" @click="handleEdit(record)">
                <edit-outlined /> {{ $t('common.edit') }}
              </a-button>
              <a-button
                type="link"
                size="small"
                @click="handleToggleStatus(record)"
              >
                <check-circle-outlined v-if="!record.isActive" />
                <stop-outlined v-else />
                {{ record.isActive ? $t('common.deactivate') : $t('common.activate') }}
              </a-button>
              <a-popconfirm
                :title="$t('user.deleteConfirmation')"
                :ok-text="$t('common.yes')"
                :cancel-text="$t('common.no')"
                @confirm="handleDelete(record.id)"
              >
                <a-button type="link" danger size="small">
                  <delete-outlined /> {{ $t('common.delete') }}
                </a-button>
              </a-popconfirm>
            </div>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal
      v-model:open="modalVisible"
      :title="editingUser ? $t('user.editUser') : $t('user.createUser')"
      :confirm-loading="modalLoading"
      @ok="handleSubmit"
    >
      <a-form :model="formData" layout="vertical">
        <a-form-item :label="$t('auth.email')" name="email" required>
          <a-input v-model:value="formData.email" :disabled="!!editingUser" />
        </a-form-item>

        <a-form-item v-if="!editingUser" :label="$t('auth.password')" name="password" required>
          <a-input-password v-model:value="formData.password" />
        </a-form-item>

        <a-form-item :label="$t('user.firstName')" name="firstName">
          <a-input v-model:value="formData.firstName" />
        </a-form-item>

        <a-form-item :label="$t('user.lastName')" name="lastName">
          <a-input v-model:value="formData.lastName" />
        </a-form-item>

        <a-form-item :label="$t('user.phoneNumber')" name="phoneNumber">
          <a-input v-model:value="formData.phoneNumber" />
        </a-form-item>

        <a-form-item :label="$t('user.role')" name="role" required>
          <a-select v-model:value="formData.role">
            <a-select-option value="Admin">{{ $t('user.roleAdmin') }}</a-select-option>
            <a-select-option value="Provider">{{ $t('user.roleProvider') }}</a-select-option>
            <a-select-option value="Client">{{ $t('user.roleClient') }}</a-select-option>
          </a-select>
        </a-form-item>

        <a-form-item v-if="editingUser" :label="$t('user.status')" name="isActive">
          <a-switch v-model:checked="formData.isActive" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import {
  PlusOutlined,
  SearchOutlined,
  EditOutlined,
  DeleteOutlined,
  CheckCircleOutlined,
  StopOutlined,
} from '@ant-design/icons-vue'
import { usersApi } from '../../api/users.api'
import type { User } from '../../types'

const { t } = useI18n()

dayjs.extend(relativeTime)

const loading = ref(false)
const modalVisible = ref(false)
const modalLoading = ref(false)
const users = ref<User[]>([])
const editingUser = ref<User | null>(null)

const filters = reactive({
  search: '',
  role: undefined,
  isActive: undefined,
})

const pagination = reactive({
  current: 1,
  pageSize: 10,
  total: 0,
  showSizeChanger: true,
  showTotal: (total: number) => `${t('common.total')} ${total} ${t('user.users')}`,
})

const formData = reactive({
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  phoneNumber: '',
  role: 'Client' as 'Admin' | 'Provider' | 'Client',
  isActive: true,
})

const columns = computed(() => [
  { title: t('user.nameEmail'), key: 'name', width: 250 },
  { title: t('user.phone'), dataIndex: 'phoneNumber', key: 'phoneNumber' },
  { title: t('user.role'), key: 'role', width: 100 },
  { title: t('user.status'), key: 'status', width: 100 },
  { title: t('user.lastLogin'), key: 'lastLogin', width: 150 },
  { title: t('common.actions'), key: 'actions', width: 280, fixed: 'right' },
])

const getRoleColor = (role: string) => {
  const colors: Record<string, string> = {
    Admin: 'red',
    Provider: 'blue',
    Client: 'green',
  }
  return colors[role] || 'default'
}

const formatDate = (date?: string) => {
  if (!date) return t('user.never')
  return dayjs(date).fromNow()
}

const loadUsers = async () => {
  loading.value = true
  try {
    const response = await usersApi.getUsers({
      pageNumber: pagination.current,
      pageSize: pagination.pageSize,
      search: filters.search || undefined,
      role: filters.role,
      isActive: filters.isActive,
    })
    users.value = response.items
    pagination.total = response.totalCount
  } catch (error) {
    message.error(t('messages.failedToLoadUsers'))
  } finally {
    loading.value = false
  }
}

const handleTableChange = (pag: any) => {
  pagination.current = pag.current
  pagination.pageSize = pag.pageSize
  loadUsers()
}

const handleSearch = () => {
  pagination.current = 1
  loadUsers()
}

const resetFilters = () => {
  filters.search = ''
  filters.role = undefined
  filters.isActive = undefined
  pagination.current = 1
  loadUsers()
}

const showCreateModal = () => {
  editingUser.value = null
  Object.assign(formData, {
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    phoneNumber: '',
    role: 'Client',
    isActive: true,
  })
  modalVisible.value = true
}

const handleEdit = (user: User) => {
  editingUser.value = user
  Object.assign(formData, {
    email: user.email,
    firstName: user.firstName || '',
    lastName: user.lastName || '',
    phoneNumber: user.phoneNumber || '',
    role: user.role,
    isActive: user.isActive,
  })
  modalVisible.value = true
}

const handleSubmit = async () => {
  modalLoading.value = true
  try {
    if (editingUser.value) {
      await usersApi.updateUser(editingUser.value.id, {
        firstName: formData.firstName,
        lastName: formData.lastName,
        phoneNumber: formData.phoneNumber,
        role: formData.role,
        isActive: formData.isActive,
      })
      message.success(t('messages.userUpdatedSuccessfully'))
    } else {
      await usersApi.createUser(formData)
      message.success(t('messages.userCreatedSuccessfully'))
    }
    modalVisible.value = false
    loadUsers()
  } catch (error) {
    message.error(t('messages.operationFailed'))
  } finally {
    modalLoading.value = false
  }
}

const handleToggleStatus = async (user: User) => {
  try {
    await usersApi.toggleUserStatus(user.id)
    message.success(t('messages.userStatusUpdated'))
    loadUsers()
  } catch (error) {
    message.error(t('messages.failedToUpdateUserStatus'))
  }
}

const handleDelete = async (id: string) => {
  try {
    await usersApi.deleteUser(id)
    message.success(t('messages.userDeletedSuccessfully'))
    loadUsers()
  } catch (error) {
    message.error(t('messages.failedToDeleteUser'))
  }
}

onMounted(() => {
  loadUsers()
})
</script>
