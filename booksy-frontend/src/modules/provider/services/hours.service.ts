// hours.service.ts - Working hours API client
import { httpClient } from '@/core/api/client/http-client'
import type {
  BusinessHoursWithBreaks,
  HolidaySchedule,
  ExceptionSchedule,
  UpdateBusinessHoursRequest,
  AddHolidayRequest,
  AddExceptionRequest,
  HolidaysResponse,
  ExceptionsResponse,
} from '../types/hours.types'

const BASE_URL = '/api/v1/providers'

class HoursService {
  /**
   * Get business hours for a provider
   */
  async getBusinessHours(providerId: string): Promise<{ businessHours: BusinessHoursWithBreaks[] }> {
    const response = await httpClient.get<{ businessHours: BusinessHoursWithBreaks[] }>(
      `${BASE_URL}/${providerId}/business-hours`
    )
    return response.data
  }

  /**
   * Update business hours for a provider
   */
  async updateBusinessHours(request: UpdateBusinessHoursRequest): Promise<void> {
    await httpClient.put(
      `${BASE_URL}/${request.providerId}/business-hours`,
      { businessHours: request.businessHours }
    )
  }

  /**
   * Get all holidays for a provider
   */
  async getHolidays(providerId: string): Promise<HolidaysResponse> {
    const response = await httpClient.get<HolidaysResponse>(
      `${BASE_URL}/${providerId}/holidays`
    )
    return response.data
  }

  /**
   * Add a holiday
   */
  async addHoliday(request: AddHolidayRequest): Promise<HolidaySchedule> {
    const response = await httpClient.post<HolidaySchedule>(
      `${BASE_URL}/${request.providerId}/holidays`,
      request.holiday
    )
    return response.data
  }

  /**
   * Delete a holiday
   */
  async deleteHoliday(providerId: string, holidayId: string): Promise<void> {
    await httpClient.delete(`${BASE_URL}/${providerId}/holidays/${holidayId}`)
  }

  /**
   * Get all exception schedules for a provider
   */
  async getExceptions(providerId: string): Promise<ExceptionsResponse> {
    const response = await httpClient.get<ExceptionsResponse>(
      `${BASE_URL}/${providerId}/exceptions`
    )
    return response.data
  }

  /**
   * Add an exception schedule
   */
  async addException(request: AddExceptionRequest): Promise<ExceptionSchedule> {
    const response = await httpClient.post<ExceptionSchedule>(
      `${BASE_URL}/${request.providerId}/exceptions`,
      request.exception
    )
    return response.data
  }

  /**
   * Delete an exception schedule
   */
  async deleteException(providerId: string, exceptionId: string): Promise<void> {
    await httpClient.delete(`${BASE_URL}/${providerId}/exceptions/${exceptionId}`)
  }
}

export const hoursService = new HoursService()
