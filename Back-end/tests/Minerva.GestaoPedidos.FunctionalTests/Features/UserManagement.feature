@Users @E2E
Feature: Gerenciamento de Usuários
  Como um cliente da API
  Eu quero registrar e consultar usuários
  Para garantir que os dados estão persistidos

  Scenario: Registrar um usuário com sucesso retorna 201
    Given que eu tenho os dados de um usuário válido "Bruno" email "bruno@email.com"
    When eu envio uma requisição POST para "api/v1/Users"
    Then o status code da resposta deve ser 201
    And o corpo da resposta deve conter o ID do usuário

  Scenario: Criar usuário sem token retorna 401
    Given que estou usando um cliente sem token de autenticação
    When eu faço uma requisição POST para "api/v1/Users" sem token
    Then o status code da resposta deve ser 401
    And a mensagem de erro deve indicar "Não autorizado"

  Scenario: Criar usuário com e-mail vazio retorna 400 com mensagem em português
    Given que eu tenho os dados de um usuário com e-mail vazio
    When eu envio uma requisição POST para "api/v1/Users"
    Then o status code da resposta deve ser 400
    And a mensagem de erro deve conter "e-mail"

  Scenario: Falha ao criar usuário retorna 500 com mensagem amigável
    Given que o servidor está configurado para simular falha no repositório de usuários
    When eu envio uma requisição POST para "api/v1/Users" com dados válidos
    Then o status code da resposta deve ser 500
    And o detalhe da resposta deve ser "Ocorreu um erro interno no servidor."
