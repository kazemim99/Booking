<template>
  <div class="staff-management-dashboard">
    <!-- Header -->
    <div class="dashboard-header">
      <div class="header-content">
        <h1 class="header-title">مدیریت کارکنان</h1>
        <p class="header-description">مدیریت تیم خود، دعوت از کارکنان جدید و بررسی درخواست‌ها</p>
      </div>
      <div class="header-actions">
        <AppButton
          variant="primary"
          size="medium"
          @click="showInviteModal = true"
        >
          <i class="icon-user-plus"></i>
          دعوت کارمند جدید
        </AppButton>
      </div>
    </div>

    <!-- Stats Cards -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon" style="background: linear-gradient(135deg, #10b981 0%, #059669 100%)">
          <i class="icon-users"></i>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ staffCount }}</div>
          <div class="stat-label">کل کارکنان</div>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon" style="background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)">
          <i class="icon-user-check"></i>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ activeStaffCount }}</div>
          <div class="stat-label">کارکنان فعال</div>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon" style="background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%)">
          <i class="icon-mail"></i>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ pendingInvitationsCount }}</div>
          <div class="stat-label">دعوت‌های در انتظار</div>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon" style="background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%)">
          <i class="icon-clipboard"></i>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ pendingRequestsCount }}</div>
          <div class="stat-label">درخواست‌های پیوستن</div>
        </div>
      </div>
    </div>

    <!-- Tabs -->
    <div class="tabs-container">
      <div class="tabs">
        <button
          class="tab"
          :class="{ active: activeTab === 'staff' }"
          @click="activeTab = 'staff'"
        >
          <i class="icon-users"></i>
          کارکنان ({{ staffCount }})
        </button>
        <button
          class="tab"
          :class="{ active: activeTab === 'invitations' }"
          @click="activeTab = 'invitations'"
        >
          <i class="icon-mail"></i>
          دعوت‌ها ({{ pendingInvitationsCount }})
        </button>
        <button
          class="tab"
          :class="{ active: activeTab === 'requests' }"
          @click="activeTab === 'requests'"
        >
          <i class="icon-clipboard"></i>
          درخواست‌ها ({{ pendingRequestsCount }})
        </button>
      </div>
    </div>

    <!-- Tab Content -->
    <div class="tab-content">
      <!-- Staff List Tab -->
      <div v-if="activeTab === 'staff'" class="staff-tab">
        <!-- Filters -->
        <div class="filters-bar">
          <div class="search-box">
            <i class="icon-search"></i>
            <input
              v-model="searchQuery"
              type="text"
              placeholder="جستجوی کارکنان..."
              class="search-input"
            />
          </div>

          <div class="filter-group">
            <select v-model="statusFilter" class="filter-select">
              <option value="all">همه وضعیت‌ها</option>
              <option value="active">فعال</option>
              <option value="inactive">غیرفعال</option>
            </select>
          </div>
        </div>

        <!-- Staff List -->
        <div v-if="isLoadingStaff" class="loading-state">
          <div class="spinner"></div>
          <p>در حال بارگذاری کارکنان...</p>
        </div>

        <div v-else-if="filteredStaff.length === 0" class="empty-state">
          <i class="icon-users"></i>
          <h3>کارمندی یافت نشد</h3>
          <p v-if="searchQuery">جستجوی دیگری امتحان کنید یا فیلترها را تغییر دهید</p>
          <p v-else>هنوز کارمندی ندارید. با کلیک روی دکمه بالا کارمند جدید دعوت کنید.</p>
        </div>

        <div v-else class="staff-grid">
          <StaffMemberCard
            v-for="staff in filteredStaff"
            :key="staff.id"
            :staff="staff"
            @view="viewStaffDetails"
            @edit="editStaff"
            @remove="confirmRemoveStaff"
          />
        </div>

        <!-- Pagination -->
        <div v-if="totalPages > 1" class="pagination">
          <button
            class="pagination-btn"
            :disabled="currentPage === 1"
            @click="goToPage(currentPage - 1)"
          >
            <i class="icon-chevron-right"></i>
          </button>

          <div class="pagination-pages">
            <button
              v-for="page in displayedPages"
              :key="page"
              class="pagination-page"
              :class="{ active: page === currentPage }"
              @click="goToPage(page)"
            >
              {{ page }}
            </button>
          </div>

          <button
            class="pagination-btn"
            :disabled="currentPage === totalPages"
            @click="goToPage(currentPage + 1)"
          >
            <i class="icon-chevron-left"></i>
          </button>
        </div>
      </div>

      <!-- Invitations Tab -->
      <div v-else-if="activeTab === 'invitations'" class="invitations-tab">
        <div v-if="isLoadingInvitations" class="loading-state">
          <div class="spinner"></div>
          <p>در حال بارگذاری دعوت‌ها...</p>
        </div>

        <div v-else-if="pendingInvitations.length === 0" class="empty-state">
          <i class="icon-mail"></i>
          <h3>دعوتی وجود ندارد</h3>
          <p>دعوت‌های ارسال شده در اینجا نمایش داده می‌شوند</p>
        </div>

        <div v-else class="invitations-list">
          <InvitationCard
            v-for="invitation in pendingInvitations"
            :key="invitation.id"
            :invitation="invitation"
            @resend="resendInvitation"
            @cancel="cancelInvitation"
          />
        </div>
      </div>

      <!-- Join Requests Tab -->
      <div v-else-if="activeTab === 'requests'" class="requests-tab">
        <div v-if="isLoadingRequests" class="loading-state">
          <div class="spinner"></div>
          <p>در حال بارگذاری درخواست‌ها...</p>
        </div>

        <div v-else-if="pendingJoinRequests.length === 0" class="empty-state">
          <i class="icon-clipboard"></i>
          <h3>درخواستی وجود ندارد</h3>
          <p>درخواست‌های پیوستن در اینجا نمایش داده می‌شوند</p>
        </div>

        <div v-else class="requests-list">
          <JoinRequestCard
            v-for="request in pendingJoinRequests"
            :key="request.id"
            :request="request"
            @approve="approveRequest"
            @reject="rejectRequest"
          />
        </div>
      </div>
    </div>

    <!-- Invite Staff Modal -->
    <InviteStaffModal
      v-if="showInviteModal"
      :organization-id="organizationId"
      @close="showInviteModal = false"
      @invited="handleInvitationSent"
    />

    <!-- Remove Staff Confirmation Modal -->
    <ConfirmationModal
      v-if="showRemoveConfirm"
      title="حذف کارمند"
      :message="`آیا مطمئن هستید که می‌خواهید ${staffToRemove?.fullName} را از تیم خود حذف کنید؟`"
      confirm-text="حذف کارمند"
      cancel-text="انصراف"
      variant="danger"
      @confirm="handleRemoveStaff"
      @cancel="showRemoveConfirm = false"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useHierarchyStore } from '../../stores/hierarchy.store'
