# Documentação Técnica e Guia de Apresentação — Tech Lead

**Sistema de Gestão de Pedidos — Minerva Foods**

Este documento justifica as decisões arquiteturais do front-end, conectando cada escolha a problemas de negócio: **escala**, **segurança** e **manutenção**. Serve como guia de estudo e roteiro para apresentação com autoridade técnica.

---

## 1. Resumo da Arquitetura (O "Porquê")

### Adoção da Arquitetura Modular: Core / Shared / Features / App

A estrutura em quatro pilares não é arbitrária: responde à necessidade de uma **multinacional** (Minerva Foods) operar com times distribuídos, múltiplos domínios e evolução independente de funcionalidades.

| Pilar | Responsabilidade | Justificativa de negócio |
|-------|------------------|---------------------------|
| **Core** | Infraestrutura transversal (API, auth, mocks) | **Baixo acoplamento**: alterações em autenticação ou contrato de API não exigem mudanças em features. Segurança (JWT, guards) fica centralizada e auditável. |
| **Shared** | Componentes, modelos e utilitários reutilizáveis | **Alta coesão** e **redução de carga cognitiva**: um único lugar para Design System e contratos de dados. Facilita consistência visual e tipagem entre times. |
| **Features** | Módulos de domínio (auth, dashboard, orders) | **Isolamento de domínios**: cada feature pode evoluir (e até ser extraída para Micro Front-end) sem impactar as outras. Facilita ownership por squad (ex.: squad Pedidos vs squad Dashboard). |
| **App** | Orquestração (rotas, layout, providers, estilos globais) | **Ponto único de composição**: rotas e guards definem o mapa da aplicação; trocar backend ou adicionar um novo módulo exige mudanças localizadas aqui. |

**Por que é superior ao “padrão comum” React (pastas por tipo: components/, hooks/, utils/)?**

- Em projetos por **tipo**, qualquer nova tela espalha arquivos em várias pastas; o contexto de negócio se dilui.
- Na arquitetura **modular**, o domínio (ex.: `features/orders/`) concentra página, hooks, API e regras. Isso reduz a distância entre “onde está a regra” e “onde está a tela”, permitindo **escalabilidade horizontal** (novos times = novas features) e preparação para **Micro Front-ends** (cada feature pode virar um deployável isolado no futuro).

---

## 2. Padrões de Projeto (Design Patterns) Aplicados

### 2.1 Container / Presenter (Smart vs Dumb Components)

**O que foi feito:** A lógica de dados e side-effects fica em **containers** (páginas e hooks); a apresentação fica em **componentes visuais** reutilizáveis.

- **OrdersListPage** (container): mantém estado (page, limit, sortBy, filters), chama `useOrders(params)` e `useApproveOrder()`, e repassa apenas props para subcomponentes.
- **OrderFilterBar**, **OrderTableHead**, **OrderTableRow**, **OrderPagination**: recebem callbacks e dados via props; não conhecem TanStack Query nem regras de negócio.
- **order-rules.ts** e **orders-api.ts**: regras e chamadas HTTP fora da UI.

**Justificativa:** Facilita **testes** (componentes puros testáveis com props mockadas) e **reutilização** (ex.: tabela de pedidos em outro contexto só trocando o container). Reduz carga cognitiva ao ler a tela: a página é um “orquestrador”; os detalhes de CSS e tabela ficam encapsulados.

### 2.2 RBAC (Role-Based Access Control)

**Implementação:**

- **Roles** definidos em `shared/models/auth.ts`: `MANAGER` e `ANALYST`.
- **AuthGuard**: exige usuário autenticado; redireciona para `/login` com `state.from` para retorno pós-login.
- **RoleGuard**: exige autenticação **e** role em `allowedRoles`; se não autorizado, redireciona para `/orders` ou exibe `AccessDenied`.
- **Rotas:** `/dashboard` → `RoleGuard(allowedRoles: [MANAGER])`; `/orders` → `AuthGuard` (qualquer autenticado).
- **Menu (dashboard-nav-config.ts):** `getVisibleNavItems(role)` filtra itens por `allowedRoles`; Analista não vê “Dashboard”.

**Justificativa de negócio:** Minerva precisa segregar **visão estratégica** (KPIs, apenas Gestores) e **operação** (listagem e aprovação de pedidos, Gestores e Analistas). O RBAC no front-end garante que a UI não exponha rotas/actions indevidas; a autorização real deve ser reforçada no backend em produção.

