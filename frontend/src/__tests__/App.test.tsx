/**
 * Testes de componente — App principal e abas.
 *
 * Renderiza o App completo e verifica que as abas renderizam
 * corretamente com dados mockados.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from '../App';
import type { PagedResult } from '../types';

/** Envolve uma lista de itens no envelope paginado que a API devolve hoje. */
function paged<T>(items: T[]): PagedResult<T> {
  return {
    items,
    page: 1,
    pageSize: 10,
    totalItems: items.length,
    totalPages: items.length === 0 ? 0 : 1,
    hasNext: false,
    hasPrevious: false,
  };
}

// Mock do módulo api para evitar chamadas HTTP reais
vi.mock('../api', () => ({
  getPeople: vi.fn(),
  createPerson: vi.fn(),
  deletePerson: vi.fn(),
  getTransactions: vi.fn(),
  createTransaction: vi.fn(),
  updateTransaction: vi.fn(),
  deleteTransaction: vi.fn(),
  getTotals: vi.fn(),
}));

import * as api from '../api';

beforeEach(() => {
  vi.clearAllMocks();
  // Valores padrão dos mocks (envelope paginado)
  (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue(paged([]));
  (api.getTransactions as ReturnType<typeof vi.fn>).mockResolvedValue(paged([]));
  (api.getTotals as ReturnType<typeof vi.fn>).mockResolvedValue({
    peopleTotals: [],
    grandTotalIncome: 0,
    grandTotalExpense: 0,
    grandBalance: 0,
  });
});

/**
 * Renderiza o App e aguarda o carregamento inicial da aba Pessoas se estabilizar.
 * Sem isto, a Promise assíncrona do loadPeople resolve DEPOIS do fim do teste
 * síncrono, gerando o warning "An update to X was not wrapped in act(...)".
 */
async function renderAppSettled() {
  render(<App />);
  await waitFor(() => {
    expect(screen.queryByText('Carregando pessoas...')).not.toBeInTheDocument();
  });
}

// ============================================================
// APP — Navegação entre abas
// ============================================================

describe('App', () => {
  it('should render the header', async () => {
    await renderAppSettled();
    expect(screen.getByText('💰 Controle de Gastos Residenciais')).toBeInTheDocument();
  });

  it('should render all three tab buttons', async () => {
    await renderAppSettled();
    expect(screen.getByText('👥 Pessoas')).toBeInTheDocument();
    expect(screen.getByText('💳 Transações')).toBeInTheDocument();
    expect(screen.getByText('📊 Totais')).toBeInTheDocument();
  });

  it('should show People tab by default', async () => {
    await renderAppSettled();
    expect(screen.getByText('Cadastro de Pessoas')).toBeInTheDocument();
  });

  it('should switch to Transactions tab on click', async () => {
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByText('💳 Transações'));

    await waitFor(() => {
      expect(screen.getByText('Cadastro de Transações')).toBeInTheDocument();
    });
  });

  it('should switch to Totals tab on click', async () => {
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByText('📊 Totais'));

    await waitFor(() => {
      expect(screen.getByText('Consulta de Totais')).toBeInTheDocument();
    });
  });
});

// ============================================================
// PEOPLE TAB
// ============================================================

