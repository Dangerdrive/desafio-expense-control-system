import { useState, useEffect, useCallback } from 'react';
import * as api from '../api';
import ConfirmDialog from './ConfirmDialog';
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
  // Edição/exclusão
  const [editingId, setEditingId] = useState<number | null>(null);
  const [pendingDelete, setPendingDelete] = useState<Transaction | null>(null);
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(''); setSuccess('');
    const amountNum = parseAmountInput(amount);
    const personIdNum = parseInt(personId);
    if (!description.trim()) { setError('Descrição é obrigatória.'); return; }
    if (amountNum === null) { setError('Valor deve ser maior que zero (use até 2 casas decimais).'); return; }
    if (isNaN(personIdNum)) { setError('Selecione uma pessoa.'); return; }
    setLoading(true);
    try {
      const dto = { description: description.trim(), amount: amountNum, date, type, personId: personIdNum };
      if (editingId !== null) {
        await api.updateTransaction(editingId, dto);
        setSuccess('Transação atualizada com sucesso!');
      } else {
        await api.createTransaction(dto);
        setSuccess('Transação registrada com sucesso!');
      }
      cancelEdit();
      await loadData();
    } catch (err) { setError(getErrorMessage(err)); }
    finally { setLoading(false); }
  };

  /** Preenche o formulário com os dados de uma transação para edição. */
  const startEdit = (tx: Transaction) => {
    setEditingId(tx.id);
    setDescription(tx.description);
    setAmount(maskAmountInput(String(tx.amount)));
    setDate(tx.date);
    setType(tx.type);
    setPersonId(String(tx.personId));
    setError(''); setSuccess('');
  };

  /** Sai do modo de edição e limpa o formulário. */
  const cancelEdit = () => {
    setEditingId(null);
    setDescription(''); setAmount(''); setDate(todayISO()); setPersonId('');
  };

  /** Abre o modal de confirmação de exclusão. */
  const confirmDelete = (tx: Transaction) => setPendingDelete(tx);

  const handleDelete = async () => {
    if (!pendingDelete) return;
    const tx = pendingDelete;
    setPendingDelete(null);
    setError(''); setSuccess('');
    try {
      await api.deleteTransaction(tx.id);
      setSuccess(`Transação "${tx.description}" removida.`);
      await loadData();
    } catch (err) { setError(getErrorMessage(err)); }
  };

  return (
    <section>
      <h2>Cadastro de Transações</h2>
      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}
      <form onSubmit={handleSubmit} className="form-row">
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
          {loading ? 'Salvando...' : editingId !== null ? '💾 Salvar' : '➕ Registrar'}
        </button>
        {editingId !== null && (
          <button type="button" className="btn btn-secondary" onClick={cancelEdit} disabled={loading}>
            ✖ Cancelar
          </button>
        )}
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
          <thead><tr><th>Data</th><th>Descrição</th><th>Valor</th><th>Tipo</th><th>Pessoa</th><th>Ações</th></tr></thead>
          <tbody>
            {transactions.map(tx => (
              <tr key={tx.id}>
                <td>{formatDate(tx.date)}</td>
                <td>{tx.description}</td>
                <td className={tx.type === 'receita' ? 'text-green' : 'text-red'}>{formatCurrency(tx.amount)}</td>
                <td><span className={`badge ${tx.type === 'receita' ? 'badge-income' : 'badge-expense'}`}>{tx.type === 'receita' ? '📈 Receita' : '📉 Despesa'}</span></td>
                <td>{tx.personName}</td>
                <td>
                  <button className="btn btn-secondary btn-sm" onClick={() => startEdit(tx)} aria-label={`Editar ${tx.description}`}>✏️ Editar</button>{' '}
                  <button className="btn btn-danger btn-sm" onClick={() => confirmDelete(tx)} aria-label={`Excluir ${tx.description}`}>🗑️ Excluir</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <ConfirmDialog
        open={pendingDelete !== null}
        title="Excluir transação"
        message={`Excluir a transação "${pendingDelete?.description}"? Esta ação não pode ser desfeita.`}
        confirmLabel="Excluir"
        cancelLabel="Cancelar"
        danger
        onConfirm={handleDelete}
        onCancel={() => setPendingDelete(null)}
      />
    </section>
  );
}

export default TransactionsTab;
