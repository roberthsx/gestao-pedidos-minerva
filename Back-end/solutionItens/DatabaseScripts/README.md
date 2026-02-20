# Scripts de Infraestrutura - Minerva.GestaoPedidos

Scripts para subir e configurar a infraestrutura de dados (PostgreSQL, MongoDB, Kafka, Redis) conforme cada tecnologia.

## Estrutura

| Pasta      | Tecnologia | Descrição |
|-----------|------------|-----------|
| `postgres/` | PostgreSQL | Banco de escrita (EF Core): init, schema, inserts e **drop** (schema ou database). |
| `mongodb/`  | MongoDB    | Banco de leitura (CQRS): collections, índices e **drop** do database. |
| `kafka/`    | Kafka      | Tópicos: criação e **exclusão** de tópicos. |
| `redis/`    | Redis      | Cache (JWT etc.); sem DDL, apenas documentação. |

---

## PostgreSQL (Write Store)

- **00_init_database.sql**  
  Cria o banco `minerva_db` e o usuário `admin` (senha `admin_password`).  
  Executar como superuser:
  ```bash
  psql -U postgres -f postgres/00_init_database.sql
  ```
  Em seguida conectar em `minerva_db` e rodar os GRANTs (ou executar o restante do script conectado a `minerva_db`).

- **01_schema.sql**  
  Cria todas as tabelas do EF Core: **Profiles**, Users, Customers, PaymentConditions, Orders, OrderItems, DeliveryTerms.  
  Executar conectado ao banco:
  ```bash
  psql -U admin -d minerva_db -f postgres/01_schema.sql
  ```
- **02_insert_profiles.sql**  
  Inserts dos perfis: **Admin**, **Gestão**, **Analista** (códigos: ADMIN, GESTAO, ANALISTA). Executar antes dos usuários:
  ```bash
  psql -U admin -d minerva_db -f postgres/02_insert_profiles.sql
  ```
- **02_insert_users.sql**  
  Inserts de usuários de exemplo (com ProfileId). Executar após o schema e após **02_insert_profiles.sql**:
  ```bash
  psql -U admin -d minerva_db -f postgres/02_insert_users.sql
  ```

- **drop_schema.sql**  
  Remove **todas as tabelas** do banco `minerva_db` (mantém o banco). Ordem respeitando FKs.  
  ```bash
  psql -U admin -d minerva_db -f postgres/drop_schema.sql
  ```

- **drop_database.sql**  
  Remove o **banco** `minerva_db`. Executar conectado a outro banco (ex.: `postgres`) como superuser; encerra conexões ativas antes.  
  ```bash
  psql -U postgres -d postgres -f postgres/drop_database.sql
  ```

**Docker (exemplo):**
```bash
docker run -d --name postgres -e POSTGRES_USER=admin -e POSTGRES_PASSWORD=admin_password -e POSTGRES_DB=minerva_db -p 5432:5432 postgres:16-alpine
# Depois: docker exec -i postgres psql -U admin -d minerva_db < solutionItens/DatabaseScripts/postgres/01_schema.sql
```

---

## MongoDB (Read Store)

- **01_init_collections_indexes.js**  
  Cria o banco `minerva_db`, as collections `users` e `orders` e os índices usados pelas queries.  

  Executar com `mongosh`:
  ```bash
  mongosh "mongodb://admin:admin_password@localhost:27017/minerva_db?authSource=admin" < mongodb/01_init_collections_indexes.js
  ```
  Ou a partir da raiz do repositório (solutionItens/DatabaseScripts como working dir):
  ```bash
  mongosh "mongodb://admin:admin_password@localhost:27017/minerva_db?authSource=admin" 01_init_collections_indexes.js
  ```

**Docker (exemplo):**
```bash
docker run -d --name mongodb -e MONGO_INITDB_ROOT_USERNAME=admin -e MONGO_INITDB_ROOT_PASSWORD=admin_password -p 27017:27017 mongo:8
# Depois (a partir da raiz do Back-end):
# docker cp solutionItens/DatabaseScripts/mongodb/01_init_collections_indexes.js mongodb:/tmp/
# docker exec mongodb mongosh -u admin -p admin_password --authenticationDatabase admin minerva_db --file /tmp/01_init_collections_indexes.js
```

- **drop_database.js**  
  Remove o banco **minerva_db** e todas as collections.  
  ```bash
  mongosh "mongodb://admin:admin_password@localhost:27017/minerva_db?authSource=admin" < mongodb/drop_database.js
  ```

---

## Kafka (Mensageria)

- **Fluxo order-created (cálculo de entrega)**  
  Após criar um pedido, a API publica o `OrderId` no tópico **order-created**. O Worker consome essas mensagens, calcula **DeliveryDays = 10** e persiste em **DeliveryTerms** (PostgreSQL). Em caso de falha após **3 tentativas**, a mensagem é enviada para **order-created-dlq** (DLQ). Os tópicos `order-created` e `order-created-dlq` são criados automaticamente pelo serviço **init_kafka** no `docker-compose.yml`.

- **create-topics.sh** (Linux/macOS)  
  Cria o tópico `orders.created` (e outros se descomentados).  
  Variável opcional: `KAFKA_BOOTSTRAP_SERVERS` (padrão `localhost:9092`).

- **create-topics.ps1** (Windows)  
  Equivalente em PowerShell; usa `kafka-topics` no PATH ou dentro do container.

Executar **dentro do container Kafka** (exemplo Apache Kafka KRaft):
```bash
docker exec -it <container_kafka> kafka-topics --bootstrap-server localhost:9092 \
  --create --topic orders.created --partitions 3 --replication-factor 1 --if-not-exists
```

Ou copiar o script para o container e executar:
```bash
docker cp solutionItens/DatabaseScripts/kafka/create-topics.sh <container_kafka>:/tmp/
docker exec -it <container_kafka> bash /tmp/create-topics.sh
```

- **delete-topics.sh** / **delete-topics.ps1** — Exclui os tópicos (ex.: `orders.created`).

---

## Redis (Cache)

Não há scripts de schema. Uso apenas de connection string (ver `redis/README.md`).

---

## Ordem sugerida para subir a infra

1. **PostgreSQL**: `00_init_database.sql` → conectar em `minerva_db` → `01_schema.sql` → `02_insert_profiles.sql` → `02_insert_users.sql`
2. **MongoDB**: `01_init_collections_indexes.js`
3. **Kafka**: criar tópicos com `create-topics.sh` ou comando `kafka-topics`
4. **Redis**: subir o container com `--requirepass` e configurar a connection string na API

As connection strings e variáveis de ambiente estão documentadas no `.env.example` na raiz do Back-end.

---

## Ordem para derrubar (drop)

1. **PostgreSQL**: `drop_schema.sql` (só tabelas) ou `drop_database.sql` (banco inteiro; executar conectado a `postgres`).
2. **MongoDB**: `drop_database.js`.
3. **Kafka**: `delete-topics.sh` ou `delete-topics.ps1` (opcional).
4. **Redis**: não há estrutura a dropar; parar o container se desejar.
