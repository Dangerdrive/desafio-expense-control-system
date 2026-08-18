/**
 * Testes de componente — TransactionsTab isolada.
 *
 * Complementa App.test.tsx cobrindo validações do formulário, filtros
 * por período/ordenação, cancelamento de edição e caminhos de erro.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import TransactionsTab from '../components/TransactionsTab';
import type { PagedResult, Person, Transaction } from '../types';

vi.mock('../api', () => ({
  getPeople: vi.fn(),
  getTransactions: vi.fn(),
  createTransaction: vi.fn(),
  updateTransaction: vi.fn(),
  deleteTransaction: vi.fn(),
}));

import * as api from '../api';

const getPeople = api.getPeople as ReturnType<typeof vi.fn>;
const getTransactions = api.getTransactions as ReturnType<typeof vi.fn>;
const createTransaction = api.createTransaction as ReturnType<typeof vi.fn>;
const deleteTransaction = api.deleteTransaction as ReturnType<typeof vi.fn>;

function paged<T>(items: T[], overrides: Partial<PagedResult<T>> = {}): PagedResult<T> {
  return {
    items,
    page: 1,
    pageSize: 10,
    totalItems: items.length,
    totalPages: items.length === 0 ? 0 : 1,
    hasNext: false,
    hasPrevious: false,
    ...overrides,
  };
}

const adult: Person = { id: 1, name: 'João', age: 30 };
const minor: Person = { id: 2, name: 'Bia', age: 10 };
const expense: Transaction = {
  id: 5, description: 'Aluguel', amount: 1500.5, date: '2026-01-15',
  type: 'despesa', personId: 1, personName: 'João',
};

beforeEach(() => {
  vi.clearAllMocks();
  getPeople.mockResolvedValue(paged([adult]));
  getTransactions.mockResolvedValue(paged<Transaction>([]));
});

/** Renderiza e aguarda o fim do carregamento inicial da listagem. */
async function renderSettled() {
  render(<TransactionsTab />);
  await waitFor(() => {
    expect(screen.queryByText('Carregando transações...')).not.toBeInTheDocument();
  });
}

describe('TransactionsTab — listagem', () => {
  it('should show an error message when the initial load fails', async () => {
    getTransactions.mockRejectedValue(new Error('Backend offline'));

    render(<TransactionsTab />);

    expect(await screen.findByText('Backend offline')).toBeInTheDocument();
  });

  it('should use the generic fallback when the load error is not an Error', async () => {
    getTransactions.mockRejectedValue('falhou');
    getPeople.mockRejectedValue('falhou');

    render(<TransactionsTab />);

    expect(await screen.findByText('falhou')).toBeInTheDocument();
  });

  it('should render the row formatted in pt-BR with the expense badge', async () => {
    getTransactions.mockResolvedValue(paged([expense]));

    await renderSettled();

    const row = screen.getByText('Aluguel').closest('tr')!;
    expect(within(row).getByText('15/01/2026')).toBeInTheDocument();
    expect(within(row).getByText(/1\.500,50/)).toBeInTheDocument();
    expect(within(row).getByText('📉 Despesa')).toBeInTheDocument();
  });

  it('should render the income badge in green', async () => {
    getTransactions.mockResolvedValue(paged([
      { ...expense, id: 6, description: 'Salário', type: 'receita' as const, amount: 5000 },
    ]));

    await renderSettled();

    const row = screen.getByText('Salário').closest('tr')!;
    expect(within(row).getByText('📈 Receita')).toBeInTheDocument();
  });

  it('should flag minors in the person selector', async () => {
    getPeople.mockResolvedValue(paged([adult, minor]));

    await renderSettled();

    expect(screen.getByRole('option', { name: 'Bia (10a 🔞)' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'João (30a)' })).toBeInTheDocument();
  });

  it('should disable the submit button when there is no person registered', async () => {
    getPeople.mockResolvedValue(paged<Person>([]));

    await renderSettled();

    expect(screen.getByText('➕ Registrar')).toBeDisabled();
    expect(screen.getByText(/Cadastre uma pessoa antes/)).toBeInTheDocument();
  });
});

describe('TransactionsTab — filtros', () => {
  it('should reload with the period filter and reset to the first page', async () => {
    const user = userEvent.setup();
    await renderSettled();

    await user.type(screen.getByLabelText('Data inicial'), '2026-01-01');
    await user.type(screen.getByLabelText('Data final'), '2026-01-31');

    await waitFor(() => {
      expect(getTransactions).toHaveBeenLastCalledWith({
        from: '2026-01-01', to: '2026-01-31', sort: 'date_desc', page: 1, pageSize: 10,
      });
    });
  });

  it('should reload with the ascending sort', async () => {
    const user = userEvent.setup();
    await renderSettled();

    await user.selectOptions(screen.getByLabelText('Ordenar'), 'date_asc');

    await waitFor(() => {
      expect(getTransactions).toHaveBeenLastCalledWith(
        expect.objectContaining({ sort: 'date_asc', page: 1 }),
      );
    });
  });
});

