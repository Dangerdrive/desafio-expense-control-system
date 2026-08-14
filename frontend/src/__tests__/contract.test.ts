/**
 * Teste de CONTRATO frontend ↔ backend.
 *
 * Usa contracts/api-contract.json (fonte única de verdade) — o MESMO arquivo
 * que o ContractTests do backend valida contra as respostas reais da API.
 *
 * 1. Tempo de compilação: as atribuições tipadas abaixo falham no `tsc -b`
 *    se o contrato divergir dos tipos TS (Person, Transaction, TotalsResponse).
 * 2. Tempo de execução: valida os campos/tipos esperados e que a camada api
 *    consegue consumir exatamente o que o backend devolve.
 */
import { describe, it, expect, vi } from 'vitest';
import type { Person, Transaction, TotalsResponse, PagedResult } from '../types';
import contract from '../../../contracts/api-contract.json';

import { getPeople, getTransactions, getTotals } from '../api';

function mockResponse(status: number, data: unknown) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(data),
  });
}

describe('Contrato da API (frontend ↔ backend)', () => {
  // ===== Verificação de TIPO (tempo de compilação) =====
  // Se um campo do contrato for renomeado/removido, estas atribuições deixam
  // de compilar (o build `tsc -b` falha). O resolveJsonModule amplia literais
  // para `string`, então o campo `type` (união 'receita'|'despesa') é
  // convertido explicitamente — o valor em si é validado em runtime abaixo.
  const personFixture: Person = contract.person;
  const totalsFixture: TotalsResponse = contract.totals;
  const transactionFixture: Transaction = {
    ...contract.transaction,
    type: contract.transaction.type as Transaction['type'],
  };
  const personPageFixture: PagedResult<Person> = contract.personPage;
  const transactionPageFixture: PagedResult<Transaction> = {
    ...contract.transactionPage,
    items: contract.transactionPage.items.map(item => ({
      ...item,
      type: item.type as Transaction['type'],
    })),
  };

  it('person: os campos do contrato batem com os tipos TS', () => {
    expect(Object.keys(personFixture).sort()).toEqual(['age', 'id', 'name']);
    expect(typeof personFixture.id).toBe('number');
    expect(typeof personFixture.name).toBe('string');
    expect(typeof personFixture.age).toBe('number');
  });

  it('transaction: os campos do contrato batem com os tipos TS', () => {
    expect(Object.keys(transactionFixture).sort()).toEqual([
      'amount',
      'date',
      'description',
      'id',
      'personId',
      'personName',
      'type',
    ]);
    expect(typeof transactionFixture.amount).toBe('number');
    expect(typeof transactionFixture.date).toBe('string');
    expect(transactionFixture.date).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(typeof transactionFixture.type).toBe('string');
    expect(transactionFixture.type).toMatch(/^(receita|despesa)$/);
    expect(typeof transactionFixture.personName).toBe('string');
  });

  it('totals: os campos do contrato batem com os tipos TS', () => {
    expect(Object.keys(totalsFixture).sort()).toEqual([
      'grandBalance',
      'grandTotalExpense',
      'grandTotalIncome',
      'peopleTotals',
    ]);
    expect(typeof totalsFixture.grandBalance).toBe('number');
    expect(Array.isArray(totalsFixture.peopleTotals)).toBe(true);

    const item = totalsFixture.peopleTotals[0];
    expect(Object.keys(item).sort()).toEqual([
      'balance',
      'personId',
      'personName',
      'totalExpense',
      'totalIncome',
    ]);
  });

  it('personPage: os campos do envelope paginado batem com o tipo PagedResult', () => {
    expect(Object.keys(personPageFixture).sort()).toEqual([
      'hasNext',
      'hasPrevious',
      'items',
      'page',
      'pageSize',
      'totalItems',
      'totalPages',
    ]);
    expect(Array.isArray(personPageFixture.items)).toBe(true);
    expect(typeof personPageFixture.page).toBe('number');
    expect(typeof personPageFixture.pageSize).toBe('number');
    expect(typeof personPageFixture.totalItems).toBe('number');
    expect(typeof personPageFixture.totalPages).toBe('number');
    expect(typeof personPageFixture.hasNext).toBe('boolean');
    expect(typeof personPageFixture.hasPrevious).toBe('boolean');
  });

  it('transactionPage: os itens do envelope batem com o tipo Transaction', () => {
    const item = transactionPageFixture.items[0];
    expect(Object.keys(item).sort()).toEqual([
      'amount',
      'date',
      'description',
      'id',
      'personId',
      'personName',
      'type',
    ]);
    expect(item.type).toMatch(/^(receita|despesa)$/);
  });

  it('a camada api consegue consumir exatamente o contrato do backend', async () => {
    // Simula o backend devolvendo exatamente o contrato compartilhado
    const mockFetch = vi
      .fn()
      .mockResolvedValueOnce(mockResponse(200, contract.personPage))
      .mockResolvedValueOnce(mockResponse(200, contract.transactionPage))
      .mockResolvedValueOnce(mockResponse(200, contract.totals));
    vi.stubGlobal('fetch', mockFetch);

    const people = await getPeople();
    expect(people).toEqual(contract.personPage);
    expect(people.items).toEqual([contract.person]);

    const transactions = await getTransactions();
    expect(transactions).toEqual(contract.transactionPage);
    expect(transactions.items).toEqual([contract.transaction]);

    const totals = await getTotals();
    expect(totals).toEqual(contract.totals);
  });
});
