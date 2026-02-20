@Auth @E2E
Feature: Autenticação (Login)
  Como usuário da API de gestão de pedidos
  Eu quero realizar login com matrícula e senha
  Para obter um token de acesso aos recursos protegidos

  Scenario: Login com credenciais válidas retorna 200 e token
    Given que sou um usuário autenticável com matrícula "admin" e senha "Admin@123"
    When eu realizo login na API
    Then o status code da resposta deve ser 200
    And o corpo da resposta deve conter "accessToken", "expiresIn" e "user"

  Scenario: Credenciais inválidas retornam 401 Não autorizado
    Given que sou um usuário com matrícula "admin" e senha incorreta "SenhaErrada"
    When eu realizo login na API
    Then o status code da resposta deve ser 401
    And a resposta deve indicar erro de autenticação

  Scenario: Payload inválido (sem matrícula e senha) retorna 400
    Given que eu envio um payload de login vazio
    When eu realizo login na API
    Then o status code da resposta deve ser 400
    And a mensagem de erro deve conter "obrigat"

  Scenario: Falha de infraestrutura no login retorna 500 com mensagem amigável
    Given que o servidor está configurado para simular falha no serviço de autenticação
    When eu realizo login na API com matrícula "admin" e senha "Admin@123"
    Then o status code da resposta deve ser 500
    And o detalhe da resposta deve ser "Ocorreu um erro interno no servidor."
