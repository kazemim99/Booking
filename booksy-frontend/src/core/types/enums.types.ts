/**
 * Global Enumeration Types
 * Centralized enumerations used across the Booksy application
 */

// ==================== User & Authentication ====================

/**
 * User roles in the system
 */
export enum UserRole {
  Customer = 'customer',
  Provider = 'provider',
  Staff = 'staff',
  Admin = 'admin',
  SuperAdmin = 'super_admin',
}

/**
 * User account status
 */
export enum UserStatus {
  Draft = 'Draft',
  Active = 'active',
  Inactive = 'inactive',
  Pending = 'pending',
  Suspended = 'suspended',
  Banned = 'banned',
  Deleted = 'deleted',
}

/**
 * User account type
 */
export enum UserType {
  Individual = 'individual',
  Business = 'business',
  Professional = 'professional',
}

/**
 * Gender options
 */
export enum Gender {
  Male = 'male',
  Female = 'female',
  Other = 'other',
  PreferNotToSay = 'prefer_not_to_say',
}

// ==================== Provider & Business ====================

/**
 * Provider/Business types
 */
export enum ProviderType {
  Salon = 'salon',
  Barbershop = 'barbershop',
  Spa = 'spa',
  Clinic = 'clinic',
  Medical = 'medical',
  GymFitness = 'gym_fitness',
  Professional = 'professional',
  Other = 'other',
}

/**
 * Provider verification status
 */
export enum ProviderStatus {
  Drafted = 'Drafted',
  PendingVerification = 'PendingVerification',
  Verified = 'Verified',
  Active = 'Active',
  Inactive = 'Inactive',
  Suspended = 'Suspended',
  Archived = 'Archived',
}

/**
 * Staff roles in provider business
 */
export enum StaffRole {
  Owner = 'owner',
  Manager = 'manager',
  ServiceProvider = 'service_provider',
  Specialist = 'specialist',
  Receptionist = 'receptionist',
  Assistant = 'assistant',
}

/**
 * Staff member status
 */
export enum StaffStatus {
  Active = 'active',
  Inactive = 'inactive',
  OnLeave = 'on_leave',
  Suspended = 'suspended',
  Terminated = 'terminated',
}

// ==================== Services ====================

/**
 * Service categories
 */
export enum ServiceCategory {
  // Hair Services
  Haircut = 'haircut',
  HairColoring = 'hair_coloring',
  HairStyling = 'hair_styling',
  HairTreatment = 'hair_treatment',

  // Beauty Services
  Makeup = 'makeup',
  Facial = 'facial',
  Skincare = 'skincare',
  Waxing = 'waxing',
  Threading = 'threading',

  // Nail Services
  Manicure = 'manicure',
  Pedicure = 'pedicure',
  NailArt = 'nail_art',

  // Massage & Spa
  Massage = 'massage',
  Spa = 'spa',
  Aromatherapy = 'aromatherapy',

  // Medical & Clinical
  Laser = 'laser',
  Botox = 'botox',
  Filler = 'filler',
  Mesotherapy = 'mesotherapy',
  PRP = 'prp',

  // Fitness
  PersonalTraining = 'personal_training',
  Yoga = 'yoga',
  Pilates = 'pilates',
  Zumba = 'zumba',

  // Other
  Consultation = 'consultation',
  Other = 'other',
}

/**
 * Service status
 */
export enum ServiceStatus {
  Active = 'active',
  Inactive = 'inactive',
  Draft = 'draft',
  Archived = 'archived',
}

/**
 * Service pricing type
 */
export enum PricingType {
  Fixed = 'fixed',
  Range = 'range',
  PerHour = 'per_hour',
  PerSession = 'per_session',
  Custom = 'custom',
}

// ==================== Booking & Appointments ====================

/**
 * Booking/Appointment status
 */
export enum BookingStatus {
  Requested = 'requested',
  Pending = 'pending',
  Confirmed = 'confirmed',
  InProgress = 'in_progress',
  Completed = 'completed',
  Cancelled = 'cancelled',
  NoShow = 'no_show',
  Rescheduled = 'rescheduled',
}

