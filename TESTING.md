# 🧪 Documentação de Testes — Expense Control System

> **Status:** ✅ Suite completa com **242 testes** (111 backend + 131 frontend) + **5 E2E (Playwright)**, todos passando.

---

## 📊 Resumo da Suite

| Camada | Framework | Tipo | Quantidade | Status |
|--------|-----------|------|-----------|--------|
| Backend — Unit | xUnit + EF Core InMemory | Serviços + Middleware + Repository + Converter | 66 | ✅ 66/66 |
| Backend — Integration | xUnit + WebApplicationFactory | Controllers HTTP + Contrato | 45 | ✅ 45/45 |
| Frontend — Unit | Vitest + mock fetch | API layer + utils | 48 | ✅ 48/48 |
| Frontend — Component | Vitest + Testing Library | React components | 77 | ✅ 77/77 |
| Frontend — Contrato | Vitest (contracts/api-contract.json) | Schema da API | 6 | ✅ 6/6 |
| **TOTAL** | | | **242** | **✅ 242/242** |
| E2E — Playwright | Playwright + Chromium | Fluxos completos (UI + API) | 5 | ✅ 5/5 |

---

## 🔧 Como executar

### Backend

```bash
cd tests/backend
dotnet test                          # Executa todos
dotnet test --filter "Unit"          # Só unitários
dotnet test --filter "Integration"   # Só integração
dotnet test --logger "console;verbosity=detailed"  # Output detalhado
```

### Frontend

```bash
cd frontend
npm test                             # Executa todos (vitest run)
npm run test:watch                   # Modo watch (dev)
npm run test:coverage                # Com relatório de cobertura
npx vitest run --reporter=verbose    # Output detalhado
```

---

## 📋 Backend — Testes Unitários

### PersonServiceTests (9 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1 | `CreateAsync_WithValidData` | Criação com dados válidos |
| 2 | `CreateAsync_WithMinimumAge` | Idade 0 (recém-nascido) — limite inferior |
| 3 | `CreateAsync_WithMaximumAge` | Idade 150 — limite superior |
| 4 | `GetAllAsync_WithNoPeople` | Lista vazia quando não há pessoas |
| 5 | `GetAllAsync_WithMultiplePeople` | Lista ordenada alfabeticamente |
| 6 | `GetAllAsync_WithPagination` | Paginação: só os itens da página (Skip/Take) |
| 7 | `DeleteAsync_WithExistingPerson` | Remove pessoa existente → true |
| 8 | `DeleteAsync_WithNonExistingPerson` | Remove pessoa inexistente → false |
| 9 | `DeleteAsync_ShouldCascadeDeleteTransactions` | **Cascata:** deletar pessoa remove transações |

### TransactionServiceTests (21 testes)

| # | Teste | Cenário | Regra de Negócio |
|---|-------|---------|-----------------|
| 1 | `CreateAsync_AdultWithIncome` | Adulto + receita | ✅ Permitido |
| 2 | `CreateAsync_AdultWithExpense` | Adulto + despesa | ✅ Permitido |
| 3 | `CreateAsync_MinorWithIncome` | **Menor + receita** | ❌ Bloqueado |
| 4 | `CreateAsync_MinorWithExpense` | Menor + despesa | ✅ Permitido |
| 5 | `CreateAsync_MinorExactly17_WithIncome` | 17 anos + receita (boundary) | ❌ Bloqueado |
| 6 | `CreateAsync_Exactly18_WithIncome` | **18 anos + receita (boundary)** | ✅ Permitido |
| 7 | `CreateAsync_WithNonExistingPerson` | Pessoa inválida | ❌ Bloqueado |
| 8 | `GetAllAsync_WithNoTransactions` | Lista vazia | — |
| 9 | `GetAllAsync_WithMultipleTransactions` | Lista com dados | — |
| 10 | `CreateAsync_WithVeryLargeAmount` | Valor máximo (R$999M) | ✅ Edge case |
| 11 | `CreateAsync_WithDecimalPrecision` | Centavos preservados | ✅ Precisão |
| 12 | `CreateAsync_ShouldPreserveDate` | Data informada é preservada | ✅ |
| 13 | `GetAllAsync_WithDateRange` | Filtro por período (de/até) | — |
| 14 | `GetAllAsync_WithSortAscending` | Ordenação por data crescente | — |
| 15 | `GetAllAsync_DefaultOrder` | Ordem padrão: mais recente primeiro | — |
| 16 | `GetAllAsync_WithPagination` | Paginação: só os itens da página | — |
| 17 | `UpdateAsync_ShouldUpdateFields` | PUT atualiza os campos | ✅ |
| 18 | `UpdateAsync_WithNonExistingId` | PUT id inexistente → null | ❌ 404 |
| 19 | `UpdateAsync_MinorWithIncome` | PUT menor + receita → throw | ❌ Bloqueado |
| 20 | `DeleteAsync_ShouldRemoveTransaction` | DELETE remove transação → true | ✅ |
| 21 | `DeleteAsync_WithNonExistingId` | DELETE id inexistente → false | ❌ 404 |

