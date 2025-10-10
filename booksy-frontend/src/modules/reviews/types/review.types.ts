// src/mocks/types/review.types.ts

export enum ReviewStatus {
    Pending = 'Pending',
    Published = 'Published',
    Flagged = 'Flagged',
    Removed = 'Removed',
}

export interface Review {
    id: string
    appointmentId: string
    providerId: string
    serviceId: string
    clientId: string

    // Ratings (1-5)
    overallRating: number
    qualityRating: number
    valueRating: number
    punctualityRating: number
    cleanlinessRating?: number
    professionalismRating?: number

    // Content
    title?: string
    comment?: string
    photos?: string[]

    // Status
    status: ReviewStatus
    flagReason?: string

    // Engagement
    helpful: number
    notHelpful: number

    // Response
    providerResponse?: ProviderResponse

    // Timestamps
    createdAt: string
    publishedAt?: string
    lastModifiedAt?: string
}

export interface ProviderResponse {
    providerId: string
    responseText: string
    respondedAt: string
}

export interface ReviewRequest {
    appointmentId: string
    overallRating: number
    qualityRating: number
    valueRating: number
    punctualityRating: number
    cleanlinessRating?: number
    professionalismRating?: number
    title?: string
    comment?: string
}

export interface ReviewStats {
    totalReviews: number
    averageRating: number
    ratingDistribution: {
        5: number
        4: number
        3: number
        2: number
        1: number
    }
    recommendationRate: number
}

// ===== Notification Types =====

export enum NotificationType {
    BookingConfirmation = 'BookingConfirmation',
    BookingReminder = 'BookingReminder',
    BookingCancellation = 'BookingCancellation',
    BookingRescheduled = 'BookingRescheduled',
    ReviewRequest = 'ReviewRequest',
    ProviderMessage = 'ProviderMessage',
    SystemAlert = 'SystemAlert',
    PaymentConfirmation = 'PaymentConfirmation',
    PromotionalOffer = 'PromotionalOffer',
}

export enum NotificationChannel {
    Email = 'Email',
    SMS = 'SMS',
    Push = 'Push',
    InApp = 'InApp',
}

export enum NotificationPriority {
    Low = 'Low',
    Normal = 'Normal',
    High = 'High',
    Urgent = 'Urgent',
}

export interface Notification {
    id: string
    userId: string

    // Content
    type: NotificationType
    title: string
    message: string
    metadata?: Record<string, unknown>

    // Delivery
    channels: NotificationChannel[]
    priority: NotificationPriority

    // Status
    isRead: boolean
    readAt?: string

    // Actions
    actionUrl?: string
    actionLabel?: string

    // Scheduling
    scheduledFor?: string
    sentAt?: string

    // Timestamps
    createdAt: string
    expiresAt?: string
}

export interface NotificationPreferences {
    userId: string
    channels: {
        email: boolean
        sms: boolean
        push: boolean
        inApp: boolean
    }
    types: Record<NotificationType, boolean>
    quietHours?: {
        enabled: boolean
        startTime: string
        endTime: string
        timezone: string
    }
}

// ===== Staff Types =====

export interface StaffMember {
    id: string
    providerId: string

    // Personal Info
    firstName: string
    lastName: string
    email: string
    phone: string

    // Profile
    title?: string
    bio?: string
    photoUrl?: string

    // Employment
    employmentStatus: EmploymentStatus
    hireDate: string
    terminationDate?: string

    // Qualifications
    qualifications: string[]
    certifications: Certification[]
    specialties: string[]

    // Settings
    isActive: boolean
    acceptsOnlineBookings: boolean

    // Services
    serviceIds: string[]

    // Schedule
    scheduleId?: string

    // Metadata
    totalAppointments: number
    averageRating?: number

    // Timestamps
    createdAt: string
    lastModifiedAt?: string
}

export enum EmploymentStatus {
    FullTime = 'FullTime',
    PartTime = 'PartTime',
    Contract = 'Contract',
    Intern = 'Intern',
}

export interface Certification {
    id: string
    name: string
    issuedBy: string
    issuedDate: string
    expiryDate?: string
    credentialId?: string
}

// ===== Payment Types =====

export enum PaymentStatus {
    Pending = 'Pending',
    Processing = 'Processing',
    Completed = 'Completed',
    Failed = 'Failed',
    Refunded = 'Refunded',
    PartiallyRefunded = 'PartiallyRefunded',
}

export enum PaymentMethod {
    CreditCard = 'CreditCard',
    DebitCard = 'DebitCard',
    Cash = 'Cash',
    BankTransfer = 'BankTransfer',
    Wallet = 'Wallet',
}

export interface Payment {
    id: string
    appointmentId: string
    userId: string
    providerId: string

    // Amount
    amount: number
    currency: string

    // Payment Details
    method: PaymentMethod
    status: PaymentStatus

    // Transaction
    transactionId?: string
    gatewayResponse?: Record<string, unknown>

    // Refund
    refundAmount?: number
    refundReason?: string
    refundedAt?: string

    // Timestamps
    createdAt: string
    processedAt?: string
    completedAt?: string
}