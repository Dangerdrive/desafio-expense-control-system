interface PaginationProps {
  page: number;
  totalPages: number;
  totalItems: number;
  onPageChange: (page: number) => void;
}

/**
 * Controles de paginação reutilizáveis (usados nas abas Pessoas e Transações).
 * Mostra "Página X de Y (N itens)" com botões Anterior/Próxima.
 * Só é exibido quando há mais de uma página.
 */
function Pagination({ page, totalPages, totalItems, onPageChange }: PaginationProps) {
  if (totalPages <= 1) return null;

  return (
    <div className="pagination" role="navigation" aria-label="Paginação">
      <button
        type="button"
        className="btn btn-secondary btn-sm"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
        aria-label="Página anterior"
      >
        ← Anterior
      </button>
      <span className="pagination-info">
        Página {page} de {totalPages} ({totalItems} itens)
      </span>
      <button
        type="button"
        className="btn btn-secondary btn-sm"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
        aria-label="Próxima página"
      >
        Próxima →
      </button>
    </div>
  );
}

export default Pagination;
