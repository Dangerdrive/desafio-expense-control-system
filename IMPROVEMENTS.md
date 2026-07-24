# 📋 Plano de Melhorias & Testes — Expense Control System

> Documento de planejamento: todas as melhorias propostas, justificativas e plano de execução.

---

## 🎯 Objetivo

Transformar o MVP funcional em um software **com qualidade de produção**: testado, robusto,
documentado e com cobertura de testes em múltiplos níveis.

---

## 📊 Matriz de Qualidade: Níveis de Teste

```
                    ┌──────────────────────────────┐
                    │         E2E (end-to-end)      │  ← Futuro: Playwright/Cypress
                    │    Fluxo completo no browser   │
                    ├──────────────────────────────┤
                    │   INTEGRATION TESTS           │  ← ✅ Vamos implementar
                    │   API controllers + DB real   │     WebApplicationFactory
                    ├──────────────────────────────┤
                    │   UNIT TESTS                  │  ← ✅ Vamos implementar
                    │   Services, regras de negócio  │     xUnit + Moq + InMemory
                    └──────────────────────────────┘
                              ▲
                              │ Maior quantidade de testes
                              │ (pirâmide de testes)
```

---

## 🗺️ Ação 1: Testes Unitários — Backend (.NET)

### O que testar

| Serviço | Cenários de teste | Quantidade |
|---------|------------------|------------|
| `PersonService` | Criar pessoa válida, criar com dados inválidos, listar pessoas, deletar existente, deletar inexistente, verificar cascata | ~6 |
| `TransactionService` | Criar despesa adulto, criar receita adulto, bloquear receita para menor, permitir despesa para menor, pessoa inexistente, valor inválido | ~7 |
| `TotalsService` | Totais com pessoas sem transação, com transações mistas, total geral zerado, total geral com valores | ~5 |

### Ferramentas

- **xUnit** — framework de testes
- **Moq** — mocking do DbContext
- **EF Core InMemory** — banco em memória para simular o SQLite

### Por que InMemory + Moq?

Usamos **InMemory database** para os testes de serviço (é mais realista que mockar DbSet manualmente).
Usamos **Moq** apenas para cenários onde precisamos simular falhas de infraestrutura.

---

## 🗺️ Ação 2: Testes de Integração — Backend (.NET)

### O que testar

| Controller | Cenários | Tipo |
|-----------|----------|------|
| `PeopleController` | POST → 201, GET → 200 com lista, DELETE → 204, DELETE inexistente → 404 | HTTP |
| `TransactionsController` | POST válido → 201, POST menor+receita → 400, POST pessoa inválida → 400, GET → 200 | HTTP |
| `TotalsController` | GET → 200 com estrutura correta, GET sem dados → 200 com arrays vazios | HTTP |

### Ferramentas

- **Microsoft.AspNetCore.Mvc.Testing** — `WebApplicationFactory` para subir a API real em memória
- **xUnit** — executor

---

## 🗺️ Ação 3: Testes Unitários — Frontend (React)

### O que testar

| Alvo | Cenários |
|------|----------|
| `api/index.ts` | Mock do fetch: sucesso, erro 400, erro 500, 204 No Content |
| `<PeopleTab>` | Renderiza formulário, cria pessoa, exibe erro, exibe lista |
| `<TransactionsTab>` | Renderiza select de pessoas, bloqueia form sem pessoa, exibe regra |
| `<TotalsTab>` | Renderiza totais zerados, renderiza cards de total geral |

### Ferramentas

- **Vitest** — test runner (nativo do ecossistema Vite)
- **@testing-library/react** — renderização e queries
- **@testing-library/jest-dom** — matchers semânticos
- **msw** (opcional) — mock de API

### Por que Vitest e não Jest?

- Vitest é nativo do ecossistema Vite (mesma config, mesma velocidade)
- Sem necessidade de configurar Babel ou transformações
- Compatível com a API do Jest

---

## 🗺️ Ação 4: Melhorias de Código (QA-Level)

### 4.1 Backend

| Melhoria | Descrição | Impacto |
|----------|-----------|---------|
| Validação de ModelState | Middleware global para respostas de validação padronizadas | 🔴 Alta |
| Tratamento global de exceções | Middleware `ExceptionHandler` para erros 500 | 🔴 Alta |
| Logging estruturado | `ILogger` nos services para auditoria | 🟡 Média |
| Health Check | Endpoint `/health` para monitoramento | 🟢 Baixa |
| Versionamento da API | Prefixo `/api/v1/` nos endpoints | 🟢 Baixa |

### 4.2 Frontend

| Melhoria | Descrição | Impacto |
|----------|-----------|---------|
| Loading states | Spinners/skeleton durante chamadas API | 🔴 Alta |
| Error boundary | Componente que captura erros React e mostra fallback | 🔴 Alta |
| Confirmação de delete | Modal estilizado no lugar de `window.confirm` | 🟡 Média |
| Máscara de valor monetário | Input formatado como moeda (R$) | 🟡 Média |
| Acessibilidade | ARIA labels, navegação por teclado | 🟡 Média |

---

## 📈 Métricas Alvo de Qualidade

| Métrica | Alvo |
|---------|------|
| Cobertura de testes unitários (backend) | > 85% |
| Cobertura de testes unitários (frontend) | > 70% |
| Testes de integração passando | 100% |
| Regras de negócio cobertas por teste | 100% (todas as 3 regras) |

---

## 📁 Estrutura final pós-melhorias

```
expense-control-system/
├── IMPROVEMENTS.md              ← Este arquivo (plano)
├── TESTING.md                   ← Documentação de testes (a ser criado)
├── backend/
│   ├── Backend.sln              ← Solution file
│   ├── Backend.csproj           ← Projeto principal
│   └── Backend.Tests/           ← NOVO: projeto de testes
│       ├── Backend.Tests.csproj
│       ├── Unit/
│       │   ├── PersonServiceTests.cs
│       │   ├── TransactionServiceTests.cs
│       │   └── TotalsServiceTests.cs
│       └── Integration/
│           ├── PeopleControllerTests.cs
│           ├── TransactionsControllerTests.cs
│           └── TotalsControllerTests.cs
└── frontend/
    ├── vitest.config.ts         ← NOVO: config do Vitest
    └── src/
        └── __tests__/           ← NOVO: pasta de testes
            ├── api.test.ts
            ├── PeopleTab.test.tsx
            ├── TransactionsTab.test.tsx
            └── TotalsTab.test.tsx
```

---

## ⏱️ Ordem de execução

1. ✅ Criar plano (este documento)
2. 🔜 Setup: projeto de teste backend (xUnit)
3. 🔜 Testes unitários backend (services)
4. 🔜 Testes de integração backend (controllers)
5. 🔜 Setup: Vitest + Testing Library no frontend
6. 🔜 Testes unitários frontend (API + componentes)
7. 🔜 Documentação final (TESTING.md)
8. 🔜 Executar suite completa e validar