### 2.3 API Mocking (MSW) — Backend-less Development

**O que foi feito:**

- **MSW (Mock Service Worker)** ativado apenas em `import.meta.env.DEV` (`main.tsx`); intercepta requisições antes de chegarem à rede.
- **Handlers** em `core/api/mocks/handlers.ts`: `POST /api/auth/login`, `GET /api/orders` (com query params), `PUT /api/orders/:id/approve`.
- **GET /api/orders** exige `Authorization: Bearer <token>`; retorna 401 sem token.
- Respostas com **delay (500 ms)** para simular latência e validar estados de loading.

**Justificativa:** O front-end desenvolve e testa contra um **contrato de API estável** (login retorna JWT + role; orders retornam `{ data, total, page, lastPage }`) sem depender do backend. Isso acelera o ciclo de desenvolvimento e garante que **segurança (JWT)** e **formato de resposta** sejam respeitados desde o início. Ao trocar para API real, basta desativar o worker; a camada `core/api` e `features/orders/orders-api.ts` permanecem.

### 2.4 Observer Pattern — TanStack Query (React Query)

**O que foi feito:**

- **useOrders(params):** `queryKey: ['orders', params]` — page, limit, sortBy, order, status, date compõem a chave; mudança de filtro/página gera nova requisição e cache separado.
- **useApproveOrder():** mutation que, em `onSuccess`, invalida `queryKey: ['orders']`, forçando refetch das listas que usam pedidos.
- **QueryClient** configurado em `app/providers`; toda a árvore usa o mesmo cliente.

**Justificativa:** O estado assíncrono (lista de pedidos, loading, erro) deixa de ser gerenciado manualmente com `useState`/`useEffect`. O **cache** evita requisições duplicadas para a mesma combinação de parâmetros; a **invalidação** após aprovação mantém a UI consistente sem refetch manual. Padrão observer (subscription ao estado do servidor) com suporte a retry, deduplicação e devtools.

---

## 3. Regras de Negócio Implementadas

### 3.1 Aprovação manual (threshold R$ 5.000,00)

- **Constante:** `MANUAL_APPROVAL_THRESHOLD = 5_000` em `features/orders/order-rules.ts`.
- **Regra:** `TotalAmount > 5.000` → status **CREATED**, `requiresManualApproval = true`; caso contrário → **PAID**, `requiresManualApproval = false`.
- **Funções exportadas:** `resolveOrderStatus(totalAmount)` e `requiresManualApproval(totalAmount)` para uso em formulários ou validações.
- **UI:** Na listagem, pedidos com `requiresManualApproval` exibem badge “Pendente” e botão “Aprovar”; `PUT /api/orders/:id/approve` atualiza para PAID e remove a flag.

**Justificativa:** Centralizar o valor e a lógica em um único módulo evita duplicação e facilita mudança futura de threshold (ex.: por região ou canal).

### 3.2 Paginação e filtros “server-side” no Mock

- **GET /api/orders** aceita: `page`, `limit`, `sortBy` (date | status), `order` (asc | desc), `status` (CREATED | PAID | CANCELLED | PENDING), `date`, `dateFrom`, `dateTo`.
- **getOrdersFiltered** em `core/api/mocks/data.ts`: aplica filtros e ordenação no array em memória, depois faz slice para a página solicitada; retorna `{ data, total, page, lastPage }`.
- **Filtro PENDING:** quando `status === 'PENDING'`, considera apenas pedidos com `requiresManualApproval === true`.

**Justificativa:** Simular **comportamento server-side** no mock prepara o front para grandes volumes: a UI já consome uma API paginada e filtrada; ao conectar ao backend real, a troca é de implementação, não de contrato. Evita carregar milhares de registros no cliente e mantém performance e escalabilidade.

---

## 4. Guia de Estudo — Talk Points (Perguntas Difíceis)

### 4.1 “Como você lidaria com latência global em uma aplicação usada em várias regiões?”

**Resposta sugerida:** Hoje o front-end consome uma API com delay simulado (500 ms). Em produção, estratégias possíveis com a arquitetura atual: (1) **TanStack Query** já oferece cache por `queryKey`; podemos definir `staleTime` e `cacheTime` maiores para dados menos voláteis, reduzindo round-trips. (2) A **separação Core/Features** permite introduzir um BFF (Backend for Front-end) por região ou um CDN para assets, sem reescrever features. (3) A listagem já é **paginada e filtrada no “servidor”**; em backend real, podemos manter esse contrato e adicionar índices/read replicas por região para reduzir latência de leitura.

