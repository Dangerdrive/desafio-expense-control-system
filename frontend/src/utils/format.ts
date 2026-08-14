/**
 * Utilitários de formatação compartilhados entre os componentes.
 */

/** Formata um número como moeda brasileira (BRL). */
export function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

/** Converte "YYYY-MM-DD" (ISO) para "DD/MM/YYYY" (exibição pt-BR). */
export function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-');
  if (!year || !month || !day) return isoDate;
  return `${day}/${month}/${year}`;
}

/**
 * Máscara para campo de valor monetário.
 * Aceita apenas dígitos e UM separador decimal (`.` ou `,`), com no máximo
 * 2 casas decimais. Retorna a string "limpa" para armazenar no state.
 */
export function maskAmountInput(raw: string): string {
  // Remove qualquer caractere que não seja dígito ou separador
  let cleaned = raw.replace(/[^\d.,]/g, '');
  // Garante apenas um separador decimal (primeiro de `.` ou `,` vira o separador)
  const firstSeparator = cleaned.search(/[.,]/);
  if (firstSeparator !== -1) {
    const before = cleaned.slice(0, firstSeparator).replace(/[.,]/g, '');
    const after = cleaned.slice(firstSeparator + 1).replace(/[.,]/g, '').slice(0, 2);
    cleaned = `${before}.${after}`;
  }
  return cleaned;
}

/**
 * Converte a string do campo de valor para number com segurança.
 * Aceita "." ou "," como separador decimal. Retorna null se inválido.
 *
 * Evita o bug clássico do parseFloat("12,34") === 12 (parada na vírgula),
 * que silenciosamente descartava os centavos em teclados pt-BR.
 */
export function parseAmountInput(value: string): number | null {
  const normalized = value.replace(',', '.').trim();
  if (normalized === '' || !/^\d+(\.\d{1,2})?$/.test(normalized)) return null;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
}
