/**
 * Testes de componente — ErrorBoundary.
 *
 * O React registra o erro capturado no console; silenciamos console.error
 * nestes testes para não poluir a saída da suite.
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ErrorBoundary from '../components/ErrorBoundary';

function Boom({ message }: { message: string }): never {
  throw new Error(message);
}

/** Filho cujo comportamento é controlado externamente pelo teste. */
function FlakyChild({ shouldThrow }: { shouldThrow: () => boolean }) {
  if (shouldThrow()) throw new Error('Falha temporária');
  return <p>Conteúdo recuperado</p>;
}

let consoleError: ReturnType<typeof vi.spyOn>;

beforeEach(() => {
  consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
});

afterEach(() => {
  consoleError.mockRestore();
});

describe('ErrorBoundary', () => {
  it('should render children when there is no error', () => {
    render(
      <ErrorBoundary>
        <p>Tudo certo</p>
      </ErrorBoundary>,
    );

    expect(screen.getByText('Tudo certo')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('should render the fallback UI with the error message', () => {
    render(
      <ErrorBoundary>
        <Boom message="Erro de renderização" />
      </ErrorBoundary>,
    );

    const alert = screen.getByRole('alert');
    expect(alert).toBeInTheDocument();
    expect(screen.getByText('😵 Algo deu errado')).toBeInTheDocument();
    expect(screen.getByText('Erro de renderização')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Tentar novamente' })).toBeInTheDocument();
  });

  it('should show a default message when the error has no message', () => {
    render(
      <ErrorBoundary>
        <Boom message="" />
      </ErrorBoundary>,
    );

    expect(screen.getByText('Erro inesperado.')).toBeInTheDocument();
  });

  it('should log the captured error via componentDidCatch', () => {
    render(
      <ErrorBoundary>
        <Boom message="Erro logado" />
      </ErrorBoundary>,
    );

    expect(consoleError).toHaveBeenCalledWith(
      'ErrorBoundary capturou um erro:',
      expect.objectContaining({ message: 'Erro logado' }),
      expect.anything(),
    );
  });

  it('should re-render the children after clicking "Tentar novamente"', async () => {
    const user = userEvent.setup();
    let failing = true;
    render(
      <ErrorBoundary>
        <FlakyChild shouldThrow={() => failing} />
      </ErrorBoundary>,
    );

    expect(screen.getByRole('alert')).toBeInTheDocument();

    failing = false;
    await user.click(screen.getByRole('button', { name: 'Tentar novamente' }));

    expect(await screen.findByText('Conteúdo recuperado')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });
});
