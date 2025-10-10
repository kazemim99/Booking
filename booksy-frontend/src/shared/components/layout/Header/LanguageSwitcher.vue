<template>
  <div class="language-switcher" v-click-outside="closeMenu">
    <!-- Language Button -->
    <button class="language-button" @click="toggleMenu" aria-label="Language Switcher">
      <svg
        class="globe-icon"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
        />
      </svg>
      <span class="language-label">{{ currentLanguageLabel }}</span>
      <svg
        class="dropdown-arrow"
        :class="{ rotated: isMenuOpen }"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
      </svg>
    </button>

    <!-- Dropdown Menu -->
    <transition name="dropdown">
      <div v-if="isMenuOpen" class="dropdown-menu">
        <ul class="language-list">
          <li
            v-for="lang in languages"
            :key="lang.code"
            class="language-item"
            :class="{ active: currentLanguage === lang.code }"
          >
            <button class="language-option" @click="switchLanguage(lang.code)">
              <span class="language-flag">{{ lang.flag }}</span>
              <div class="language-info">
                <span class="language-name">{{ lang.name }}</span>
                <span class="language-native">{{ lang.nativeName }}</span>
              </div>
              <svg
                v-if="currentLanguage === lang.code"
                class="check-icon"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M5 13l4 4L19 7"
                />
              </svg>
            </button>
          </li>
        </ul>
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRTLInstance } from '@/core/composables/useRTL'
import type { Language } from '@/core/composables/useRTL'

interface LanguageOption {
  code: Language
  name: string
  nativeName: string
  flag: string
  direction: 'ltr' | 'rtl'
}

const { locale } = useI18n()
const rtl = useRTLInstance()
const isMenuOpen = ref(false)

// Available languages
const languages: LanguageOption[] = [
  {
    code: 'fa',
    name: 'Persian',
    nativeName: 'فارسی',
    flag: '🇮🇷',
    direction: 'rtl',
  },
  {
    code: 'en',
    name: 'English',
    nativeName: 'English',
    flag: '🇺🇸',
    direction: 'ltr',
  },
  {
    code: 'ar',
    name: 'Arabic',
    nativeName: 'العربية',
    flag: '🇸🇦',
    direction: 'rtl',
  },
]

// Current language
const currentLanguage = computed(() => rtl.currentLanguage.value)

// Current language label
const currentLanguageLabel = computed(() => {
  const lang = languages.find((l) => l.code === currentLanguage.value)
  return lang?.name || 'English'
})

function toggleMenu(): void {
  isMenuOpen.value = !isMenuOpen.value
}

function closeMenu(): void {
  isMenuOpen.value = false
}

function switchLanguage(langCode: Language): void {
  // Update RTL state and direction
  rtl.setLanguage(langCode)

  // Update i18n locale
  locale.value = langCode

  closeMenu()

  // Optional: Show toast notification
  console.log(`Language switched to: ${langCode}`)
}
</script>

<style scoped lang="scss">
.language-switcher {
  position: relative;
}

.language-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: none;
  border: none;
  cursor: pointer;
  padding: 0.5rem;
  border-radius: 8px;
  color: #6b7280;
  transition: all 0.2s;

  &:hover {
    background: #f3f4f6;
    color: #667eea;
  }
}

.globe-icon {
  width: 20px;
  height: 20px;
}

.language-label {
  font-weight: 500;
  font-size: 0.875rem;

  @media (max-width: 768px) {
    display: none;
  }
}

.dropdown-arrow {
  width: 16px;
  height: 16px;
  transition: transform 0.2s;

  &.rotated {
    transform: rotate(180deg);
  }
}

.dropdown-menu {
  position: absolute;
  top: calc(100% + 0.5rem);
  right: 0;
  width: 260px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
  overflow: hidden;
  z-index: 100;
}

.language-list {
  list-style: none;
  margin: 0;
  padding: 0.5rem;
}

.language-item {
  border-radius: 8px;
  overflow: hidden;

  &.active {
    background: #f0f4ff;
  }
}

.language-option {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem;
  background: none;
  border: none;
  cursor: pointer;
  text-align: left;
  transition: background 0.2s;

  &:hover {
    background: #f3f4f6;
  }

  .language-item.active & {
    &:hover {
      background: #e0e7ff;
    }
  }
}

.language-flag {
  font-size: 1.5rem;
  line-height: 1;
}

.language-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
}

.language-name {
  font-weight: 500;
  color: #374151;
  font-size: 0.875rem;
}

.language-native {
  font-size: 0.75rem;
  color: #6b7280;
}

.check-icon {
  width: 20px;
  height: 20px;
  color: #667eea;
  flex-shrink: 0;
}

// Dropdown transition
.dropdown-enter-active,
.dropdown-leave-active {
  transition: all 0.2s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
