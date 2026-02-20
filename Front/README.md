# Minerva Foods — Front-end (React/Vite)

> Documentação técnica do front-end do Sistema de Gestão de Pedidos Minerva, voltada a desenvolvedores e stakeholders técnicos.

---

## Índice

- [Visão Geral da Arquitetura](#-visão-geral-da-arquitetura)
- [Stack Tecnológica](#-stack-tecnológica)
- [Guia de Setup com Docker](#-guia-de-setup---com-docker-recomendado)
- [Guia de Setup sem Docker](#-guia-de-setup---sem-docker-local)
- [Considerações de Build](#-considerações-de-build)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Autenticação e API](#-autenticação-e-api)
- [Mock API (MSW)](#-mock-api-msw)
- [Testes](#-testes)

---

## Visão Geral da Arquitetura

### Arquitetura proposta

O front-end segue uma **arquitetura em camadas inspirada em Clean Architecture**, com **organização baseada em features e componentes reutilizáveis**:

| Camada | Responsabilidade |
|--------|-------------------|
| **`app/`** | Esqueleto da aplicação: providers, rotas, layout e estilos globais. |
| **`core/`** | Infraestrutura e singletons: cliente HTTP (Axios), autenticação, query client. |
| **`features/`** | Módulos de negócio (auth, orders, dashboard), com páginas, formulários e hooks de API. |
| **`shared/`** | Componentes de UI, modelos de dados e utilitários compartilhados. |

A UI é construída com **componentes funcionais** e composição; não há adoção formal de Atomic Design, mas os componentes em `shared/components/` (Button, Input, Badge, Card, Modal, etc.) funcionam como átomos/moléculas reutilizáveis, e as features compõem telas e fluxos completos.

### Fluxo de dados

- **Context API (React)**: O estado de **autenticação** (usuário, token, login/logout) é centralizado no `AuthProvider` (`core/auth/use-auth.tsx`). O token é persistido em `localStorage` sob a chave `@Minerva:Auth` e reidratado na carga da página (F5), com suporte a **silent refresh** antes da expiração.
- **TanStack React Query**: Usado para **dados assíncronos da API** (listagem de pedidos, clientes, condições de pagamento). Garante cache, refetch e estados de loading/erro de forma declarativa, reduzindo estado local e requisições duplicadas.
- **Hooks customizados**: Cada feature expõe hooks (ex.: `useOrders`, `useAuth`) que encapsulam chamadas à API e regras de negócio, mantendo as páginas focadas em apresentação.

Não há Redux; a combinação Context (auth) + React Query (server state) atende aos requisitos atuais com menos boilerplate e boa rastreabilidade.

### Justificativa das escolhas

| Tecnologia | Motivação |
|------------|-----------|
| **Vite** | Build e HMR extremamente rápidos (ESM nativo, Rollup em produção). Melhor DX e ciclos de feedback curtos no desenvolvimento. |
| **React** | Ecossistema maduro, componentes reutilizáveis e performance via Virtual DOM e reconciliação. Amplamente adotado e com boa documentação. |
| **Axios** | Integração com a API Minerva: baseURL configurável, interceptors para **Bearer token**, **timeout global** (10s), **headers de rastreamento** (X-Correlation-ID, X-Causation-ID) e tratamento centralizado de erros (401, 503, timeout) com toasts. |

### Variáveis de ambiente e API

A **URL base da API** é definida em tempo de **build** via `VITE_API_URL`. O Vite expõe variáveis que começam com `VITE_` em `import.meta.env`. O cliente Axios (`core/api/axios.ts`) consome assim:

```ts
const baseURL =
  typeof import.meta.env.VITE_API_URL !== 'undefined' && import.meta.env.VITE_API_URL !== ''
    ? import.meta.env.VITE_API_URL
    : '/api'
```

- **Desenvolvimento**: configure `.env` com `VITE_API_URL=http://localhost:5002/api` (ou a URL do back-end Minerva).
- **Produção (Docker)**: `VITE_API_URL` é passada como **ARG/ENV** no estágio de build da imagem, garantindo que o bundle gerado já aponte para a API correta (detalhes em [Considerações de Build](#-considerações-de-build)).

---

## Stack Tecnológica

| Tecnologia | Uso e justificativa |
|------------|----------------------|
| **React + TypeScript** | Tipagem estática, melhor autocomplete e refatoração segura; redução de bugs em tempo de execução e contratos claros com a API. |
| **Tailwind CSS** | Estilização utilitária e consistente, design tokens (ex.: `--minerva-navy`, `--minerva-green`), agilidade na UI e menor contexto de CSS global. Uso de `clsx` e `tailwind-merge` para composição de classes. |
| **Vite** | Bundler e dev server: ESM nativo, HMR rápido, build otimizado para produção. |
| **Axios** | Cliente HTTP para integração com a API Minerva (interceptors, timeout, headers). |
| **TanStack React Query** | Cache e sincronização do estado vindo do servidor (pedidos, clientes, condições de pagamento). |
| **React Router** | Roteamento SPA, rotas protegidas e redirecionamento por perfil. |
| **React Hook Form + Zod** | Formulários controlados, validação tipada e boa performance (menos re-renders). |
| **Docker** | Paridade entre ambientes: mesmo build (Node) e mesmo runtime (Nginx) em dev/prod, evitando “funciona na minha máquina”. |

---

## Guia de Setup — Com Docker (Recomendado)

Garante ambiente reproduzível e alinhado à produção.

### Pré-requisitos

- [Docker](https://docs.docker.com/get-docker/) e [Docker Compose](https://docs.docker.com/compose/install/) instalados.

### Passo a passo

1. **Clone o repositório** (se ainda não tiver) e entre na pasta do front-end:

   ```bash
   cd Front
   ```

2. **(Opcional) Defina a URL da API**  
   Por padrão o `docker-compose` usa `VITE_API_URL=http://localhost:5002/api`. Para alterar, crie um arquivo `.env` na raiz do projeto:

   ```env
   VITE_API_URL=https://sua-api.minerva.com/api
   ```

3. **Suba o container em modo detached, forçando rebuild da imagem**:

   ```bash
   docker-compose up -d --build
   ```

4. **Acesse a aplicação** no navegador:

   ```
   http://localhost:3000
   ```

### Mapeamento de portas

| Host (sua máquina) | Container | Descrição |
|-------------------|-----------|------------|
| **3000** | **80** | O Nginx dentro do container escuta na porta 80; o `docker-compose` mapeia `3000:80`, então você acessa `http://localhost:3000`. |

Para usar outra porta no host (ex.: 8080), altere em `docker-compose.yml`:

```yaml
ports:
  - "8080:80"
```

### Verificar logs do container

```bash
# Logs em tempo real
docker-compose logs -f minerva_frontend

# Últimas N linhas
docker-compose logs --tail=100 minerva_frontend
```

### Parar e remover

```bash
docker-compose down
```

---

## Guia de Setup — Sem Docker (Local)

Para desenvolvimento direto na máquina, com HMR e ferramentas do Node.

### Pré-requisitos

- **Node.js** em versão **LTS** (recomendado 20.x). Verifique:

  ```bash
  node -v
  ```

- npm (já incluso no Node).

### Passo a passo

1. **Instale as dependências**:

   ```bash
   npm install
   ```

2. **Configure o arquivo `.env` na raiz do projeto** (obrigatório para apontar à API):

   ```env
   VITE_API_URL=http://localhost:5002/api
   ```

   Ajuste a URL se o back-end Minerva estiver em outro host ou porta.

3. **Inicie o servidor de desenvolvimento**:

   ```bash
   npm run dev
   ```

4. **Acesse a URL exibida no terminal** (geralmente `http://localhost:5173`).

### Scripts úteis

| Comando | Descrição |
|---------|-----------|
| `npm run dev` | Sobe o dev server (Vite) com HMR. |
| `npm run build` | Compila para produção (`dist/`). Faz `tsc -b` e `vite build`. |
| `npm run preview` | Serve a pasta `dist/` localmente para testar o build. |
| `npm run lint` | Executa o ESLint nos arquivos TypeScript/React. |

---

## Considerações de Build

O front-end é uma **SPA**: o Vite gera HTML, CSS e JS estáticos; em produção eles são servidos pelo **Nginx**.

### Injeção da URL da API no build

As variáveis `VITE_*` são **embutidas no bundle em tempo de build** (não em tempo de execução). Por isso:

1. **Dockerfile (estágio de build)**  
   - `ARG VITE_API_URL=http://localhost:5002/api` permite passar o valor na construção da imagem.  
   - `ENV VITE_API_URL=$VITE_API_URL` garante que o `npm run build` enxergue a variável.  
   - O `docker-compose` repassa: `args: VITE_API_URL: ${VITE_API_URL:-http://localhost:5002/api}`.

2. **Durante `npm run build`**  
   - O Vite substitui todas as ocorrências de `import.meta.env.VITE_API_URL` pelo valor definido no momento do build.  
   - O resultado em `dist/` já contém a URL correta da API; não é possível alterá-la apenas trocando variáveis de ambiente no container em runtime.

3. **Nginx**  
   - O segundo estágio do Dockerfile copia `dist/` para `/usr/share/nginx/html` e usa `nginx.conf` com `try_files $uri $uri/ /index.html` para roteamento SPA.  
   - O Nginx **não injeta** variáveis no JS; ele apenas serve os arquivos estáticos gerados no estágio de build.

**Resumo**: Para mudar a API em produção, é necessário **reconstruir a imagem** (ou o build local) com o `VITE_API_URL` desejado (via `.env` ou `docker-compose`/`docker build --build-arg`).

---

## Estrutura do Projeto

```
src/
├── app/                    # Esqueleto da aplicação
│   ├── providers/          # TanStack Query, AuthContext, React Router, ToastContainer
│   ├── routes/             # Rotas e ProtectedRoute por perfil
│   ├── layout/              # DashboardLayout e navegação
│   └── styles/             # Tailwind + variáveis CSS (index.css)
├── core/                   # Infraestrutura e singletons
│   ├── api/                # Axios (baseURL, interceptors), correlation ID, constants, mocks (MSW)
│   ├── auth/               # AuthProvider, ProtectedRoute, AuthLoadingScreen, useAuth
│   └── services/           # authService, orderService, customerService, paymentConditionService
├── features/               # Módulos de negócio
│   ├── auth/               # Login (página + formulário)
│   ├── dashboard/          # Dashboard (gerente)
│   └── orders/             # Listagem, filtros, criação, hooks (useOrders), regras e schema Zod
└── shared/                 # Reutilizável
    ├── components/         # Button, Input, Badge, Card, Modal, Skeleton, Tooltip
    ├── models/             # Tipos (Order, Customer, OrderItem, Auth, etc.)
    └── utils/              # cn, format (moeda, data), crypto
```

Imports absolutos: `@/app/...`, `@/core/...`, `@/features/...`, `@/shared/...` (configurados no `vite.config` / `tsconfig`).

---

## Autenticação e API

- **Login**: contrato com matrícula/senha; resposta com `accessToken`, `expiresIn` e dados do usuário. Armazenamento em `localStorage` (`@Minerva:Auth`) e reidratação no F5.
- **Interceptor Axios**: envia `Authorization: Bearer <token>` em todas as requisições (exceto login); em 401 pode acionar silent refresh e reenviar a requisição.
- **Rotas protegidas**: `ProtectedRoute` exige autenticação e, quando aplicável, perfil (ex.: MANAGER para `/dashboard`). Redirecionamento para `/login` quando não autenticado.
- **Rastreamento**: todas as requisições incluem `X-Correlation-ID` e `X-Causation-ID`; erros exibidos em toast podem incluir o correlation ID para suporte.

---

## Mock API (MSW)

Em **development** (`npm run dev`), o **Mock Service Worker** pode interceptar chamadas para permitir desenvolvimento sem o back-end:

- **`src/core/api/mocks/`**: `data.ts`, `handlers.ts`, `browser.ts`.
- **POST /api/auth/login**: retorna token e role (MANAGER/ANALYST) conforme o usuário.
- **GET /api/orders** e outros endpoints: exigem `Authorization: Bearer` e retornam dados de exemplo.
- O worker é ativado apenas quando `import.meta.env.DEV` é verdadeiro (ver `main.tsx`).

---

## Testes

A suíte usa **Vitest** como runner, **React Testing Library** para componentes e **MSW** (Mock Service Worker) para interceptar chamadas HTTP, garantindo que os testes não dependam da API real.

- **Configuração**: `vitest.config.ts`, `vitest.setup.ts` e `src/core/api/mocks/server.ts` (MSW em Node).
- **Unitários**: `src/shared/components/__tests__/` (Button, Input, Card).
- **Integração**: `src/features/auth/__tests__/` (login com `registrationNumber`, mensagem 401 em PT) e `src/features/orders/__tests__/` (lista do mock, clique em aprovar, estado de erro 500).

Comandos:

```bash
npm run test        # watch
npm run test:run    # uma execução
npm run test:ui     # interface visual
```

---

## Qualidade e convenções

- Separação de lógica (hooks, services) e UI (componentes/páginas).
- Uso de TypeScript estrito e modelos em `shared/models/` alinhados à API.
- Imports absolutos e estrutura de pastas consistente para escalabilidade e onboarding de novos desenvolvedores.
