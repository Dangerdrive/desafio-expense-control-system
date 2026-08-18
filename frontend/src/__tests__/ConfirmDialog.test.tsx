/**
 * Testes de componente — ConfirmDialog.
 *
 * Testa o componente isolado: labels padrão, variante "danger" e o
 * fechamento ao clicar no overlay (sem fechar ao clicar no conteúdo).
 */
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ConfirmDialog from '../components/ConfirmDialog';

const baseProps = {
  open: true,
  title: 'Remover pessoa',
  message: 'Remover "Ana"?',
  onConfirm: vi.fn(),
  onCancel: vi.fn(),
};

describe('ConfirmDialog', () => {
  it('should render nothing when closed', () => {
    const { container } = render(<ConfirmDialog {...baseProps} open={false} />);

    expect(container).toBeEmptyDOMElement();
  });

  it('should use the default labels and the primary style', () => {
    render(<ConfirmDialog {...baseProps} />);

    expect(screen.getByRole('dialog')).toHaveAttribute('aria-modal', 'true');
    expect(screen.getByText('Remover pessoa')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Cancelar' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Confirmar' })).toHaveClass('btn-primary');
  });

  it('should use the danger style and custom labels', () => {
    render(<ConfirmDialog {...baseProps} danger confirmLabel="Remover" cancelLabel="Voltar" />);

    expect(screen.getByRole('button', { name: 'Remover' })).toHaveClass('btn-danger');
    expect(screen.getByRole('button', { name: 'Voltar' })).toBeInTheDocument();
  });

  it('should call the callbacks of the buttons', async () => {
    const user = userEvent.setup();
    const onConfirm = vi.fn();
    const onCancel = vi.fn();
    render(<ConfirmDialog {...baseProps} onConfirm={onConfirm} onCancel={onCancel} />);

    await user.click(screen.getByRole('button', { name: 'Confirmar' }));
    expect(onConfirm).toHaveBeenCalledTimes(1);

    await user.click(screen.getByRole('button', { name: 'Cancelar' }));
    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('should cancel when clicking the overlay but not the dialog itself', async () => {
    const user = userEvent.setup();
    const onCancel = vi.fn();
    const { container } = render(<ConfirmDialog {...baseProps} onCancel={onCancel} />);

    await user.click(screen.getByRole('dialog'));
    expect(onCancel).not.toHaveBeenCalled();

    await user.click(container.querySelector('.modal-overlay')!);
    expect(onCancel).toHaveBeenCalledTimes(1);
  });
});
