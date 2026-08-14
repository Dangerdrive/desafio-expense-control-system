/**
 * Extrai uma mensagem de erro legível a partir de um valor desconhecido
 * lançado em um catch (o padrão moderno do TypeScript: catch (err: unknown)).
 *
 * Evita acessos inseguros como `(err as any).message` que podem lançar
 * exceções dentro do próprio handler de erro.
 */
export function getErrorMessage(err: unknown, fallback = 'Erro inesperado.'): string {
  if (err instanceof Error) return err.message || fallback;
  if (typeof err === 'string' && err.trim() !== '') return err;
  return fallback;
}
