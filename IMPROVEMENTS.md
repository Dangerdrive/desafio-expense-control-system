# 📋 Plano de Melhorias — Expense Control System

> Documento vivo de planejamento de qualidade. Itens concluídos são marcados com ✅.

---

## 📊 Status geral

| Área | Progresso | Status |
|------|-----------|--------|
| Testes unitários (backend) | 45/45 | ✅ Concluído |
| Testes de integração (backend) | 41/41 | ✅ Concluído |
| Testes API (frontend) | 16/16 | ✅ Concluído |
| Testes componente (frontend) | 22/22 | ✅ Concluído |
| Testes de contrato (frontend ↔ backend) | 4/4 | ✅ Concluído |
| Repository Pattern | Implementado | ✅ Concluído |
| Refatoração Services (IRepository) | Implementado | ✅ Concluído |
| Documentação (README, SETUP, TESTING, API_REFERENCE) | Criada | ✅ Concluído |
| Melhorias de código (QA-Level) | 5/5 | ✅ Concluído |
| Melhorias de frontend (UX) | 5/5 | ✅ Concluído |
| Testes E2E | 5/5 | ✅ Concluído |
| CI Pipeline (GitHub Actions) | Implementado | ✅ Concluído |
| Campo Data em transações + filtro/ordenação | Implementado | ✅ Concluído |
| Editar/Excluir transação (CRUD completo) | Implementado | ✅ Concluído |

---

## 🎯 Objetivo

Evoluir o MVP funcional para **qualidade de produção**: testado, robusto, documentado e com cobertura de testes em múltiplos níveis.

---

## 🏗️ Pirâmide de Testes (visão alvo)

```
                    ┌──────────────────────────────┐
                    │         E2E (end-to-end)      │  ← ✅ 5 testes (Concluído)
                    │    Fluxo completo no browser   │     Playwright
                    ├──────────────────────────────┤
                    │   INTEGRATION TESTS           │  ← ✅ 41 testes (Concluído)
                    │   API controllers HTTP        │     WebApplicationFactory
                    ├──────────────────────────────┤
                    │   UNIT TESTS                  │  ← ✅ 87 testes (Concluído)
                    │   Services + API + Comps      │     xUnit + Vitest
                    └──────────────────────────────┘
                              ▲
                              │ Maior quantidade de testes
                              │ (pirâmide de testes)
```

---

## ✅ Ação 1: Testes Unitários — Backend (.NET) — CONCLUÍDO

| Serviço | Cenários de teste | Qtde. | Status |
|---------|------------------|-------|--------|
| `PersonService` | Criar, listar vazio, listar com dados, deletar existente/inexistente, cascata, verificar existência, obter idade | 12 | ✅ |
| `TransactionService` | Adulto receita/despesa, menor receita/despesa, boundary 17 anos, pessoa inexistente, valor grande, precisão decimal | 11 | ✅ |
| `TotalsService` | Sem pessoas, sem transações, cenário completo, saldo negativo, só receitas, só despesas, ordenação | 7 | ✅ |
| `ExceptionHandlingMiddleware` | Exceção não tratada → 500 padronizado, resposta de sucesso não alterada | 2 | ✅ |

**Ferramentas:** xUnit + EF Core InMemory

---

## ✅ Ação 2: Testes de Integração — Backend (.NET) — CONCLUÍDO

| Controller | Cenários | Qtde. | Status |
|-----------|----------|-------|--------|
| `PeopleController` | POST 201, GET 200, GET por ID, DELETE 204/404, cascata, validações, erro unificado | 12 | ✅ |
| `TransactionsController` | Adulto receita, menor receita→400, menor despesa, pessoa inválida, GET 200, GET por ID, personName | 14 | ✅ |
| `TotalsController` | Estrutura JSON, cálculos corretos, campos obrigatórios | 3 | ✅ |

**Ferramentas:** `WebApplicationFactory` + xUnit

---

## ✅ Ação 3: Testes Frontend — CONCLUÍDO

| Alvo | Cenários | Qtde. | Status |
|------|----------|-------|--------|
| `api/index.ts` | Sucesso, erro 400, erro 500, 204 No Content, regra de menor, network failures | 14 | ✅ |
| Componentes React | Renderização, navegação entre abas, formulários, estados vazios, loading/erro | 20 | ✅ |

**Ferramentas:** Vitest + Testing Library + mock fetch

---

## 🔜 Ação 4: Melhorias de Código (QA-Level)

### Backend

| # | Melhoria | Descrição | Impacto | Esforço |
|---|----------|-----------|---------|---------|
| 1 | Validação de ModelState | Middleware global para padronizar respostas de erro | 🔴 Alta | 2h |
| 2 | Tratamento global de exceções | Middleware `ExceptionHandler` para erros 500 | 🔴 Alta | 1h |
| 3 | Logging estruturado | `ILogger` nos services para auditoria | 🟡 Média | 3h |
| 4 | Health Check | Endpoint `/health` para monitoramento | 🟢 Baixa | 30min |
| 5 | Versionamento da API | Prefixo `/api/v1/` nos endpoints | 🟢 Baixa | 1h |

### Frontend

| # | Melhoria | Descrição | Impacto | Esforço |
|---|----------|-----------|---------|---------|
| 1 | Loading states | Spinners/skeleton durante chamadas API | 🔴 Alta | 2h |
| 2 | Error boundary | Componente que captura erros React e mostra fallback | 🔴 Alta | 1h |
| 3 | Confirmação de delete | Modal estilizado no lugar de `window.confirm` | 🟡 Média | 2h |
| 4 | Máscara de valor monetário | Input formatado como moeda (R$) | 🟡 Média | 2h |
| 5 | Acessibilidade | ARIA labels, navegação por teclado, contraste | 🟡 Média | 3h |

---

## 🔜 Ação 5: Testes E2E (Futuro)

| Ferramenta | Cenários |
|-----------|----------|
| **Playwright** (recomendado) | Fluxo completo: criar pessoa → criar transação → verificar totais → deletar pessoa |
| Ou **Cypress** | Mesmos cenários, sintaxe diferente |

---

## 📈 Métricas de Qualidade

| Métrica | Alvo | Atual |
|---------|------|-------|
| Cobertura de testes unitários (backend) | > 85% | ✅ 100% das regras cobertas |
| Cobertura de testes unitários (frontend) | > 70% | ✅ API + componentes cobertos |
| Testes de integração passando | 100% | ✅ 29/29 |
| Regras de negócio cobertas por teste | 100% | ✅ 4/4 regras |
| Build sem warnings | 0 warnings | ✅ Backend + Frontend |
| Documentação atualizada | Completa | ✅ 5 documentos |

---

## ⏱️ Ordem de execução

1. ✅ Criar plano (este documento)
2. ✅ Setup: projeto de teste backend (xUnit)
3. ✅ Testes unitários backend (services)
4. ✅ Testes de integração backend (controllers)
5. ✅ Setup: Vitest + Testing Library no frontend
6. ✅ Testes unitários frontend (API + componentes)
7. ✅ Documentação final (README, SETUP, TESTING, API_REFERENCE)
8. ✅ Repository Pattern refactor
9. 🔜 Melhorias QA-Level (backend + frontend)
10. 🔜 Testes E2E (Playwright)

---

<p align="center">
  <sub>Última atualização: 2026-07-23</sub>
</p>
