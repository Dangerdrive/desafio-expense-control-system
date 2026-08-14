import { useState, useEffect, useCallback } from 'react';
import * as api from '../api';
import { formatCurrency, formatDate, maskAmountInput, parseAmountInput } from '../utils/format';
import { getErrorMessage } from '../utils/errors';
import type { Person, Transaction } from '../types';

/** Retorna a data de hoje no formato ISO "YYYY-MM-DD" (local). */
function todayISO(): string {
  const d = new Date();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${m}-${day}`;
}

/**
 * Aba de cadastro de transações: criação e listagem (receitas/despesas),
 * com filtro por período e ordenação por data.
 * Regra de negócio: menores de 18 anos só podem ter despesas (validada no backend).
 */
function TransactionsTab() {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [people, setPeople] = useState<Person[]>([]);
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState('');
  const [date, setDate] = useState(todayISO());
  const [type, setType] = useState<'receita' | 'despesa'>('despesa');
  const [personId, setPersonId] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);
  const [loadingList, setLoadingList] = useState(false);
  // Filtros da listagem
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [sort, setSort] = useState<'date_asc' | 'date_desc'>('date_desc');

  const loadData = useCallback(async () => {
    setLoadingList(true);
    try {
      const [txs, ppl] = await Promise.all([
        api.getTransactions({ from: from || undefined, to: to || undefined, sort }),
        api.getPeople(),
      ]);
      setTransactions(txs); setPeople(ppl);
    } catch (err) { setError(getErrorMessage(err, 'Erro ao carregar dados.')); }
    finally { setLoadingList(false); }
  }, [from, to, sort]);

  useEffect(() => { loadData(); }, [loadData]);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(''); setSuccess('');
    const amountNum = parseAmountInput(amount);
    const personIdNum = parseInt(personId);
    if (!description.trim()) { setError('Descrição é obrigatória.'); return; }
    if (amountNum === null) { setError('Valor deve ser maior que zero (use até 2 casas decimais).'); return; }
    if (isNaN(personIdNum)) { setError('Selecione uma pessoa.'); return; }
    setLoading(true);
    try {
      await api.createTransaction({ description: description.trim(), amount: amountNum, date, type, personId: personIdNum });
      setSuccess('Transação registrada com sucesso!');
      setDescription(''); setAmount(''); setDate(todayISO()); setPersonId('');
      await loadData();
    } catch (err) { setError(getErrorMessage(err)); }
    finally { setLoading(false); }
  };

  return (
    <section>
      <h2>Cadastro de Transações</h2>
      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}
      <form onSubmit={handleCreate} className="form-row">
        <input type="text" placeholder="Descrição" value={description} onChange={e => setDescription(e.target.value)} className="input" maxLength={200} />
        <input type="text" inputMode="decimal" aria-label="Valor" placeholder="Valor (ex: 12,50)" value={amount} onChange={e => setAmount(maskAmountInput(e.target.value))} className="input input-sm" />
        <input type="date" aria-label="Data" value={date} onChange={e => setDate(e.target.value)} className="input input-sm" />
        <select aria-label="Tipo" value={type} onChange={e => setType(e.target.value as 'receita' | 'despesa')} className="input input-sm">
          <option value="despesa">Despesa</option>
          <option value="receita">Receita</option>
        </select>
        <select aria-label="Pessoa" value={personId} onChange={e => setPersonId(e.target.value)} className="input">
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

      <div className="filter-bar">
        <label>De <input type="date" aria-label="Data inicial" value={from} onChange={e => setFrom(e.target.value)} className="input input-sm" /></label>
        <label>Até <input type="date" aria-label="Data final" value={to} onChange={e => setTo(e.target.value)} className="input input-sm" /></label>
        <select aria-label="Ordenar" value={sort} onChange={e => setSort(e.target.value as 'date_asc' | 'date_desc')} className="input input-sm">
          <option value="date_desc">Mais recentes primeiro</option>
          <option value="date_asc">Mais antigas primeiro</option>
        </select>
      </div>

      {loadingList ? (
        <p className="empty-msg">Carregando transações...</p>
      ) : transactions.length === 0 ? (
        <p className="empty-msg">Nenhuma transação registrada.</p>
      ) : (
        <table className="table">
          <thead><tr><th>Data</th><th>Descrição</th><th>Valor</th><th>Tipo</th><th>Pessoa</th></tr></thead>
          <tbody>
            {transactions.map(tx => (
              <tr key={tx.id}>
                <td>{formatDate(tx.date)}</td>
                <td>{tx.description}</td>
                <td className={tx.type === 'receita' ? 'text-green' : 'text-red'}>{formatCurrency(tx.amount)}</td>
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

export default TransactionsTab;