### 4.2 “Onde a segurança do JWT poderia falhar e como mitigar?”

**Resposta sugerida:** O JWT é armazenado em **localStorage** e enviado via interceptor Axios em todo request. Riscos: (1) **XSS** — um script malicioso pode ler o token; mitigação: em cenários de maior sensibilidade, considerar httpOnly cookies para o access token ou refresh token. (2) **Token expirado** — hoje não validamos exp; em produção, o backend deve retornar 401 e o front deve limpar storage e redirecionar para login (interceptor de resposta). (3) **Autorização** — o RBAC no front (RoleGuard, menu) é UX; o backend **deve** revalidar role em toda operação sensível (ex.: PUT /orders/:id/approve).

### 4.3 “Por que MSW em vez de mocks manuais ou JSON estático?”

**Resposta sugerida:** MSW intercepta no nível **Service Worker**, ou seja, as requisições passam pelo mesmo fluxo (Axios, interceptor, URL) que em produção. Assim garantimos: (1) **contrato real** — mesmos headers (Authorization), query params e corpo; (2) **comportamento de rede** — delay, 401 sem token; (3) **zero mudança** no código de features ao trocar para API real; (4) testes E2E podem usar os mesmos handlers. Mocks manuais ou JSON estático não testam interceptor nem query params da mesma forma.

### 4.4 “Como a arquitetura facilita a entrada de um novo time no projeto?”

**Resposta sugerida:** Um time novo pode assumir uma **feature** (ex.: `features/orders/`) com fronteiras claras: modelos em `shared/models`, API em `core/api` e na própria `orders-api.ts`, regras em `order-rules.ts`. O time não precisa dominar auth nem dashboard; apenas o contrato da API de pedidos e os tipos. A pasta **features** reduz conflitos de merge e permite convenções internas (ex.: componentes de tabela em `orders/components/`). Para evoluir para Micro Front-ends, cada feature pode ser extraída para um repositório/deploy com roteamento por prefixo.

### 4.5 “Como você testaria a regra de aprovação manual sem depender da UI?”

**Resposta sugerida:** A regra está isolada em `order-rules.ts` (funções puras `resolveOrderStatus` e `requiresManualApproval`). Basta **testes unitários** com vários valores de `totalAmount` (acima e abaixo de 5.000) e conferir status e flag. O mock em `data.ts` usa a mesma lógica ao gerar pedidos (CREATED + totalAmount > 5_000 → requiresManualApproval). A UI e a API apenas **consumem** essa regra; testes de integração podem validar que GET/orders retorna pedidos com a flag correta e que PUT/approve altera o estado conforme esperado.

---

## 5. Estrutura de Pastas Explicada