/**
 * Cancellation initiator
 */
export enum CancellationBy {
  Customer = 'customer',
  Provider = 'provider',
  System = 'system',
  Admin = 'admin',
}

/**
 * Booking source
 */
export enum BookingSource {
  Web = 'web',
  Mobile = 'mobile',
  App = 'app',
  Phone = 'phone',
  WalkIn = 'walk_in',
  Admin = 'admin',
}

// ==================== Payment ====================

/**
 * Payment status
 */
export enum PaymentStatus {
  Pending = 'pending',
  Authorized = 'authorized',
  Captured = 'captured',
  Completed = 'completed',
  Failed = 'failed',
  Cancelled = 'cancelled',
  Refunded = 'refunded',
  PartiallyRefunded = 'partially_refunded',
}

/**
 * Payment method
 */
export enum PaymentMethod {
  Cash = 'cash',
  Card = 'card',
  OnlinePayment = 'online_payment',
  BankTransfer = 'bank_transfer',
  Wallet = 'wallet',
  Credit = 'credit',
}

/**
 * Payment provider/gateway
 */
export enum PaymentProvider {
  ZarinPal = 'zarinpal',
  Behpardakht = 'behpardakht',
  Saman = 'saman',
  Parsian = 'parsian',
  Pasargad = 'pasargad',
  Mellat = 'mellat',
  Cash = 'cash',
  Other = 'other',
}

/**
 * Refund status
 */
export enum RefundStatus {
  Pending = 'pending',
  Processing = 'processing',
  Completed = 'completed',
  Failed = 'failed',
  Rejected = 'rejected',
}

/**
 * Payout status
 */
export enum PayoutStatus {
  Pending = 'pending',
  Scheduled = 'scheduled',
  Processing = 'processing',
  Paid = 'paid',
  Failed = 'failed',
  Cancelled = 'cancelled',
}

// ==================== Calendar & Schedule ====================

/**
 * Day of week
 */
export enum WeekDay {
  Saturday = 'saturday',
  Sunday = 'sunday',
  Monday = 'monday',
  Tuesday = 'tuesday',
  Wednesday = 'wednesday',
  Thursday = 'thursday',
  Friday = 'friday',
}

/**
 * Time slot status
 */
export enum TimeSlotStatus {
  Available = 'available',
  Booked = 'booked',
  Reserved = 'reserved',
  Blocked = 'blocked',
  Unavailable = 'unavailable',
}

/**
 * Recurrence pattern
 */
export enum RecurrencePattern {
  None = 'none',
  Daily = 'daily',
  Weekly = 'weekly',
  Monthly = 'monthly',
  Custom = 'custom',
}

// ==================== Notifications ====================

/**
 * Notification type
 */
export enum NotificationType {
  BookingConfirmed = 'booking_confirmed',
  BookingCancelled = 'booking_cancelled',
  BookingReminder = 'booking_reminder',
  BookingRescheduled = 'booking_rescheduled',
  PaymentReceived = 'payment_received',
  PaymentFailed = 'payment_failed',
  RefundProcessed = 'refund_processed',
  ReviewReceived = 'review_received',
  ProviderVerified = 'provider_verified',
  ProviderRejected = 'provider_rejected',
  SystemAlert = 'system_alert',
  PromotionalOffer = 'promotional_offer',
  General = 'general',
}

/**
 * Notification channel
 */
export enum NotificationChannel {
  Email = 'email',
  SMS = 'sms',
  Push = 'push',
  InApp = 'in_app',
  WhatsApp = 'whatsapp',
}

/**
 * Notification status
 */
export enum NotificationStatus {
  Pending = 'pending',
  Sent = 'sent',
  Delivered = 'delivered',
  Read = 'read',
  Failed = 'failed',
}

/**
 * Notification priority
 */
export enum NotificationPriority {
  Low = 'low',
  Normal = 'normal',
  High = 'high',
  Urgent = 'urgent',
}

// ==================== Reviews & Ratings ====================

/**
 * Review status
 */
