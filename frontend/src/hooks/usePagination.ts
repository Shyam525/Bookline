import { useState, useCallback, useMemo } from 'react';

export interface UsePaginationOptions<TFilters extends Record<string, unknown> = Record<string, unknown>> {
  initialPage?: number;
  initialPageSize?: number;
  initialSort?: string;
  initialDirection?: 'asc' | 'desc';
  initialFilters?: TFilters;
  maxPageSize?: number;
}

export interface UsePaginationReturn<TFilters extends Record<string, unknown> = Record<string, unknown>> {
  page: number;
  pageSize: number;
  sort?: string;
  direction: 'asc' | 'desc';
  filters: TFilters;
  setPage: (page: number) => void;
  setPageSize: (size: number) => void;
  setSort: (field: string, direction?: 'asc' | 'desc') => void;
  setFilter: <K extends keyof TFilters>(key: K, value: TFilters[K]) => void;
  setFilters: (filters: Partial<TFilters> | ((prev: TFilters) => TFilters)) => void;
  resetFilters: () => void;
  buildQueryKey: (scope: string, tenantId?: string) => unknown[];
  getQueryParams: () => Record<string, string | number | boolean>;
  getPaginationProps: (totalCount?: number) => {
    currentPage: number;
    totalPages: number;
    totalItems: number;
    pageSize: number;
    onPageChange: (page: number) => void;
    onPageSizeChange: (size: number) => void;
  };
}

export function usePagination<TFilters extends Record<string, unknown> = Record<string, unknown>>({
  initialPage = 1,
  initialPageSize = 20,
  initialSort,
  initialDirection = 'desc',
  initialFilters = {} as TFilters,
  maxPageSize = 100,
}: UsePaginationOptions<TFilters> = {}): UsePaginationReturn<TFilters> {
  const [page, setPageState] = useState<number>(Math.max(1, initialPage));
  const [pageSize, setPageSizeState] = useState<number>(Math.min(maxPageSize, Math.max(1, initialPageSize)));
  const [sort, setSortState] = useState<string | undefined>(initialSort);
  const [direction, setDirectionState] = useState<'asc' | 'desc'>(initialDirection);
  const [filters, setFiltersState] = useState<TFilters>(initialFilters);

  const setPage = useCallback((newPage: number) => {
    setPageState(Math.max(1, newPage));
  }, []);

  const setPageSize = useCallback((newSize: number) => {
    const safeSize = Math.min(maxPageSize, Math.max(1, newSize));
    setPageSizeState(safeSize);
    setPageState(1); // Section 84: Reset page to 1 when changing page size
  }, [maxPageSize]);

  const setSort = useCallback((field: string, newDir?: 'asc' | 'desc') => {
    setSortState(field);
    if (newDir) {
      setDirectionState(newDir);
    } else {
      setDirectionState((prev) => (sort === field && prev === 'asc' ? 'desc' : 'asc'));
    }
    setPageState(1); // Section 84: Reset page to 1 when sort changes
  }, [sort]);

  const setFilter = useCallback(<K extends keyof TFilters>(key: K, value: TFilters[K]) => {
    setFiltersState((prev) => ({ ...prev, [key]: value }));
    setPageState(1); // Section 84: Reset page to 1 when filter changes
  }, []);

  const setFilters = useCallback((update: Partial<TFilters> | ((prev: TFilters) => TFilters)) => {
    setFiltersState((prev) => {
      const next = typeof update === 'function' ? update(prev) : { ...prev, ...update };
      return next;
    });
    setPageState(1); // Section 84: Reset page to 1 when filters change
  }, []);

  const resetFilters = useCallback(() => {
    setFiltersState(initialFilters);
    setPageState(1);
  }, [initialFilters]);

  // Section 84.5: Include relevant filters, sorting and pagination in TanStack Query keys
  const buildQueryKey = useCallback((scope: string, tenantId?: string) => {
    return [
      scope,
      tenantId ?? 'global',
      {
        page,
        pageSize,
        sort,
        direction,
        ...filters,
      },
    ];
  }, [page, pageSize, sort, direction, filters]);

  const getQueryParams = useCallback(() => {
    const params: Record<string, string | number | boolean> = {
      page,
      pageSize,
    };
    if (sort) params.sort = sort;
    if (direction) params.direction = direction;

    for (const [key, val] of Object.entries(filters)) {
      if (val !== undefined && val !== null && val !== '') {
        params[key] = val as string | number | boolean;
      }
    }
    return params;
  }, [page, pageSize, sort, direction, filters]);

  const getPaginationProps = useCallback((totalCount: number = 0) => {
    const totalPages = pageSize <= 0 ? 0 : Math.ceil(Math.max(0, totalCount) / pageSize);
    return {
      currentPage: page,
      totalPages,
      totalItems: totalCount,
      pageSize,
      onPageChange: setPage,
      onPageSizeChange: setPageSize,
    };
  }, [page, pageSize, setPage, setPageSize]);

  return {
    page,
    pageSize,
    sort,
    direction,
    filters,
    setPage,
    setPageSize,
    setSort,
    setFilter,
    setFilters,
    resetFilters,
    buildQueryKey,
    getQueryParams,
    getPaginationProps,
  };
}
