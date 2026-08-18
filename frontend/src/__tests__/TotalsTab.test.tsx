/**
 * Testes de componente — TotalsTab isolada.
 *
 * Cobre os estados de saldo negativo, lista vazia e a atualização manual,
 * que App.test.tsx não exercita.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import TotalsTab from '../components/TotalsTab';
import type { TotalsResponse } from '../types';

vi.mock('../api', () => ({ getTotals: vi.fn() }));

import * as api from '../api';

const getTotals = api.getTotals as ReturnType<typeof vi.fn>;

const emptyTotals: TotalsResponse = {
  peopleTotals: [],
  grandTotalIncome: 0,
  grandTotalExpense: 0,
  grandBalance: 0,
};

beforeEach(() => {
  vi.clearAllMocks();
  getTotals.mockResolvedValue(emptyTotals);
});

describe('TotalsTab', () => {
  it('should show a message when there is no person to total', async () => {
    render(<TotalsTab />);

    expect(await screen.findByText('Nenhuma pessoa cadastrada para exibir totais.')).toBeInTheDocument();
    expect(screen.getByText('📊 Total Geral')).toBeInTheDocument();
  });

  it('should mark a negative balance as red for the person and the grand total', async () => {
    getTotals.mockResolvedValue({
      peopleTotals: [
        { personId: 1, personName: 'João', totalIncome: 1000, totalExpense: 2500, balance: -1500 },
      ],
      grandTotalIncome: 1000,
      grandTotalExpense: 2500,
      grandBalance: -1500,
    });

    render(<TotalsTab />);

    const row = (await screen.findByText('João')).closest('tr')!;
    expect(within(row).getByText(/-R\$\s?1\.500,00/).closest('td')).toHaveClass('text-red');
    expect(screen.getByText('Saldo Líquido').closest('.total-card')).toHaveClass('balance-negative');
  });

  it('should mark a positive balance as green', async () => {
    getTotals.mockResolvedValue({
      peopleTotals: [
        { personId: 1, personName: 'João', totalIncome: 5000, totalExpense: 2000, balance: 3000 },
      ],
      grandTotalIncome: 5000,
      grandTotalExpense: 2000,
      grandBalance: 3000,
    });

    render(<TotalsTab />);

    const row = (await screen.findByText('João')).closest('tr')!;
    expect(within(row).getByText(/R\$\s?3\.000,00/).closest('td')).toHaveClass('text-green');
    expect(screen.getByText('Saldo Líquido').closest('.total-card')).toHaveClass('balance-positive');
  });

  it('should refetch the totals when the refresh button is clicked', async () => {
    const user = userEvent.setup();
    render(<TotalsTab />);

    await screen.findByText('📊 Total Geral');
    await user.click(screen.getByText('🔄 Atualizar Totais'));

    await waitFor(() => expect(getTotals).toHaveBeenCalledTimes(2));
  });

  it('should use the generic fallback when the error is not an Error', async () => {
    getTotals.mockRejectedValue({ status: 500 });

    render(<TotalsTab />);

    expect(await screen.findByText('Erro ao consultar totais.')).toBeInTheDocument();
  });
});
