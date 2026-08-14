/**
 * Utilitários de formatação compartilhados entre os componentes.
 */

/** Formata um número como moeda brasileira (BRL). */
export function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}
