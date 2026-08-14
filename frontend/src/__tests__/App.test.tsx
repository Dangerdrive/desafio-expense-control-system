/**
 * Testes de componente — App principal e abas.
 *
 * Renderiza o App completo e verifica que as abas renderizam
 * corretamente com dados mockados.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from '../App';

// Mock do módulo api para evitar chamadas HTTP reais
vi.mock('../api', () => ({
  getPeople: vi.fn(),
  createPerson: vi.fn(),
  deletePerson: vi.fn(),
  getTransactions: vi.fn(),
  createTransaction: vi.fn(),
  getTotals: vi.fn(),
}));

import * as api from '../api';

beforeEach(() => {
  vi.clearAllMocks();
  // Valores padrão dos mocks
  (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue([]);
  (api.getTransactions as ReturnType<typeof vi.fn>).mockResolvedValue([]);
  (api.getTotals as ReturnType<typeof vi.fn>).mockResolvedValue({
    peopleTotals: [],
    grandTotalIncome: 0,
    grandTotalExpense: 0,
    grandBalance: 0,
  });
});

// ============================================================
// APP — Navegação entre abas
// ============================================================

describe('App', () => {
  it('should render the header', () => {
    render(<App />);
    expect(screen.getByText('💰 Controle de Gastos Residenciais')).toBeInTheDocument();
  });

  it('should render all three tab buttons', () => {
    render(<App />);
    expect(screen.getByText('👥 Pessoas')).toBeInTheDocument();
    expect(screen.getByText('💳 Transações')).toBeInTheDocument();
    expect(screen.getByText('📊 Totais')).toBeInTheDocument();
  });

  it('should show People tab by default', () => {
    render(<App />);
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
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue([
      { id: 1, name: 'João', age: 30 },
      { id: 2, name: 'Maria', age: 25 },
    ]);

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText('João')).toBeInTheDocument();
      expect(screen.getByText('Maria')).toBeInTheDocument();
    });
  });

  it('should show the create form', () => {
    render(<App />);
    expect(screen.getByPlaceholderText('Nome')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Idade')).toBeInTheDocument();
    expect(screen.getByText('➕ Adicionar')).toBeInTheDocument();
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
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue([
      { id: 1, name: 'João', age: 30 },
    ]);

    render(<App />);
    await user.click(screen.getByText('💳 Transações'));

    await waitFor(() => {
      expect(screen.getByPlaceholderText('Descrição')).toBeInTheDocument();
      expect(screen.getByLabelText('Valor')).toBeInTheDocument();
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
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue([
      { id: 1, name: 'Novo Usuário', age: 25 },
    ]);

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
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue([
      { id: 1, name: 'João', age: 30 },
    ]);
    (api.getTransactions as ReturnType<typeof vi.fn>).mockResolvedValue([]);
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
    (api.getPeople as ReturnType<typeof vi.fn>).mockResolvedValue([
      { id: 1, name: 'João', age: 30 },
    ]);
    (api.getTransactions as ReturnType<typeof vi.fn>).mockResolvedValue([]);
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
});
