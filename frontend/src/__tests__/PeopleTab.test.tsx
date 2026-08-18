/**
 * Testes de componente — PeopleTab isolada.
 *
 * Complementa App.test.tsx cobrindo os caminhos de erro e o fluxo de
 * remoção com o modal de confirmação (incluindo o recuo de página quando
 * a última pessoa da página é removida).
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import PeopleTab from '../components/PeopleTab';
import type { PagedResult, Person } from '../types';

vi.mock('../api', () => ({
  getPeople: vi.fn(),
  createPerson: vi.fn(),
  deletePerson: vi.fn(),
}));

import * as api from '../api';

const getPeople = api.getPeople as ReturnType<typeof vi.fn>;
const createPerson = api.createPerson as ReturnType<typeof vi.fn>;
const deletePerson = api.deletePerson as ReturnType<typeof vi.fn>;

function paged(items: Person[], overrides: Partial<PagedResult<Person>> = {}): PagedResult<Person> {
  return {
    items,
    page: 1,
    pageSize: 10,
    totalItems: items.length,
    totalPages: items.length === 0 ? 0 : 1,
    hasNext: false,
    hasPrevious: false,
    ...overrides,
  };
}

beforeEach(() => {
  vi.clearAllMocks();
  getPeople.mockResolvedValue(paged([]));
});

/** Renderiza e aguarda o fim do carregamento inicial da listagem. */
async function renderSettled() {
  render(<PeopleTab />);
  await waitFor(() => {
    expect(screen.queryByText('Carregando pessoas...')).not.toBeInTheDocument();
  });
}

describe('PeopleTab — listagem', () => {
  it('should show the loading message while fetching', () => {
    getPeople.mockImplementation(() => new Promise(() => { /* nunca resolve */ }));

    render(<PeopleTab />);

    expect(screen.getByText('Carregando pessoas...')).toBeInTheDocument();
  });

  it('should show an error message when the list fails to load', async () => {
    getPeople.mockRejectedValue(new Error('Backend offline'));

    render(<PeopleTab />);

    expect(await screen.findByText('Backend offline')).toBeInTheDocument();
  });

  it('should use the generic fallback when the load error is not an Error', async () => {
    getPeople.mockRejectedValue({ status: 500 });

    render(<PeopleTab />);

    expect(await screen.findByText('Erro ao carregar pessoas.')).toBeInTheDocument();
  });

  it('should flag minors in the list', async () => {
    getPeople.mockResolvedValue(paged([
      { id: 1, name: 'Ana', age: 30 },
      { id: 2, name: 'Bia', age: 10 },
    ]));

    await renderSettled();

    expect(within(screen.getByText('Bia').closest('tr')!).getByText(/🔞/)).toBeInTheDocument();
    expect(screen.getByText('Ana').closest('tr')!.textContent).not.toContain('🔞');
  });
});

