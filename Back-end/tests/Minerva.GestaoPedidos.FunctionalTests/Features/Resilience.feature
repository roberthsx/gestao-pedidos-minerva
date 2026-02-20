@Resilience @Resiliência
Feature: Fluxo de Resiliência
  Como cliente da API Minerva Foods
  Eu quero receber uma resposta de erro amigável quando a infraestrutura falhar
  Para não ser exposto a detalhes técnicos ou páginas de erro brutas

  Scenario: Quando a infraestrutura falha o usuário recebe erro amigável (ProblemDetails)
    Given que o servidor está configurado para simular falha de infraestrutura no repositório de pedidos
    When eu faço uma requisição GET para listar pedidos com token válido
    Then o status code da resposta deve ser 500
    And a resposta deve ser ProblemDetails com status e título e não uma página de erro técnica
    And o detalhe da resposta deve ser "Ocorreu um erro interno no servidor."
