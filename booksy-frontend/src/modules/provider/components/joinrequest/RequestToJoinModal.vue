<template>
  <Teleport to="body">
    <div class="modal-overlay" @click="handleClose">
      <div class="modal-container" @click.stop>
        <div class="modal-header">
          <h2 class="modal-title">درخواست پیوستن به سازمان</h2>
          <button class="modal-close" @click="handleClose">
            <i class="icon-x"></i>
          </button>
        </div>

        <div class="modal-body">
          <!-- Organization Info -->
          <div class="organization-preview">
            <div class="organization-logo" v-if="organization.logoUrl">
              <img :src="organization.logoUrl" :alt="organization.businessName" />
            </div>
            <div class="organization-logo-placeholder" v-else>
              <i class="icon-building"></i>
            </div>

            <div class="organization-details">
              <h3>{{ organization.businessName }}</h3>
              <p v-if="organization.description">{{ organization.description }}</p>
              <div class="organization-tags">
                <span class="tag" v-if="organization.type">
                  <i class="icon-tag"></i>
                  {{ organization.type }}
                </span>
                <span class="tag" v-if="organization.city">
                  <i class="icon-map-pin"></i>
                  {{ organization.city }}
                </span>
              </div>
            </div>
          </div>

          <!-- Request Form -->
          <form @submit.prevent="handleSubmit">
            <!-- Message -->
            <div class="form-group">
              <label for="message" class="form-label">
                پیام شما (اختیاری)
              </label>
              <textarea
                id="message"
                v-model="formData.message"
                class="form-textarea"
                rows="5"
                placeholder="درباره تخصص‌ها و تجربیات خود بنویسید و دلیل پیوستن به این سازمان را توضیح دهید..."
                maxlength="1000"
              ></textarea>
              <div class="textarea-footer">
                <span class="form-hint">پیام شما به مدیر سازمان ارسال می‌شود</span>
                <span class="char-count">{{ messageLength }}/1000</span>
              </div>
            </div>

            <!-- Info Box -->
            <div class="info-box">
              <i class="icon-info-circle"></i>
              <div class="info-content">
                <h4 class="info-title">نحوه عملکرد درخواست:</h4>
                <ul class="info-list">
                  <li>درخواست شما به مدیر سازمان ارسال می‌شود</li>
                  <li>مدیر سازمان درخواست را بررسی و تصمیم می‌گیرد</li>
                  <li>در صورت تأیید، شما به تیم سازمان اضافه خواهید شد</li>
                  <li>از وضعیت درخواست خود در پنل کاربری مطلع می‌شوید</li>
                </ul>
              </div>
            </div>

            <!-- Requirements Checklist -->
            <div class="requirements-section">
              <h4 class="requirements-title">موارد مورد نیاز:</h4>
              <div class="requirement-item">
                <input
                  type="checkbox"
                  id="hasProfile"
                  v-model="requirements.hasProfile"
                  class="requirement-checkbox"
                />
                <label for="hasProfile">پروفایل حرفه‌ای من کامل است</label>
              </div>
              <div class="requirement-item">
                <input
                  type="checkbox"
                  id="hasExperience"
                  v-model="requirements.hasExperience"
                  class="requirement-checkbox"
                />
                <label for="hasExperience">تجربه کافی در این حوزه دارم</label>
              </div>
              <div class="requirement-item">
                <input
                  type="checkbox"
                  id="acceptTerms"
                  v-model="requirements.acceptTerms"
                  class="requirement-checkbox"
                />
                <label for="acceptTerms">
                  شرایط و قوانین کار در سازمان را می‌پذیرم <span class="required">*</span>
                </label>
              </div>
            </div>
          </form>
        </div>

        <div class="modal-footer">
          <AppButton variant="secondary" size="medium" @click="handleClose" :disabled="isSubmitting">
            انصراف
          </AppButton>
          <AppButton
            variant="primary"
            size="medium"
            @click="handleSubmit"
            :disabled="!canSubmit || isSubmitting"
            :loading="isSubmitting"
          >
            <i v-if="!isSubmitting" class="icon-send"></i>
            {{ isSubmitting ? 'در حال ارسال...' : 'ارسال درخواست' }}
          </AppButton>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import { useHierarchyStore } from '../../stores/hierarchy.store'
import { useAuthStore } from '@/core/stores/modules/auth.store'
import AppButton from '@/shared/components/ui/Button/AppButton.vue'

interface Organization {
  id: string
  businessName: string
  description?: string
  logoUrl?: string
  type?: string
  city?: string
}

interface Props {
  organization: Organization
}

