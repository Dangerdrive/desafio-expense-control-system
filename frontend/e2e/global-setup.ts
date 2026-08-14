import { rmSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

/**
 * Remove o banco SQLite usado pelos testes E2E antes de cada execução.
 * Isso garante que os testes sempre comecem com o banco vazio.
 */
export default function globalSetup(): void {
  const dir = path.dirname(fileURLToPath(import.meta.url));
  const dbDir = path.resolve(dir, '../backend');

  for (const suffix of ['', '-shm', '-wal']) {
    rmSync(path.join(dbDir, `ExpenseControl.e2e.db${suffix}`), { force: true });
  }
}