describe('TransactionsTab — validação do formulário', () => {
  it('should require a description', async () => {
    const user = userEvent.setup();
    await renderSettled();

    await user.click(screen.getByText('➕ Registrar'));

    expect(await screen.findByText('Descrição é obrigatória.')).toBeInTheDocument();
    expect(createTransaction).not.toHaveBeenCalled();
  });

  it('should reject an invalid amount', async () => {
    const user = userEvent.setup();
    await renderSettled();

    await user.type(screen.getByPlaceholderText('Descrição'), 'Aluguel');
    await user.type(screen.getByLabelText('Valor'), '0');
    await user.click(screen.getByText('➕ Registrar'));

    expect(await screen.findByText('Valor deve ser maior que zero (use até 2 casas decimais).')).toBeInTheDocument();
    expect(createTransaction).not.toHaveBeenCalled();
  });

  it('should require a person', async () => {
    const user = userEvent.setup();
    await renderSettled();

    await user.type(screen.getByPlaceholderText('Descrição'), 'Aluguel');
    await user.type(screen.getByLabelText('Valor'), '1500');
    await user.click(screen.getByText('➕ Registrar'));

    expect(await screen.findByText('Selecione uma pessoa.')).toBeInTheDocument();
    expect(createTransaction).not.toHaveBeenCalled();
  });

  it('should mask the amount input, keeping a single separator and two decimals', async () => {
    const user = userEvent.setup();
    await renderSettled();

    await user.type(screen.getByLabelText('Valor'), '1,2345');

    expect(screen.getByLabelText('Valor')).toHaveValue('1.23');
  });

  it('should send the amount typed with a comma as a number', async () => {
    const user = userEvent.setup();
    createTransaction.mockResolvedValue({});

    await renderSettled();

    await user.type(screen.getByPlaceholderText('Descrição'), '  Mercado  ');
    await user.type(screen.getByLabelText('Valor'), '12,34');
    await user.selectOptions(screen.getByLabelText('Pessoa'), '1');
    await user.click(screen.getByText('➕ Registrar'));

    await waitFor(() => {
      expect(createTransaction).toHaveBeenCalledWith(
        expect.objectContaining({ description: 'Mercado', amount: 12.34, type: 'despesa', personId: 1 }),
      );
    });
  });
});

describe('TransactionsTab — edição', () => {
  it('should clear the form and leave edit mode on cancel', async () => {
    const user = userEvent.setup();
    getTransactions.mockResolvedValue(paged([expense]));

    await renderSettled();

    await user.click(screen.getByRole('button', { name: 'Editar Aluguel' }));
    expect(screen.getByPlaceholderText('Descrição')).toHaveValue('Aluguel');
    expect(screen.getByLabelText('Valor')).toHaveValue('1500.5');
    expect(screen.getByLabelText('Data')).toHaveValue('2026-01-15');

    await user.click(screen.getByText('✖ Cancelar'));

    expect(screen.getByPlaceholderText('Descrição')).toHaveValue('');
    expect(screen.getByLabelText('Valor')).toHaveValue('');
    expect(screen.getByText('➕ Registrar')).toBeInTheDocument();
  });
});

describe('TransactionsTab — remoção', () => {
  it('should keep the transaction when the confirmation is dismissed', async () => {
    const user = userEvent.setup();
    getTransactions.mockResolvedValue(paged([expense]));

    await renderSettled();

    await user.click(screen.getByRole('button', { name: 'Excluir Aluguel' }));
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Cancelar' }));

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(deleteTransaction).not.toHaveBeenCalled();
  });

  it('should go back one page when the last transaction of the page is removed', async () => {
    const user = userEvent.setup();
    getTransactions.mockResolvedValue(paged([expense], {
      page: 2, totalItems: 11, totalPages: 2, hasPrevious: true,
    }));
    deleteTransaction.mockResolvedValue(undefined);

    await renderSettled();

    await user.click(screen.getByRole('button', { name: 'Excluir Aluguel' }));
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Excluir' }));

    await waitFor(() => {
      expect(getTransactions).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1 }));
    });
  });

  it('should show an error when the deletion fails', async () => {
    const user = userEvent.setup();
    getTransactions.mockResolvedValue(paged([expense]));
    deleteTransaction.mockRejectedValue(new Error('Falha ao excluir.'));

    await renderSettled();

    await user.click(screen.getByRole('button', { name: 'Excluir Aluguel' }));
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Excluir' }));

    expect(await screen.findByText('Falha ao excluir.')).toBeInTheDocument();
  });
});
