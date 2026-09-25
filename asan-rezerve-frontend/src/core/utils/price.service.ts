/**
 * Price formatting and parsing utilities
 * Used across components for consistent price display and handling
 */

/**
 * Format a price string with comma separators for display
 * @param price - Price value as string or number
 * @returns Formatted price string (e.g., "500,000")
 */
export function formatPriceDisplay(price: string | number): string {
  if (!price) return ''
  const numPrice = price.toString().replace(/,/g, '')
  return numPrice.replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

/**
 * Parse price input and return clean numeric string
 * Removes commas and validates that only digits are present
 * @param value - Price input value
 * @returns Clean numeric string if valid, empty string if invalid
 */
export function parsePriceInput(value: string): string {
  const numValue = value.replace(/,/g, '')
  return /^\d*$/.test(numValue) ? numValue : ''
}

/**
 * Convert price string to number
 * @param price - Price value as string or number
 * @returns Numeric value of the price
 */
export function priceToNumber(price: string | number): number {
  const numPrice = price.toString().replace(/,/g, '')
  return parseFloat(numPrice) || 0
}
