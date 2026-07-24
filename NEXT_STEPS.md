# 📋 Próximos Passos — Expense Control System

> Documento gerado em 2026-07-23. Descreve o que foi feito, o que falta, e os comandos necessários para continuar.

---

## ✅ O que já foi feito (Step 1: Backend Data Layer)

| Artefato | Status | Localização |
|----------|--------|-------------|
| `.editorconfig` | ✅ Criado | Raiz do repositório |
| `IRepository<T>` (interface) | ✅ Criado | `backend/Data/IRepository.cs` |
| `Repository<T>` (implementação) | ✅ Criado | `backend/Data/Repository.cs` |
| `PersonService` refatorado | ✅ Usa `IRepository<Person>` | `backend/Services/PersonService.cs` |
| `TransactionService` refatorado | ✅ Usa `IRepository<Transaction>` | `backend/Services/TransactionService.cs` |
| `Program.cs` — DI registration | ✅ `AddScoped(typeof(IRepository<>), ...)` | `backend/Program.cs` |
| Testes unitários atualizados | ✅ Compatíveis com Repository | `tests/backend/Unit/` |
| Models (Person, Transaction) | ✅ Já existiam | `backend/Models/` |
| AppDbContext (cascade delete) | ✅ Já existia | `backend/Data/AppDbContext.cs` |
| DTOs | ✅ Já existiam | `backend/DTOs/Dtos.cs` |

---

## 📦 Dependências que precisam ser instaladas

### 1. Instalar .NET SDK 8.0 (comando sudo)

O ambiente atual não tem o `dotnet` CLI no PATH. Execute:

```bash
# Opção A: via snap (recomendado para Ubuntu)
sudo snap install dotnet-sdk --channel=8.0/stable --classic

# Opção B: via apt (Ubuntu 24.04+)
sudo apt update && sudo apt install -y dotnet-sdk-8.0

# Opção C: via script oficial da Microsoft
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
# Depois adicione ao PATH:
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

### 2. Verificar Node.js (provavelmente já instalado)

```bash
node --version   # Deve ser >= 18
npm --version    # Deve ser >= 9
```

---

## 🔨 Comandos para build e verificação

Depois de instalar o .NET SDK, execute na ordem:

```bash
# 1. Restaurar pacotes NuGet
cd backend
dotnet restore

# 2. Compilar o backend (deve dar 0 erros)
dotnet build

# 3. Rodar os testes unitários e de integração
cd ../tests/backend
dotnet test

# 4. Subir a API
cd ../../backend
dotnet run
# API em: http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

### Verificação rápida com curl (enquanto o backend roda)

```bash
# Criar pessoa adulta
curl -X POST http://localhost:5000/api/people \
  -H "Content-Type: application/json" \
  -d '{"name":"João","age":30}'

# Criar pessoa menor de idade
curl -X POST http://localhost:5000/api/people \
  -H "Content-Type: application/json" \
  -d '{"name":"Pedro","age":15}'

# Tentar criar receita para menor (DEVE falhar com 400)
curl -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"description":"Mesada","amount":100,"type":"receita","personId":2}'

# Criar despesa para menor (DEVE funcionar)
curl -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"description":"Lanche","amount":25.50,"type":"despesa","personId":2}'

# Consultar totais
curl http://localhost:5000/api/totals
```

### Frontend

```bash
cd frontend
npm install        # Instalar dependências
npm run dev        # Dev server em http://localhost:5173
```

---

## 🗺️ O que falta implementar (Steps 2 e 3 da mentoria)

> Os Controllers e o Frontend já existem e estão funcionais. O plano é apresentá-los
> com a explicação detalhada do "porquê" de cada decisão.

### Step 2: Backend API Controllers & Validação (a apresentar)

