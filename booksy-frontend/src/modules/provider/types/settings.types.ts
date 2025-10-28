// ============================================
// Provider Settings Types
// ============================================

// ============================================
// Booking Preferences
// ============================================

export interface BookingWindowSettings {
  minAdvanceBookingHours: number // Minimum hours in advance
  maxAdvanceBookingDays: number // Maximum days in advance
}

export interface BookingApprovalSettings {
  requiresApproval: boolean // Require manual approval for bookings
  autoApproveForReturningCustomers: boolean
  autoApproveThresholdBookings: number // After N bookings, auto-approve
}

export interface CancellationPolicy {
  allowCancellation: boolean
  cancellationWindowHours: number // Hours before appointment
  chargeNoShowFee: boolean
  noShowFeePercentage?: number
  allowRescheduling: boolean
  rescheduleWindowHours: number
}

export interface DepositSettings {
  requiresDeposit: boolean
  depositType: 'Fixed' | 'Percentage'
  depositAmount?: number
  depositPercentage?: number
  refundableDeposit: boolean
  refundWindowHours?: number
}

export interface BookingPreferences {
  bookingWindow: BookingWindowSettings
  approval: BookingApprovalSettings
  cancellationPolicy: CancellationPolicy
  depositSettings: DepositSettings
  allowOnlinePayment: boolean
  allowWalkins: boolean
  bufferBetweenBookings: number // Minutes
}

// ============================================
// Notification Settings
// ============================================

export enum NotificationChannel {
  Email = 'Email',
  SMS = 'SMS',
  Push = 'Push',
  InApp = 'InApp',
}

export interface NotificationPreference {
  enabled: boolean
  channels: NotificationChannel[]
}

export interface NotificationRecipient {
  userId: string
  name: string
  email: string
  phone?: string
  receiveBookings: boolean
  receiveCancellations: boolean
  receiveReviews: boolean
  receivePayments: boolean
}

export interface QuietHours {
  enabled: boolean
  startTime: string // HH:mm format
  endTime: string // HH:mm format
  timezone: string
}

export interface NotificationSettings {
  // Booking Notifications
  newBooking: NotificationPreference
  bookingCancelled: NotificationPreference
  bookingRescheduled: NotificationPreference
  bookingConfirmed: NotificationPreference

  // Reminder Notifications
  customerReminder: NotificationPreference
  customerReminderHours: number // Hours before appointment
  providerReminder: NotificationPreference
  providerReminderMinutes: number // Minutes before appointment

  // Review Notifications
  newReview: NotificationPreference
  reviewResponse: NotificationPreference

  // Payment Notifications
  paymentReceived: NotificationPreference
  paymentFailed: NotificationPreference
  payoutProcessed: NotificationPreference

  // Recipients
  recipients: NotificationRecipient[]

  // Quiet Hours
  quietHours: QuietHours
}

// ============================================
// Business Policies
// ============================================

export interface PolicyVersion {
  version: string
  effectiveDate: string
  content: string
  lastModifiedBy: string
  lastModifiedAt: string
}

export interface CancellationPolicyDetails {
  currentVersion: PolicyVersion
  versions: PolicyVersion[]
  displayOnBooking: boolean
  requiresCustomerAcceptance: boolean
}

export interface PrivacyPolicy {
  currentVersion: PolicyVersion
  versions: PolicyVersion[]
  displayOnBooking: boolean
  requiresCustomerAcceptance: boolean
}

export interface TermsAndConditions {
  currentVersion: PolicyVersion
  versions: PolicyVersion[]
  displayOnBooking: boolean
  requiresCustomerAcceptance: boolean
}

export interface BusinessPolicies {
  cancellationPolicy: CancellationPolicyDetails
  privacyPolicy: PrivacyPolicy
  termsAndConditions: TermsAndConditions
  customPolicies: PolicyVersion[]
}

// ============================================
// Operating Preferences
// ============================================

export enum Language {
  English = 'en',
  Persian = 'fa',
  Spanish = 'es',
  French = 'fr',
  German = 'de',
  Arabic = 'ar',
}

export enum DateFormat {
  MMDDYYYY = 'MM/DD/YYYY',
  DDMMYYYY = 'DD/MM/YYYY',
  YYYYMMDD = 'YYYY-MM-DD',
}

export enum TimeFormat {
  Hour12 = '12h',
  Hour24 = '24h',
}

export enum Currency {
  USD = 'USD',
  EUR = 'EUR',
  GBP = 'GBP',
  IRR = 'IRR',
  AED = 'AED',
}

export interface DefaultServiceSettings {
  defaultDuration: number // Minutes
  defaultPreparationTime: number // Minutes
  defaultBufferTime: number // Minutes
  defaultAllowOnlineBooking: boolean
  defaultRequiresDeposit: boolean
  defaultDepositPercentage: number
}

export interface LocalizationSettings {
  timezone: string
  language: Language
  secondaryLanguage?: Language
  dateFormat: DateFormat
  timeFormat: TimeFormat
  currency: Currency
  firstDayOfWeek: 0 | 1 | 2 | 3 | 4 | 5 | 6 // 0 = Sunday, 1 = Monday, etc.
}