### TotalsServiceTests (7 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1 | `GetTotalsAsync_WithNoPeople` | Sem pessoas → vazio + zeros |
| 2 | `GetTotalsAsync_WithPeopleButNoTransactions` | Pessoas sem transações → zeros |
| 3 | `GetTotalsAsync_ShouldCalculateCorrectTotals` | Cenário completo: 2 pessoas, receitas + despesas |
| 4 | `GetTotalsAsync_WithNegativeBalance` | Saldo negativo (mais despesas) |
| 5 | `GetTotalsAsync_OnlyIncome` | Só receitas → saldo positivo |
| 6 | `GetTotalsAsync_OnlyExpense` | Só despesas → saldo negativo |
| 7 | `GetTotalsAsync_ShouldOrderByName` | Ordenação alfabética |

---

### ExceptionHandlingMiddlewareTests (2 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1 | `InvokeAsync_WhenNextThrows_ShouldReturn500WithUnifiedMessage` | Exceção não tratada → 500 `{ message }` |
| 2 | `InvokeAsync_WhenNextSucceeds_ShouldNotInterfere` | Resposta de sucesso não é alterada |

### TransactionTypeJsonConverterTests (19 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1-4 | `Read_WithStringValue_ShouldParseCaseInsensitive` | `"receita"`, `"despesa"`, `"Receita"`, `"DESPESA"` |
| 5-6 | `Read_WithDefinedNumericValue_ShouldParse` | `1` → Receita, `2` → Despesa |
| 7-15 | `Read_WithInvalidValue_ShouldThrowWithPtBrMessage` | String inválida/vazia, `0`, `3`, `1.5`, `true`, `null`, `{}`, `[]` |
| 16-17 | `Write_ShouldSerializeAsLowercaseString` | Contrato em minúsculas |
| 18 | `Write_WithUndefinedEnumValue_ShouldSerializeLowercasedName` | Valor fora do enum |
| 19 | `RoundTrip_ShouldPreserveValue` | Serializa → desserializa |

---

## 📋 Backend — Testes de Integração

### PeopleControllerTests (13 testes HTTP)

| # | Teste | Verbo | Status Esperado |
|---|-------|-------|----------------|
| 1 | `Post_WithValidData` | POST | 201 Created |
| 2 | `Get_WithPeople` | GET | 200 + lista paginada |
| 3 | `Get_WithPagination` | GET | 200 + metadados (page, totalPages...) |
| 4 | `Get_WithExistingPerson_ShouldReturn200` | GET | 200 + pessoa |
| 5 | `Get_WithNonExistingPerson_ShouldReturn404` | GET | 404 Not Found |
| 6 | `Delete_WithExistingPerson` | DELETE | 204 No Content |
| 7 | `Delete_WithNonExistingPerson` | DELETE | 404 Not Found |
| 8 | `Delete_ShouldRemoveAssociatedTransactions` | DELETE | 204 + transações removidas (cascata) |
| 9 | `Post_WithEmptyName` | POST | 400 Bad Request |
| 10 | `Post_WithNegativeAge` | POST | 400 Bad Request |
| 11 | `Post_WithAgeAbove150` | POST | 400 Bad Request |
| 12 | `Post_WithNameTooLong` | POST | 400 Bad Request |
| 13 | `Post_ValidationError_ShouldReturnUnifiedMessageShape` | POST | 400 + `{ message }` (sem `errors`) |

### TransactionsControllerTests (23 testes HTTP)

