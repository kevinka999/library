import axios, { AxiosError } from 'axios'

export interface ProblemDetails {
  type?: string | null
  title?: string | null
  status?: number | null
  detail?: string | null
  instance?: string | null
}

export interface ValidationProblemDetails extends ProblemDetails {
  errors?: Record<string, string[]>
}

export type ApiErrorKind =
  | 'validation'
  | 'not-found'
  | 'stale'
  | 'precondition'
  | 'integration'
  | 'unexpected'

export class ApiError extends Error {
  readonly kind: ApiErrorKind
  readonly status?: number
  readonly fieldErrors: Record<string, string[]>

  constructor(
    kind: ApiErrorKind,
    status?: number,
    fieldErrors: Record<string, string[]> = {},
  ) {
    super(kind)
    this.name = 'ApiError'
    this.kind = kind
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5168',
  timeout: 10_000,
  headers: {
    Accept: 'application/json',
    'Content-Type': 'application/json',
  },
})

export function normalizeApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error
  if (!(error instanceof AxiosError)) return new ApiError('unexpected')
  const status = error.response?.status
  const details = error.response?.data as ValidationProblemDetails | undefined
  if (status === 400) return new ApiError('validation', status, details?.errors ?? {})
  if (status === 404) return new ApiError('not-found', status)
  if (status === 412) return new ApiError('stale', status)
  if (status === 428) return new ApiError('precondition', status)
  return new ApiError('unexpected', status)
}

export function requireEtag(etag: string | undefined): string {
  if (!etag) throw new ApiError('integration')
  return etag
}