export enum ReviewStatus {
  Pending = 'pending',
  Approved = 'approved',
  Rejected = 'rejected',
  Flagged = 'flagged',
  Hidden = 'hidden',
}

/**
 * Review rating (1-5 stars)
 */
export enum Rating {
  OneStar = 1,
  TwoStars = 2,
  ThreeStars = 3,
  FourStars = 4,
  FiveStars = 5,
}

// ==================== Localization ====================

/**
 * Supported languages
 */
export enum Language {
  Persian = 'fa',
  English = 'en',
}

/**
 * Currency codes (ISO 4217)
 */
export enum Currency {
  IRR = 'IRR', // Iranian Rial
  USD = 'USD', // US Dollar
  EUR = 'EUR', // Euro
}

/**
 * Date format preference
 */
export enum DateFormat {
  Gregorian = 'gregorian',
  Jalaali = 'jalaali',
  Hijri = 'hijri',
}

/**
 * Number format preference
 */
export enum NumberFormat {
  Western = 'western', // 0-9
  Persian = 'persian', // ۰-۹
  Arabic = 'arabic', // ٠-٩
}

// ==================== Media & Files ====================

/**
 * Media type
 */
export enum MediaType {
  Image = 'image',
  Video = 'video',
  Audio = 'audio',
  Document = 'document',
  Other = 'other',
}

/**
 * Image category
 */
export enum ImageCategory {
  Profile = 'profile',
  Cover = 'cover',
  Gallery = 'gallery',
  Portfolio = 'portfolio',
  BeforeAfter = 'before_after',
  Facility = 'facility',
  Certificate = 'certificate',
  Other = 'other',
}

// ==================== Analytics & Reporting ====================

/**
 * Report period
 */
export enum ReportPeriod {
  Today = 'today',
  Yesterday = 'yesterday',
  LastWeek = 'last_week',
  LastMonth = 'last_month',
  LastQuarter = 'last_quarter',
  LastYear = 'last_year',
  Custom = 'custom',
}

/**
 * Report type
 */
export enum ReportType {
  Revenue = 'revenue',
  Bookings = 'bookings',
  Customers = 'customers',
  Services = 'services',
  Performance = 'performance',
  Custom = 'custom',
}

/**
 * Chart type
 */
export enum ChartType {
  Line = 'line',
  Bar = 'bar',
  Pie = 'pie',
  Doughnut = 'doughnut',
  Area = 'area',
  Scatter = 'scatter',
}

// ==================== System & Admin ====================

/**
 * Log level
 */
export enum LogLevel {
  Debug = 'debug',
  Info = 'info',
  Warning = 'warning',
  Error = 'error',
  Critical = 'critical',
}

/**
 * Action type for audit logs
 */
export enum ActionType {
  Create = 'create',
  Read = 'read',
  Update = 'update',
  Delete = 'delete',
  Login = 'login',
  Logout = 'logout',
  PasswordChange = 'password_change',
  PasswordReset = 'password_reset',
  Export = 'export',
  Import = 'import',
  Other = 'other',
}

/**
 * Entity type for audit logs
 */
export enum EntityType {
  User = 'user',
  Provider = 'provider',
  Service = 'service',
  Booking = 'booking',
  Payment = 'payment',
  Review = 'review',
  Notification = 'notification',
  Other = 'other',
}

// ==================== Helper Functions ====================

/**
 * Get enum values as array
 */
export function getEnumValues<T extends Record<string, string | number>>(
  enumObj: T
): Array<T[keyof T]> {
  return Object.values(enumObj)
}

/**
 * Get enum keys as array
 */
export function getEnumKeys<T extends Record<string, string | number>>(
  enumObj: T
): Array<keyof T> {
  return Object.keys(enumObj) as Array<keyof T>
}

/**
 * Check if value is valid enum value
 */
export function isValidEnumValue<T extends Record<string, string | number>>(
  enumObj: T,
  value: unknown
): value is T[keyof T] {
  return Object.values(enumObj).includes(value as T[keyof T])
}

/**
 * Get enum label (converts snake_case to Title Case)
 */
export function getEnumLabel(value: string): string {
  return value
    .split('_')
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ')
}