| # | Teste | Verbo | Status | Regra |
|---|-------|-------|--------|-------|
| 1 | `Post_AdultWithIncome` | POST | 201 | ✅ |
| 2 | `Post_MinorWithIncome` | POST | 400 | ❌ Menores de 18 |
| 3 | `Post_MinorWithExpense` | POST | 201 | ✅ |
| 4 | `Post_WithNonExistingPerson` | POST | 400 | ❌ |
| 5 | `Get_ShouldReturn200` | GET | 200 | — |
| 6 | `Post_AdultWithExpense` | POST | 201 | ✅ |
| 7 | `Post_WithZeroAmount` | POST | 400 | ❌ Valor deve ser > 0 |
| 8 | `Post_WithInvalidType` | POST | 400 | ❌ Tipo inválido |
| 9 | `Post_WithEmptyDescription` | POST | 400 | ❌ Descrição vazia |
| 10 | `Post_WithDescriptionTooLong` | POST | 400 | ❌ Descrição > 200 chars |
| 11 | `Post_ShouldReturnPersonName` | POST | 201 | ✅ `personName` preenchido |
| 12 | `Get_ShouldPopulatePersonName` | GET | 200 | ✅ `personName` preenchido |
| 13 | `Get_WithExistingTransaction_ShouldReturn200` | GET | 200 | ✅ |
| 14 | `Get_WithNonExistingTransaction_ShouldReturn404` | GET | 404 | ❌ |
| 15 | `Post_WithMissingDate` | POST | 400 | ❌ Data obrigatória |
| 16 | `Get_WithDateFilterAndSort` | GET | 200 | ✅ Filtro período + ordenação |
| 17 | `Get_DefaultOrder` | GET | 200 | ✅ Mais recente primeiro |
| 18 | `Put_ShouldReturnUpdatedTransaction` | PUT | 200 | ✅ |
| 19 | `Put_WithNonExistingId` | PUT | 404 | ❌ |
| 20 | `Put_MinorWithIncome` | PUT | 400 | ❌ Menores de 18 |
| 21 | `Delete_ShouldReturn204` | DELETE | 204 | ✅ |
| 22 | `Delete_WithNonExistingId` | DELETE | 404 | ❌ |
| 23 | `Get_WithPagination` | GET | 200 | ✅ Metadados da página |

### ContractTests (6 testes HTTP — contratos da API)

| # | Teste | Valida |
|---|-------|--------|
| 1 | `PeopleResponse_ShouldMatchContract` | `person` (GET /api/people/{id}) |
| 2 | `TransactionResponse_ShouldMatchContract` | `transaction` (GET /api/transactions/{id}) |
| 3 | `PeopleListResponse_ShouldMatchContract` | `personPage` (GET /api/people) |
| 4 | `TransactionsListResponse_ShouldMatchContract` | `transactionPage` (GET /api/transactions) |
| 5 | `TotalsResponse_ShouldMatchContract` | `totals` (GET /api/totals) |
| 6 | `ErrorResponse_ShouldMatchContract` | `error` (formato `{ message }`) |

### TotalsControllerTests (3 testes HTTP)

| # | Teste | Verbo | Status | Validação |
|---|-------|-------|--------|-----------|
| 1 | `Get_ShouldReturn200WithValidStructure` | GET | 200 | Estrutura do JSON |
| 2 | `Get_WithFullData` | GET | 200 | Cálculos corretos |
| 3 | `Get_ResponseStructure` | GET | 200 | Campos obrigatórios |

---

## 📋 Frontend — Testes da API Layer

### api.test.ts (22 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1 | `getPeople` — sucesso | Retorna envelope paginado de pessoas |
| 2 | `getPeople` — page/pageSize | Envia `?page=&pageSize=` quando informado |
| 3 | `getPeople` — erro 500 | Lança exceção com mensagem |
| 4 | `createPerson` — sucesso | POST com dados corretos |
| 5 | `deletePerson` — sucesso | DELETE → void (204) |
| 6 | `deletePerson` — 404 | Lança "Pessoa não encontrada" |
| 7 | `getTransactions` — sucesso | Retorna envelope paginado de transações |
| 8 | `getTransactions` — filtros | Envia from/to/sort/page/pageSize |
| 9 | `getTransactions` — sem params | URL sem query string |
| 10 | `createTransaction` — adulto | POST receita → 201 |
| 11 | `createTransaction` — regra violada | Menor + receita → erro 400 |
| 12 | `getTotals` — com dados | Estrutura correta |
| 13 | `getTotals` — vazio | Arrays vazios, zeros |
| 14 | `getPeople` — network failure | `Failed to fetch` |
| 15 | `createTransaction` — network failure | `Failed to fetch` |
| 16 | `getTotals` — network failure | `Network error` |
| 17 | `deletePerson` — 204 sem body | Não tenta parsear JSON em 204 |
| 18 | `updateTransaction` — sucesso | PUT com payload correto |
| 19 | `updateTransaction` — 404 | Transação inexistente |
| 20 | `deleteTransaction` — sucesso | DELETE → void (204) |
| 21 | erro sem `message` | Fallback `Erro {status}` |
| 22 | corpo de erro não-JSON | Fallback `Erro desconhecido` |

