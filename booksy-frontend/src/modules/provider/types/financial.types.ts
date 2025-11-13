/**
 * Financial & Payout Types
 *
 * Type definitions for provider earnings, transactions, and payouts.
 * Based on backend Financial and Payout APIs.
 */

// ============================================================================
// ENUMS
// ============================================================================

export enum PayoutStatus {
  Pending = 'Pending',
  Processing = 'Processing',
  Completed = 'Completed',
  Failed = 'Failed',
  Cancelled = 'Cancelled',
}

export enum TransactionType {
  Booking = 'Booking',
  Refund = 'Refund',
  Adjustment = 'Adjustment',
  Payout = 'Payout',
  Commission = 'Commission',
}

export enum PayoutMethod {
  BankTransfer = 'BankTransfer',
  Check = 'Check',
  PayPal = 'PayPal',
  Stripe = 'Stripe',
  ZarinPal = 'ZarinPal',
}

export enum EarningsPeriod {
  Today = 'Today',
  ThisWeek = 'ThisWeek',
  ThisMonth = 'ThisMonth',
  LastMonth = 'LastMonth',
  Custom = 'Custom',
}

// ============================================================================
// LABEL MAPPINGS
// ============================================================================

export const PAYOUT_STATUS_LABELS: Record<PayoutStatus, string> = {
  [PayoutStatus.Pending]: 'در انتظار',
  [PayoutStatus.Processing]: 'در حال پردازش',
  [PayoutStatus.Completed]: 'تکمیل شده',
  [PayoutStatus.Failed]: 'ناموفق',
  [PayoutStatus.Cancelled]: 'لغو شده',
}

export const TRANSACTION_TYPE_LABELS: Record<TransactionType, string> = {
  [TransactionType.Booking]: 'رزرو',
  [TransactionType.Refund]: 'بازگشت وجه',
  [TransactionType.Adjustment]: 'تعدیل',
  [TransactionType.Payout]: 'واریز',
  [TransactionType.Commission]: 'کمیسیون',
}

export const PAYOUT_METHOD_LABELS: Record<PayoutMethod, string> = {
  [PayoutMethod.BankTransfer]: 'انتقال بانکی',
  [PayoutMethod.Check]: 'چک',
  [PayoutMethod.PayPal]: 'پی‌پال',
  [PayoutMethod.Stripe]: 'استرایپ',
  [PayoutMethod.ZarinPal]: 'زرین‌پال',
}

// ============================================================================
// CORE ENTITIES
// ============================================================================

/**
 * Provider earnings summary
 */
export interface EarningsSummary {
  providerId: string
  totalRevenue: number // Total revenue before commission
  platformCommission: number // Commission amount
  netEarnings: number // Revenue after commission
  totalBookings: number
  completedBookings: number
  cancelledBookings: number
  refundedAmount: number
  pendingPayouts: number
  completedPayouts: number
  currency: string
  startDate: string // ISO date
  endDate: string // ISO date
  commissionPercentage: number
}

/**
 * Earnings breakdown by period
 */
export interface EarningsBreakdown {
  date: string // ISO date or period label
  revenue: number
  commission: number
  netEarnings: number
  bookingsCount: number
}

/**
 * Transaction entity
 */
export interface Transaction {
  id: string
  providerId: string
  bookingId?: string
  type: TransactionType
  amount: number
  currency: string
  description: string
  status: string
  createdAt: string
  processedAt?: string
  metadata?: Record<string, any>
}

/**
 * Payout entity
 */
export interface Payout {
  id: string
  providerId: string
  amount: number
  currency: string
  method: PayoutMethod
  status: PayoutStatus
  requestedAt: string
  processedAt?: string
  completedAt?: string
  failureReason?: string
  transactionIds: string[]
  bankAccountLast4?: string
  referenceNumber?: string
  notes?: string
}

/**
 * Bank account information
 */
export interface BankAccount {
  id: string
  providerId: string
  accountHolderName: string
  bankName: string
  accountNumber: string // Last 4 digits visible
  routingNumber?: string
  iban?: string
  isDefault: boolean
  isVerified: boolean
  createdAt: string
}

// ============================================================================
// REQUEST/RESPONSE TYPES
// ============================================================================

/**
 * Request to get provider earnings
 */