import type { StaffMember, ProviderInvitation, ProviderJoinRequest } from '../../types/hierarchy.types'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'
import StaffMemberCard from './StaffMemberCard.vue'
import InvitationCard from './InvitationCard.vue'
import JoinRequestCard from './JoinRequestCard.vue'
import InviteStaffModal from './InviteStaffModal.vue'
import ConfirmationModal from '@/shared/components/ConfirmationModal.vue'
import { useToast } from '@/core/composables/useToast'

// ============================================
// Props
// ============================================

interface Props {
  organizationId: string
}

const props = defineProps<Props>()

// ============================================
// Composables
// ============================================

const hierarchyStore = useHierarchyStore()
const { showSuccess, showError } = useToast()

// ============================================
// State
// ============================================

const activeTab = ref<'staff' | 'invitations' | 'requests'>('staff')
const searchQuery = ref('')
const statusFilter = ref<'all' | 'active' | 'inactive'>('all')
const currentPage = ref(1)
const pageSize = ref(12)

const showInviteModal = ref(false)
const showRemoveConfirm = ref(false)
const staffToRemove = ref<StaffMember | null>(null)

// ============================================
// Computed
// ============================================

const staffCount = computed(() => hierarchyStore.staffCount)
const activeStaffCount = computed(() => hierarchyStore.activeStaffCount)

const staffList = computed(() => hierarchyStore.staffMembers?.items || [])

