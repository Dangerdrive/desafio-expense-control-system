/**
 * Serviço de comunicação com a API backend.
 * Centraliza todas as chamadas HTTP para manter o código organizado.
 */
import type {
  Person,
  CreatePersonDto,
  Transaction,
  CreateTransactionDto,
  TotalsResponse,
} from '../types';

// URL base da API.
// Pode ser sobrescrita pela variável de ambiente VITE_API_URL (ex: em produção).
const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5000/api';

/**
 * Helper genérico para requisições HTTP.
 * Lança erro com a mensagem do backend em caso de falha.
 */
async function request<T>(url: string, options?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE}${url}`, {
      headers: { 'Content-Type': 'application/json' },
      ...options,
    });
  } catch {
    // Falha de rede (backend offline, CORS bloqueado, etc.).
    // O fetch lança um TypeError genérico ("Failed to fetch"); aqui convertemos
    // para uma mensagem amigável em PT-BR em vez de vazar o texto do browser.
    throw new Error('Não foi possível conectar ao servidor. Verifique se o backend está em execução.');
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Erro desconhecido' }));
    throw new Error(error.message || `Erro ${response.status}`);
  }

  // 204 No Content (usado no DELETE)
  if (response.status === 204) return undefined as T;

  return response.json();
}

// ===================== PESSOAS =====================

/** Lista todas as pessoas. */
export function getPeople(): Promise<Person[]> {
  return request<Person[]>('/people');
}

/** Cria uma nova pessoa. */
export function createPerson(dto: CreatePersonDto): Promise<Person> {
  return request<Person>('/people', {
    method: 'POST',
    body: JSON.stringify(dto),
  });
}

/** Remove uma pessoa pelo ID. (Transações são removidas em cascata) */
export function deletePerson(id: number): Promise<void> {
  return request<void>(`/people/${id}`, { method: 'DELETE' });
}

// ===================== TRANSAÇÕES =====================

/** Lista todas as transações. */
export function getTransactions(): Promise<Transaction[]> {
  return request<Transaction[]>('/transactions');
}

/** Cria uma nova transação. Aplica regra: <18 anos só despesa. */
export function createTransaction(dto: CreateTransactionDto): Promise<Transaction> {
  return request<Transaction>('/transactions', {
    method: 'POST',
    body: JSON.stringify(dto),
  });
}

// ===================== TOTAIS =====================

/** Consulta os totais (receitas, despesas, saldo) por pessoa e geral. */
export function getTotals(): Promise<TotalsResponse> {
  return request<TotalsResponse>('/totals');
}
