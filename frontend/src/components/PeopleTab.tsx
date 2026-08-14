import { useState, useEffect, useCallback } from 'react';
import * as api from '../api';
import type { Person } from '../types';

/**
 * Aba de cadastro de pessoas: criação, listagem e remoção (com cascata).
 */
function PeopleTab() {
  const [people, setPeople] = useState<Person[]>([]);
  const [name, setName] = useState('');
  const [age, setAge] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);
  const [loadingList, setLoadingList] = useState(false);

  const loadPeople = useCallback(async () => {
    setLoadingList(true);
    try { setPeople(await api.getPeople()); } catch { setError('Erro ao carregar pessoas.'); }
    finally { setLoadingList(false); }
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
      {loadingList ? (
        <p className="empty-msg">Carregando pessoas...</p>
      ) : people.length === 0 ? (
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

export default PeopleTab;