export interface GetEarningsRequest {
  providerId: string
  startDate: string // ISO date
  endDate: string // ISO date
  commissionPercentage?: number // Optional override
  groupBy?: 'day' | 'week' | 'month'
}

/**
 * Response for earnings query
 */
export interface GetEarningsResponse {
  summary: EarningsSummary
  breakdown: EarningsBreakdown[]
}

/**
 * Request to create a payout
 */
export interface CreatePayoutRequest {
  providerId: string
  amount: number
  method: PayoutMethod
  bankAccountId?: string
  notes?: string
}

/**
 * Request to get payout list
 */
export interface GetPayoutsRequest {
  providerId: string
  status?: PayoutStatus
  startDate?: string
  endDate?: string
  page?: number
  pageSize?: number
}

/**
 * Request to get transaction history
 */
export interface GetTransactionsRequest {
  providerId: string
  type?: TransactionType
  startDate?: string
  endDate?: string
  page?: number
  pageSize?: number
  sortBy?: 'createdAt' | 'amount'
  sortOrder?: 'asc' | 'desc'
}

// ============================================================================
// VIEW MODELS
// ============================================================================

/**
 * Financial dashboard summary
 */
export interface FinancialDashboard {
  currentMonthEarnings: EarningsSummary
  previousMonthEarnings: EarningsSummary
  pendingPayoutAmount: number
  nextPayoutDate?: string
  recentTransactions: Transaction[]
  recentPayouts: Payout[]
}

/**
 * Payout card view
 */
export interface PayoutCardView {
  id: string
  amount: number
  currency: string
  method: PayoutMethod
  methodLabel: string
  status: PayoutStatus
  statusLabel: string
  statusColor: string
  requestedAt: string
  completedAt?: string
  bankAccountLast4?: string
  referenceNumber?: string
}

/**
 * Transaction card view
 */
export interface TransactionCardView {
  id: string
  type: TransactionType
  typeLabel: string
  amount: number
  currency: string
  description: string
  createdAt: string
  isPositive: boolean // true for income, false for deduction
}

// ============================================================================
// FILTER/SEARCH TYPES
// ============================================================================

/**
 * Financial filters
 */
export interface FinancialFilters {
  period?: EarningsPeriod
  startDate?: string
  endDate?: string
  transactionType?: TransactionType
  payoutStatus?: PayoutStatus
}

/**
 * Date range for queries
 */
export interface DateRange {
  startDate: string
  endDate: string
  label: string
}

// ============================================================================
// STATISTICS TYPES
// ============================================================================

/**
 * Revenue statistics
 */
export interface RevenueStatistics {
  totalRevenue: number
  averageBookingValue: number
  topServiceRevenue: {
    serviceId: string
    serviceName: string
    revenue: number
  }[]
  growthRate: number // Percentage change from previous period
}

/**
 * Payout statistics
 */
export interface PayoutStatistics {
  totalPaidOut: number
  averagePayoutAmount: number
  payoutFrequency: number // Days between payouts
  successRate: number // Percentage of successful payouts
}

// ============================================================================
// UTILITY TYPES
// ============================================================================

/**
 * Status colors for UI
 */
export const PAYOUT_STATUS_COLORS: Record<PayoutStatus, string> = {
  [PayoutStatus.Pending]: '#f59e0b', // amber
  [PayoutStatus.Processing]: '#3b82f6', // blue
  [PayoutStatus.Completed]: '#10b981', // green
  [PayoutStatus.Failed]: '#ef4444', // red
  [PayoutStatus.Cancelled]: '#6b7280', // gray
}

/**
 * Predefined date ranges
 */
export const PREDEFINED_DATE_RANGES: DateRange[] = [
  {
    startDate: new Date(new Date().setHours(0, 0, 0, 0)).toISOString(),
    endDate: new Date(new Date().setHours(23, 59, 59, 999)).toISOString(),
    label: 'امروز',
  },
  {
    startDate: new Date(new Date().setDate(new Date().getDate() - 7)).toISOString(),
    endDate: new Date().toISOString(),
    label: '۷ روز گذشته',
  },
  {
    startDate: new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString(),
    endDate: new Date().toISOString(),
    label: 'این ماه',
  },
  {
    startDate: new Date(new Date().getFullYear(), new Date().getMonth() - 1, 1).toISOString(),
    endDate: new Date(new Date().getFullYear(), new Date().getMonth(), 0).toISOString(),
    label: 'ماه گذشته',
  },
]