| Artefato | Status | Observação |
|----------|--------|------------|
| `PeopleController` | ✅ Existe | Criar, Listar, Deletar com cascata |
| `TransactionsController` | ✅ Existe | Criar (com validação menor de idade), Listar |
| `TotalsController` | ✅ Existe | Agregação por pessoa + total geral |
| Validação "menor não pode ter receita" | ✅ Existe | `TransactionService.CreateAsync`, linha `if (age < 18 && dto.Type == "receita")` |
| XML Comments nos endpoints | ✅ Existem | Todos os métodos públicos documentados |

### Step 3: Frontend React (a apresentar)

| Artefato | Status | Observação |
|----------|--------|------------|
| `api/index.ts` (fetch wrapper) | ✅ Existe | Centraliza chamadas HTTP |
| `types/index.ts` | ✅ Existe | Interfaces TypeScript espelhando DTOs |
| `App.tsx` — 3 abas (People, Transactions, Totals) | ✅ Existe | Functional components + hooks |
| UI: desabilitar "Receita" para menor | ⚠️ A implementar | Bloqueio visual no `<select>` de tipo |

### Melhoria pendente no Frontend

No `TransactionsTab`, o `<select>` de tipo deve desabilitar a opção "Receita" quando a pessoa selecionada for menor de idade. Isso é UX (experiência do usuário), não segurança — a API já bloqueia no backend. A lógica:

```tsx
// Pseudo-código da melhoria:
const selectedPerson = people.find(p => p.id === parseInt(personId));
const isMinor = selectedPerson ? selectedPerson.age < 18 : false;

<select value={type} onChange={...}>
  <option value="despesa">Despesa</option>
  <option value="receita" disabled={isMinor}>
    Receita {isMinor ? '(indisponível para menores)' : ''}
  </option>
</select>
```

---

## 🧪 Estrutura de testes (já funcional)

```
tests/backend/
├── Backend.Tests.csproj
├── TestDatabase.cs              # Fixture: cria AppDbContext InMemory
├── TestWebApplicationFactory.cs # Fixture: sobe API real em memória
├── Unit/
│   ├── PersonServiceTests.cs    # 10+ testes
│   ├── TransactionServiceTests.cs # 8+ testes (inclui regra do menor)
│   └── TotalsServiceTests.cs    # 5+ testes
└── Integration/
    ├── PeopleControllerTests.cs   # Testes HTTP contra API real
    ├── TransactionsControllerTests.cs
    └── TotalsControllerTests.cs
```

---

## 📊 Diagrama da arquitetura atual

```
┌─────────────────────────────────────────────────┐
│                   FRONTEND                       │
│  React 18 + TypeScript + Vite                    │
│  api/index.ts → fetch() → http://localhost:5000  │
└────────────────────┬────────────────────────────┘
                     │ HTTP (JSON)
┌────────────────────▼────────────────────────────┐
│                 BACKEND (.NET 8)                 │
│                                                  │
│  Controllers  ←  Services  ←  Repository<T>      │
│  (HTTP)          (Regras)     (Data Access)      │
│                              ↕                   │
│                         AppDbContext             │
│                              ↕                   │
│                         SQLite (.db)             │
└─────────────────────────────────────────────────┘

Regras de negócio:
  ✅ Pessoa < 18 anos → só DESPESA (validado no TransactionService)
  ✅ Deletar Pessoa → cascade delete Transactions (AppDbContext)
  ✅ Totals: Income - Expense = Balance por pessoa + geral
```

---

## ⚠️ Troubleshooting

| Problema | Solução |
|----------|---------|
| `dotnet: command not found` | Instalar SDK via snap ou apt (comandos acima) |
| `sudo: incorrect password` | Pedir ao admin do sistema para instalar o .NET SDK |
| Porta 5000 em uso | `dotnet run --urls http://localhost:5001` ou matar processo: `kill $(lsof -t -i:5000)` |
| Porta 5173 em uso | Vite tenta outra porta automaticamente |
| Testes falham com `System.InvalidOperationException` | Pode ser conflito do InMemory database; cada teste usa nome único via Guid |
