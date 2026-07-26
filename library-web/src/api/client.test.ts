import { AxiosError } from 'axios'
import { describe, expect, it } from 'vitest'
import { normalizeApiError } from './client'

describe('API error normalization', () => {
  it('normalizes validation and stale HTTP failures', () => {
    const validation = new AxiosError()
    Object.assign(validation, {
      response: { status: 400, data: { errors: { Title: ['Required'] } } },
    })
    expect(normalizeApiError(validation)).toMatchObject({
      kind: 'validation',
      status: 400,
      fieldErrors: { Title: ['Required'] },
    })

    const stale = new AxiosError()
    Object.assign(stale, { response: { status: 412 } })
    expect(normalizeApiError(stale)).toMatchObject({ kind: 'stale', status: 412 })
  })
})
