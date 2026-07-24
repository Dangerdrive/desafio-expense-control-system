# 🛠 Guia de Setup — Sistema de Controle de Gastos Residenciais

> Guia prático para instalar dependências, executar o projeto e resolver problemas comuns.

---

## 📋 Índice

- [Pré-requisitos](#-pré-requisitos)
- [Instalação](#-instalação)
  - [.NET SDK 8.0](#1-net-sdk-80)
  - [Node.js](#2-nodejs)
- [Executando o projeto](#-executando-o-projeto)
  - [Backend](#backend)
  - [Frontend](#frontend)
  - [Executar ambos simultaneamente](#executar-ambos-simultaneamente)
- [Verificação rápida](#-verificação-rápida)
- [Troubleshooting](#-troubleshooting)
- [Recriando o projeto do zero](#-recriando-o-projeto-do-zero)

---

## 📋 Pré-requisitos

| Ferramenta | Versão Mínima | Como verificar |
|-----------|---------------|----------------|
| **.NET SDK** | 8.0 | `dotnet --version` |
| **Node.js** | 18+ | `node --version` |
| **npm** | 9+ | `npm --version` |

---

## 🔧 Instalação

### 1. .NET SDK 8.0

```bash
# Ubuntu/Debian (via snap — recomendado)
sudo snap install dotnet-sdk --channel=8.0/stable --classic

# Ou via script oficial da Microsoft (não requer sudo)
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc
source ~/.bashrc

# Ou via apt (Ubuntu 24.04+)
sudo apt update && sudo apt install -y dotnet-sdk-8.0
```

Após instalar, verifique:

```bash
dotnet --version  # Deve exibir 8.0.x
```

### 2. Node.js

```bash
# Via nvm (recomendado — permite múltiplas versões)
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.0/install.sh | bash
source ~/.bashrc
nvm install 22
nvm use 22

# Ou via NodeSource (Ubuntu/Debian)
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
sudo apt install -y nodejs
```

Após instalar, verifique:

```bash
node --version   # Deve ser ≥ 18
npm --version    # Deve ser ≥ 9
```

---

## 🚀 Executando o projeto

### Backend

```bash
cd backend

# Restaurar pacotes NuGet
dotnet restore

# Compilar (deve resultar em 0 warnings, 0 errors)
dotnet build

# Executar a API
dotnet run
```

| Serviço | URL |
|---------|-----|
| API REST | `http://localhost:5000` |
| Swagger UI | `http://localhost:5000/swagger` |

O banco de dados SQLite (`ExpenseControl.db`) é criado automaticamente no primeiro `dotnet run`. Se precisar resetar o banco, basta deletar este arquivo e reiniciar.

### Frontend

```bash
cd frontend

# Instalar dependências
npm install

# Iniciar servidor de desenvolvimento
npm run dev
```

| Serviço | URL |
|---------|-----|
| App React | `http://localhost:5173` |

### Executar ambos simultaneamente

Abra dois terminais:

```bash
# Terminal 1 — Backend
cd backend && dotnet run

# Terminal 2 — Frontend
cd frontend && npm run dev
```

Ou use um único terminal com processos em background:

```bash
cd backend && dotnet run &
cd ../frontend && npm run dev
```

---

## 🧪 Verificação rápida

Com o backend rodando, teste os endpoints principais:

```bash
# 1. Criar uma pessoa adulta
curl -s -X POST http://localhost:5000/api/people \
  -H "Content-Type: application/json" \
  -d '{"name":"João","age":30}'

# 2. Criar uma pessoa menor de idade
curl -s -X POST http://localhost:5000/api/people \
  -H "Content-Type: application/json" \
  -d '{"name":"Maria","age":15}'

# 3. Tentar criar receita para menor (DEVE FALHAR com 400)
curl -s -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"description":"Mesada","amount":100,"type":"receita","personId":2}'

# 4. Criar despesa para menor (DEVE FUNCIONAR)
curl -s -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"description":"Lanche","amount":25.50,"type":"despesa","personId":2}'

# 5. Criar receita para adulto
curl -s -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"description":"Salário","amount":5000,"type":"receita","personId":1}'

# 6. Consultar totais
curl -s http://localhost:5000/api/totals | python3 -m json.tool

# 7. Listar pessoas
curl -s http://localhost:5000/api/people | python3 -m json.tool
```

### Resultado esperado

O passo 3 deve retornar:

```json
{"message":"Menores de 18 anos não podem cadastrar receitas, apenas despesas."}
```

O passo 6 deve mostrar os totais calculados corretamente para cada pessoa e o total geral.

---

## ⚠️ Troubleshooting

| Problema | Causa provável | Solução |
|----------|---------------|---------|
| `dotnet: command not found` | .NET SDK não instalado ou não está no PATH | Instalar via snap ou script (ver seção [Instalação](#1-net-sdk-80)) |
| `npm: command not found` | Node.js não instalado | Instalar via nvm (ver seção [Instalação](#2-nodejs)) |
| Erro de CORS no console do navegador | Backend não está rodando ou porta errada | Verificar se `dotnet run` está ativo em `:5000` |
| `ERR_CONNECTION_REFUSED` no frontend | Backend não iniciado | Executar `cd backend && dotnet run` primeiro |
| Porta 5000 já em uso | Outro processo usando a porta | `lsof -i :5000` para identificar; ou alterar em `launchSettings.json` + `api/index.ts` |
| Porta 5173 já em uso | Outra instância do Vite rodando | `lsof -i :5173` e `kill <PID>` |
| Banco de dados corrompido | `ExpenseControl.db` em estado inconsistente | Deletar `backend/ExpenseControl.db` e reiniciar o backend |
| `dotnet build` falha com erro de pacote | Cache NuGet corrompido | `dotnet nuget locals all --clear && dotnet restore` |
| Testes falham ao executar `dotnet test` | Projeto de testes não compilou | Executar `dotnet build` primeiro na solution de testes |

### Como matar processos em portas específicas

```bash
# Encontrar PID da porta 5000
lsof -i :5000
# ou
ss -tlnp | grep 5000

# Matar o processo
kill -9 <PID>
```

---

## 🏗️ Recriando o projeto do zero

Se você quiser entender cada passo da construção deste projeto, eis o roteiro resumido:

1. **Backend scaffold:** `dotnet new webapi -n Backend --no-https -o backend`
2. **Pacotes NuGet:** `dotnet add package Microsoft.EntityFrameworkCore.Sqlite` + `Microsoft.EntityFrameworkCore.Design`
3. **Models:** Criar `Person.cs` e `Transaction.cs` com data annotations
4. **DbContext:** Criar `AppDbContext.cs` com `OnModelCreating` para cascade delete
5. **DTOs:** Criar `Dtos.cs` com todos os contratos de entrada/saída
6. **Repository:** Criar `IRepository⟨T⟩` e `Repository⟨T⟩` (genérico)
7. **Services:** `PersonService`, `TransactionService` (regra < 18), `TotalsService`
8. **Controllers:** `PeopleController`, `TransactionsController`, `TotalsController`
9. **Program.cs:** Configurar DI, CORS, Swagger, `EnsureCreated()`
10. **Frontend scaffold:** `npm create vite@latest frontend -- --template react-ts`
11. **Tipos + API:** Criar interfaces TypeScript e funções fetch
12. **App.tsx:** Componente principal com 3 abas e lógica de estado
13. **Estilos:** CSS para layout, tabelas, badges, cards de totais

Os arquivos fonte no repositório contêm comentários detalhados explicando o *porquê* de cada decisão de design. Consulte-os para entender a fundo a arquitetura.

namespace Backend.Models;

public class Person
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0, 150)]
    public int Age { get; set; }

    // Relacionamento: uma pessoa tem muitas transações
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
```

Criar `backend/Models/Transaction.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class Transaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [RegularExpression("^(receita|despesa)$")]
    public string Type { get; set; } = string.Empty;  // "receita" ou "despesa"

    [Required]
    public int PersonId { get; set; }

    [ForeignKey("PersonId")]
    public Person? Person { get; set; }
}
```

---

### Passo 4: Backend — Criar o DbContext

Criar `backend/Data/AppDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext : DbContext
{
    public DbSet<Models.Person> People { get; set; } = null!;
    public DbSet<Models.Transaction> Transactions { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configura exclusão em cascata: deletar pessoa → deleta transações
        modelBuilder.Entity<Models.Person>()
            .HasMany(p => p.Transactions)
            .WithOne(t => t.Person)
            .HasForeignKey(t => t.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

### Passo 5: Backend — Criar os DTOs

Criar `backend/DTOs/Dtos.cs` com os contratos de entrada/saída da API:
- `CreatePersonDto` (Name, Age)
- `PersonResponseDto` (Id, Name, Age)
- `CreateTransactionDto` (Description, Amount, Type, PersonId)
- `TransactionResponseDto` (Id, Description, Amount, Type, PersonId, PersonName)
- `PersonTotalsDto` (PersonId, PersonName, TotalIncome, TotalExpense, Balance)
- `TotalsResponseDto` (lista de PersonTotals + grand totals)

---

### Passo 6: Backend — Criar os Services (lógica de negócio)

Criar `backend/Services/PersonService.cs` com:
- `CreateAsync` → insere pessoa no banco
- `GetAllAsync` → lista todas ordenadas por nome
- `DeleteAsync` → remove pessoa (cascata automática pelo EF Core)
- `ExistsAsync` / `GetAgeAsync` → helpers de validação

Criar `backend/Services/TransactionService.cs` com:
- `CreateAsync` → valida se pessoa existe + **regra: <18 anos = só despesa**
- `GetAllAsync` → lista todas com nome da pessoa (via Include)

Criar `backend/Services/TotalsService.cs` com:
- `GetTotalsAsync` → para cada pessoa, soma receitas e despesas; consolida total geral

---

### Passo 7: Backend — Criar os Controllers

Criar `backend/Controllers/PeopleController.cs`:
- `POST /api/people` → criar pessoa
- `GET /api/people` → listar pessoas
- `DELETE /api/people/{id}` → remover (cascata)

Criar `backend/Controllers/TransactionsController.cs`:
- `POST /api/transactions` → criar transação (com try/catch para regra de menor)
- `GET /api/transactions` → listar transações
- ⚠️ Sem endpoints de edição/exclusão (conforme especificação)

Criar `backend/Controllers/TotalsController.cs`:
- `GET /api/totals` → consultar totais por pessoa + geral

---

### Passo 8: Backend — Configurar Program.cs

```csharp
using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=ExpenseControl.db"));

// Services (injeção de dependência)
builder.Services.AddScoped<PersonService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<TotalsService>();

// CORS para o frontend (Vite na porta 5173)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Criar banco automaticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.MapControllers();
app.Run();
```

**Pontos-chave:**
- `EnsureCreated()` → cria o banco e tabelas sem precisar de migrations manuais
- CORS configurado para `localhost:5173` (porta padrão do Vite)
- Porta alterada para `5000` no `Properties/launchSettings.json`

---

### Passo 9: Frontend — Scaffold com Vite

```bash
# Voltar para a raiz do projeto
cd ..

# Criar projeto React + TypeScript com Vite
npm create vite@latest frontend -- --template react-ts

cd frontend
npm install
```

---

### Passo 10: Frontend — Criar tipos TypeScript

Criar `frontend/src/types/index.ts` com as interfaces que espelham os DTOs do backend:

```typescript
export interface Person {
  id: number;
  name: string;
  age: number;
}

export interface CreatePersonDto {
  name: string;
  age: number;
}

export interface Transaction {
  id: number;
  description: string;
  amount: number;
  type: 'receita' | 'despesa';
  personId: number;
  personName: string;
}

export interface CreateTransactionDto {
  description: string;
  amount: number;
  type: 'receita' | 'despesa';
  personId: number;
}

export interface PersonTotals {
  personId: number;
  personName: string;
  totalIncome: number;
  totalExpense: number;
  balance: number;
}

export interface TotalsResponse {
  peopleTotals: PersonTotals[];
  grandTotalIncome: number;
  grandTotalExpense: number;
  grandBalance: number;
}
```

---

### Passo 11: Frontend — Criar camada de API

Criar `frontend/src/api/index.ts` com funções fetch para cada endpoint:

```typescript
const API_BASE = 'http://localhost:5000/api';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${url}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({ message: 'Erro desconhecido' }));
    throw new Error(err.message);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

export const getPeople    = ()          => request<Person[]>('/people');
export const createPerson = (dto: CreatePersonDto) => request<Person>('/people', { method: 'POST', body: JSON.stringify(dto) });
export const deletePerson = (id: number) => request<void>(`/people/${id}`, { method: 'DELETE' });

export const getTransactions    = ()          => request<Transaction[]>('/transactions');
export const createTransaction = (dto: CreateTransactionDto) => request<Transaction>('/transactions', { method: 'POST', body: JSON.stringify(dto) });

export const getTotals = () => request<TotalsResponse>('/totals');
```

---

### Passo 12: Frontend — Criar App.tsx (componente principal)

A aplicação é uma Single Page Application com 3 abas:

| Aba | Componente | Funcionalidade |
|-----|-----------|----------------|
| 👥 Pessoas | `<PeopleTab>` | Form de criação + tabela com botão de remover |
| 💳 Transações | `<TransactionsTab>` | Form com select de pessoa + tipo + valor + tabela |
| 📊 Totais | `<TotalsTab>` | Tabela de totais por pessoa + cards de total geral |

Cada aba é um componente funcional com seus próprios estados (`useState`) e chamadas à API (`useEffect` + `useCallback`).

---

### Passo 13: Frontend — Estilização

Criar `frontend/src/App.css` com:
- Layout responsivo (max-width 960px centralizado)
- Tabs com visual de "pills" (fundo cinza, ativa branca com sombra)
- Tabelas com hover e bordas sutis
- Badges coloridos para receita (verde) e despesa (vermelho)
- Cards de totais gerais em grid
- Alertas de erro (vermelho) e sucesso (verde)
- Regra de negócio destacada em box amarelo

---

## 🧪 Como testar

### Teste manual da API

Com o backend rodando (`dotnet run` na pasta `backend`):

```bash
# 1. Criar um adulto
curl -s -X POST http://localhost:5000/api/people \
  -H "Content-Type: application/json" \
  -d '{"name":"João","age":30}'
# → {"id":1,"name":"João","age":30}

# 2. Criar um menor
curl -s -X POST http://localhost:5000/api/people \
  -H "Content-Type: application/json" \
  -d '{"name":"Maria","age":15}'
# → {"id":2,"name":"Maria","age":15}

# 3. Tentar receita para menor (DEVE FALHAR)
curl -s -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"description":"Mesada","amount":100,"type":"receita","personId":2}'
# → {"message":"Menores de 18 anos não podem cadastrar receitas, apenas despesas."}

# 4. Despesa para menor (DEVE FUNCIONAR)
curl -s -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"description":"Lanche","amount":25.50,"type":"despesa","personId":2}'
# → {"id":1,...}

# 5. Receita para adulto (DEVE FUNCIONAR)
curl -s -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"description":"Salário","amount":5000,"type":"receita","personId":1}'
# → {"id":2,...}

# 6. Consultar totais
curl -s http://localhost:5000/api/totals
# → Totais por pessoa + total geral
```

### Teste pelo frontend

1. Inicie o backend: `cd backend && dotnet run`
2. Inicie o frontend: `cd frontend && npm run dev`
3. Acesse `http://localhost:5173`
4. Navegue entre as abas e teste todas as funcionalidades

---

## 📊 Diagrama da arquitetura

```
┌─────────────────────────────────────────────────────────┐
│                    Frontend (React)                       │
│                  http://localhost:5173                    │
│                                                          │
│  ┌──────────┐  ┌──────────────┐  ┌──────────┐          │
│  │ Pessoas  │  │ Transações   │  │  Totais  │          │
│  │   Tab    │  │    Tab       │  │   Tab    │          │
│  └────┬─────┘  └──────┬───────┘  └────┬─────┘          │
│       │               │               │                 │
│       └───────────────┼───────────────┘                 │
│                       │ fetch()                          │
└───────────────────────┼─────────────────────────────────┘
                        │ HTTP/REST
                        ▼
┌───────────────────────┼─────────────────────────────────┐
│                  Backend (.NET 8)                         │
│                http://localhost:5000                      │
│                                                          │
│  ┌──────────────┐  ┌────────────────┐  ┌─────────────┐ │
│  │  Controllers │  │   Services     │  │  DbContext   │ │
│  │  (endpoints) │──│ (regras neg.)  │──│  (EF Core)   │ │
│  └──────────────┘  └────────────────┘  └──────┬──────┘ │
│                                               │         │
└───────────────────────────────────────────────┼─────────┘
                                                │
                                                ▼
                                        ┌──────────────┐
                                        │   SQLite DB   │
                                        │ ExpenseControl│
                                        │    .db        │
                                        └──────────────┘
```

---

## ⚠️ Troubleshooting

| Problema | Solução |
|----------|---------|
| `dotnet: command not found` | Instalar .NET 8 SDK (`snap install dotnet-sdk --classic` ou baixar de dotnet.microsoft.com) |
| Erro de CORS no console do navegador | Verificar se o backend está rodando em `localhost:5000` e o CORS permite `localhost:5173` |
| `npm: command not found` | Instalar Node.js (nodejs.org ou `apt install nodejs`) |
| Porta 5000 já em uso | Alterar `applicationUrl` no `launchSettings.json` e `API_BASE` no `api/index.ts` |
| Banco de dados corrompido | Deletar `backend/ExpenseControl.db` e reiniciar o backend (recria automaticamente) |

---

## 📁 Resumo da estrutura final

```
expense-control-system/
├── README.md                       # Documentação do projeto
├── SETUP.md                        # Este arquivo
├── backend/
│   ├── Backend.csproj              # Projeto .NET + dependências NuGet
│   ├── Program.cs                  # Bootstrap (DI, CORS, DB, middleware)
│   ├── appsettings.json            # Connection string SQLite
│   ├── Properties/
│   │   └── launchSettings.json     # Porta 5000
│   ├── Models/
│   │   ├── Person.cs               # Entidade Pessoa
│   │   └── Transaction.cs          # Entidade Transação
│   ├── Data/
│   │   └── AppDbContext.cs         # Contexto EF Core + cascata
│   ├── DTOs/
│   │   └── Dtos.cs                 # Contratos da API
│   ├── Services/
│   │   ├── PersonService.cs        # Lógica de pessoas
│   │   ├── TransactionService.cs   # Lógica de transações + regra <18
│   │   └── TotalsService.cs        # Cálculo de totais
│   └── Controllers/
│       ├── PeopleController.cs     # /api/people
│       ├── TransactionsController.cs # /api/transactions
│       └── TotalsController.cs     # /api/totals
└── frontend/
    ├── package.json                # Dependências npm
    ├── vite.config.ts              # Configuração Vite
    ├── tsconfig.json               # Configuração TypeScript
    └── src/
        ├── main.tsx                # Entry point React
        ├── App.tsx                 # Componente principal (3 abas)
        ├── App.css                 # Estilos
        ├── index.css               # Reset global
        ├── types/
        │   └── index.ts            # Interfaces TypeScript
        └── api/
            └── index.ts            # Chamadas HTTP ao backend
```
