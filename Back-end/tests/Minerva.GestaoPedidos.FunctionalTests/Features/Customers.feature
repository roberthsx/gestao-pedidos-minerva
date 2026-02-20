@Customers @E2E
Feature: Clientes (Lookup)
  Como usuário da API
  Eu quero listar clientes para preencher seleções
  Para cadastrar pedidos vinculados a clientes

  Scenario: Listar clientes com token retorna 200
    Given que sou um usuário autenticado
    When eu faço uma requisição GET para "api/v1/customers"
    Then o status code da resposta deve ser 200
    And o corpo da resposta deve ser uma lista (possivelmente vazia)

  Scenario: Listar clientes sem token retorna 401
    Given que estou usando um cliente sem token de autenticação
    When eu faço uma requisição GET para "api/v1/customers"
    Then o status code da resposta deve ser 401
    And a mensagem de erro deve indicar "Não autorizado"

  Scenario: Falha ao listar clientes retorna 500 com mensagem amigável
    Given que o servidor está configurado para simular falha no lookup de clientes
    When eu faço uma requisição GET para "api/v1/customers" com token válido
    Then o status code da resposta deve ser 500
    And o detalhe da resposta deve ser "Ocorreu um erro interno no servidor."
