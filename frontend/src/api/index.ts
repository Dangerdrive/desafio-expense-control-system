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
  PagedResult,
} from '../types';

// URL base da API.
// Pode ser sobrescrita pela variável de ambiente VITE_API_URL (ex: em produção).
const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5000/api';

/**
 * Extrai a mensagem de erro do corpo JSON do backend ({ message }).
 * Retorna null quando o corpo não segue esse contrato.
 */
function extractMessage(body: unknown): string | null {
  if (typeof body === 'object' && body !== null && 'message' in body) {
    const { message } = body as { message?: unknown };
    if (typeof message === 'string' && message.trim() !== '') return message;
  }
  return null;
}

/**
 * Helper genérico para requisições HTTP.
 * Lança erro com a mensagem do backend em caso de falha.
 *
 * Toda falha vira um Error com mensagem legível em PT-BR, mas o erro original
 * é preservado em `cause` (e logado) para que a causa raiz — timeout de rede,
 * HTML de proxy no lugar de JSON, etc. — não seja perdida no caminho.
 */
async function request<T>(url: string, options?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE}${url}`, {
      headers: { 'Content-Type': 'application/json' },
      ...options,
    });
  } catch (err) {
    // Falha de rede (backend offline, CORS bloqueado, etc.).
    // O fetch lança um TypeError genérico ("Failed to fetch"); aqui convertemos
    // para uma mensagem amigável em PT-BR em vez de vazar o texto do browser.
    console.error(`Falha de rede em ${options?.method ?? 'GET'} ${url}:`, err);
    throw new Error(
      'Não foi possível conectar ao servidor. Verifique se o backend está em execução.',
      { cause: err },
    );
  }

  if (!response.ok) {
    let body: unknown = null;
    let parseError: unknown = null;
    try {
      body = await response.json();
    } catch (err) {
      // Resposta de erro sem JSON válido (ex: 502 devolvendo HTML).
      parseError = err;
    }

    const message = extractMessage(body);
    if (message === null) {
      console.error(
        `Resposta de erro sem mensagem em ${options?.method ?? 'GET'} ${url} (HTTP ${response.status}):`,
        parseError ?? body,
      );
    }

    throw new Error(message ?? `Erro ${response.status} ao comunicar com o servidor.`, {
      cause: parseError ?? body,
    });
  }

  // 204 No Content (usado no DELETE)
  if (response.status === 204) return undefined as T;

  try {
    return (await response.json()) as T;
  } catch (err) {
    // Corpo de sucesso ilegível: sem este tratamento o SyntaxError cru do
    // JSON.parse ("Unexpected token <") chegaria à UI.
    console.error(`Resposta inválida em ${options?.method ?? 'GET'} ${url}:`, err);
    throw new Error('O servidor devolveu uma resposta inválida.', { cause: err });
  }
}

// ===================== PESSOAS =====================

/** Parâmetros de paginação da listagem de pessoas. */
export interface PersonQuery {
  page?: number;
  pageSize?: number;
}

/**
 * Lista pessoas paginadas.
 * Padrão: página 1, 10 itens por página.
 */
export function getPeople(params?: PersonQuery): Promise<PagedResult<Person>> {
  const query = new URLSearchParams();
  if (params?.page) query.set('page', String(params.page));
  if (params?.pageSize) query.set('pageSize', String(params.pageSize));
  const qs = query.toString();
  return request<PagedResult<Person>>(`/people${qs ? `?${qs}` : ''}`);
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

/** Parâmetros de filtro/ordenação/paginação de transações. */
export interface TransactionQuery {
  from?: string; // data inicial "YYYY-MM-DD" (inclusiva)
  to?: string;   // data final "YYYY-MM-DD" (inclusiva)
  sort?: 'date_asc' | 'date_desc'; // ordenação por data (padrão: date_desc)
  page?: number;
  pageSize?: number;
}

/**
 * Lista transações paginadas, opcionalmente filtradas por período e ordenadas por data.
 * Padrão: página 1, 10 itens por página, ordenação por data decrescente.
 */
export function getTransactions(params?: TransactionQuery): Promise<PagedResult<Transaction>> {
  const query = new URLSearchParams();
  if (params?.from) query.set('from', params.from);
  if (params?.to) query.set('to', params.to);
  if (params?.sort) query.set('sort', params.sort);
  if (params?.page) query.set('page', String(params.page));
  if (params?.pageSize) query.set('pageSize', String(params.pageSize));
  const qs = query.toString();
  return request<PagedResult<Transaction>>(`/transactions${qs ? `?${qs}` : ''}`);
}

/** Cria uma nova transação. Aplica regra: <18 anos só despesa. */
export function createTransaction(dto: CreateTransactionDto): Promise<Transaction> {
  return request<Transaction>('/transactions', {
    method: 'POST',
    body: JSON.stringify(dto),
  });
}

/** Atualiza uma transação existente pelo ID. Aplica as mesmas regras do create. */
export function updateTransaction(id: number, dto: CreateTransactionDto): Promise<Transaction> {
  return request<Transaction>(`/transactions/${id}`, {
    method: 'PUT',
    body: JSON.stringify(dto),
  });
}

/** Remove uma transação pelo ID. */
export function deleteTransaction(id: number): Promise<void> {
  return request<void>(`/transactions/${id}`, { method: 'DELETE' });
}

// ===================== TOTAIS =====================

/** Consulta os totais (receitas, despesas, saldo) por pessoa e geral. */
export function getTotals(): Promise<TotalsResponse> {
  return request<TotalsResponse>('/totals');
}