interface Emits {
  (e: 'close'): void
  (e: 'submitted'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const hierarchyStore = useHierarchyStore()
const authStore = useAuthStore()

const formData = reactive({
  message: '',
})

const requirements = reactive({
  hasProfile: false,
  hasExperience: false,
  acceptTerms: false,
})

const isSubmitting = ref(false)

const messageLength = computed(() => formData.message.length)

const canSubmit = computed(() => {
  return requirements.acceptTerms
})

function handleClose() {
  if (!isSubmitting.value) {
    emit('close')
  }
}

async function handleSubmit() {
  if (!canSubmit.value) return

  isSubmitting.value = true

  try {
    const currentUserId = authStore.currentUser?.id
    if (!currentUserId) {
      throw new Error('کاربر وارد نشده است')
    }

    await hierarchyStore.createJoinRequest(props.organization.id, {
      organizationId: props.organization.id,
      requesterId: currentUserId,
      message: formData.message || undefined,
    })

    // Show success message
    alert('درخواست شما با موفقیت ارسال شد!')

    emit('submitted')
    emit('close')
  } catch (error) {
    console.error('Error creating join request:', error)
    alert('خطا در ارسال درخواست. لطفاً دوباره تلاش کنید.')
  } finally {
    isSubmitting.value = false
  }
}
</script>

<style scoped lang="scss">
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-container {
  background: white;
  border-radius: 16px;
  max-width: 600px;
  width: 100%;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.5rem;
  border-bottom: 1px solid #e2e8f0;

  .modal-title {
    font-size: 1.25rem;
    color: #1a202c;
    font-weight: 600;
    margin: 0;
  }

  .modal-close {
    background: none;
    border: none;
    color: #a0aec0;
    cursor: pointer;
    padding: 0.5rem;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 6px;
    transition: all 0.2s;

    &:hover {
      background: #f7fafc;
      color: #718096;
    }

    i {
      font-size: 1.25rem;
    }
  }
}

.modal-body {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem;
}

.organization-preview {
  background: #f7fafc;
  border-radius: 12px;
  padding: 1.5rem;
  margin-bottom: 1.5rem;
  display: flex;
  gap: 1rem;

  .organization-logo,
  .organization-logo-placeholder {
    width: 70px;
    height: 70px;
    flex-shrink: 0;
    border-radius: 10px;
    overflow: hidden;
  }

  .organization-logo {
    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
  }

  .organization-logo-placeholder {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    font-size: 1.75rem;
  }

  .organization-details {
    flex: 1;

    h3 {
      font-size: 1.125rem;
      color: #1a202c;
      margin-bottom: 0.5rem;
      font-weight: 600;
    }

    p {
      color: #718096;
      font-size: 0.95rem;
      margin-bottom: 0.75rem;
      line-height: 1.5;
    }

    .organization-tags {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;

      .tag {
        display: inline-flex;
        align-items: center;
        gap: 0.375rem;
        background: white;
        padding: 0.375rem 0.75rem;
        border-radius: 6px;
        font-size: 0.875rem;
        color: #4a5568;

        i {
          color: #667eea;
        }
      }
    }
  }
}

.form-group {
  margin-bottom: 1.5rem;

  .form-label {
    display: block;
    font-weight: 500;
    margin-bottom: 0.5rem;
    color: #2d3748;
    font-size: 0.95rem;
  }

  .form-textarea {
    width: 100%;
    padding: 0.75rem 1rem;
    border: 1px solid #cbd5e0;
    border-radius: 8px;
    font-size: 1rem;
    font-family: inherit;
    resize: vertical;
    transition: all 0.2s;

    &:focus {
      outline: none;
      border-color: #667eea;
      box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
    }

    &::placeholder {
      color: #a0aec0;
    }
  }

  .textarea-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: 0.5rem;

    .form-hint {
      font-size: 0.875rem;
      color: #a0aec0;
    }

    .char-count {
      font-size: 0.875rem;
      color: #a0aec0;
    }
  }
}

.info-box {
  background: #ebf8ff;
  border: 1px solid #90cdf4;
  border-radius: 8px;
  padding: 1rem;
  margin-bottom: 1.5rem;
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;

  i {
    color: #3182ce;
    font-size: 1.25rem;
    margin-top: 0.125rem;
  }

  .info-content {
    flex: 1;

    .info-title {
      font-size: 0.95rem;
      font-weight: 600;
      color: #2c5282;
      margin-bottom: 0.5rem;
    }

    .info-list {
      margin: 0;
      padding-right: 1.25rem;
      color: #2c5282;
      font-size: 0.875rem;
      line-height: 1.6;

      li {
        margin-bottom: 0.25rem;
      }
    }
  }
}

.requirements-section {
  background: #fff5f5;
  border: 1px solid #feb2b2;
  border-radius: 8px;
  padding: 1rem;
  margin-bottom: 1.5rem;

  .requirements-title {
    font-size: 0.95rem;
    font-weight: 600;
    color: #c53030;
    margin-bottom: 0.75rem;
  }

  .requirement-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 0.75rem;

    &:last-child {
      margin-bottom: 0;
    }

    .requirement-checkbox {
      width: 18px;
      height: 18px;
      cursor: pointer;
    }

    label {
      flex: 1;
      color: #742a2a;
      font-size: 0.95rem;
      cursor: pointer;

      .required {
        color: #f56565;
      }
    }
  }
}

.modal-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 1rem;
  padding: 1.5rem;
  border-top: 1px solid #e2e8f0;
}

@media (max-width: 640px) {
  .modal-container {
    max-height: 95vh;
  }

  .organization-preview {
    flex-direction: column;
    text-align: center;

    .organization-logo,
    .organization-logo-placeholder {
      margin: 0 auto;
    }

    .organization-details {
      .organization-tags {
        justify-content: center;
      }
    }
  }

  .modal-footer {
    flex-direction: column-reverse;

    button {
      width: 100%;
    }
  }
}
</style>
