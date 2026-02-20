@PaymentConditions @E2E
Feature: Condições de Pagamento (Lookup)
  Como usuário da API
  Eu quero listar condições de pagamento
  Para selecionar à vista ou parcelado ao criar pedidos

  Scenario: Listar condições de pagamento com token retorna 200
    Given que sou um usuário autenticado
    When eu faço uma requisição GET para "api/v1/payment-conditions"
    Then o status code da resposta deve ser 200
    And o corpo da resposta deve ser uma lista (possivelmente vazia)

  Scenario: Listar condições de pagamento sem token retorna 401
    Given que estou usando um cliente sem token de autenticação
    When eu faço uma requisição GET para "api/v1/payment-conditions"
    Then o status code da resposta deve ser 401
    And a mensagem de erro deve indicar "Não autorizado"
