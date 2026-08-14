import { Component, type ErrorInfo, type ReactNode } from 'react';

interface ErrorBoundaryProps {
  children: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
  message: string;
}

/**
 * Error Boundary: captura erros de renderização em qualquer componente filho
 * e exibe uma UI amigável em vez de desmontar a aplicação inteira.
 */
class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false, message: '' };

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, message: error.message || 'Erro inesperado.' };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    // Em produção, enviar para um serviço de monitoramento (Sentry, etc.)
    console.error('ErrorBoundary capturou um erro:', error, info.componentStack);
  }

  handleReset = (): void => {
    this.setState({ hasError: false, message: '' });
  };

  render(): ReactNode {
    if (this.state.hasError) {
      return (
        <div className="error-boundary" role="alert">
          <h2>😵 Algo deu errado</h2>
          <p>{this.state.message}</p>
          <button type="button" className="btn btn-primary" onClick={this.handleReset}>
            Tentar novamente
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
