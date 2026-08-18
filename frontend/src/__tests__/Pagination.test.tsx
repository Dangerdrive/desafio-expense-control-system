/**
 * Testes de componente — Pagination.
 *
 * Testa o componente isoladamente (sem o App) para cobrir os limites
 * de página que a listagem real raramente atinge nos testes de integração.
 */
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Pagination from '../components/Pagination';

describe('Pagination', () => {
  it('should render nothing when there is a single page or none', () => {
    const { container, rerender } = render(
      <Pagination page={1} totalPages={1} totalItems={3} onPageChange={vi.fn()} />,
    );
    expect(container).toBeEmptyDOMElement();

    rerender(<Pagination page={1} totalPages={0} totalItems={0} onPageChange={vi.fn()} />);
    expect(container).toBeEmptyDOMElement();
  });

  it('should show the current page, total pages and item count', () => {
    render(<Pagination page={2} totalPages={3} totalItems={25} onPageChange={vi.fn()} />);

    expect(screen.getByText('Página 2 de 3 (25 itens)')).toBeInTheDocument();
  });

  it('should disable the previous button on the first page', () => {
    render(<Pagination page={1} totalPages={3} totalItems={25} onPageChange={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'Página anterior' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Próxima página' })).toBeEnabled();
  });

  it('should disable the next button on the last page', () => {
    render(<Pagination page={3} totalPages={3} totalItems={25} onPageChange={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'Próxima página' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Página anterior' })).toBeEnabled();
  });

  it('should request the previous page', async () => {
    const user = userEvent.setup();
    const onPageChange = vi.fn();
    render(<Pagination page={2} totalPages={3} totalItems={25} onPageChange={onPageChange} />);

    await user.click(screen.getByRole('button', { name: 'Página anterior' }));

    expect(onPageChange).toHaveBeenCalledWith(1);
  });

  it('should request the next page', async () => {
    const user = userEvent.setup();
    const onPageChange = vi.fn();
    render(<Pagination page={2} totalPages={3} totalItems={25} onPageChange={onPageChange} />);

    await user.click(screen.getByRole('button', { name: 'Próxima página' }));

    expect(onPageChange).toHaveBeenCalledWith(3);
  });
});