describe('PeopleTab', () => {
  it('should show empty message when no people', async () => {
    render(<App />);
    await waitFor(() => {
      expect(screen.getByText('Nenhuma pessoa cadastrada.')).toBeInTheDocument();
    });
  });

  it('should render people list when data exists', async () => {
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue(paged([
      { id: 1, name: 'João', age: 30 },
      { id: 2, name: 'Maria', age: 25 },
    ]));

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText('João')).toBeInTheDocument();
      expect(screen.getByText('Maria')).toBeInTheDocument();
    });
  });

  it('should show the create form', async () => {
    await renderAppSettled();
    expect(screen.getByPlaceholderText('Nome')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Idade')).toBeInTheDocument();
    expect(screen.getByText('➕ Adicionar')).toBeInTheDocument();
  });

  it('should paginate the people list', async () => {
    const user = userEvent.setup();
    const mock = api.getPeople as ReturnType<typeof vi.fn>;
    mock
      .mockResolvedValueOnce({
        items: [{ id: 1, name: 'Ana', age: 30 }],
        page: 1, pageSize: 10, totalItems: 11, totalPages: 2, hasNext: true, hasPrevious: false,
      })
      .mockResolvedValueOnce({
        items: [{ id: 11, name: 'Bruno', age: 40 }],
        page: 2, pageSize: 10, totalItems: 11, totalPages: 2, hasNext: false, hasPrevious: true,
      });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText('Ana')).toBeInTheDocument();
      expect(screen.getByText(/Página 1 de 2/)).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: 'Próxima página' }));

    await waitFor(() => {
      expect(mock).toHaveBeenLastCalledWith({ page: 2, pageSize: 10 });
      expect(screen.getByText('Bruno')).toBeInTheDocument();
      expect(screen.getByText(/Página 2 de 2/)).toBeInTheDocument();
    });
  });
});

// ============================================================
// TRANSACTIONS TAB
// ============================================================

describe('TransactionsTab', () => {
  it('should show warning when no people exist', async () => {
    const user = userEvent.setup();
    render(<App />);
    await user.click(screen.getByText('💳 Transações'));

    await waitFor(() => {
      expect(screen.getByText(/Cadastre uma pessoa antes/)).toBeInTheDocument();
    });
  });

  it('should show the business rule info', async () => {
    const user = userEvent.setup();
    render(<App />);
    await user.click(screen.getByText('💳 Transações'));

    await waitFor(() => {
      expect(screen.getByText(/Menores de 18 anos/)).toBeInTheDocument();
    });
  });

  it('should show transaction form fields', async () => {
    const user = userEvent.setup();
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue(paged([
      { id: 1, name: 'João', age: 30 },
    ]));

    render(<App />);
    await user.click(screen.getByText('💳 Transações'));

    await waitFor(() => {
      expect(screen.getByPlaceholderText('Descrição')).toBeInTheDocument();
      expect(screen.getByLabelText('Valor')).toBeInTheDocument();
    });
  });

  it('should show pagination controls for the transaction list', async () => {
    const user = userEvent.setup();
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue(paged([
      { id: 1, name: 'João', age: 30 },
    ]));
    (api.getTransactions as ReturnType<typeof vi.fn>).mockResolvedValue({
      items: [{ id: 1, description: 'Salário', amount: 5000, date: '2026-01-15', type: 'receita', personId: 1, personName: 'João' }],
      page: 1, pageSize: 10, totalItems: 11, totalPages: 2, hasNext: true, hasPrevious: false,
    });

    render(<App />);
    await user.click(screen.getByText('💳 Transações'));

    await waitFor(() => {
      expect(screen.getByText('Salário')).toBeInTheDocument();
      expect(screen.getByText(/Página 1 de 2/)).toBeInTheDocument();
    });
  });
});

// ============================================================
// TOTALS TAB
// ============================================================

