import { useState, useEffect, useCallback } from 'react';
import './App.css';
import * as api from './api';
import type { Person, Transaction, TotalsResponse } from './types';

function App() {
  const [activeTab, setActiveTab] = useState<'people' | 'transactions' | 'totals'>('people');

  return (
    <div className="app">
      <header className="app-header">
        <h1>💰 Controle de Gastos Residenciais</h1>
      </header>
      <nav className="tab-nav">
        <button className={`tab-btn ${activeTab === 'people' ? 'active' : ''}`} onClick={() => setActiveTab('people')}>👥 Pessoas</button>
        <button className={`tab-btn ${activeTab === 'transactions' ? 'active' : ''}`} onClick={() => setActiveTab('transactions')}>💳 Transações</button>
        <button className={`tab-btn ${activeTab === 'totals' ? 'active' : ''}`} onClick={() => setActiveTab('totals')}>📊 Totais</button>
      </nav>
      <main className="app-main">
        {activeTab === 'people' && <PeopleTab />}
        {activeTab === 'transactions' && <TransactionsTab />}
        {activeTab === 'totals' && <TotalsTab />}
      </main>
    </div>
  );
}

function PeopleTab() {
  const [people, setPeople] = useState<Person[]>([]);
  const [name, setName] = useState('');
  const [age, setAge] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  const loadPeople = useCallback(async () => {
    try { setPeople(await api.getPeople()); } catch { setError('Erro ao carregar pessoas.'); }
  }, []);

  useEffect(() => { loadPeople(); }, [loadPeople]);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(''); setSuccess('');
    const ageNum = parseInt(age);
    if (!name.trim()) { setError('Nome é obrigatório.'); return; }
    if (isNaN(ageNum) || ageNum < 0 || ageNum > 150) { setError('Idade inválida (0-150).'); return; }
    setLoading(true);
    try {
      await api.createPerson({ name: name.trim(), age: ageNum });
      setSuccess('Pessoa cadastrada com sucesso!');
      setName(''); setAge('');
      await loadPeople();
    } catch (err: any) { setError(err.message); }
    finally { setLoading(false); }
  };

  const handleDelete = async (id: number, personName: string) => {
    if (!confirm(`Remover "${personName}"? Todas as transações desta pessoa também serão removidas.`)) return;
    setError('');
    try { await api.deletePerson(id); setSuccess(`"${personName}" removida.`); await loadPeople(); }
    catch (err: any) { setError(err.message); }
  };

  return (
    <section>
      <h2>Cadastro de Pessoas</h2>
      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}
      <form onSubmit={handleCreate} className="form-row">
        <input type="text" placeholder="Nome" value={name} onChange={e => setName(e.target.value)} className="input" maxLength={100} />
        <input type="number" placeholder="Idade" value={age} onChange={e => setAge(e.target.value)} className="input input-sm" min={0} max={150} />
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? 'Salvando...' : '➕ Adicionar'}
        </button>
      </form>
      {people.length === 0 ? (
        <p className="empty-msg">Nenhuma pessoa cadastrada.</p>
      ) : (
        <table className="table">
          <thead><tr><th>ID</th><th>Nome</th><th>Idade</th><th>Ações</th></tr></thead>
          <tbody>
            {people.map(p => (
              <tr key={p.id}>
                <td>{p.id}</td><td>{p.name}</td>
                <td>{p.age} {p.age < 18 ? '🔞' : ''}</td>
                <td><button className="btn btn-danger btn-sm" onClick={() => handleDelete(p.id, p.name)}>🗑️ Remover</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

function TransactionsTab() {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [people, setPeople] = useState<Person[]>([]);
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState('');
  const [type, setType] = useState<'receita' | 'despesa'>('despesa');
  const [personId, setPersonId] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  const loadData = useCallback(async () => {
    try {
      const [txs, ppl] = await Promise.all([api.getTransactions(), api.getPeople()]);
      setTransactions(txs); setPeople(ppl);
    } catch { setError('Erro ao carregar dados.'); }
  }, []);

  useEffect(() => { loadData(); }, [loadData]);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(''); setSuccess('');
    const amountNum = parseFloat(amount);
    const personIdNum = parseInt(personId);
    if (!description.trim()) { setError('Descrição é obrigatória.'); return; }
    if (isNaN(amountNum) || amountNum <= 0) { setError('Valor deve ser maior que zero.'); return; }
    if (isNaN(personIdNum)) { setError('Selecione uma pessoa.'); return; }
    setLoading(true);
    try {
      await api.createTransaction({ description: description.trim(), amount: amountNum, type, personId: personIdNum });
      setSuccess('Transação registrada com sucesso!');
      setDescription(''); setAmount(''); setPersonId('');
      await loadData();
    } catch (err: any) { setError(err.message); }
    finally { setLoading(false); }
  };

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

  return (
    <section>
      <h2>Cadastro de Transações</h2>
      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}
      <form onSubmit={handleCreate} className="form-row">
        <input type="text" placeholder="Descrição" value={description} onChange={e => setDescription(e.target.value)} className="input" maxLength={200} />
        <input type="number" placeholder="Valor" value={amount} onChange={e => setAmount(e.target.value)} className="input input-sm" min="0.01" step="0.01" />
        <select value={type} onChange={e => setType(e.target.value as 'receita' | 'despesa')} className="input input-sm">
          <option value="despesa">Despesa</option>
          <option value="receita">Receita</option>
        </select>
        <select value={personId} onChange={e => setPersonId(e.target.value)} className="input">
          <option value="">Selecione uma pessoa...</option>
          {people.map(p => (
            <option key={p.id} value={p.id}>{p.name} ({p.age}a{p.age < 18 ? ' 🔞' : ''})</option>
          ))}
        </select>
        <button type="submit" className="btn btn-primary" disabled={loading || people.length === 0}>
          {loading ? 'Salvando...' : '➕ Registrar'}
        </button>
      </form>
      {people.length === 0 && <p className="empty-msg">⚠️ Cadastre uma pessoa antes de registrar transações.</p>}
      <div className="rule-info">ℹ️ <strong>Regra:</strong> Menores de 18 anos só podem ter <em>despesas</em> cadastradas.</div>
      {transactions.length === 0 ? (
        <p className="empty-msg">Nenhuma transação registrada.</p>
      ) : (
        <table className="table">
          <thead><tr><th>ID</th><th>Descrição</th><th>Valor</th><th>Tipo</th><th>Pessoa</th></tr></thead>
          <tbody>
            {transactions.map(tx => (
              <tr key={tx.id}>
                <td>{tx.id}</td><td>{tx.description}</td>
                <td className={tx.type === 'receita' ? 'text-green' : 'text-red'}>{fmt(tx.amount)}</td>
                <td><span className={`badge ${tx.type === 'receita' ? 'badge-income' : 'badge-expense'}`}>{tx.type === 'receita' ? '📈 Receita' : '📉 Despesa'}</span></td>
                <td>{tx.personName}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

function TotalsTab() {
  const [totals, setTotals] = useState<TotalsResponse | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const loadTotals = useCallback(async () => {
    setLoading(true); setError('');
    try { setTotals(await api.getTotals()); }
    catch { setError('Erro ao consultar totais.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { loadTotals(); }, [loadTotals]);

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

  return (
    <section>
      <h2>Consulta de Totais</h2>
      {error && <div className="alert alert-error">{error}</div>}
      <button onClick={loadTotals} className="btn btn-secondary" disabled={loading}>
        {loading ? 'Atualizando...' : '🔄 Atualizar Totais'}
      </button>
      {totals && (
        <>
          {totals.peopleTotals.length === 0 ? (
            <p className="empty-msg">Nenhuma pessoa cadastrada para exibir totais.</p>
          ) : (
            <table className="table">
              <thead><tr><th>Pessoa</th><th>Receitas</th><th>Despesas</th><th>Saldo</th></tr></thead>
              <tbody>
                {totals.peopleTotals.map(pt => (
                  <tr key={pt.personId}>
                    <td><strong>{pt.personName}</strong></td>
                    <td className="text-green">{fmt(pt.totalIncome)}</td>
                    <td className="text-red">{fmt(pt.totalExpense)}</td>
                    <td className={pt.balance >= 0 ? 'text-green' : 'text-red'}><strong>{fmt(pt.balance)}</strong></td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          <div className="grand-totals">
            <h3>📊 Total Geral</h3>
            <div className="totals-grid">
              <div className="total-card income">
                <span className="total-label">Total de Receitas</span>
                <span className="total-value">{fmt(totals.grandTotalIncome)}</span>
              </div>
              <div className="total-card expense">
                <span className="total-label">Total de Despesas</span>
                <span className="total-value">{fmt(totals.grandTotalExpense)}</span>
              </div>
              <div className={`total-card ${totals.grandBalance >= 0 ? 'balance-positive' : 'balance-negative'}`}>
                <span className="total-label">Saldo Líquido</span>
                <span className="total-value">{fmt(totals.grandBalance)}</span>
              </div>
            </div>
          </div>
        </>
      )}
    </section>
  );
}

export default App;
