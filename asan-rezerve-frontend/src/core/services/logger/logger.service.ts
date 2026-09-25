/**
 * Logger Service
 *
 * Centralized logging service for the application
 * Provides structured logging with different levels
 */

export enum LogLevel {
  DEBUG = 0,
  INFO = 1,
  WARN = 2,
  ERROR = 3,
}

export interface LogEntry {
  level: LogLevel
  message: string
  data?: unknown
  timestamp: Date
  context?: string
}

class LoggerService {
  private logLevel: LogLevel = LogLevel.DEBUG
  private logs: LogEntry[] = []
  private maxLogs = 1000 // Keep last 1000 logs in memory

  constructor() {
    // Set log level based on environment
    if (import.meta.env.PROD) {
      this.logLevel = LogLevel.WARN
    }
  }

  /**
   * Log a debug message
   */
  debug(message: string, data?: unknown, context?: string): void {
    this.log(LogLevel.DEBUG, message, data, context)
  }

  /**
   * Log an info message
   */
  info(message: string, data?: unknown, context?: string): void {
    this.log(LogLevel.INFO, message, data, context)
  }

  /**
   * Log a warning message
   */
  warn(message: string, data?: unknown, context?: string): void {
    this.log(LogLevel.WARN, message, data, context)
  }

  /**
   * Log an error message
   */
  error(message: string, data?: unknown, context?: string): void {
    this.log(LogLevel.ERROR, message, data, context)
  }

  /**
   * Core logging method
   */
  private log(level: LogLevel, message: string, data?: unknown, context?: string): void {
    if (level < this.logLevel) {
      return // Skip logs below current log level
    }

    const entry: LogEntry = {
      level,
      message,
      data,
      timestamp: new Date(),
      context,
    }

    // Store in memory
    this.logs.push(entry)
    if (this.logs.length > this.maxLogs) {
      this.logs.shift()
    }

    // Console output
    this.outputToConsole(entry)

    // In production, you might want to send errors to a logging service
    if (import.meta.env.PROD && level === LogLevel.ERROR) {
      // TODO: Send to external logging service (e.g., Sentry, LogRocket)
    }
  }

  /**
   * Output log entry to console
   */
  private outputToConsole(entry: LogEntry): void {
    const timestamp = entry.timestamp.toISOString()
    const contextStr = entry.context ? `[${entry.context}]` : ''
    const prefix = `${timestamp} ${contextStr}`

    switch (entry.level) {
      case LogLevel.DEBUG:
        console.debug(prefix, entry.message, entry.data || '')
        break
      case LogLevel.INFO:
        console.info(prefix, entry.message, entry.data || '')
        break
      case LogLevel.WARN:
        console.warn(prefix, entry.message, entry.data || '')
        break
      case LogLevel.ERROR:
        console.error(prefix, entry.message, entry.data || '')
        break
    }
  }

  /**
   * Get all stored logs
   */
  getLogs(): LogEntry[] {
    return [...this.logs]
  }

  /**
   * Get logs by level
   */
  getLogsByLevel(level: LogLevel): LogEntry[] {
    return this.logs.filter((log) => log.level === level)
  }

  /**
   * Clear all stored logs
   */
  clearLogs(): void {
    this.logs = []
  }

  /**
   * Set the minimum log level
   */
  setLogLevel(level: LogLevel): void {
    this.logLevel = level
  }
}

// Export singleton instance
export const loggerService = new LoggerService()

// Export class for testing/mocking
export { LoggerService }