describe('TotalsTab', () => {
  it('should show refresh button', async () => {
    const user = userEvent.setup();
    render(<App />);
    await user.click(screen.getByText('📊 Totais'));

    await waitFor(() => {
      expect(screen.getByText('🔄 Atualizar Totais')).toBeInTheDocument();
    });
  });

  it('should render totals when data exists', async () => {
    const user = userEvent.setup();
    (api.getTotals as ReturnType<typeof vi.fn>).mockResolvedValue({
      peopleTotals: [
        { personId: 1, personName: 'João', totalIncome: 5000, totalExpense: 2000, balance: 3000 },
      ],
      grandTotalIncome: 5000,
      grandTotalExpense: 2000,
      grandBalance: 3000,
    });

    render(<App />);
    await user.click(screen.getByText('📊 Totais'));

    await waitFor(() => {
      expect(screen.getByText('João')).toBeInTheDocument();
      expect(screen.getByText('📊 Total Geral')).toBeInTheDocument();
      expect(screen.getByText('Saldo Líquido')).toBeInTheDocument();
    });
  });

  it('should show loading state while fetching totals', async () => {
    const user = userEvent.setup();
    (api.getTotals as ReturnType<typeof vi.fn>).mockImplementation(
      () => new Promise(() => { /* nunca resolve */ })
    );

    render(<App />);
    await user.click(screen.getByText('📊 Totais'));

    await waitFor(() => {
      expect(screen.getByText('Carregando totais...')).toBeInTheDocument();
    });
  });

  it('should show error message when totals fetch fails', async () => {
    const user = userEvent.setup();
    (api.getTotals as ReturnType<typeof vi.fn>).mockRejectedValue(new Error('Falha na rede'));

    render(<App />);
    await user.click(screen.getByText('📊 Totais'));

    await waitFor(() => {
      expect(screen.getByText('Falha na rede')).toBeInTheDocument();
    });
  });
});

// ============================================================
// FORM SUBMISSION — People Tab
// ============================================================

describe('PeopleTab — form submission', () => {
  it('should show validation error when submitting empty form', async () => {
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByText('➕ Adicionar'));

    await waitFor(() => {
      expect(screen.getByText('Nome é obrigatório.')).toBeInTheDocument();
    });
  });

  it('should create a person successfully', async () => {
    const user = userEvent.setup();
    (api.createPerson as ReturnType<typeof vi.fn>).mockResolvedValue({
      id: 1, name: 'Novo Usuário', age: 25,
    });
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue(paged([
      { id: 1, name: 'Novo Usuário', age: 25 },
    ]));

    render(<App />);

    await user.type(screen.getByPlaceholderText('Nome'), 'Novo Usuário');
    await user.type(screen.getByPlaceholderText('Idade'), '25');
    await user.click(screen.getByText('➕ Adicionar'));

    await waitFor(() => {
      expect(screen.getByText('Pessoa cadastrada com sucesso!')).toBeInTheDocument();
    });
  });

  it('should show error when createPerson API fails', async () => {
    const user = userEvent.setup();
    (api.createPerson as ReturnType<typeof vi.fn>).mockRejectedValue(
      new Error('Erro do servidor')
    );

    render(<App />);

    await user.type(screen.getByPlaceholderText('Nome'), 'Teste');
    await user.type(screen.getByPlaceholderText('Idade'), '30');
    await user.click(screen.getByText('➕ Adicionar'));

    await waitFor(() => {
      expect(screen.getByText('Erro do servidor')).toBeInTheDocument();
    });
  });
});

// ============================================================
// FORM SUBMISSION — Transactions Tab
// ============================================================

