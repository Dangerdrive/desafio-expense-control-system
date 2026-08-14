/**
 * Testes unitários da camada de API (fetch wrapper).
 *
 * Mockamos o fetch global para simular respostas do backend
 * sem depender de um servidor rodando.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';

// Mock global do fetch
const mockFetch = vi.fn();
vi.stubGlobal('fetch', mockFetch);

// Importações após o mock (para que usem o fetch mockado)
import { getPeople, createPerson, deletePerson, getTransactions, createTransaction, getTotals } from '../api';

function mockResponse(status: number, data: unknown) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(data),
  });
}

beforeEach(() => {
  mockFetch.mockReset();
});

// ============================================================
// GET /api/people
// ============================================================

describe('getPeople', () => {
  it('should return a list of people on success', async () => {
    const mockData = [
      { id: 1, name: 'João', age: 30 },
      { id: 2, name: 'Maria', age: 25 },
    ];
    mockFetch.mockResolvedValueOnce(mockResponse(200, mockData));

    const result = await getPeople();

    expect(result).toEqual(mockData);
    expect(mockFetch).toHaveBeenCalledWith(
      'http://localhost:5000/api/people',
      expect.objectContaining({ headers: { 'Content-Type': 'application/json' } }),
    );
  });

  it('should throw on server error', async () => {
    mockFetch.mockResolvedValueOnce(mockResponse(500, { message: 'Erro interno' }));

    await expect(getPeople()).rejects.toThrow('Erro interno');
  });
});

// ============================================================
// POST /api/people
// ============================================================

describe('createPerson', () => {
  it('should create a person and return it', async () => {
    const dto = { name: 'Novo', age: 30 };
    const mockResponseData = { id: 3, name: 'Novo', age: 30 };
    mockFetch.mockResolvedValueOnce(mockResponse(201, mockResponseData));

    const result = await createPerson(dto);

    expect(result).toEqual(mockResponseData);
    expect(mockFetch).toHaveBeenCalledWith(
      'http://localhost:5000/api/people',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify(dto),
      }),
    );
  });
});

// ============================================================
// DELETE /api/people/:id
// ============================================================

describe('deletePerson', () => {
  it('should delete a person and return void', async () => {
    mockFetch.mockResolvedValueOnce(mockResponse(204, null));

    const result = await deletePerson(1);

    expect(result).toBeUndefined();
    expect(mockFetch).toHaveBeenCalledWith(
      'http://localhost:5000/api/people/1',
      expect.objectContaining({ method: 'DELETE' }),
    );
  });

  it('should throw if person not found', async () => {
    mockFetch.mockResolvedValueOnce(mockResponse(404, { message: 'Pessoa não encontrada.' }));

    await expect(deletePerson(999)).rejects.toThrow('Pessoa não encontrada.');
  });
});

// ============================================================
// GET /api/transactions
// ============================================================

describe('getTransactions', () => {
  it('should return transactions with person names', async () => {
    const mockData = [
      { id: 1, description: 'Salário', amount: 5000, date: '2026-01-15', type: 'receita', personId: 1, personName: 'João' },
    ];
    mockFetch.mockResolvedValueOnce(mockResponse(200, mockData));

    const result = await getTransactions();

    expect(result).toEqual(mockData);
  });

  it('should send from/to/sort query params when provided', async () => {
    mockFetch.mockResolvedValueOnce(mockResponse(200, []));

    await getTransactions({ from: '2026-01-01', to: '2026-12-31', sort: 'date_asc' });

    const url = mockFetch.mock.calls[0][0] as string;
    expect(url).toContain('/api/transactions?');
    expect(url).toContain('from=2026-01-01');
    expect(url).toContain('to=2026-12-31');
    expect(url).toContain('sort=date_asc');
  });

  it('should not append query string when no params', async () => {
    mockFetch.mockResolvedValueOnce(mockResponse(200, []));

    await getTransactions();

    const url = mockFetch.mock.calls[0][0] as string;
    expect(url).toBe('http://localhost:5000/api/transactions');
  });
});

// ============================================================
// POST /api/transactions
// ============================================================

describe('createTransaction', () => {
  it('should create transaction for adult', async () => {
    const dto = { description: 'Salário', amount: 5000, date: '2026-01-15', type: 'receita' as const, personId: 1 };
    const mockResponseData = { id: 1, ...dto, personName: 'João' };
    mockFetch.mockResolvedValueOnce(mockResponse(201, mockResponseData));

    const result = await createTransaction(dto);

    expect(result.type).toBe('receita');
  });

  it('should throw when business rule is violated (minor + income)', async () => {
    const dto = { description: 'Mesada', amount: 100, date: '2026-01-15', type: 'receita' as const, personId: 2 };
    mockFetch.mockResolvedValueOnce(
      mockResponse(400, { message: 'Menores de 18 anos não podem cadastrar receitas, apenas despesas.' }),
    );

    await expect(createTransaction(dto)).rejects.toThrow('Menores de 18 anos');
  });
});

// ============================================================
// GET /api/totals
// ============================================================

describe('getTotals', () => {
  it('should return totals structure', async () => {
    const mockData = {
      peopleTotals: [{ personId: 1, personName: 'João', totalIncome: 5000, totalExpense: 2000, balance: 3000 }],
      grandTotalIncome: 5000,
      grandTotalExpense: 2000,
      grandBalance: 3000,
    };
    mockFetch.mockResolvedValueOnce(mockResponse(200, mockData));

    const result = await getTotals();

    expect(result.grandBalance).toBe(3000);
    expect(result.peopleTotals).toHaveLength(1);
  });

  it('should return empty totals when no data', async () => {
    const mockData = {
      peopleTotals: [],
      grandTotalIncome: 0,
      grandTotalExpense: 0,
      grandBalance: 0,
    };
    mockFetch.mockResolvedValueOnce(mockResponse(200, mockData));

    const result = await getTotals();

    expect(result.peopleTotals).toHaveLength(0);
    expect(result.grandBalance).toBe(0);
  });
});

// ============================================================
// NETWORK ERRORS (fetch rejection, não resposta HTTP)
// ============================================================

describe('network error handling', () => {
  it('getPeople should throw friendly message on network failure', async () => {
    mockFetch.mockRejectedValueOnce(new Error('Failed to fetch'));

    await expect(getPeople()).rejects.toThrow('Não foi possível conectar ao servidor');
  });

  it('createTransaction should throw friendly message on network failure', async () => {
    mockFetch.mockRejectedValueOnce(new Error('Failed to fetch'));

    await expect(
      createTransaction({ description: 'A', amount: 10, date: '2026-01-15', type: 'despesa', personId: 1 })
    ).rejects.toThrow('Não foi possível conectar ao servidor');
  });

  it('getTotals should throw friendly message on network failure', async () => {
    mockFetch.mockRejectedValueOnce(new Error('Network error'));

    await expect(getTotals()).rejects.toThrow('Não foi possível conectar ao servidor');
  });
});

// ============================================================
// EDGE CASE: 204 No Content (DELETE)
// ============================================================

describe('204 No Content handling', () => {
  it('deletePerson should not try to parse JSON on 204', async () => {
    mockFetch.mockResolvedValueOnce(mockResponse(204, null));

    // Não deve lançar erro tentando fazer .json() em corpo vazio
    const result = await deletePerson(1);
    expect(result).toBeUndefined();
  });
});