describe('PeopleTab — criação', () => {
  it('should reject a blank name without calling the API', async () => {
    const user = userEvent.setup();
    await renderSettled();

    await user.type(screen.getByPlaceholderText('Nome'), '   ');
    await user.type(screen.getByPlaceholderText('Idade'), '30');
    await user.click(screen.getByText('➕ Adicionar'));

    expect(await screen.findByText('Nome é obrigatório.')).toBeInTheDocument();
    expect(createPerson).not.toHaveBeenCalled();
  });

  it('should reject a missing age', async () => {
    const user = userEvent.setup();
    await renderSettled();

    await user.type(screen.getByPlaceholderText('Nome'), 'Ana');
    await user.click(screen.getByText('➕ Adicionar'));

    expect(await screen.findByText('Idade inválida (0-150).')).toBeInTheDocument();
    expect(createPerson).not.toHaveBeenCalled();
  });

  // O input já limita min=0/max=150 no browser; aqui submetemos o form
  // diretamente para exercitar a validação do próprio componente.
  it.each(['-1', '151'])('should reject the out-of-range age "%s"', async (age) => {
    await renderSettled();

    fireEvent.change(screen.getByPlaceholderText('Nome'), { target: { value: 'Ana' } });
    fireEvent.change(screen.getByPlaceholderText('Idade'), { target: { value: age } });
    fireEvent.submit(screen.getByPlaceholderText('Nome').closest('form')!);

    expect(await screen.findByText('Idade inválida (0-150).')).toBeInTheDocument();
    expect(createPerson).not.toHaveBeenCalled();
  });

  it('should trim the name, reset the form and reload the first page', async () => {
    const user = userEvent.setup();
    getPeople.mockResolvedValue(paged([{ id: 1, name: 'Ana', age: 30 }]));
    createPerson.mockResolvedValue({ id: 1, name: 'Ana', age: 30 });

    await renderSettled();

    await user.type(screen.getByPlaceholderText('Nome'), '  Ana  ');
    await user.type(screen.getByPlaceholderText('Idade'), '30');
    await user.click(screen.getByText('➕ Adicionar'));

    await waitFor(() => {
      expect(createPerson).toHaveBeenCalledWith({ name: 'Ana', age: 30 });
    });
    expect(await screen.findByText('Pessoa cadastrada com sucesso!')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nome')).toHaveValue('');
    expect(screen.getByPlaceholderText('Idade')).toHaveValue(null);
    expect(getPeople).toHaveBeenLastCalledWith({ page: 1, pageSize: 10 });
  });

  it('should disable the submit button while saving', async () => {
    const user = userEvent.setup();
    createPerson.mockImplementation(() => new Promise(() => { /* nunca resolve */ }));

    await renderSettled();

    await user.type(screen.getByPlaceholderText('Nome'), 'Ana');
    await user.type(screen.getByPlaceholderText('Idade'), '30');
    await user.click(screen.getByText('➕ Adicionar'));

    expect(await screen.findByText('Salvando...')).toBeDisabled();
  });
});

describe('PeopleTab — remoção', () => {
  it('should not delete when the confirmation is cancelled', async () => {
    const user = userEvent.setup();
    getPeople.mockResolvedValue(paged([{ id: 1, name: 'Ana', age: 30 }]));

    await renderSettled();

    await user.click(screen.getByRole('button', { name: 'Remover Ana' }));
    const dialog = screen.getByRole('dialog');
    expect(within(dialog).getByText(/Remover "Ana"\?/)).toBeInTheDocument();

    await user.click(within(dialog).getByRole('button', { name: 'Cancelar' }));

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(deletePerson).not.toHaveBeenCalled();
  });

  it('should delete the person after confirming', async () => {
    const user = userEvent.setup();
    getPeople.mockResolvedValue(paged([{ id: 1, name: 'Ana', age: 30 }]));
    deletePerson.mockResolvedValue(undefined);

    await renderSettled();

    await user.click(screen.getByRole('button', { name: 'Remover Ana' }));
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Remover' }));

    await waitFor(() => expect(deletePerson).toHaveBeenCalledWith(1));
    expect(await screen.findByText('"Ana" removida.')).toBeInTheDocument();
  });

  it('should go back one page when the last person of the page is removed', async () => {
    const user = userEvent.setup();
    getPeople.mockResolvedValue(paged(
      [{ id: 11, name: 'Zeca', age: 40 }],
      { page: 2, totalItems: 11, totalPages: 2, hasPrevious: true },
    ));
    deletePerson.mockResolvedValue(undefined);

    await renderSettled();

    await user.click(screen.getByRole('button', { name: 'Remover Zeca' }));
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Remover' }));

    await waitFor(() => {
      expect(getPeople).toHaveBeenLastCalledWith({ page: 1, pageSize: 10 });
    });
  });

  it('should show an error when the deletion fails', async () => {
    const user = userEvent.setup();
    getPeople.mockResolvedValue(paged([{ id: 1, name: 'Ana', age: 30 }]));
    deletePerson.mockRejectedValue(new Error('Falha ao remover.'));

    await renderSettled();

    await user.click(screen.getByRole('button', { name: 'Remover Ana' }));
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Remover' }));

    expect(await screen.findByText('Falha ao remover.')).toBeInTheDocument();
  });
});
