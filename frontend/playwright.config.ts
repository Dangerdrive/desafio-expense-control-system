import { defineConfig, devices } from '@playwright/test';

/**
 * Configuração dos testes E2E (Playwright).
 *
 * Inicia automaticamente o backend (.NET) e o frontend (Vite) antes dos testes.
 * O backend usa um banco SQLite dedicado (ExpenseControl.e2e.db) para não
 * interferir no banco de desenvolvimento — o globalSetup remove esse arquivo
 * a cada execução, garantindo um estado limpo.
 */
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
        // Banco isolado para os testes E2E (removido no globalSetup)
        ConnectionStrings__DefaultConnection: 'Data Source=ExpenseControl.e2e.db',
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
