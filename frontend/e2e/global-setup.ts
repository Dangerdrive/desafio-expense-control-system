import { readdirSync, rmSync } from 'node:fs';
import net from 'node:net';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

/**
 * Remove os bancos SQLite E2E antigos antes de cada execução.
 *
 * Cada execução usa um nome único (ExpenseControl.e2e-<timestamp>.db). Aqui
 * removemos apenas os arquivos antigos — e SOMENTE quando vamos subir um
 * backend novo. Se um backend já estiver rodando na porta 5000
 * (reuseExistingServer), apagar o arquivo por baixo de uma conexão aberta
 * quebra o SQLite ("no such table" / "attempt to write a readonly database").
 */
export default async function globalSetup(): Promise<void> {
  // Este arquivo fica em frontend/e2e/ — o backend está dois níveis acima.
  const dbDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../backend');

  if (await isPortOpen(5000)) {
    console.log('[global-setup] Backend reutilizado na porta 5000 — mantendo o banco E2E existente.');
    return;
  }

  for (const file of readdirSync(dbDir)) {
    if (file.startsWith('ExpenseControl.e2e-')) {
      rmSync(path.join(dbDir, file), { force: true });
    }
  }
}

/** Retorna true se algo está escutando na porta (localhost). */
function isPortOpen(port: number): Promise<boolean> {
  return new Promise((resolve) => {
    const socket = net.connect({ port, host: '127.0.0.1' });
    socket.once('connect', () => { socket.destroy(); resolve(true); });
    socket.once('error', () => resolve(false));
  });
}
