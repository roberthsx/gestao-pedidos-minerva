@Order @E2E
Feature: Fluxo de Pedidos (E2E)
  Como usuário da API de gestão de pedidos
  Eu quero realizar login, criar pedidos e aprovar pedidos
  Para validar as regras de negócio de valor e aprovação manual

  Scenario: Pedido pequeno vai direto para Pago
    Given que eu realizei login com matrícula "admin" e senha "Admin@123"
    And existe um cliente e condição de pagamento no banco para o teste
    When eu crio um pedido com valor total menor ou igual a 5000
    Then o status do pedido deve ser "Pago"
    And o pedido deve ter RequiresManualApproval false

  Scenario: Pedido grande exige aprovação manual
    Given que eu realizei login com matrícula "admin" e senha "Admin@123"
    And existe um cliente e condição de pagamento no banco para o teste
    When eu crio um pedido com valor total maior que 5000
    Then o status do pedido deve ser "Criado"
    And o pedido deve ter RequiresManualApproval true

  Scenario: Criar pedido com dados zerados retorna 400 e mensagem de validação
    Given que eu realizei login com matrícula "admin" e senha "Admin@123"
    When eu tento criar um pedido com CustomerId 0 e PaymentConditionId 0 e itens vazios
    Then o status code da resposta deve ser 400
    And o corpo da resposta deve conter mensagem de erro de validação

  Scenario: Cadastrar item de pedido com quantidade zero retorna 400
    Given que eu realizei login com matrícula "admin" e senha "Admin@123"
    And existe um cliente e condição de pagamento no banco para o teste
    When eu tento criar um pedido com um item com quantidade zero
    Then o status code da resposta deve ser 400
    And a mensagem de erro deve ser "A quantidade deve ser maior que zero."

  Scenario: Aprovar pedido manualmente e validar mudança de status para Pago
    Given que eu realizei login com matrícula "admin" e senha "Admin@123"
    And existe um cliente e condição de pagamento no banco para o teste
    When eu crio um pedido com valor total maior que 5000
    And o status do pedido deve ser "Criado"
    When eu aprovo o pedido criado
    Then o status do pedido deve ser "Pago"
