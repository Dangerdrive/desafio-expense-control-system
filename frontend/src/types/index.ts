/**
 * Tipos compartilhados do sistema de controle de gastos.
 * Espelham os DTOs do backend.
 */

/** Dados de uma pessoa cadastrada. */
export interface Person {
  id: number;
  name: string;
  age: number;
}

/** Dados para criação de uma pessoa. */
export interface CreatePersonDto {
  name: string;
  age: number;
}

/** Dados de uma transação. */
export interface Transaction {
  id: number;
  description: string;
  amount: number;
  date: string; // formato ISO "YYYY-MM-DD"
  type: 'receita' | 'despesa';
  personId: number;
  personName: string;
}

/** Dados para criação de uma transação. */
export interface CreateTransactionDto {
  description: string;
  amount: number;
  date: string; // formato ISO "YYYY-MM-DD"
  type: 'receita' | 'despesa';
  personId: number;
}

/** Totais financeiros de uma pessoa. */
export interface PersonTotals {
  personId: number;
  personName: string;
  totalIncome: number;
  totalExpense: number;
  balance: number;
}

/** Resposta da consulta de totais. */
export interface TotalsResponse {
  peopleTotals: PersonTotals[];
  grandTotalIncome: number;
  grandTotalExpense: number;
  grandBalance: number;
}

/** Erro retornado pela API. */
export interface ApiError {
  message: string;
}