### utils.test.ts (26 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1-6 | `getErrorMessage` | `Error`, mensagem vazia → fallback, string, string em branco, tipos não-`Error`, fallback custom |
| 7-9 | `formatCurrency` | Positivo, zero, negativo (BRL) |
| 10-11 | `formatDate` | ISO → `DD/MM/YYYY`; entrada inválida devolvida sem alteração |
| 12-19 | `maskAmountInput` | Remove caracteres inválidos, normaliza `,`→`.`, um só separador, máx. 2 decimais |
| 20-26 | `parseAmountInput` | `12,34` → `12.34`, zero/negativo/3 decimais/malformado → `null` |

---

## 📋 Frontend — Testes de Componente

### App.test.tsx (25 testes)

| # | Teste | Componente |
|---|-------|-----------|
| 1 | Renderiza header | App |
| 2 | Renderiza 3 botões de tab | App |
| 3 | Mostra People tab por padrão | App |
| 4 | Navega para Transactions tab | App |
| 5 | Navega para Totals tab | App |
| 6 | Mensagem de vazio (sem pessoas) | PeopleTab |
| 7 | Lista pessoas quando há dados | PeopleTab |
| 8 | Formulário de criação visível | PeopleTab |
| 9 | **Paginação da lista de pessoas** (próxima página) | PeopleTab |
| 10 | Aviso quando não há pessoas | TransactionsTab |
| 11 | Regra de negócio visível | TransactionsTab |
| 12 | Campos do formulário de transação | TransactionsTab |
| 13 | **Paginação da lista de transações** | TransactionsTab |
| 14 | Botão de atualizar totais | TotalsTab |
| 15 | Renderiza totais com dados | TotalsTab |
| 16 | Estado de carregamento (loading) | TotalsTab |
| 17 | Mensagem de erro na consulta | TotalsTab |
| 18 | Erro de validação (form vazio) | PeopleTab |
| 19 | Criação de pessoa com sucesso | PeopleTab |
| 20 | Erro ao criar pessoa | PeopleTab |
| 21 | Criação de transação com sucesso | TransactionsTab |
| 22 | Erro regra de negócio na UI | TransactionsTab |
| 23 | Edição de transação (chama updateTransaction) | TransactionsTab |
| 24 | Exclusão de transação (modal + deleteTransaction) | TransactionsTab |
| 25 | Volta para a aba Pessoas e marca como ativa | App |

### PeopleTab.test.tsx (14 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1-4 | Listagem | Loading, erro da API, fallback genérico, marcação de menores (🔞) |
| 5-9 | Criação | Nome em branco, idade ausente, idade fora de 0-150, trim + reset + reload, botão desabilitado ao salvar |
| 10-14 | Remoção | Cancelar no modal, confirmar, recuo de página quando a página esvazia, erro na exclusão |

### TransactionsTab.test.tsx (17 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1-6 | Listagem | Erro da API, fallback genérico, formatação pt-BR + badges, menores no seletor, submit desabilitado sem pessoas |
| 7-8 | Filtros | Período (de/até) e ordenação recarregam na página 1 |
| 9-13 | Formulário | Descrição/valor/pessoa obrigatórios, máscara do valor, `12,34` enviado como número |
| 14 | Edição | Cancelar limpa o formulário e sai do modo edição |
| 15-17 | Remoção | Cancelar no modal, recuo de página, erro na exclusão |

### TotalsTab.test.tsx (5 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1 | Sem pessoas | Mensagem de lista vazia + total geral |
| 2-3 | Saldo | Negativo → vermelho/`balance-negative`; positivo → verde/`balance-positive` |
| 4 | Atualizar | Botão refaz a consulta |
| 5 | Erro não-`Error` | Fallback `Erro ao consultar totais.` |

### Pagination.test.tsx (6 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1 | `totalPages <= 1` | Não renderiza nada |
| 2 | Info da página | "Página X de Y (N itens)" |
| 3-4 | Limites | Botão anterior/próxima desabilitado na primeira/última página |
| 5-6 | Navegação | `onPageChange(page ± 1)` |

