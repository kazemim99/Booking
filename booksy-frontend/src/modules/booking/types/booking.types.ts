// src/mocks/types/booking.types.ts

export enum AppointmentStatus {
    Pending = 'Pending',
    Confirmed = 'Confirmed',
    InProgress = 'InProgress',
    Completed = 'Completed',
    Cancelled = 'Cancelled',
    NoShow = 'NoShow',
    Rescheduled = 'Rescheduled',
}

export enum CancellationReason {
    ClientRequest = 'ClientRequest',
    ProviderUnavailable = 'ProviderUnavailable',
    Emergency = 'Emergency',
    ScheduleConflict = 'ScheduleConflict',
    Other = 'Other',
}

export interface Appointment {
    id: string
    providerId: string
    clientId: string
    serviceId: string
    staffMemberId?: string

    // Scheduling
    scheduledStartTime: string
    scheduledEndTime: string
    actualStartTime?: string
    actualEndTime?: string
    duration: number

    // Status & Info
    status: AppointmentStatus
    bookingNotes?: string
    internalNotes?: string

    // Pricing
    basePrice: number
    totalPrice: number
    deposit?: number
    currency: string

    // Cancellation
    cancellationReason?: CancellationReason
    cancelledAt?: string
    cancelledBy?: string

    // Metadata
    isRecurring: boolean
    recurringGroupId?: string
    remindersEnabled: boolean
    lastReminderSentAt?: string

    // Timestamps
    createdAt: string
    confirmedAt?: string
    completedAt?: string
    lastModifiedAt?: string
}

export interface BookingRequest {
    providerId: string
    serviceId: string
    staffMemberId?: string
    scheduledStartTime: string
    bookingNotes?: string
}

export interface RescheduleRequest {
    appointmentId: string
    newStartTime: string
    reason?: string
}

export interface CancellationRequest {
    appointmentId: string
    reason: CancellationReason
    notes?: string
}

// ===== Service Types =====

export enum ServiceStatus {
    Active = 'Active',
    Inactive = 'Inactive',
    Archived = 'Archived',
}

export enum ServiceType {
    Standard = 'Standard',
    Package = 'Package',
    Membership = 'Membership',
}

export interface Service {
    id: string
    providerId: string

    // Basic Info
    name: string
    description: string
    category: ServiceCategory
    type: ServiceType

    // Pricing
    basePrice: number
    currency: string
    pricingTiers?: PriceTier[]
    requiresDeposit: boolean
    depositAmount?: number

    // Time Management
    duration: number // minutes
    preparationTime: number // minutes
    bufferTime: number // minutes

    // Configuration
    status: ServiceStatus
    allowOnlineBooking: boolean
    requiresApproval: boolean
    availableForMobileService: boolean
    maxAdvanceBookingDays: number
    minAdvanceBookingHours: number

    // Staff
    assignedStaffIds: string[]
    requiresSpecificStaff: boolean

    // Media
    imageUrl?: string
    images?: string[]

    // Metadata
    tags: string[]
    popularityScore: number
    totalBookings: number
    averageRating?: number

    // Timestamps
    createdAt: string
    activatedAt?: string
    archivedAt?: string
    lastModifiedAt?: string
}

export enum ServiceCategory {
    HairCare = 'HairCare',
    Skincare = 'Skincare',
    Massage = 'Massage',
    Nails = 'Nails',
    Makeup = 'Makeup',
    Fitness = 'Fitness',
    Consulting = 'Consulting',
    Medical = 'Medical',
    Dental = 'Dental',
    Wellness = 'Wellness',
    Other = 'Other',
}

export interface PriceTier {
    id: string
    name: string
    price: number
    description?: string
}

export interface ServiceOption {
    id: string
    name: string
    priceAdjustment: number
    durationAdjustment: number
    description?: string
}

// ===== Schedule Types =====

export interface Schedule {
    id: string
    providerId: string
    staffMemberId?: string

    workingHours: WorkingHours[]
    exceptions: ScheduleException[]

    // Settings
    slotDuration: number // minutes
    allowOverlapping: boolean
    autoConfirmBookings: boolean

    createdAt: string
    lastModifiedAt?: string
}

export interface WorkingHours {
    id: string
    dayOfWeek: number // 0-6
    isOpen: boolean
    openTime: string
    closeTime: string
    breaks: TimeBreak[]
}

export interface TimeBreak {
    startTime: string
    endTime: string
    reason?: string
}

export interface ScheduleException {
    id: string
    date: string
    isOpen: boolean
    openTime?: string
    closeTime?: string
    reason: string
    type: ExceptionType
}

export enum ExceptionType {
    Holiday = 'Holiday',
    Vacation = 'Vacation',
    SpecialHours = 'SpecialHours',
    Closure = 'Closure',
}

export interface AvailabilitySlot {
    startTime: string
    endTime: string
    available: boolean
    staffMemberId?: string
}

export interface AvailabilityRequest {
    providerId: string
    serviceId: string
    staffMemberId?: string
    startDate: string
    endDate: string
}