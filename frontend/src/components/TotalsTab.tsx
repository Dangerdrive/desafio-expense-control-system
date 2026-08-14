import { useState, useEffect, useCallback } from 'react';
import * as api from '../api';
import { formatCurrency } from '../utils/format';
import type { TotalsResponse } from '../types';

/**
 * Aba de consulta de totais: receitas, despesas e saldo por pessoa + total geral.
 */
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

  return (
    <section>
      <h2>Consulta de Totais</h2>
      {error && <div className="alert alert-error">{error}</div>}
      <button onClick={loadTotals} className="btn btn-secondary" disabled={loading}>
        {loading ? 'Atualizando...' : '🔄 Atualizar Totais'}
      </button>
      {loading && !totals && <p className="empty-msg">Carregando totais...</p>}
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
                    <td className="text-green">{formatCurrency(pt.totalIncome)}</td>
                    <td className="text-red">{formatCurrency(pt.totalExpense)}</td>
                    <td className={pt.balance >= 0 ? 'text-green' : 'text-red'}><strong>{formatCurrency(pt.balance)}</strong></td>
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
                <span className="total-value">{formatCurrency(totals.grandTotalIncome)}</span>
              </div>
              <div className="total-card expense">
                <span className="total-label">Total de Despesas</span>
                <span className="total-value">{formatCurrency(totals.grandTotalExpense)}</span>
              </div>
              <div className={`total-card ${totals.grandBalance >= 0 ? 'balance-positive' : 'balance-negative'}`}>
                <span className="total-label">Saldo Líquido</span>
                <span className="total-value">{formatCurrency(totals.grandBalance)}</span>
              </div>
            </div>
          </div>
        </>
      )}
    </section>
  );
}

export default TotalsTab;