// ============================================================================
// TYPE GUARDS
// ============================================================================

export function isEarningsSummary(obj: unknown): obj is EarningsSummary {
  return (
    typeof obj === 'object' &&
    obj !== null &&
    'providerId' in obj &&
    'totalRevenue' in obj &&
    'netEarnings' in obj
  )
}

export function isPayout(obj: unknown): obj is Payout {
  return (
    typeof obj === 'object' &&
    obj !== null &&
    'id' in obj &&
    'providerId' in obj &&
    'amount' in obj &&
    'status' in obj
  )
}

export function isTransaction(obj: unknown): obj is Transaction {
  return (
    typeof obj === 'object' &&
    obj !== null &&
    'id' in obj &&
    'type' in obj &&
    'amount' in obj
  )
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Get payout status label
 */
export function getPayoutStatusLabel(status: PayoutStatus): string {
  return PAYOUT_STATUS_LABELS[status] || status
}

/**
 * Get payout status color
 */
export function getPayoutStatusColor(status: PayoutStatus): string {
  return PAYOUT_STATUS_COLORS[status] || '#6b7280'
}

/**
 * Get transaction type label
 */
export function getTransactionTypeLabel(type: TransactionType): string {
  return TRANSACTION_TYPE_LABELS[type] || type
}

/**
 * Check if transaction is income or deduction
 */
export function isIncomeTransaction(type: TransactionType): boolean {
  return type === TransactionType.Booking
}

/**
 * Format currency amount
 */
export function formatCurrency(amount: number, currency: string = 'IRR'): string {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']

  // Format number with thousands separator
  const formatted = amount.toLocaleString('en-US')

  // Convert to Persian digits
  const persianFormatted = formatted.replace(/\d/g, (digit) => persianDigits[parseInt(digit)])

  // Add currency symbol
  if (currency === 'IRR') {
    return `${persianFormatted} تومان`
  }

  return `${persianFormatted} ${currency}`
}

/**
 * Calculate earnings growth rate
 */
export function calculateGrowthRate(current: number, previous: number): number {
  if (previous === 0) return current > 0 ? 100 : 0
  return ((current - previous) / previous) * 100
}

/**
 * Format growth rate for display
 */
export function formatGrowthRate(rate: number): string {
  const sign = rate >= 0 ? '+' : ''
  const rounded = Math.round(rate * 10) / 10
  return `${sign}${rounded}%`
}

/**
 * Get date range for period
 */
export function getDateRangeForPeriod(period: EarningsPeriod): DateRange {
  const now = new Date()

  switch (period) {
    case EarningsPeriod.Today:
      return {
        startDate: new Date(now.setHours(0, 0, 0, 0)).toISOString(),
        endDate: new Date(now.setHours(23, 59, 59, 999)).toISOString(),
        label: 'امروز',
      }

    case EarningsPeriod.ThisWeek:
      const weekStart = new Date(now)
      weekStart.setDate(now.getDate() - now.getDay())
      weekStart.setHours(0, 0, 0, 0)
      return {
        startDate: weekStart.toISOString(),
        endDate: new Date().toISOString(),
        label: 'این هفته',
      }

    case EarningsPeriod.ThisMonth:
      return {
        startDate: new Date(now.getFullYear(), now.getMonth(), 1).toISOString(),
        endDate: new Date().toISOString(),
        label: 'این ماه',
      }

    case EarningsPeriod.LastMonth:
      const lastMonthStart = new Date(now.getFullYear(), now.getMonth() - 1, 1)
      const lastMonthEnd = new Date(now.getFullYear(), now.getMonth(), 0, 23, 59, 59, 999)
      return {
        startDate: lastMonthStart.toISOString(),
        endDate: lastMonthEnd.toISOString(),
        label: 'ماه گذشته',
      }

    default:
      return PREDEFINED_DATE_RANGES[0]
  }
}

/**
 * Format date relative to now
 */
export function formatRelativeDate(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays === 0) return 'امروز'
  if (diffDays === 1) return 'دیروز'
  if (diffDays < 7) return `${diffDays} روز پیش`
  if (diffDays < 30) return `${Math.floor(diffDays / 7)} هفته پیش`
  if (diffDays < 365) return `${Math.floor(diffDays / 30)} ماه پیش`
  return `${Math.floor(diffDays / 365)} سال پیش`
}