export interface OperatingPreferences {
  defaultServiceSettings: DefaultServiceSettings
  localization: LocalizationSettings
  displayBusinessHoursOnProfile: boolean
  displayPricingOnProfile: boolean
  displayStaffOnProfile: boolean
  allowCustomerNotes: boolean
  allowCustomerCancellation: boolean
  allowCustomerRescheduling: boolean
}

// ============================================
// Integration Settings
// ============================================

export enum IntegrationStatus {
  NotConnected = 'NotConnected',
  Connected = 'Connected',
  Error = 'Error',
  Syncing = 'Syncing',
}

export interface CalendarIntegration {
  provider: 'Google' | 'Outlook' | 'Apple'
  status: IntegrationStatus
  accountEmail?: string
  syncEnabled: boolean
  syncDirection: 'OneWay' | 'TwoWay'
  calendarId?: string
  lastSyncAt?: string
  lastSyncStatus?: string
  errorMessage?: string
}

export interface PaymentGatewayIntegration {
  provider: 'Stripe' | 'PayPal' | 'Square' | 'Zarinpal'
  status: IntegrationStatus
  accountId?: string
  isLive: boolean // Live vs Test mode
  enabledPaymentMethods: string[]
  defaultCurrency: Currency
  lastTransactionAt?: string
  errorMessage?: string
}

export interface SocialMediaIntegration {
  platform: 'Facebook' | 'Instagram' | 'Twitter' | 'LinkedIn'
  status: IntegrationStatus
  accountHandle?: string
  accountUrl?: string
  autoShare: boolean
  connectedAt?: string
  errorMessage?: string
}

export interface IntegrationSettings {
  calendar: CalendarIntegration[]
  paymentGateway: PaymentGatewayIntegration[]
  socialMedia: SocialMediaIntegration[]
  webhookUrl?: string
  apiKey?: string
}

// ============================================
// Account Security
// ============================================

export interface TwoFactorAuth {
  enabled: boolean
  method: 'SMS' | 'Email' | 'Authenticator'
  phoneNumber?: string
  email?: string
  backupCodes?: string[]
  enabledAt?: string
}

export interface TrustedDevice {
  id: string
  deviceName: string
  deviceType: string
  browser: string
  operatingSystem: string
  ipAddress: string
  location?: string
  addedAt: string
  lastUsedAt: string
}

export interface StaffPermission {
  userId: string
  staffName: string
  email: string
  role: 'Owner' | 'Manager' | 'Staff' | 'Receptionist'
  permissions: {
    manageBookings: boolean
    manageServices: boolean
    manageStaff: boolean
    manageCustomers: boolean
    viewReports: boolean
    manageSettings: boolean
    processPayments: boolean
  }
  addedAt: string
  addedBy: string
}

export interface LoginActivity {
  id: string
  timestamp: string
  ipAddress: string
  location?: string
  deviceType: string
  browser: string
  success: boolean
  failureReason?: string
}

export interface AccountSecurity {
  twoFactorAuth: TwoFactorAuth
  trustedDevices: TrustedDevice[]
  staffPermissions: StaffPermission[]
  loginActivity: LoginActivity[]
  passwordLastChangedAt?: string
  passwordExpirationDays: number
  requirePasswordChange: boolean
}

// ============================================
// Provider Settings (Complete)
// ============================================

export interface ProviderSettings {
  providerId: string
  bookingPreferences: BookingPreferences
  notificationSettings: NotificationSettings
  businessPolicies: BusinessPolicies
  operatingPreferences: OperatingPreferences
  integrationSettings: IntegrationSettings
  accountSecurity: AccountSecurity
  lastModifiedAt: string
  lastModifiedBy: string
}

// ============================================
// Request/Response Types
// ============================================

export interface UpdateBookingPreferencesRequest {
  providerId: string
  bookingPreferences: Partial<BookingPreferences>
}

export interface UpdateNotificationSettingsRequest {
  providerId: string
  notificationSettings: Partial<NotificationSettings>
}

export interface UpdateBusinessPoliciesRequest {
  providerId: string
  businessPolicies: Partial<BusinessPolicies>
}

export interface UpdateOperatingPreferencesRequest {
  providerId: string
  operatingPreferences: Partial<OperatingPreferences>
}

export interface UpdateIntegrationSettingsRequest {
  providerId: string
  integrationSettings: Partial<IntegrationSettings>
}

export interface UpdateAccountSecurityRequest {
  providerId: string
  accountSecurity: Partial<AccountSecurity>
}

export interface SettingsUpdateResult {
  success: boolean
  providerId: string
  settingsUpdated: string[]
  errors?: string[]
  lastModifiedAt: string
}

// ============================================
// Settings Navigation
// ============================================

export enum SettingsSection {
  BookingPreferences = 'booking-preferences',
  Notifications = 'notifications',
  BusinessPolicies = 'business-policies',
  OperatingPreferences = 'operating-preferences',
  Integrations = 'integrations',
  AccountSecurity = 'account-security',
}

export interface SettingsTab {
  id: SettingsSection
  label: string
  icon: string
  description: string
  badge?: string | number
}
