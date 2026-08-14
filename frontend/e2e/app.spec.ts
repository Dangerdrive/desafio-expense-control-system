import { expect, test, type Page } from '@playwright/test';

/**
 * Testes E2E — fluxos principais do usuário contra a aplicação real
 * (backend .NET + frontend React/Vite + banco SQLite isolado).
 */

const unique = (prefix: string) => `${prefix} ${Date.now()}`;

async function createPerson(page: Page, name: string, age: number): Promise<void> {
  await page.goto('/');
  await page.getByPlaceholder('Nome').fill(name);
  await page.getByPlaceholder('Idade').fill(String(age));
  await page.getByRole('button', { name: /Adicionar/ }).click();
  await expect(page.getByText('Pessoa cadastrada com sucesso!')).toBeVisible();
}

async function gotoTransactions(page: Page): Promise<void> {
  await page.getByRole('button', { name: /Transações/ }).click();
  await expect(page.getByRole('heading', { name: 'Cadastro de Transações' })).toBeVisible();
}

test.describe('Fluxo de Pessoas', () => {
  test('cadastra uma pessoa e ela aparece na listagem', async ({ page }) => {
    const name = unique('Ana');

    await createPerson(page, name, 25);

    // A célula com o nome exato aparece (exact: true evita casar com "Remover Ana …")
    await expect(page.getByRole('cell', { name, exact: true })).toBeVisible();
  });

  test('remove uma pessoa confirmando no modal estilizado', async ({ page }) => {
    const name = unique('Bruno');

    await createPerson(page, name, 40);

    // Abre o modal de confirmação
    const row = page.getByRole('row', { name: new RegExp(name) });
    await row.getByRole('button', { name: /Remover/ }).click();

    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(/Todas as transações desta pessoa também serão removidas/)).toBeVisible();

    // Confirma a exclusão
    await dialog.getByRole('button', { name: 'Remover', exact: true }).click();

    await expect(page.getByText(`"${name}" removida.`)).toBeVisible();
    await expect(page.getByRole('cell', { name, exact: true })).toHaveCount(0);
  });
});

test.describe('Fluxo de Transações', () => {
  test('cadastra uma receita e ela aparece na listagem', async ({ page }) => {
    const person = unique('Carlos');
    const description = unique('Receita');

    await createPerson(page, person, 30);
    await gotoTransactions(page);

    await page.getByPlaceholder('Descrição').fill(description);
    await page.getByLabel('Valor').fill('2500,50');
    await page.getByLabel('Tipo').selectOption('receita');
    await page.getByLabel('Pessoa').selectOption({ label: `${person} (30a)` });
    await page.getByRole('button', { name: /Registrar/ }).click();

    await expect(page.getByText('Transação registrada com sucesso!')).toBeVisible();
    await expect(page.getByRole('cell', { name: description, exact: true })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'R$ 2.500,50', exact: true })).toBeVisible();
  });

  test('menor de idade não pode cadastrar receita (regra de negócio)', async ({ page }) => {
    const person = unique('Duda');

    await createPerson(page, person, 15);
    await gotoTransactions(page);

    await page.getByPlaceholder('Descrição').fill('Mesada');
    await page.getByLabel('Valor').fill('100');
    await page.getByLabel('Tipo').selectOption('receita');
    await page.getByLabel('Pessoa').selectOption({ label: `${person} (15a 🔞)` });
    await page.getByRole('button', { name: /Registrar/ }).click();

    await expect(
      page.getByText('Menores de 18 anos não podem cadastrar receitas, apenas despesas.')
    ).toBeVisible();
  });
});

test.describe('Fluxo de Totais', () => {
  test('reflete receitas e despesas no resumo financeiro', async ({ page }) => {
    const person = unique('Elena');

    await createPerson(page, person, 35);
    await gotoTransactions(page);

    // Receita de R$ 1000
    await page.getByPlaceholder('Descrição').fill('Salário');
    await page.getByLabel('Valor').fill('1000');
    await page.getByLabel('Tipo').selectOption('receita');
    await page.getByLabel('Pessoa').selectOption({ label: `${person} (35a)` });
    await page.getByRole('button', { name: /Registrar/ }).click();
    await expect(page.getByText('Transação registrada com sucesso!')).toBeVisible();

    // Despesa de R$ 400
    await page.getByPlaceholder('Descrição').fill('Aluguel');
    await page.getByLabel('Valor').fill('400');
    await page.getByLabel('Tipo').selectOption('despesa');
    await page.getByLabel('Pessoa').selectOption({ label: `${person} (35a)` });
    await page.getByRole('button', { name: /Registrar/ }).click();
    await expect(page.getByText('Transação registrada com sucesso!')).toBeVisible();

    // Abre a aba de totais
    await page.getByRole('button', { name: /Totais/ }).click();

    const totalsRow = page.getByRole('row', { name: new RegExp(person) });
    await expect(totalsRow).toBeVisible();
    await expect(totalsRow.getByText('R$ 1.000,00')).toBeVisible(); // receitas
    await expect(totalsRow.getByText('R$ 400,00')).toBeVisible();   // despesas
    await expect(totalsRow.getByText('R$ 600,00')).toBeVisible();   // saldo
  });
});
