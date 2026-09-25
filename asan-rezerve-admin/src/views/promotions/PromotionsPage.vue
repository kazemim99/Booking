<template>
  <div class="page-container">
    <a-page-header :title="t('promotions.title')" :sub-title="t('promotions.subtitle')">
      <template #extra>
        <a-button type="primary" @click="openCreate">
          <template #icon><plus-outlined /></template>
          {{ t('promotions.newCampaign') }}
        </a-button>
      </template>
    </a-page-header>

    <a-card>
      <a-tabs :active-key="p.owner.value" @change="(key: string | number) => p.setOwner(key as PromotionOwner)">
        <a-tab-pane key="Platform" :tab="t('promotions.tabs.Platform')" />
        <a-tab-pane key="Provider" :tab="t('promotions.tabs.Provider')" />
      </a-tabs>

      <div class="filters">
        <a-input-search
          v-model:value="p.search.value"
          :placeholder="t('promotions.searchPlaceholder')"
          allow-clear
          class="filters__search"
          @search="p.applyFilters"
        />
        <a-select v-model:value="p.status.value" class="filters__status" @change="p.applyFilters">
          <a-select-option value="all">{{ t('promotions.allStatuses') }}</a-select-option>
          <a-select-option v-for="s in STATUSES" :key="s" :value="s">{{ t(`promotions.status.${s}`) }}</a-select-option>
        </a-select>
      </div>

      <a-alert v-if="p.error.value" type="error" show-icon :message="p.error.value" class="mb">
        <template v-if="!p.loaded.value" #action>
          <a-button size="small" @click="p.load">{{ t('promotions.retry') }}</a-button>
        </template>
      </a-alert>

      <a-table
        :columns="columns"
        :data-source="p.items.value"
        :loading="p.loading.value"
        :pagination="pagination"
        row-key="id"
        :scroll="{ x: 960 }"
        @change="(pg: { current?: number }) => p.goTo(pg.current ?? 1)"
      >
        <template #emptyText>
          <a-empty :description="t(`promotions.empty.${p.owner.value}`)">
            <a-button v-if="p.owner.value === 'Platform'" type="primary" @click="openCreate">
              {{ t('promotions.newCampaign') }}
            </a-button>
          </a-empty>
        </template>

        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'title'">
            <a class="title-link" @click="openDetails(record as Promotion)">{{ record.title }}</a>
            <div class="sub">
              <a-tag v-if="record.activation === 'Code'" class="code-tag">{{ record.code }}</a-tag>
              <span v-else>{{ t('promotions.activation.Automatic') }}</span>
              <span v-if="record.owner === 'Provider' && record.providerName"> · {{ record.providerName }}</span>
            </div>
          </template>
          <template v-else-if="column.key === 'benefit'">
            <span class="benefit">{{ benefitText(record as Promotion) }}</span>
            <div v-if="conditionsOf(record as Promotion).length" class="sub">{{ conditionsOf(record as Promotion).join(' · ') }}</div>
          </template>
          <template v-else-if="column.key === 'period'">
            <div>{{ formatDate(record.startsAt) }}</div>
            <div class="sub">{{ record.endsAt ? formatDate(record.endsAt) : t('promotions.form.noEnd') }}</div>
          </template>
          <template v-else-if="column.key === 'state'">
            <a-tag :color="STATE_COLORS[record.state as PromotionState]">{{ t(`promotions.state.${record.state}`) }}</a-tag>
            <a-tooltip v-if="record.pausedByPlatform" :title="t('promotions.pausedByPlatform')">
              <lock-outlined />
            </a-tooltip>
          </template>
          <template v-else-if="column.key === 'usage'">
            <div>{{ usageText(record as Promotion) }}</div>
            <a-progress
              v-if="record.totalUsageLimit"
              :percent="Math.min(100, Math.round((record.uses / record.totalUsageLimit) * 100))"
              size="small"
              :show-info="false"
            />
            <div class="sub">{{ formatToman(record.totalDiscount) }}</div>
          </template>
          <template v-else-if="column.key === 'salons'">
            <span v-if="record.owner === 'Platform'">{{ record.joinedSalons ?? 0 }}</span>
            <span v-else>—</span>
          </template>
          <template v-else-if="column.key === 'actions'">
            <a-space wrap>
              <a-button v-if="record.owner === 'Platform' && record.status !== 'Ended'" size="small" @click="openEdit(record as Promotion)">
                {{ t('common.edit') }}
              </a-button>
              <template v-for="action in availableActions(record as Promotion)" :key="action">
                <a-popconfirm
                  v-if="action === 'end'"
                  :title="t('promotions.confirmEnd')"
                  :ok-text="t('common.yes')"
                  :cancel-text="t('common.no')"
                  @confirm="act(record as Promotion, 'end')"
                >
                  <a-button size="small" danger :loading="p.busy.value === record.id">{{ t('promotions.actions.end') }}</a-button>
                </a-popconfirm>
                <a-button v-else size="small" :loading="p.busy.value === record.id" @click="act(record as Promotion, action)">
                  {{ t(`promotions.actions.${action}`) }}
                </a-button>
              </template>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <campaign-form-drawer
      :open="formOpen"
      :editing="editing"
      :save="p.save"
      @close="formOpen = false"
      @saved="onSaved"
    />

    <a-drawer :open="!!details" :width="520" :title="details?.promotion.title" @close="details = null">
      <template v-if="details">
        <a-descriptions :column="1" size="small" bordered>
          <a-descriptions-item :label="t('promotions.columns.benefit')">{{ benefitText(details.promotion) }}</a-descriptions-item>
          <a-descriptions-item :label="t('promotions.columns.state')">
            <a-tag :color="STATE_COLORS[details.promotion.state]">{{ t(`promotions.state.${details.promotion.state}`) }}</a-tag>
          </a-descriptions-item>
          <a-descriptions-item v-if="details.promotion.code" :label="t('promotions.form.code')">
            <span dir="ltr" class="mono">{{ details.promotion.code }}</span>
          </a-descriptions-item>
          <a-descriptions-item :label="t('promotions.form.validity')">
            {{ formatDateTime(details.promotion.startsAt) }} — {{ details.promotion.endsAt ? formatDateTime(details.promotion.endsAt) : t('promotions.form.noEnd') }}
          </a-descriptions-item>
          <a-descriptions-item v-if="conditionsOf(details.promotion).length" :label="t('promotions.form.sectionConditions')">
            {{ conditionsOf(details.promotion).join(' · ') }}
          </a-descriptions-item>
          <a-descriptions-item :label="t('promotions.columns.usage')">{{ usageText(details.promotion) }}</a-descriptions-item>
          <a-descriptions-item :label="t('promotions.totalDiscount')">{{ formatToman(details.promotion.totalDiscount) }}</a-descriptions-item>
          <a-descriptions-item v-if="details.promotion.providerName" :label="t('promotions.salon')">
            <a @click="router.push(`/providers/${details.promotion.providerId}`)">{{ details.promotion.providerName }}</a>
          </a-descriptions-item>
        </a-descriptions>

        <template v-if="details.promotion.owner === 'Platform'">
          <h4 class="participants-title">{{ t('promotions.participants', { n: details.participants.length }) }}</h4>
          <a-empty v-if="!details.participants.length" :description="t('promotions.noParticipants')" />
          <a-list v-else size="small" :data-source="details.participants">
            <template #renderItem="{ item }">
              <a-list-item>
                <a @click="router.push(`/providers/${item.providerId}`)">{{ item.providerName || item.providerId }}</a>
                <template #extra>{{ formatDate(item.joinedAt) }}</template>
              </a-list-item>
            </template>
          </a-list>
        </template>
      </template>
    </a-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import { LockOutlined, PlusOutlined } from '@ant-design/icons-vue'
