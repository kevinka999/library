import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { useDebounce } from './use-debounce'

describe('useDebounce', () => {
  afterEach(() => vi.useRealTimers())

  it('publishes only the latest value after the configured delay', () => {
    vi.useFakeTimers()
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 400),
      { initialProps: { value: 'initial' } },
    )

    rerender({ value: 'intermediate' })
    rerender({ value: 'final' })
    expect(result.current).toBe('initial')

    act(() => vi.advanceTimersByTime(399))
    expect(result.current).toBe('initial')

    act(() => vi.advanceTimersByTime(1))
    expect(result.current).toBe('final')
  })
})