### ConfirmDialog.test.tsx (5 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1 | Fechado | Não renderiza nada |
| 2-3 | Estilos/labels | Padrão (`btn-primary`) e variante `danger` com labels custom |
| 4 | Callbacks | `onConfirm`/`onCancel` |
| 5 | Overlay | Clique no overlay cancela; clique no diálogo não |

### ErrorBoundary.test.tsx (5 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1 | Sem erro | Renderiza os filhos |
| 2-3 | Fallback | Mensagem do erro e mensagem padrão quando vazia |
| 4 | `componentDidCatch` | Loga o erro capturado |
| 5 | Reset | "Tentar novamente" volta a renderizar os filhos |

### contract.test.ts (6 testes)

| # | Teste | Valida |
|---|-------|--------|
| 1 | `person` | Campos batem com o tipo TS `Person` |
| 2 | `transaction` | Campos batem com o tipo TS `Transaction` |
| 3 | `totals` | Campos batem com o tipo TS `TotalsResponse` |
| 4 | `personPage` | Envelope bate com o tipo TS `PagedResult<Person>` |
| 5 | `transactionPage` | Itens do envelope batem com `Transaction` |
| 6 | `a camada api consegue consumir o contrato` | `getPeople`/`getTransactions`/`getTotals` consomem os fixtures |

---

## 🏗️ Arquitetura de Testes

```
┌─────────────────────────────────────────────────────┐
│                 Pirâmide de Testes                    │
│                                                      │
│                    ┌──────────┐                      │
│                    │   E2E    │  ← 5 testes          │
│                    │(Playwright)│    Playwright       │
│                   ─┴──────────┴─                     │
│                 ┌────────────────┐                   │
│                 │  Integration   │  ← 45 tests       │
│                 │  (Controllers) │     WebAppFactory │
│                ─┴────────────────┴─                  │
│          ┌─────────────────────────────┐             │
│          │       Unit Tests            │  ← 197 tests│
│          │  (Services + API + Comps)   │             │
│          └─────────────────────────────┘             │
│                                                      │
│  Backend: xUnit + InMemory + WebApplicationFactory   │
│  Frontend: Vitest + Testing Library + mock fetch     │
└─────────────────────────────────────────────────────┘
```

---

## 🎯 Cobertura de Regras de Negócio

| Regra | Teste Unitário | Teste Integração | Status |
|-------|---------------|-----------------|--------|
| Menor de 18 só pode despesa | `TransactionServiceTests` #3, #4, #5, #6 | `TransactionsControllerTests` #2, #3 | ✅ 100% |
| Delete pessoa → cascata transações | `PersonServiceTests` #8 | `PeopleControllerTests` #5 | ✅ 100% |
| Pessoa deve existir na transação | `TransactionServiceTests` #7 | `TransactionsControllerTests` #4 | ✅ 100% |
| Totais: receita - despesa = saldo | `TotalsServiceTests` #3-6 | `TotalsControllerTests` #2 | ✅ 100% |
| Validação de entrada (DTOs) | — | `PeopleControllerTests` #6-9, `TransactionsControllerTests` #7-10 | ✅ 100% |

---10

## 📁 Estrutura de arquivos de teste

```
expense-control-system/
├── TESTING.md                          # Este arquivo
├── tests/
│   └── backend/
│       ├── Backend.Tests.csproj        # Projeto xUnit + coverlet
│       ├── TestDatabase.cs             # Fixture InMemory (unit)
│       ├── TestWebApplicationFactory.cs # Factory p/ integração
│       ├── Unit/
│       │   ├── PersonServiceTests.cs   # 9 testes
│       │   ├── TransactionServiceTests.cs # 21 testes
│       │   ├── TotalsServiceTests.cs   # 7 testes
│       │   ├── RepositoryTests.cs      # 8 testes
│       │   ├── ExceptionHandlingMiddlewareTests.cs # 2 testes
│       │   └── TransactionTypeJsonConverterTests.cs # 19 testes
│       └── Integration/
│           ├── PeopleControllerTests.cs    # 13 testes
│           ├── TransactionsControllerTests.cs # 23 testes
│           ├── TotalsControllerTests.cs    # 3 testes
│           └── ContractTests.cs            # 6 testes
└── frontend/
    ├── vite.config.ts                 # Config Vitest + coverage thresholds
    └── src/
        ├── test-setup.ts               # Setup Testing Library
        └── __tests__/
            ├── api.test.ts             # 22 testes
            ├── utils.test.ts           # 26 testes
            ├── App.test.tsx            # 25 testes
            ├── PeopleTab.test.tsx      # 14 testes
            ├── TransactionsTab.test.tsx # 17 testes
            ├── TotalsTab.test.tsx      # 5 testes
            ├── Pagination.test.tsx     # 6 testes
            ├── ConfirmDialog.test.tsx  # 5 testes
            ├── ErrorBoundary.test.tsx  # 5 testes
            └── contract.test.ts        # 6 testes
```

