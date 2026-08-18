import { useCallback, useState } from 'react';
import type { PagedResult } from '../types';

/**
 * Estado compartilhado para listagens paginadas.
 */
export function usePagedList<T>() {
  const [items, setItems] = useState<T[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalItems, setTotalItems] = useState(0);

  const applyPagedResult = useCallback((result: PagedResult<T>) => {
    setItems(result.items);
    setPage(result.page);
    setTotalPages(result.totalPages);
    setTotalItems(result.totalItems);
  }, []);

  const getPageAfterRemoval = useCallback((itemCount: number) =>
    itemCount === 1 && page > 1 ? page - 1 : page, [page]);

  return {
    items,
    page,
    setPage,
    totalPages,
    totalItems,
    applyPagedResult,
    getPageAfterRemoval,
  };
}
