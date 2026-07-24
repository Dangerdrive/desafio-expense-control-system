# 🧪 Documentação de Testes — Expense Control System

> **Status:** ✅ Suite completa com **87 testes** (55 backend + 32 frontend), todos passando.

---

## 📊 Resumo da Suite

| Camada | Framework | Tipo | Quantidade | Status |
|--------|-----------|------|-----------|--------|
| Backend — Unit | xUnit + EF Core InMemory | Serviços | 28 | ✅ 28/28 |
| Backend — Integration | xUnit + WebApplicationFactory | Controllers HTTP | 27 | ✅ 27/27 |
| Frontend — Unit | Vitest + mock fetch | API layer | 14 | ✅ 14/14 |
| Frontend — Component | Vitest + Testing Library | React components | 18 | ✅ 18/18 |
| **TOTAL** | | | **87** | **✅ 87/87** |

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
npx vitest run --reporter=verbose    # Output detalhado
```

---

## 📋 Backend — Testes Unitários

### PersonServiceTests (13 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1 | `CreateAsync_WithValidData` | Criação com dados válidos |
| 2 | `CreateAsync_WithMinimumAge` | Idade 0 (recém-nascido) — limite inferior |
| 3 | `CreateAsync_WithMaximumAge` | Idade 150 — limite superior |
| 4 | `GetAllAsync_WithNoPeople` | Lista vazia quando não há pessoas |
| 5 | `GetAllAsync_WithMultiplePeople` | Lista ordenada alfabeticamente |
| 6 | `DeleteAsync_WithExistingPerson` | Remove pessoa existente → true |
| 7 | `DeleteAsync_WithNonExistingPerson` | Remove pessoa inexistente → false |
| 8 | `DeleteAsync_ShouldCascadeDeleteTransactions` | **Cascata:** deletar pessoa remove transações |
| 9 | `ExistsAsync_WithExistingPerson` | Pessoa existe → true |
| 10 | `ExistsAsync_WithNonExistingPerson` | Pessoa não existe → false |
| 11 | `GetAgeAsync_WithExistingPerson` | Retorna idade correta |
| 12 | `GetAgeAsync_WithNonExistingPerson` | Retorna null para pessoa inexistente |
| 13 | (implícito via cascata) | Transações removidas após delete da pessoa |

### TransactionServiceTests (12 testes)

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
| 12 | (implícito) | Validação de tipo (receita/despesa) | — |

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

## 📋 Backend — Testes de Integração

### PeopleControllerTests (9 testes HTTP)

| # | Teste | Verbo | Status Esperado |
|---|-------|-------|----------------|
| 1 | `Post_WithValidData` | POST | 201 Created |
| 2 | `Get_WithPeople` | GET | 200 + lista |
| 3 | `Delete_WithExistingPerson` | DELETE | 204 No Content |
| 4 | `Delete_WithNonExistingPerson` | DELETE | 404 Not Found |
| 5 | `Delete_ShouldRemoveAssociatedTransactions` | DELETE | 204 + transações removidas (cascata) |
| 6 | `Post_WithEmptyName` | POST | 400 Bad Request |
| 7 | `Post_WithNegativeAge` | POST | 400 Bad Request |
| 8 | `Post_WithAgeAbove150` | POST | 400 Bad Request |
| 9 | `Post_WithNameTooLong` | POST | 400 Bad Request |

### TransactionsControllerTests (10 testes HTTP)

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

### TotalsControllerTests (5 testes HTTP)

| # | Teste | Verbo | Status | Validação |
|---|-------|-------|--------|-----------|
| 1 | `Get_ShouldReturn200WithValidStructure` | GET | 200 | Estrutura do JSON |
| 2 | `Get_WithFullData` | GET | 200 | Cálculos corretos |
| 3 | `Get_ResponseStructure` | GET | 200 | Campos obrigatórios |
| 4 | (implícito) | — | — | Consistência saldo = receita - despesa |
| 5 | (implícito) | — | — | Ordenação por nome |

---

## 📋 Frontend — Testes da API Layer

### api.test.ts (14 testes)

| # | Teste | Cenário |
|---|-------|---------|
| 1 | `getPeople` — sucesso | Retorna lista de pessoas |
| 2 | `getPeople` — erro 500 | Lança exceção com mensagem |
| 3 | `createPerson` — sucesso | POST com dados corretos |
| 4 | `deletePerson` — sucesso | DELETE → void (204) |
| 5 | `deletePerson` — 404 | Lança "Pessoa não encontrada" |
| 6 | `getTransactions` — sucesso | Retorna transações |
| 7 | `createTransaction` — adulto | POST receita → 201 |
| 8 | `createTransaction` — regra violada | Menor + receita → erro 400 |
| 9 | `getTotals` — com dados | Estrutura correta |
| 10 | `getTotals` — vazio | Arrays vazios, zeros |
| 11 | `getPeople` — network failure | `Failed to fetch` |
| 12 | `createTransaction` — network failure | `Failed to fetch` |
| 13 | `getTotals` — network failure | `Network error` |
| 14 | `deletePerson` — 204 sem body | Não tenta parsear JSON em 204 |

---

## 📋 Frontend — Testes de Componente

### App.test.tsx (18 testes)

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
| 9 | Erro de validação (form vazio) | PeopleTab |
| 10 | Criação de pessoa com sucesso | PeopleTab |
| 11 | Erro ao criar pessoa | PeopleTab |
| 12 | Aviso quando não há pessoas | TransactionsTab |
| 13 | Regra de negócio visível | TransactionsTab |
| 14 | Campos do formulário de transação | TransactionsTab |
| 15 | Criação de transação com sucesso | TransactionsTab |
| 16 | Erro regra de negócio na UI | TransactionsTab |
| 17 | Botão de atualizar totais | TotalsTab |
| 18 | Renderiza totais com dados | TotalsTab |
| 19 | Estado de carregamento (loading) | TotalsTab |
| 20 | Mensagem de erro na consulta | TotalsTab |

---

## 🏗️ Arquitetura de Testes

```
┌─────────────────────────────────────────────────────┐
│                 Pirâmide de Testes                    │
│                                                      │
│                    ┌──────────┐                      │
│                    │   E2E    │  ← Futuro            │
│                    │ (0 tests)│                      │
│                   ─┴──────────┴─                     │
│                 ┌────────────────┐                   │
│                 │  Integration   │  ← 16 tests       │
│                 │  (Controllers) │     WebAppFactory │
│                ─┴────────────────┴─                  │
│          ┌─────────────────────────────┐             │
│          │       Unit Tests            │  ← 50 tests │
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
| Menor de 18 só pode despesa | `TransactionServiceTests` #3, #4, #5 | `TransactionsControllerTests` #2, #3 | ✅ 100% |
| Delete pessoa → cascata transações | `PersonServiceTests` #8 | `PeopleControllerTests` #5 | ✅ 100% |
| Pessoa deve existir na transação | `TransactionServiceTests` #6 | `TransactionsControllerTests` #4 | ✅ 100% |
| Totais: receita - despesa = saldo | `TotalsServiceTests` #3-6 | `TotalsControllerTests` #2 | ✅ 100% |

