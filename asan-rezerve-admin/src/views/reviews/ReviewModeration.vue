<template>
  <div class="page-container">
    <a-page-header :title="t('reviews.title')" :sub-title="t('reviews.subtitle')" />

    <a-card>
      <a-tabs :active-key="m.filter.value" @change="(key: string | number) => m.setFilter(key as ModerationFilter)">
        <a-tab-pane v-for="f in FILTERS" :key="f" :tab="t(`reviews.tabs.${f}`)" />
      </a-tabs>

      <a-alert v-if="m.error.value" type="error" show-icon :message="m.error.value" class="mb">
        <template v-if="!m.loaded.value" #action>
          <a-button size="small" @click="m.load">{{ t('reviews.retry') }}</a-button>
        </template>
      </a-alert>

      <a-spin :spinning="m.loading.value">
        <a-empty
          v-if="m.loaded.value && m.items.value.length === 0"
          :description="t(`reviews.empty.${m.filter.value}`)"
        />

        <div class="queue">
          <a-card v-for="item in m.items.value" :key="item.reviewId" size="small" class="queue-item">
            <div class="queue-item__head">
              <a-rate :value="item.rating" allow-half disabled />
              <span class="muted">{{ new Date(item.createdAt).toLocaleString('fa-IR') }}</span>
              <a-tag v-if="item.reviewPending" :color="item.wasPublishedBefore ? 'orange' : 'blue'">
                {{ item.wasPublishedBefore ? t('reviews.edited') : t('reviews.firstSubmission') }}
              </a-tag>
              <a-tag v-if="item.replyPending" color="purple">{{ t('reviews.replyAwaiting') }}</a-tag>
              <a-button type="link" size="small" @click="router.push(`/providers/${item.providerId}`)">
                {{ t('navigation.providers') }}
              </a-button>
            </div>

            <div class="dims">
              <span v-for="d in dimensionsOf(item)" :key="d.key" class="dim">
                {{ t(`reviews.dimensions.${d.key}`) }}: {{ d.value }}
              </span>
            </div>

            <p class="comment">{{ item.comment || t('reviews.noComment') }}</p>

            <blockquote v-if="item.providerResponse" class="reply">
              <strong>{{ t('reviews.providerReply') }}</strong>
              <p>{{ item.providerResponse }}</p>
            </blockquote>

            <p v-if="item.moderationReason" class="muted">{{ t('reviews.reason') }}: {{ item.moderationReason }}</p>

            <div v-if="item.reports.length" class="reports">
              <strong>{{ t('reviews.reports') }} ({{ item.reportCount }})</strong>
              <ul>
                <li v-for="(r, i) in item.reports" :key="i">{{ r.reason }}</li>
              </ul>
            </div>

            <a-space wrap class="actions">
              <template v-if="item.reviewPending">
                <a-button type="primary" :loading="m.busy.value === item.reviewId" @click="decide('approve', item)">
                  {{ t('reviews.actions.approve') }}
                </a-button>
                <a-button danger @click="askReason('reject', item)">{{ t('reviews.actions.reject') }}</a-button>
              </template>
              <template v-if="item.replyPending">
                <a-button @click="decide('approveReply', item)">{{ t('reviews.actions.approveReply') }}</a-button>
                <a-button danger ghost @click="askReason('rejectReply', item)">{{ t('reviews.actions.rejectReply') }}</a-button>
              </template>
              <a-button
                v-if="item.moderationStatus === 'Published' && m.filter.value !== 'pending'"
                danger
                @click="askReason('hide', item)"
              >
                {{ t('reviews.actions.hide') }}
              </a-button>
              <a-button v-if="item.moderationStatus === 'Hidden'" @click="decide('restore', item)">
                {{ t('reviews.actions.restore') }}
              </a-button>
            </a-space>
          </a-card>
        </div>
      </a-spin>
    </a-card>

    <a-modal
      :open="!!pending"
      :title="pending ? t(`reviews.actions.${pending.action}`) : ''"
      :ok-button-props="{ disabled: !reason.trim() }"
      @ok="confirmReason"
      @cancel="pending = null"
    >
      <p v-if="pending?.action === 'reject'" class="muted">{{ t('reviews.rejectPermanent') }}</p>
      <!-- The reason is not an internal note: say who will read it before it is typed. -->
      <p v-if="reasonReader" class="reason-audience">{{ t(`reviews.reasonSeenBy.${reasonReader}`) }}</p>
      <a-textarea v-model:value="reason" :rows="3" :maxlength="500" :placeholder="t('reviews.reasonPlaceholder')" />
      <p v-if="!reason.trim()" class="muted">{{ t('reviews.reasonRequired') }}</p>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { message } from 'ant-design-vue'
import type { ModerationFilter, ModerationItem } from '../../api/reviews.api'
import { NEEDS_REASON, reasonAudience, useReviewModeration, type ModerationAction } from './useReviewModeration'

/**
 * Review moderation. Nothing a customer writes — review or salon reply — is public until it is approved here.
 * Behaviour lives in useReviewModeration (tested); this is layout.
 */
const FILTERS: ModerationFilter[] = ['pending', 'reported', 'hidden']

const { t } = useI18n()
const router = useRouter()
const m = useReviewModeration()

const pending = ref<{ action: ModerationAction; item: ModerationItem } | null>(null)
const reason = ref('')
const reasonReader = computed(() => (pending.value ? reasonAudience(pending.value.action) : null))

function dimensionsOf(item: ModerationItem) {
  return (
    [
      ['cleanliness', item.cleanlinessRating],
      ['skill', item.skillRating],
      ['punctuality', item.punctualityRating],
      ['conduct', item.conductRating],
    ] as const
  )
    .filter(([, v]) => typeof v === 'number')
    .map(([key, value]) => ({ key, value }))
}

async function decide(action: ModerationAction, item: ModerationItem, why = '') {
  if (await m.act(action, item, why)) message.success(t('reviews.done'))
}

function askReason(action: ModerationAction, item: ModerationItem) {
  if (!NEEDS_REASON.has(action)) return decide(action, item)
  reason.value = ''
  pending.value = { action, item }
}

async function confirmReason() {
  if (!pending.value || !reason.value.trim()) return
  const { action, item } = pending.value
  pending.value = null
  await decide(action, item, reason.value)
}

onMounted(m.load)
</script>

<style scoped>
.mb { margin-bottom: 12px; }
.queue { display: flex; flex-direction: column; gap: 12px; }
.queue-item__head { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.muted { color: #888; font-size: 12px; }
.dims { display: flex; gap: 8px; flex-wrap: wrap; margin-top: 6px; }
.dim { background: #f5f5f5; border-radius: 12px; padding: 0 8px; font-size: 12px; }
.comment { margin: 8px 0; white-space: pre-wrap; }
.reply { margin: 0 0 8px; padding: 6px 10px; border-inline-start: 3px solid #722ed1; background: #faf5ff; }
.reply p { margin: 4px 0 0; }
.reports ul { margin: 4px 0 8px; padding-inline-start: 18px; }
.actions { margin-top: 4px; }
.reason-audience { color: #d46b08; font-size: 12px; margin: 4px 0 0; }
</style>
