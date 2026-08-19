import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'

import CategorySelectionStep from '../CategorySelectionStep.vue'
import { ProviderCategory } from '@/core/types/enums.types'
import { getCategoryMetadata } from '@/core/constants/provider-categories'

/**
 * Component tests for the registration category step.
 *
 * Two defects this locks down:
 *  - the step used to emit its own ad-hoc ids (`barber`), which the backend's category map did
 *    not know, so men's barbershops silently registered as BeautySalon;
 *  - resuming a saved draft returns the enum member name (`"HairSalon"`) while the cards were
 *    keyed by `hair_salon`, so no card was re-selected and the user had to pick again.
 */
const mountStep = (modelValue: string | number | null = null) =>
  mount(CategorySelectionStep, {
    props: { modelValue },
    global: {
      stubs: {
        AppButton: {
          template: '<button :disabled="disabled" @click="$emit(\'click\')"><slot /></button>',
          props: ['disabled', 'variant', 'size', 'block', 'type'],
        },
      },
    },
  })

const cardFor = (wrapper: ReturnType<typeof mountStep>, category: ProviderCategory) =>
  wrapper.find(`[data-testid="category-${getCategoryMetadata(category).slug}"]`)

describe('CategorySelectionStep', () => {
  it('renders the enabled categories with their shared metadata', () => {
    const wrapper = mountStep()

    expect(cardFor(wrapper, ProviderCategory.HairSalon).exists()).toBe(true)
    expect(cardFor(wrapper, ProviderCategory.Barbershop).exists()).toBe(true)
    expect(wrapper.text()).toContain(getCategoryMetadata(ProviderCategory.HairSalon).persianName)
    expect(wrapper.text()).toContain(getCategoryMetadata(ProviderCategory.Barbershop).persianName)
  })

  it('starts with nothing selected and the next button disabled', () => {
    const wrapper = mountStep()

    expect(wrapper.findAll('.category-card.selected')).toHaveLength(0)
    const next = wrapper.findAll('button').at(-1)
    expect(next?.attributes('disabled')).toBeDefined()
  })

  it('marks the clicked category as selected', async () => {
    const wrapper = mountStep()

    await cardFor(wrapper, ProviderCategory.Barbershop).trigger('click')

    expect(cardFor(wrapper, ProviderCategory.Barbershop).classes()).toContain('selected')
    expect(cardFor(wrapper, ProviderCategory.HairSalon).classes()).not.toContain('selected')
  })

  it('emits the canonical slug the backend resolves, not an ad-hoc id', async () => {
    const wrapper = mountStep()

    await cardFor(wrapper, ProviderCategory.Barbershop).trigger('click')
    await wrapper.findAll('button').at(-1)?.trigger('click')

    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['barbershop'])
    expect(wrapper.emitted('next')).toBeTruthy()
  })

  it('does not advance while no category is chosen', async () => {
    const wrapper = mountStep()

    await wrapper.findAll('button').at(-1)?.trigger('click')

    expect(wrapper.emitted('update:modelValue')).toBeFalsy()
    expect(wrapper.emitted('next')).toBeFalsy()
  })

  it.each([
    ['HairSalon', ProviderCategory.HairSalon],
    ['hairSalon', ProviderCategory.HairSalon],
    ['hair-salon', ProviderCategory.HairSalon],
    ['hair_salon', ProviderCategory.HairSalon],
    [1, ProviderCategory.HairSalon],
    ['barber', ProviderCategory.Barbershop],
    ['Barbershop', ProviderCategory.Barbershop],
  ])('re-selects the saved category when a draft supplies %p', (saved, expected) => {
    const wrapper = mountStep(saved as string | number)

    expect(cardFor(wrapper, expected).classes()).toContain('selected')
  })

  it('leaves the step untouched when the saved value is unrecognised', () => {
    const wrapper = mountStep('not-a-category')

    expect(wrapper.findAll('.category-card.selected')).toHaveLength(0)
  })

  it('emits back without requiring a selection', async () => {
    const wrapper = mountStep()

    await wrapper.findAll('button').at(0)?.trigger('click')

    expect(wrapper.emitted('back')).toBeTruthy()
  })
})