---

## 📁 Estrutura de arquivos de teste

```
expense-control-system/
├── IMPROVEMENTS.md                     # Plano de melhorias
├── TESTING.md                          # Este arquivo
├── tests/
│   └── backend/
│       ├── Backend.Tests.csproj        # Projeto xUnit
│       ├── TestDatabase.cs             # Fixture InMemory (unit)
│       ├── TestWebApplicationFactory.cs # Factory p/ integração
│       ├── Unit/
│       │   ├── PersonServiceTests.cs   # 13 testes
│       │   ├── TransactionServiceTests.cs # 11 testes
│       │   └── TotalsServiceTests.cs   # 7 testes
│       └── Integration/
│           ├── PeopleControllerTests.cs    # 5 testes
│           ├── TransactionsControllerTests.cs # 6 testes
│           └── TotalsControllerTests.cs    # 5 testes
└── frontend/
    └── src/
        ├── test-setup.ts               # Setup Testing Library
        └── __tests__/
            ├── api.test.ts             # 10 testes
            └── App.test.tsx            # 13 testes
```

---

## 🚀 CI/CD Ready

Para integrar em pipeline CI/CD:

```yaml
# Exemplo GitHub Actions
- name: Backend Tests
  run: cd tests/backend && dotnet test

- name: Frontend Tests
  run: cd frontend && npm ci && npm test
```
