import { NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useHierarchyStore } from '@/modules/provider/stores/hierarchy.store'
import { HierarchyType } from '@/modules/provider/types/hierarchy.types'

/**
 * Route guard for organization-only routes
 * Prevents individual staff members from accessing organization management features
 */
export const organizationOnlyGuard = async (
  to: RouteLocationNormalized,
  _from: RouteLocationNormalized,
  next: NavigationGuardNext,
) => {
  const hierarchyStore = useHierarchyStore()

  // Ensure hierarchy is loaded
  if (!hierarchyStore.currentHierarchy) {
    try {
      // await hierarchyStore.loadCurrentHierarchy()
    } catch (error) {
      console.error('[organizationOnlyGuard] Failed to load hierarchy:', error)
      return next({
        name: 'ProviderDashboard',
        query: { error: 'failed-to-load-hierarchy' },
      })
    }
  }

  const provider = hierarchyStore.currentHierarchy?.provider

  // Check if provider is an organization
  if (provider?.hierarchyType !== HierarchyType.Organization) {
    console.warn(
      `[organizationOnlyGuard] Access denied to ${to.path} - User is not an organization`,
      {
        hierarchyType: provider?.hierarchyType,
        providerId: provider?.id,
      },
    )

    return next({
      name: 'Forbidden',
      params: {
        message: 'این صفحه فقط برای سازمان‌ها در دسترس است',
      },
      replace: true,
    })
  }

  next()
}

/**
 * Route guard for staff member-only routes
 * Prevents organizations from accessing individual staff member features
 */
export const staffMemberOnlyGuard = async (
  to: RouteLocationNormalized,
  _from: RouteLocationNormalized,
  next: NavigationGuardNext,
) => {
  const hierarchyStore = useHierarchyStore()

  // Ensure hierarchy is loaded
  if (!hierarchyStore.currentHierarchy) {
    try {
      // await hierarchyStore.loadCurrentHierarchy()
    } catch (error) {
      console.error('[staffMemberOnlyGuard] Failed to load hierarchy:', error)
      return next({
        name: 'ProviderDashboard',
        query: { error: 'failed-to-load-hierarchy' },
      })
    }
  }

  const provider = hierarchyStore.currentHierarchy?.provider

  // Check if provider is an individual with a parent organization
  if (
    provider?.hierarchyType !== HierarchyType.Individual ||
    !provider?.parentOrganizationId
  ) {
    console.warn(
      `[staffMemberOnlyGuard] Access denied to ${to.path} - User is not a staff member`,
      {
        hierarchyType: provider?.hierarchyType,
        parentOrganizationId: provider?.parentOrganizationId,
        providerId: provider?.id,
      },
    )

    return next({
      name: 'Forbidden',
      params: {
        message: 'این صفحه فقط برای اعضای سازمان در دسترس است',
      },
      replace: true,
    })
  }

  next()
}

/**
 * Route guard for independent individual-only routes
 * Prevents organizations and staff members from accessing independent freelancer features
 */
export const independentIndividualOnlyGuard = async (
  to: RouteLocationNormalized,
  _from: RouteLocationNormalized,
  next: NavigationGuardNext,
) => {
  const hierarchyStore = useHierarchyStore()

  // Ensure hierarchy is loaded
  if (!hierarchyStore.currentHierarchy) {
    try {
      // await hierarchyStore.loadCurrentHierarchy()
    } catch (error) {
      console.error('[independentIndividualOnlyGuard] Failed to load hierarchy:', error)
      return next({
        name: 'ProviderDashboard',
        query: { error: 'failed-to-load-hierarchy' },
      })
    }
  }

  const provider = hierarchyStore.currentHierarchy?.provider

  // Check if provider is an independent individual
  const isIndependentIndividual =
    provider?.hierarchyType === HierarchyType.Individual &&
    !provider?.parentOrganizationId

  if (!isIndependentIndividual) {
    console.warn(
      `[independentIndividualOnlyGuard] Access denied to ${to.path} - User is not an independent individual`,
      {
        hierarchyType: provider?.hierarchyType,
        parentOrganizationId: provider?.parentOrganizationId,
        providerId: provider?.id,
      },
    )

    return next({
      name: 'Forbidden',
      params: {
        message: 'این صفحه فقط برای ارائه‌دهندگان مستقل در دسترس است',
      },
      replace: true,
    })
  }

  next()
}

/**
 * Helper function to check if current provider is an organization
 */
export const isOrganization = (): boolean => {
  const hierarchyStore = useHierarchyStore()
  return (
    hierarchyStore.currentHierarchy?.provider?.hierarchyType === HierarchyType.Organization
  )
}

/**
 * Helper function to check if current provider is a staff member
 */
export const isStaffMember = (): boolean => {
  const hierarchyStore = useHierarchyStore()
  const provider = hierarchyStore.currentHierarchy?.provider
  return (
    provider?.hierarchyType === HierarchyType.Individual &&
    !!provider?.parentOrganizationId
  )
}

/**
 * Helper function to check if current provider is an independent individual
 */
export const isIndependentIndividual = (): boolean => {
  const hierarchyStore = useHierarchyStore()
  const provider = hierarchyStore.currentHierarchy?.provider
  return (
    provider?.hierarchyType === HierarchyType.Individual &&
    !provider?.parentOrganizationId
  )
}
