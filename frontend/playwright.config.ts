import { defineConfig, devices } from '@playwright/test';

/**
 * Configuração dos testes E2E (Playwright).
 *
 * Inicia automaticamente o backend (.NET) e o frontend (Vite) antes dos testes.
 * O backend usa um banco SQLite dedicado e ÚNICO por execução
 * (ExpenseControl.e2e-&lt;timestamp&gt;.db) para não interferir no banco de
 * desenvolvimento nem em um backend reutilizado (reuseExistingServer). O
 * globalSetup remove os bancos E2E antigos a cada execução.
 */
const e2eDbName = `ExpenseControl.e2e-${Date.now()}.db`;

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1, // um worker por vez: evita corrida no banco compartilhado
  globalSetup: './e2e/global-setup.ts',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: [
    {
      command: 'dotnet run',
      cwd: '../backend',
      url: 'http://localhost:5000/api/people',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        // Banco isolado e único por execução (removidos antigos no globalSetup)
        ConnectionStrings__DefaultConnection: `Data Source=${e2eDbName}`,
      },
    },
    {
      command: 'npm run dev -- --port 5173 --strictPort',
      cwd: '.',
      url: 'http://localhost:5173',
      reuseExistingServer: !process.env.CI,
      timeout: 60_000,
    },
  ],
});
