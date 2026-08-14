# 🔌 API Reference — Expense Control System

> Documentação completa dos endpoints REST da API.
> Base URL: `http://localhost:5000/api`

---

## 📋 Índice

- [Convenções](#-convenções)
- [Pessoas](#-pessoas)
  - [Criar pessoa](#criar-pessoa)
  - [Listar pessoas](#listar-pessoas)
  - [Remover pessoa](#remover-pessoa)
- [Transações](#-transações)
  - [Criar transação](#criar-transação)
  - [Listar transações](#listar-transações)
- [Totais](#-totais)
  - [Consultar totais](#consultar-totais)
- [Erros](#-erros)

---

## 📐 Convenções

| Item | Convenção |
|------|-----------|
| **Content-Type** | `application/json` |
| **Encoding** | UTF-8 |
| **Idioma** | Campos e mensagens em português |
| **Formato de data** | N/A (não há campos de data neste sistema) |
| **Formato monetário** | `decimal` (ex: `1500.00`, `99.90`) |
| **Tipos de transação** | `"receita"` ou `"despesa"` (strings, não enum) |

### Códigos de status HTTP utilizados

| Código | Significado | Quando ocorre |
|--------|-------------|---------------|
| `200 OK` | Sucesso com corpo de resposta | GET com dados |
| `201 Created` | Recurso criado com sucesso | POST bem-sucedido |
| `204 No Content` | Sucesso sem corpo de resposta | DELETE bem-sucedido |
| `400 Bad Request` | Erro de validação ou regra de negócio | Dados inválidos, menor criando receita |
| `404 Not Found` | Recurso não encontrado | DELETE em ID inexistente |

---

## 👥 Pessoas

### Criar pessoa

Cria uma nova pessoa no sistema.

```http
POST /api/people
Content-Type: application/json
```

**Request body:**

```json
{
  "name": "João Silva",
  "age": 30
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|-------------|-----------|
| `name` | `string` | ✅ Sim | 1–100 caracteres |
| `age` | `int` | ✅ Sim | 0–150 |

**Response `201 Created`:**

```json
{
  "id": 1,
  "name": "João Silva",
  "age": 30
}
```

**Exemplo curl:**

```bash
curl -X POST http://localhost:5000/api/people \
  -H "Content-Type: application/json" \
  -d '{"name":"João Silva","age":30}'
```

---

### Listar pessoas

Retorna todas as pessoas cadastradas, ordenadas alfabeticamente por nome.

```http
GET /api/people
```

**Response `200 OK`:**

```json
[
  { "id": 2, "name": "Ana Costa", "age": 25 },
  { "id": 1, "name": "João Silva", "age": 30 },
  { "id": 3, "name": "Pedro Santos", "age": 15 }
]
```

> 💡 Pessoas sem transações também aparecem na listagem. O campo `age` é usado pelo frontend para indicar visualmente menores de idade (🔞).

**Exemplo curl:**

```bash
curl http://localhost:5000/api/people
```

---

### Buscar pessoa

Retorna uma pessoa pelo ID.

```http
GET /api/people/{id}
```

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `id` | `int` (path) | Identificador da pessoa |

**Response `200 OK`:**

```json
{ "id": 1, "name": "João Silva", "age": 30 }
```

**Response `404 Not Found`:**

```json
{ "message": "Pessoa não encontrada." }
```

**Exemplo curl:**

```bash
curl http://localhost:5000/api/people/1
```

---

### Remover pessoa

Remove uma pessoa pelo ID. **Todas as transações associadas são removidas em cascata.**

```http
DELETE /api/people/{id}
```

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `id` | `int` (path) | Identificador da pessoa |

**Response `204 No Content`:**

Sem corpo. Pessoa e transações removidas com sucesso.

**Response `404 Not Found`:**

```json
{ "message": "Pessoa não encontrada." }
```

**Exemplo curl:**

```bash
curl -X DELETE http://localhost:5000/api/people/1
```

---

## 💳 Transações

### Criar transação

Cria uma nova transação (receita ou despesa) vinculada a uma pessoa existente.

```http
POST /api/transactions
Content-Type: application/json
```

**Request body:**

```json
{
  "description": "Salário",
  "amount": 5000.00,
  "type": "receita",
  "personId": 1
}
```

| Campo | Tipo | Obrigatório | Validação |
|-------|------|-------------|-----------|
| `description` | `string` | ✅ Sim | 1–200 caracteres |
| `amount` | `decimal` | ✅ Sim | > 0 |
| `type` | `string` | ✅ Sim | `"receita"` ou `"despesa"` |
| `personId` | `int` | ✅ Sim | Deve corresponder a uma pessoa existente |

**Response `201 Created`:**

```json
{
  "id": 1,
  "description": "Salário",
  "amount": 5000.00,
  "type": "receita",
  "personId": 1,
  "personName": "João Silva"
}
```

**Response `400 Bad Request` (pessoa não existe):**

```json
{ "message": "A pessoa informada não existe no cadastro." }
```

**Response `400 Bad Request` (menor criando receita):**

```json
{ "message": "Menores de 18 anos não podem cadastrar receitas, apenas despesas." }
```

> ⚠️ **Regra de negócio:** Esta validação ocorre na camada de serviço (`TransactionService`), não no Controller. O Controller apenas captura `ArgumentException` e converte para HTTP 400.

**Exemplos curl:**

```bash
# ✅ Adulto criando receita (permitido)
curl -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"description":"Salário","amount":5000,"type":"receita","personId":1}'

# ✅ Menor criando despesa (permitido)
curl -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"description":"Lanche","amount":25.50,"type":"despesa","personId":3}'

# ❌ Menor criando receita (BLOQUEADO)
curl -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"description":"Mesada","amount":100,"type":"receita","personId":3}'
```

---

### Listar transações

Retorna todas as transações cadastradas, ordenadas da mais recente para a mais antiga (ID decrescente).

```http
GET /api/transactions
```

**Response `200 OK`:**

```json
[
  {
    "id": 3,
    "description": "Lanche",
    "amount": 25.50,
    "type": "despesa",
    "personId": 3,
    "personName": "Pedro Santos"
  },
  {
    "id": 2,
    "description": "Aluguel",
    "amount": 1500.00,
    "type": "despesa",
    "personId": 1,
    "personName": "João Silva"
  },
  {
    "id": 1,
    "description": "Salário",
    "amount": 5000.00,
    "type": "receita",
    "personId": 1,
    "personName": "João Silva"
  }
]
```

**Exemplo curl:**

```bash
curl http://localhost:5000/api/transactions
```

---

### Buscar transação

Retorna uma transação pelo ID.

```http
GET /api/transactions/{id}
```

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `id` | `int` (path) | Identificador da transação |

**Response `200 OK`:**

```json
{
  "id": 1,
  "description": "Salário",
  "amount": 5000.00,
  "type": "receita",
  "personId": 1,
  "personName": "João Silva"
}
```

**Response `404 Not Found`:**

```json
{ "message": "Transação não encontrada." }
```

**Exemplo curl:**

```bash
curl http://localhost:5000/api/transactions/1
```

---

## 📊 Totais

### Consultar totais

Retorna o resumo financeiro de cada pessoa e o total geral consolidado.

```http
GET /api/totals
```

**Response `200 OK`:**

```json
{
  "peopleTotals": [
    {
      "personId": 2,
      "personName": "Ana Costa",
      "totalIncome": 3000.00,
      "totalExpense": 800.00,
      "balance": 2200.00
    },
    {
      "personId": 1,
      "personName": "João Silva",
      "totalIncome": 5000.00,
      "totalExpense": 1500.00,
      "balance": 3500.00
    },
    {
      "personId": 3,
      "personName": "Pedro Santos",
      "totalIncome": 0,
      "totalExpense": 25.50,
      "balance": -25.50
    }
  ],
  "grandTotalIncome": 8000.00,
  "grandTotalExpense": 2325.50,
  "grandBalance": 5674.50
}
```

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `peopleTotals[].personId` | `int` | ID da pessoa |
| `peopleTotals[].personName` | `string` | Nome da pessoa |
| `peopleTotals[].totalIncome` | `decimal` | Soma de todas as receitas da pessoa |
| `peopleTotals[].totalExpense` | `decimal` | Soma de todas as despesas da pessoa |
| `peopleTotals[].balance` | `decimal` | Saldo = receitas − despesas |
| `grandTotalIncome` | `decimal` | Soma das receitas de todas as pessoas |
| `grandTotalExpense` | `decimal` | Soma das despesas de todas as pessoas |
| `grandBalance` | `decimal` | Saldo geral líquido |

> 💡 Pessoas sem transações são listadas com todos os valores zerados.  
> 💡 `balance` e `grandBalance` podem ser negativos (quando despesas > receitas).  
> 💡 A ordenação é alfabética por nome da pessoa.

**Exemplo curl:**

```bash
curl http://localhost:5000/api/totals
```

---

## ⚠️ Erros

### Formato de erro padronizado

Todos os erros seguem o formato:

```json
{
  "message": "Descrição do erro em português."
}
```

### Erros possíveis por endpoint

| Endpoint | Método | Erro | Gatilho |
|----------|--------|------|---------|
| `/api/people` | `POST` | 400 | Nome vazio, idade fora do range, JSON malformado |
| `/api/people/{id}` | `DELETE` | 404 | ID não corresponde a nenhuma pessoa |
| `/api/transactions` | `POST` | 400 | Pessoa não existe, menor + receita, valor ≤ 0, tipo inválido |
| `/api/transactions` | `POST` | 400 | JSON malformado ou campos obrigatórios ausentes |

### Erros de validação do ModelState (ASP.NET Core)

Quando a validação ocorre no nível do Model Binding (antes de chegar ao Controller), o ASP.NET Core retorna um `400` com a estrutura padrão de `ValidationProblemDetails`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["O nome é obrigatório."],
    "Age": ["A idade deve estar entre 0 e 150."]
  }
}
```

> 💡 **Melhoria planejada (ver [IMPROVEMENTS.md](IMPROVEMENTS.md)):** unificar o formato de erro para que todos usem `{ "message": "..." }`. Atualmente erros de validação do ModelState usam o formato padrão do ASP.NET Core, enquanto erros de regra de negócio usam o formato simplificado.
