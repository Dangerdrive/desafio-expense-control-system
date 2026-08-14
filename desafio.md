
Objetivo:
Implementar um sistema de controle de gastos residenciais com:

Cadastros de transações;
Cadastro de pessoas;
Consulta de totais;
Deixar claro qual foi a lógica/função do que foi desenvolvido, através de comentários e documentação no próprio código.

Especificação:
Em linhas gerais, basta que o sistema cumpra os requisitos apresentados.

Tecnologias:
.NET com C# para o back-end e React com Typescript para o front-end. Os dados devem persistir após fechar a aplicação.

Funcionalidades:
Cadastro de pessoas:

Deverá ser implementado um cadastro contendo as funcionalidades básicas de gerenciamento: criação, deleção e listagem.

Em casos que se delete uma pessoa, todas a transações dessa pessoa deverão ser apagadas.

O cadastro de pessoa deverá conter:

Identificador (deve ser único e gerado automaticamente);
Nome;
Idade;


Cadastro de transações:

Deverá ser implementado um cadastro contendo as funcionalidades básicas de gerenciamento: criação e listagem (não é necessário implementar edição/deleção).

Caso a pessoa informada seja menor de idade (menor de 18 anos), apenas despesas poderão ser cadastradas.
O cadastro de transação deverá conter:

Identificador (deve ser único e gerado automaticamente);
Descrição;
Valor;
Tipo (despesa/receita);
Pessoa (identificador da pessoa);

Esse valor precisa existir no cadastro de pessoa;


Consulta de totais:

Deverá listar todas as pessoas cadastradas, exibindo o total de receitas, despesas e o saldo (receita – despesa) de cada uma.

Ao final da listagem, deverá ser exibido o total geral de todas as pessoas, incluindo o total de receitas, o total de despesas e o saldo líquido.



Critérios de avaliação:
A avaliação do teste técnico será baseada nos seguintes pontos:

Aderência às regras de negócio;
Atenção aos detalhes;
Qualidade e legibilidade do código;
Boas práticas.