```
src/
├── app/                          # Orquestração da aplicação
│   ├── App.tsx                   # Raiz da árvore de componentes
│   ├── layout/
│   │   ├── DashboardLayout.tsx   # Sidebar (logo, nav, logout) + outlet
│   │   └── dashboard-nav-config.ts # Itens de menu por role (RBAC)
│   ├── providers/
│   │   └── index.tsx             # QueryClientProvider + AuthProvider
│   ├── routes/
│   │   └── index.tsx             # Rotas + AuthGuard / RoleGuard
│   └── styles/
│       └── index.css             # Tailwind + variáveis globais
│
├── core/                         # Infraestrutura transversal
│   ├── api/
│   │   ├── axios.ts              # Instância Axios + interceptor JWT
│   │   ├── constants.ts          # Chaves localStorage (token, role)
│   │   ├── auth-api.ts           # loginWithApi (POST /api/auth/login)
│   │   ├── query-client.ts       # Configuração TanStack Query
│   │   └── mocks/
│   │       ├── browser.ts        # MSW worker (start)
│   │       ├── data.ts           # Dados em memória + getOrdersFiltered
│   │       └── handlers.ts        # Handlers MSW (login, orders, approve)
│   └── auth/
│       ├── use-auth.tsx          # AuthProvider + useAuth (token, role, login, logout)
│       ├── AuthGuard.tsx         # Redireciona para /login se não autenticado
│       ├── RoleGuard.tsx         # Exige role em allowedRoles
│       ├── AccessDenied.tsx      # Tela de acesso negado
│       └── types.ts              # AuthContextValue
│
├── features/                     # Módulos de domínio
│   ├── auth/
│   │   ├── LoginPage.tsx         # Container da tela de login
│   │   └── LoginForm.tsx         # Formulário (react-hook-form) + submit
│   ├── dashboard/
│   │   └── Dashboard.tsx        # Home (KPIs, gráfico, top clientes) — useOrders
│   └── orders/
│       ├── OrdersListPage.tsx    # Container: estado, useOrders, useApproveOrder
│       ├── orders-api.ts        # listOrders(params), approveOrder(id), tipos
│       ├── use-orders.ts        # useOrders(params), useApproveOrder()
│       ├── order-rules.ts       # MANUAL_APPROVAL_THRESHOLD, resolveOrderStatus
│       └── components/         # UI da listagem (tabela, filtros, paginação)
│           ├── OrderFilterBar.tsx
│           ├── OrderTableHead.tsx
│           ├── OrderTableRow.tsx
│           ├── OrderPagination.tsx
│           ├── OrderTableSkeleton.tsx
│           ├── OrderEmptyState.tsx
│           └── order-table-styles.ts
│
├── shared/                       # Reutilizável entre features
│   ├── components/               # Button, Card, Input, Badge, Skeleton, Tooltip
│   ├── models/                   # Order, OrderStatus, Role, Customer, etc.
│   └── utils/                   # cn (clsx + tailwind-merge), formatCurrency, formatOrderDate
│
└── main.tsx                      # enableMocking (MSW em DEV) + createRoot
```

**Responsabilidade por camada:**

- **app:** Define *como* a aplicação é montada (rotas, layout, providers) e estilos globais. Não contém lógica de negócio.
- **core:** API (Axios, JWT, MSW), autenticação e autorização. Qualquer feature que precise de “login” ou “chamar backend” depende da core.
- **features:** Lógica e UI por domínio (auth, dashboard, orders). Podem usar core e shared; não devem depender de outras features.
- **shared:** Componentes e tipos que não pertencem a um domínio único; reduz duplicação e mantém consistência.

---

## 6. Manual de Setup

### Pré-requisitos

- Node.js 18+
- npm ou yarn

### Comandos

```bash
# Instalar dependências
npm install

# Desenvolvimento (MSW ativo, hot reload)
npm run dev

# Build de produção (MSW não é incluído)
npm run build

# Preview do build
npm run preview
```

Acesse a URL exibida no terminal (ex.: `http://localhost:5173`).

### Credenciais do Mock (MSW)

| Usuário (username) | Senha   | Role    | Comportamento após login      |
|--------------------|---------|---------|-------------------------------|
| `manager` ou `admin` | qualquer | MANAGER | Redireciona para **Dashboard**; vê menu Dashboard + Pedidos. |
| Qualquer outro     | qualquer | ANALYST | Redireciona para **Pedidos**; vê apenas menu Pedidos.        |

O mock não valida senha; apenas o **username** define o role (contém “admin” ou “manager” → MANAGER).

### Como testar o fluxo de aprovação

1. Faça login com um usuário **MANAGER** ou **ANALYST** (ex.: `manager` / `123`).
2. Acesse **Pedidos** (ou clique em “Pedidos” no menu).
3. No filtro **Status**, escolha **Pendente** para listar só pedidos aguardando aprovação.
4. Em uma linha com badge “Pendente” e botão **Aprovar**, clique em **Aprovar**.
5. Após o loading, a lista é atualizada (TanStack Query invalida o cache); o pedido passa a “Pago” e o botão some.
6. (Opcional) Abra DevTools → Network e confira: `PUT /api/orders/ORD-xxxx/approve` com header `Authorization: Bearer jwt-mock-token`.

### Dados do Mock

- **30 pedidos** gerados em memória (IDs ORD-1001 a ORD-1030), distribuídos nos últimos 30 dias.
- Status: mix de **CREATED**, **PAID** e **CANCELLED**.
- Pedidos **CREATED** com valor **acima de R$ 5.000** têm `requiresManualApproval = true` (aparecem como “Pendente” e podem ser aprovados).

---

*Documento gerado para suporte a apresentação Tech Lead e revisão de arquitetura do sistema Minerva Foods — Gestão de Pedidos.*