---

## 📊 Cobertura de Código (Coverage)

### Backend

```bash
cd tests/backend
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### Frontend

```bash
cd frontend
npx vitest run --coverage
```

**Thresholds configurados no `vite.config.ts`:**
- Lines: 80%
- Branches: 70%
- Functions: 80%
- Statements: 80%

---

## 🚀 CI/CD Ready

Para integrar em pipeline CI/CD:

```yaml
# Exemplo GitHub Actions
- name: Backend Tests
  run: cd tests/backend && dotnet test /p:CollectCoverage=true

- name: Frontend Tests
  run: cd frontend && npm ci && npm test -- --coverage
```

---

## 🔎 QA Audit (2026-07-23)

Auditoria de qualidade realizada para identificar gaps e melhorias.

### Correções aplicadas (✅)

| # | Severidade | Problema | Solução |
|---|-----------|----------|---------|
| 1 | 🔴 BUG | `DeleteAsync_WithExistingPerson` passava `AppDbContext` em vez de `IRepository<Person>` para `PersonService` (causava erro de compilação) | Corrigido para `new PersonService(new Repository<Person>(context))` |
| 2 | 🟠 Boundary | Faltava teste para exatamente 18 anos criando receita | Adicionado `CreateAsync_Exactly18_WithIncome_ShouldSucceed` |
| 3 | 🟠 Validação | Zero testes de model validation (400 Bad Request) nos integration tests | Adicionados 8 testes: nome vazio, idade inválida, valor zero, tipo inválido, descrição vazia, nome/descrição muito longos |
| 4 | 🟠 Frontend | Faltavam testes de submissão de formulário, estados de erro e loading | Adicionados 7 testes: criação de pessoa, criação de transação, erros de API na UI, loading state |
| 5 | 🟠 Frontend | API layer não testava falhas de rede (`fetch` rejeitado) | Adicionados 3 testes de `Failed to fetch` + 1 teste de 204 sem body |
| 6 | 🟡 Coverage | Nenhuma configuração de threshold de cobertura | Adicionado thresholds no `vite.config.ts` e configurado `coverlet.collector` no backend |

### Recomendações futuras (📋)

| # | Prioridade | Recomendação |
|---|-----------|--------------|
| 1 | � | **E2E tests**: ✅ Implementado — Playwright com 5 fluxos completos (`npm run test:e2e`) |
| 2 | 🟢 | **TotalsService**: ✅ Refatorado para usar `IRepository<T>` (consistência arquitetural) |
| 3 | 🟡 | **Testes de Performance**: Adicionar testes de carga para endpoints críticos (ex: `/api/totals` com muitas transações) |
| 4 | � | **Testes do Repository**: ✅ Implementado — `RepositoryTests.cs` (8 testes: CRUD + Include) |
| 5 | 🟢 | **Testes de contrato**: ✅ Implementado — `contracts/api-contract.json` validado no backend (`ContractTests`) e no frontend (`contract.test.ts`) |
| 6 | 🟡 | **Mutation testing**: Usar Stryker.NET para validar qualidade dos testes |
| 7 | 🟢 | **CI Pipeline**: ✅ Implementado — GitHub Actions roda backend + frontend + E2E em push/PR |

### Resultado

- **Antes (da auditoria):** 66 testes (43 backend + 23 frontend)
- **Depois (da auditoria):** 86 testes (52 backend + 34 frontend)
- **Atualmente:** 242 testes (111 backend + 131 frontend) + 5 E2E (Playwright)
- **Aumento (desde a auditoria):** +176 testes (+267%) na suite unitária/integração, +5 E2E
- **Cobertura do frontend (v8):** 80,5% → 98,9% statements (funções: 75,4% → 98,5%)
- **Cobertura de validação de entrada:** 0% → 100%
- **Cobertura de boundary conditions:** 80% → 100%
- **Cobertura de UI states (loading/error):** 0% → 100%
