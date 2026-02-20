@Security @Transversal
Feature: Fluxo de Segurança
  Como cliente da API Minerva Foods
  Eu quero que endpoints protegidos exijam autenticação
  Para garantir que apenas usuários autorizados acessem os recursos

  Scenario: Qualquer chamada sem token retorna 401 Unauthorized
    Given que estou usando um cliente sem token de autenticação
    When eu faço uma requisição GET para "api/v1/Orders"
    Then o status code da resposta deve ser 401
    When eu faço uma requisição POST para "api/v1/Users" sem token
    Then o status code da resposta deve ser 401
