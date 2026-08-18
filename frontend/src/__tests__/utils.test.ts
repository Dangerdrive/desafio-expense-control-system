/**
 * Testes unitários dos utilitários (src/utils).
 *
 * Cobrem a formatação de moeda/data e, principalmente, a máscara e o parsing
 * do campo de valor — onde vírgula/ponto e casas decimais causam bugs sutis.
 */
import { describe, it, expect } from 'vitest';
import { getErrorMessage } from '../utils/errors';
import { formatCurrency, formatDate, maskAmountInput, parseAmountInput } from '../utils/format';

describe('getErrorMessage', () => {
  it('should return the message of an Error', () => {
    expect(getErrorMessage(new Error('Falha na rede'))).toBe('Falha na rede');
  });

  it('should use the fallback when the Error has an empty message', () => {
    expect(getErrorMessage(new Error(''))).toBe('Erro inesperado.');
    expect(getErrorMessage(new Error(''), 'Erro ao carregar.')).toBe('Erro ao carregar.');
  });

  it('should return a non-empty string thrown directly', () => {
    expect(getErrorMessage('erro em string')).toBe('erro em string');
  });

  it('should use the fallback for a blank string', () => {
    expect(getErrorMessage('   ')).toBe('Erro inesperado.');
  });

  it('should use the fallback for values that are not Error nor string', () => {
    expect(getErrorMessage(null)).toBe('Erro inesperado.');
    expect(getErrorMessage(undefined)).toBe('Erro inesperado.');
    expect(getErrorMessage(42)).toBe('Erro inesperado.');
    expect(getErrorMessage({ message: 'objeto qualquer' })).toBe('Erro inesperado.');
  });

  it('should accept a custom fallback', () => {
    expect(getErrorMessage({}, 'Erro ao carregar pessoas.')).toBe('Erro ao carregar pessoas.');
  });
});

describe('formatCurrency', () => {
  it('should format a positive value as BRL', () => {
    // Espaço não separável (U+00A0) entre "R$" e o número no locale pt-BR
    expect(formatCurrency(1234.5).replace(/\u00a0/g, ' ')).toBe('R$ 1.234,50');
  });

  it('should format zero', () => {
    expect(formatCurrency(0).replace(/\u00a0/g, ' ')).toBe('R$ 0,00');
  });

  it('should format a negative value', () => {
    expect(formatCurrency(-99.9).replace(/\u00a0/g, ' ')).toBe('-R$ 99,90');
  });
});

describe('formatDate', () => {
  it('should convert ISO date to pt-BR display format', () => {
    expect(formatDate('2026-01-15')).toBe('15/01/2026');
  });

  it('should return the input unchanged when it is not a full ISO date', () => {
    expect(formatDate('2026-01')).toBe('2026-01');
    expect(formatDate('')).toBe('');
    expect(formatDate('15/01/2026')).toBe('15/01/2026');
  });
});

describe('maskAmountInput', () => {
  it('should keep plain digits', () => {
    expect(maskAmountInput('1234')).toBe('1234');
  });

  it('should strip letters and symbols', () => {
    expect(maskAmountInput('R$ 12a,3b4')).toBe('12.34');
  });

  it('should normalize a comma separator to a dot', () => {
    expect(maskAmountInput('12,34')).toBe('12.34');
  });

  it('should keep only the first separator', () => {
    expect(maskAmountInput('1.2.3')).toBe('1.23');
    expect(maskAmountInput('1,2,3')).toBe('1.23');
  });

  it('should limit the decimals to two digits', () => {
    expect(maskAmountInput('12.3456')).toBe('12.34');
  });

  it('should preserve a trailing separator while typing', () => {
    expect(maskAmountInput('12,')).toBe('12.');
  });

  it('should handle a leading separator', () => {
    expect(maskAmountInput(',50')).toBe('.50');
  });

  it('should return an empty string when there are no valid characters', () => {
    expect(maskAmountInput('abc')).toBe('');
    expect(maskAmountInput('')).toBe('');
  });
});

describe('parseAmountInput', () => {
  it('should parse integers and decimals with a dot', () => {
    expect(parseAmountInput('200')).toBe(200);
    expect(parseAmountInput('12.34')).toBe(12.34);
  });

  it('should parse decimals with a comma (pt-BR keyboards)', () => {
    expect(parseAmountInput('12,34')).toBe(12.34);
  });

  it('should trim surrounding whitespace', () => {
    expect(parseAmountInput('  15.5  ')).toBe(15.5);
  });

  it('should reject empty or blank input', () => {
    expect(parseAmountInput('')).toBeNull();
    expect(parseAmountInput('   ')).toBeNull();
  });

  it('should reject zero and negative values', () => {
    expect(parseAmountInput('0')).toBeNull();
    expect(parseAmountInput('0.00')).toBeNull();
    expect(parseAmountInput('-10')).toBeNull();
  });

  it('should reject more than two decimals', () => {
    expect(parseAmountInput('10.123')).toBeNull();
  });

  it('should reject malformed values', () => {
    expect(parseAmountInput('abc')).toBeNull();
    expect(parseAmountInput('1.2.3')).toBeNull();
    expect(parseAmountInput('10.')).toBeNull();
    expect(parseAmountInput('.5')).toBeNull();
  });
});