const filteredStaff = computed(() => {
  let result = staffList.value

  // Search filter
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    result = result.filter(
      (staff) =>
        staff.fullName.toLowerCase().includes(query) ||
        staff.email?.toLowerCase().includes(query) ||
        staff.phone?.toLowerCase().includes(query)
    )
  }

  // Status filter
  if (statusFilter.value !== 'all') {
    const isActive = statusFilter.value === 'active'
    result = result.filter((staff) => staff.isActive === isActive)
  }

  return result
})

const pendingInvitations = computed(
  () => hierarchyStore.pendingInvitations?.items || []
)

const pendingInvitationsCount = computed(
  () => hierarchyStore.pendingInvitations?.totalCount || 0
)

const pendingJoinRequests = computed(
  () => hierarchyStore.pendingJoinRequests?.items || []
)

const pendingRequestsCount = computed(
  () => hierarchyStore.pendingJoinRequests?.totalCount || 0
)

const isLoadingStaff = computed(() => hierarchyStore.isLoadingStaff)
const isLoadingInvitations = computed(() => hierarchyStore.isLoadingInvitations)
const isLoadingRequests = computed(() => hierarchyStore.isLoadingJoinRequests)

const totalPages = computed(() => Math.ceil(filteredStaff.value.length / pageSize.value))

const displayedPages = computed(() => {
  const pages = []
  const maxPages = 5
  let start = Math.max(1, currentPage.value - 2)
  const end = Math.min(totalPages.value, start + maxPages - 1)

  if (end - start < maxPages - 1) {
    start = Math.max(1, end - maxPages + 1)
  }

  for (let i = start; i <= end; i++) {
    pages.push(i)
  }

  return pages
})

// ============================================
// Methods
// ============================================

async function loadData(): Promise<void> {
  try {
    await Promise.all([
      hierarchyStore.loadStaffMembers({ organizationId: props.organizationId }),
      hierarchyStore.loadPendingInvitations({ organizationId: props.organizationId }),
      hierarchyStore.loadPendingJoinRequests({ organizationId: props.organizationId }),
    ])
  } catch (error) {
    showError('خطا در بارگذاری اطلاعات')
    console.error('Error loading staff data:', error)
  }
}

function viewStaffDetails(staff: StaffMember): void {
  // Navigate to staff details page or show modal
  console.log('View staff:', staff)
}

function editStaff(staff: StaffMember): void {
  // Navigate to edit staff page or show modal
  console.log('Edit staff:', staff)
}

function confirmRemoveStaff(staff: StaffMember): void {
  staffToRemove.value = staff
  showRemoveConfirm.value = true
}

async function handleRemoveStaff(): Promise<void> {
  if (!staffToRemove.value) return

  try {
    await hierarchyStore.removeStaffMember(props.organizationId, staffToRemove.value.id, {
      organizationId: props.organizationId,
      staffMemberId: staffToRemove.value.id,
      reason: 'Removed by organization',
    })

    showSuccess('کارمند با موفقیت حذف شد')
    showRemoveConfirm.value = false
    staffToRemove.value = null
  } catch (error) {
    showError('خطا در حذف کارمند')
    console.error('Error removing staff:', error)
  }
}

function handleInvitationSent(): void {
  showInviteModal.value = false
  showSuccess('دعوت با موفقیت ارسال شد')
  loadData()
}

async function resendInvitation(invitation: ProviderInvitation): Promise<void> {
  // Implement resend logic
  console.log('Resend invitation:', invitation)
  showSuccess('دعوت مجدداً ارسال شد')
}

async function cancelInvitation(invitation: ProviderInvitation): Promise<void> {
  // Implement cancel logic
  console.log('Cancel invitation:', invitation)
  showSuccess('دعوت لغو شد')
  loadData()
}

async function approveRequest(request: ProviderJoinRequest): Promise<void> {
  try {
    await hierarchyStore.approveJoinRequest(props.organizationId, request.id, {
      joinRequestId: request.id,
      approvedBy: props.organizationId,
    })
    showSuccess('درخواست تأیید شد')
  } catch (error) {
    showError('خطا در تأیید درخواست')
    console.error('Error approving request:', error)
  }
}