import type { LifecycleAction, Promotion, PromotionDetails, PromotionOwner, PromotionState } from '../../api/promotions.api'
import { formatDate, formatDateTime } from '../../utils/date'
import CampaignFormDrawer from './CampaignFormDrawer.vue'
import { STATE_COLORS, availableActions, benefitText, formatToman, usageText, PERSIAN_WEEK } from './promotionForm'
import { usePromotions } from './usePromotions'

/**
 * Discounts and campaigns. Platform campaigns are created here and joined by salons in the provider app; salons'
 * own promotions are listed for oversight (pause/end, never reworded). Behaviour lives in usePromotions (tested).
 */
const STATUSES = ['Active', 'Paused', 'Ended'] as const

const { t } = useI18n()
const router = useRouter()
const p = usePromotions()

const formOpen = ref(false)
const editing = ref<Promotion | null>(null)
const details = ref<PromotionDetails | null>(null)

const columns = computed(() => [
  { title: t('promotions.columns.title'), key: 'title', width: 220 },
  { title: t('promotions.columns.benefit'), key: 'benefit', width: 220 },
  { title: t('promotions.columns.period'), key: 'period', width: 150 },
  { title: t('promotions.columns.state'), key: 'state', width: 110 },
  { title: t('promotions.columns.usage'), key: 'usage', width: 140 },
  ...(p.owner.value === 'Platform' ? [{ title: t('promotions.columns.salons'), key: 'salons', width: 90 }] : []),
  { title: t('common.actions'), key: 'actions', width: 200 },
])

