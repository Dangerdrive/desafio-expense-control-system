import { useState, useEffect, useCallback } from 'react';
import * as api from '../api';
import ConfirmDialog from './ConfirmDialog';
import Pagination from './Pagination';
import { getErrorMessage } from '../utils/errors';
import { usePagedList } from '../hooks/usePagedList';
import type { Person } from '../types';

/** Quantidade de pessoas por página na listagem. */
const PAGE_SIZE = 10;

/**
 * Aba de cadastro de pessoas: criação, listagem (com paginação) e remoção (com cascata).
 */
function PeopleTab() {
  const { items: people, page, totalPages, totalItems, applyPagedResult, getPageAfterRemoval } = usePagedList<Person>();
  const [name, setName] = useState('');
  const [age, setAge] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);
  const [loadingList, setLoadingList] = useState(false);
  // Pessoa aguardando confirmação de exclusão (null = modal fechado)
  const [pendingDelete, setPendingDelete] = useState<Person | null>(null);

  const loadPeople = useCallback(async (targetPage: number) => {
    setLoadingList(true);
    try {
      const result = await api.getPeople({ page: targetPage, pageSize: PAGE_SIZE });
      applyPagedResult(result);
    }
    catch (err) { setError(getErrorMessage(err, 'Erro ao carregar pessoas.')); }
    finally { setLoadingList(false); }
  }, [applyPagedResult]);

  useEffect(() => { loadPeople(1); }, [loadPeople]);

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
      // Volta para a primeira página para a nova pessoa ficar visível
      await loadPeople(1);
    } catch (err) { setError(getErrorMessage(err)); }
    finally { setLoading(false); }
  };

  // Abre o modal de confirmação em vez de usar window.confirm()
  const confirmDelete = (person: Person) => setPendingDelete(person);

  const handleDelete = async () => {
    if (!pendingDelete) return;
    const person = pendingDelete;
    setPendingDelete(null);
    setError(''); setSuccess('');
    try {
      await api.deletePerson(person.id);
      setSuccess(`"${person.name}" removida.`);
      // Se a página ficou vazia após a remoção, volta uma página
      const target = getPageAfterRemoval(people.length);
      await loadPeople(target);
    }
    catch (err) { setError(getErrorMessage(err)); }
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
      {loadingList ? (
        <p className="empty-msg">Carregando pessoas...</p>
      ) : people.length === 0 ? (
        <p className="empty-msg">Nenhuma pessoa cadastrada.</p>
      ) : (
        <>
          <table className="table">
            <thead><tr><th>ID</th><th>Nome</th><th>Idade</th><th>Ações</th></tr></thead>
            <tbody>
              {people.map(p => (
                <tr key={p.id}>
                  <td>{p.id}</td><td>{p.name}</td>
                  <td>{p.age} {p.age < 18 ? '🔞' : ''}</td>
                  <td><button className="btn btn-danger btn-sm" onClick={() => confirmDelete(p)} aria-label={`Remover ${p.name}`}>🗑️ Remover</button></td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination
            page={page}
            totalPages={totalPages}
            totalItems={totalItems}
            onPageChange={loadPeople}
          />
        </>
      )}
      <ConfirmDialog
        open={pendingDelete !== null}
        title="Remover pessoa"
        message={`Remover "${pendingDelete?.name}"? Todas as transações desta pessoa também serão removidas.`}
        confirmLabel="Remover"
        cancelLabel="Cancelar"
        danger
        onConfirm={handleDelete}
        onCancel={() => setPendingDelete(null)}
      />
    </section>
  );
}

export default PeopleTab;