describe('TransactionsTab — form submission', () => {
  it('should create a transaction successfully', async () => {
    const user = userEvent.setup();
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue(paged([
      { id: 1, name: 'João', age: 30 },
    ]));
    (api.getTransactions as ReturnType<typeof vi.fn>).mockResolvedValue(paged([]));
    (api.createTransaction as ReturnType<typeof vi.fn>).mockResolvedValue({
      id: 1, description: 'Conta de Luz', amount: 200, type: 'despesa', personId: 1, personName: 'João',
    });

    render(<App />);
    await user.click(screen.getByText('💳 Transações'));

    // Preenche formulário
    await waitFor(() => {
      expect(screen.getByPlaceholderText('Descrição')).toBeInTheDocument();
    });

    await user.type(screen.getByPlaceholderText('Descrição'), 'Conta de Luz');
    await user.type(screen.getByLabelText('Valor'), '200');
    await user.selectOptions(screen.getByLabelText('Pessoa'), '1');
    await user.click(screen.getByText('➕ Registrar'));

    await waitFor(() => {
      expect(screen.getByText('Transação registrada com sucesso!')).toBeInTheDocument();
    });
  });

  it('should show business rule error for minor + income', async () => {
    const user = userEvent.setup();
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue(paged([
      { id: 1, name: 'João', age: 30 },
    ]));
    (api.getTransactions as ReturnType<typeof vi.fn>).mockResolvedValue(paged([]));
    (api.createTransaction as ReturnType<typeof vi.fn>).mockRejectedValue(
      new Error('Menores de 18 anos não podem cadastrar receitas, apenas despesas.')
    );

    render(<App />);
    await user.click(screen.getByText('💳 Transações'));

    await waitFor(() => {
      expect(screen.getByPlaceholderText('Descrição')).toBeInTheDocument();
    });

    await user.type(screen.getByPlaceholderText('Descrição'), 'Mesada');
    await user.type(screen.getByLabelText('Valor'), '100');
    await user.selectOptions(screen.getByLabelText('Tipo'), 'receita');
    await user.selectOptions(screen.getByLabelText('Pessoa'), '1');
    await user.click(screen.getByText('➕ Registrar'));

    await waitFor(() => {
      expect(screen.getByText('Menores de 18 anos não podem cadastrar receitas, apenas despesas.')).toBeInTheDocument();
    });
  });

  it('should edit a transaction and call updateTransaction', async () => {
    const user = userEvent.setup();
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue(paged([
      { id: 1, name: 'João', age: 30 },
    ]));
    (api.getTransactions as ReturnType<typeof vi.fn>).mockResolvedValue(paged([
      { id: 7, description: 'Conta de Luz', amount: 200, date: '2026-01-15', type: 'despesa', personId: 1, personName: 'João' },
    ]));
    (api.updateTransaction as ReturnType<typeof vi.fn>).mockResolvedValue({});

    render(<App />);
    await user.click(screen.getByText('💳 Transações'));

    // Clica em Editar na linha
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Editar Conta de Luz/ })).toBeInTheDocument();
    });
    await user.click(screen.getByRole('button', { name: /Editar Conta de Luz/ }));

    // Formulário pré-preenchido e botão vira "Salvar"
    expect(screen.getByPlaceholderText('Descrição')).toHaveValue('Conta de Luz');
    expect(screen.getByLabelText('Valor')).toHaveValue('200');
    await user.clear(screen.getByPlaceholderText('Descrição'));
    await user.type(screen.getByPlaceholderText('Descrição'), 'Conta de Água');
    await user.click(screen.getByText('💾 Salvar'));

    await waitFor(() => {
      expect(api.updateTransaction as ReturnType<typeof vi.fn>).toHaveBeenCalledWith(7, expect.objectContaining({ description: 'Conta de Água' }));
      expect(screen.getByText('Transação atualizada com sucesso!')).toBeInTheDocument();
    });
  });

  it('should delete a transaction after confirming in the modal', async () => {
    const user = userEvent.setup();
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue(paged([
      { id: 1, name: 'João', age: 30 },
    ]));
    (api.getTransactions as ReturnType<typeof vi.fn>).mockResolvedValue(paged([
      { id: 9, description: 'Aluguel', amount: 1500, date: '2026-01-15', type: 'despesa', personId: 1, personName: 'João' },
    ]));
    (api.deleteTransaction as ReturnType<typeof vi.fn>).mockResolvedValue(undefined);

    render(<App />);
    await user.click(screen.getByText('💳 Transações'));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Excluir Aluguel/ })).toBeInTheDocument();
    });
    await user.click(screen.getByRole('button', { name: /Excluir Aluguel/ }));

    const dialog = screen.getByRole('dialog');
    expect(dialog).toBeInTheDocument();
    await user.click(within(dialog).getByRole('button', { name: /^Excluir$/ }));

    await waitFor(() => {
      expect(api.deleteTransaction as ReturnType<typeof vi.fn>).toHaveBeenCalledWith(9);
      expect(screen.getByText('Transação "Aluguel" removida.')).toBeInTheDocument();
    });
  });
});
