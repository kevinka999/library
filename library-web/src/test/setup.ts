import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll } from 'vitest'
import { server } from './server'

if (!window.localStorage) {
  const values = new Map<string, string>()
  Object.defineProperty(window, 'localStorage', {
    value: {
      get length() { return values.size },
      clear() { values.clear() },
      getItem(key: string) { return values.get(key) ?? null },
      key(index: number) { return [...values.keys()][index] ?? null },
      removeItem(key: string) { values.delete(key) },
      setItem(key: string, value: string) { values.set(key, String(value)) },
    } satisfies Storage,
  })
}

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => {
  cleanup()
  server.resetHandlers()
  window.localStorage.clear()
})
afterAll(() => server.close())
