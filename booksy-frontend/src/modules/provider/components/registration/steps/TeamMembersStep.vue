<template>
  <StepContainer
    :title="$t('provider.registration.team.title')"
    :subtitle="$t('provider.registration.team.subtitle')"
  >
    <!-- Owner (default member) -->
    <div class="team-list">
      <div class="team-member owner-member">
        <div class="member-info">
          <h4 class="member-name">{{ ownerName }}</h4>
          <p class="member-role">{{ $t('provider.registration.team.owner') }}</p>
        </div>
      </div>

      <!-- Additional team members -->
      <div v-for="member in teamMembers" :key="member.id" class="team-member">
        <div class="member-info">
          <h4 class="member-name">{{ member.name }}</h4>
          <p class="member-role">{{ member.position }}</p>
        </div>
        <button type="button" class="btn-remove" @click="removeMember(member.id)">
          <svg viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Add Staff Button -->
    <button type="button" class="btn-add-staff" @click="showAddModal = true">
      + {{ $t('provider.registration.team.addStaff') }}
    </button>

    <NavigationButtons
      :show-back="true"
      @back="$emit('back')"
      @next="handleNext"
    />

    <!-- Add Staff Modal -->
    <div v-if="showAddModal" class="modal-overlay" @click="showAddModal = false">
      <div class="modal-content" @click.stop>
        <h3 class="modal-title">{{ $t('provider.registration.team.addStaffMember') }}</h3>
        <form @submit.prevent="handleAddMember">
          <input v-model="newMember.name" type="text" class="form-input" placeholder="Name" required />
          <input v-model="newMember.email" type="email" class="form-input" placeholder="Email" required />
          <input v-model="newMember.phoneNumber" type="tel" class="form-input" placeholder="Phone" required />
          <input v-model="newMember.position" type="text" class="form-input" placeholder="Position" required />
          <div class="modal-actions">
            <button type="button" class="btn-cancel" @click="showAddModal = false">Cancel</button>
            <button type="submit" class="btn-submit">Add</button>
          </div>
        </form>
      </div>
    </div>
  </StepContainer>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import StepContainer from '../shared/StepContainer.vue'
import NavigationButtons from '../shared/NavigationButtons.vue'
import type { TeamMember } from '@/modules/provider/types/registration.types'

interface Props {
  modelValue?: TeamMember[]
  ownerName?: string
}

interface Emits {
  (e: 'update:modelValue', value: TeamMember[]): void
  (e: 'next'): void
  (e: 'back'): void
}

const props = withDefaults(defineProps<Props>(), {
  ownerName: 'Business Owner',
})
const emit = defineEmits<Emits>()

const teamMembers = ref<TeamMember[]>(props.modelValue || [])
const showAddModal = ref(false)
const newMember = ref({ name: '', email: '', phoneNumber: '', position: '' })

const handleAddMember = () => {
  teamMembers.value.push({
    id: Date.now().toString(),
    name: newMember.value.name,
    email: newMember.value.email,
    phoneNumber: newMember.value.phoneNumber,
    countryCode: '+1',
    position: newMember.value.position,
    isOwner: false,
  })
  newMember.value = { name: '', email: '', phoneNumber: '', position: '' }
  showAddModal.value = false
}

const removeMember = (id: string) => {
  teamMembers.value = teamMembers.value.filter(m => m.id !== id)
}

const handleNext = () => {
  emit('update:modelValue', teamMembers.value)
  emit('next')
}
</script>

<style scoped>
.team-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.team-member {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.25rem;
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
}

.owner-member {
  background: #f0fdf4;
  border-color: #10b981;
}

.member-info {
  flex: 1;
}

.member-name {
  font-weight: 600;
  color: #111827;
  margin-bottom: 0.25rem;
}

.member-role {
  font-size: 0.875rem;
  color: #6b7280;
}

.btn-remove {
  width: 2rem;
  height: 2rem;
  background: none;
  border: none;
  color: #ef4444;
  cursor: pointer;
  padding: 0.25rem;
}

.btn-add-staff {
  width: 100%;
  padding: 1rem;
  background: #ffffff;
  border: 2px dashed #d1d5db;
  border-radius: 0.5rem;
  color: #6b7280;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-add-staff:hover {
  border-color: #10b981;
  color: #10b981;
}

.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  padding: 2rem;
  border-radius: 0.75rem;
  width: 90%;
  max-width: 400px;
}

.modal-title {
  font-size: 1.25rem;
  font-weight: 700;
  margin-bottom: 1.5rem;
}

.form-input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  margin-bottom: 1rem;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  margin-top: 1.5rem;
}

.btn-cancel, .btn-submit {
  flex: 1;
  padding: 0.75rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
}

.btn-cancel {
  background: transparent;
  border: 1px solid #d1d5db;
  color: #6b7280;
}

.btn-submit {
  background: #111827;
  border: none;
  color: white;
}
</style>
