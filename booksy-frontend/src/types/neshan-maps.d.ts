// Import types instead of using triple-slash reference
import type { Map } from '@neshan-maps-platform/ol'

declare module '@neshan-maps-platform/vue3-openlayers' {
  import { DefineComponent } from 'vue'

  export interface NeshanMapProps {
    defaultType?: string
    mapKey: string
    serviceKey: string
    center?: { latitude: number; longitude: number }
    zoom?: number
    poi?: boolean
    traffic?: boolean
    searchUrl?: string
    reverseUrl?: string
  }

  export interface NeshanMapEvents {
    'on-init': (map: Map) => void
  }

  const NeshanMap: DefineComponent<
    NeshanMapProps,
    Record<string, never>,
    Record<string, never>,
    Record<string, never>,
    Record<string, never>,
    Record<string, never>,
    Record<string, never>,
    NeshanMapEvents
  >
  export default NeshanMap
}

// Neshan Search API types
export interface SearchResult {
  count: number
  items: PrimarySearchItem[]
}

export interface PrimarySearchItem {
  category: string
  location: { x: number; y: number }
  neighbourhood: string
  region: string
  title: string
  type: string
  address: string
}

// Neshan Reverse Geocoding API types
export interface PrimaryReverseResult {
  city: string | null
  district: string | null
  formatted_address: string | null
  in_odd_even_zone: boolean
  in_traffic_zone: boolean
  municipality_zone: string | null
  neighbourhood: string | null
  place: string | null
  route_name: string | null
  route_type: string | null
  state: string | null
  status: string | null
  village: string | null
}