async function rejectRequest(request: ProviderJoinRequest): Promise<void> {
  try {
    await hierarchyStore.rejectJoinRequest(props.organizationId, request.id, {
      joinRequestId: request.id,
      rejectedBy: props.organizationId,
      rejectionReason: 'Not suitable at this time',
    })
    showSuccess('درخواست رد شد')
  } catch (error) {
    showError('خطا در رد درخواست')
    console.error('Error rejecting request:', error)
  }
}

function goToPage(page: number): void {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page
  }
}

// ============================================
// Lifecycle
// ============================================

onMounted(() => {
  loadData()
})

// Watch for tab changes and reload data
watch(activeTab, () => {
  if (activeTab.value === 'invitations' && pendingInvitations.value.length === 0) {
    hierarchyStore.loadPendingInvitations({ organizationId: props.organizationId })
  } else if (activeTab.value === 'requests' && pendingJoinRequests.value.length === 0) {
    hierarchyStore.loadPendingJoinRequests({ organizationId: props.organizationId })
  }
})
</script>

<style scoped lang="scss">
.staff-management-dashboard {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  gap: 1rem;

  @media (max-width: 768px) {
    flex-direction: column;
    align-items: flex-start;
  }
}

.header-title {
  font-size: 2rem;
  font-weight: 700;
  color: #1a1a1a;
  margin-bottom: 0.5rem;
}

.header-description {
  font-size: 1rem;
  color: #666;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.stat-card {
  background: #fff;
  border-radius: 12px;
  padding: 1.5rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  transition: transform 0.2s;

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
  }
}

.stat-icon {
  width: 60px;
  height: 60px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.75rem;
  color: #fff;
}

.stat-value {
  font-size: 2rem;
  font-weight: 700;
  color: #1a1a1a;
}

.stat-label {
  font-size: 0.9rem;
  color: #666;
}

.tabs-container {
  background: #fff;
  border-radius: 12px;
  padding: 0.5rem;
  margin-bottom: 2rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.tabs {
  display: flex;
  gap: 0.5rem;
}

.tab {
  flex: 1;
  padding: 1rem;
  background: transparent;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  color: #666;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;

  &:hover {
    background: #f3f4f6;
    color: #1a1a1a;
  }

  &.active {
    background: linear-gradient(135deg, #7c3aed 0%, #9333ea 100%);
    color: #fff;
  }
}

.tab-content {
  background: #fff;
  border-radius: 12px;
  padding: 2rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.filters-bar {
  display: flex;
  gap: 1rem;
  margin-bottom: 2rem;

  @media (max-width: 768px) {
    flex-direction: column;
  }
}

.search-box {
  flex: 1;
  position: relative;

  i {
    position: absolute;
    right: 1rem;
    top: 50%;
    transform: translateY(-50%);
    color: #9ca3af;
  }
}

.search-input {
  width: 100%;
  padding: 0.75rem 1rem 0.75rem 3rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 0.95rem;

  &:focus {
    outline: none;
    border-color: #7c3aed;
  }
}

.filter-select {
  padding: 0.75rem 1rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 0.95rem;
  background: #fff;

  &:focus {
    outline: none;
    border-color: #7c3aed;
  }
}

.staff-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

.loading-state,
.empty-state {
  text-align: center;
  padding: 4rem 2rem;
  color: #9ca3af;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #f3f4f6;
  border-top-color: #7c3aed;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.empty-state {
  i {
    font-size: 4rem;
    margin-bottom: 1rem;
  }

  h3 {
    font-size: 1.5rem;
    font-weight: 600;
    color: #6b7280;
    margin-bottom: 0.5rem;
  }

  p {
    font-size: 1rem;
    color: #9ca3af;
  }
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 0.5rem;
  margin-top: 2rem;
}

.pagination-btn,
.pagination-page {
  padding: 0.5rem 0.75rem;
  border: 1px solid #d1d5db;
  background: #fff;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;

  &:hover:not(:disabled) {
    background: #f9fafb;
    border-color: #7c3aed;
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.pagination-page.active {
  background: #7c3aed;
  color: #fff;
  border-color: #7c3aed;
}
</style>
