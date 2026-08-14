import { useState, useEffect, useCallback } from 'react';
import * as api from '../api';
import { formatCurrency } from '../utils/format';
import type { Person, Transaction } from '../types';

/**
 * Aba de cadastro de transações: criação e listagem (receitas/despesas).
 * Regra de negócio: menores de 18 anos só podem ter despesas (validada no backend).
 */
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
  const [loadingList, setLoadingList] = useState(false);

  const loadData = useCallback(async () => {
    setLoadingList(true);
    try {
      const [txs, ppl] = await Promise.all([api.getTransactions(), api.getPeople()]);
      setTransactions(txs); setPeople(ppl);
    } catch { setError('Erro ao carregar dados.'); }
    finally { setLoadingList(false); }
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

  return (
    <section>
      <h2>Cadastro de Transações</h2>
      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}
      <form onSubmit={handleCreate} className="form-row">
        <input type="text" placeholder="Descrição" value={description} onChange={e => setDescription(e.target.value)} className="input" maxLength={200} />
        <input type="number" placeholder="Valor" value={amount} onChange={e => setAmount(e.target.value)} className="input input-sm" min="0.01" step="0.01" />
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
      {loadingList ? (
        <p className="empty-msg">Carregando transações...</p>
      ) : transactions.length === 0 ? (
        <p className="empty-msg">Nenhuma transação registrada.</p>
      ) : (
        <table className="table">
          <thead><tr><th>ID</th><th>Descrição</th><th>Valor</th><th>Tipo</th><th>Pessoa</th></tr></thead>
          <tbody>
            {transactions.map(tx => (
              <tr key={tx.id}>
                <td>{tx.id}</td><td>{tx.description}</td>
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
