import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

// Falhas assíncronas fora de um try/catch (ex: promise sem .catch) não passam
// pelo ErrorBoundary do React e desapareceriam sem rastro — logamos aqui.
window.addEventListener('unhandledrejection', event => {
  console.error('Promise rejeitada sem tratamento:', event.reason)
})

const container = document.getElementById('root')
if (!container) {
  throw new Error('Elemento #root não encontrado no index.html: a aplicação não pode ser montada.')
}

createRoot(container).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
