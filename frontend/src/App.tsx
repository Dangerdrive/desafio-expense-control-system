import { useState } from 'react';
import './App.css';
import ErrorBoundary from './components/ErrorBoundary';
import PeopleTab from './components/PeopleTab';
import TransactionsTab from './components/TransactionsTab';
import TotalsTab from './components/TotalsTab';

function App() {
  const [activeTab, setActiveTab] = useState<'people' | 'transactions' | 'totals'>('people');

  return (
    <ErrorBoundary>
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
    </ErrorBoundary>
  );
}

export default App;