const pagination = computed(() => ({
  current: p.page.value,
  pageSize: 20,
  total: p.totalCount.value,
  showSizeChanger: false,
  showTotal: (total: number) => t('promotions.totalCount', { n: total }),
}))

function conditionsOf(promotion: Promotion): string[] {
  const parts: string[] = []
  if (promotion.newCustomersOnly) parts.push(t('promotions.conditions.newCustomers'))
  if (promotion.minimumSubtotal) parts.push(t('promotions.conditions.minimum', { amount: formatToman(promotion.minimumSubtotal) }))
  if (promotion.daysOfWeek.length && promotion.daysOfWeek.length < 7) {
    const days = PERSIAN_WEEK.filter((d) => promotion.daysOfWeek.includes(d)).map((d) => t(`promotions.days.${d}`))
    parts.push(days.join('، '))
  }
  if (promotion.dailyStartTime && promotion.dailyEndTime)
    parts.push(t('promotions.conditions.hours', { from: promotion.dailyStartTime, to: promotion.dailyEndTime }))
  if (promotion.perCustomerLimit) parts.push(t('promotions.conditions.perCustomer', { n: promotion.perCustomerLimit }))
  return parts
}

function openCreate() {
  editing.value = null
  formOpen.value = true
}

function openEdit(promotion: Promotion) {
  editing.value = promotion
  formOpen.value = true
}

async function openDetails(promotion: Promotion) {
  details.value = await p.details(promotion.id)
}

function onSaved() {
  formOpen.value = false
  message.success(t('promotions.saved'))
}

async function act(promotion: Promotion, action: LifecycleAction) {
  if (await p.act(promotion, action)) message.success(t('promotions.done'))
}

onMounted(p.load)
</script>

<style scoped>
.mb { margin-bottom: 16px; }
.filters { display: flex; gap: 12px; flex-wrap: wrap; margin-bottom: 16px; }
.filters__search { max-width: 320px; }
.filters__status { width: 160px; }
.title-link { font-weight: 600; }
.sub { color: var(--color-gray-500, #8c8c8c); font-size: 12px; margin-top: 2px; }
.code-tag { font-family: monospace; direction: ltr; }
.benefit { font-weight: 600; color: var(--color-success-700, #237804); }
.mono { font-family: monospace; letter-spacing: 1px; }
.participants-title { margin: 20px 0 8px; }
</style>
