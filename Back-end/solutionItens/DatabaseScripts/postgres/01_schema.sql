-- =============================================================================
-- Minerva.GestaoPedidos - Schema PostgreSQL (tabelas do EF Core - Write Store)
-- PKs incrementais (SERIAL = INTEGER auto-increment).
-- =============================================================================
-- Executar conectado ao banco minerva_db (ex.: psql -U admin -d minerva_db -f 01_schema.sql)
-- Aspas garantem case sensitivity compatível com EF Core.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Profiles (Admin, Gestão, Analista)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Profiles" (
    "Id" SERIAL NOT NULL,
    "Code" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    CONSTRAINT "PK_Profiles" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Profiles_Code" ON "Profiles" ("Code");

-- -----------------------------------------------------------------------------
-- Users
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" SERIAL NOT NULL,
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(200) NOT NULL,
    "Active" BOOLEAN NOT NULL,
    "ProfileId" INTEGER NULL,
    "Matricula" VARCHAR(20) NULL,
    "PasswordHash" VARCHAR(255) NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Profiles_ProfileId" FOREIGN KEY ("ProfileId") REFERENCES "Profiles" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX IF NOT EXISTS "IX_Users_ProfileId" ON "Users" ("ProfileId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Matricula" ON "Users" ("Matricula") WHERE "Matricula" IS NOT NULL;

-- -----------------------------------------------------------------------------
-- Customers
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Customers" (
    "Id" SERIAL NOT NULL,
    "Name" VARCHAR(150) NOT NULL,
    "Email" VARCHAR(150) NOT NULL,
    "CreatedAtUtc" TIMESTAMP NOT NULL,
    CONSTRAINT "PK_Customers" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_Customers_Email" ON "Customers" ("Email");

-- -----------------------------------------------------------------------------
-- PaymentConditions
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "PaymentConditions" (
    "Id" SERIAL NOT NULL,
    "Description" VARCHAR(100) NOT NULL,
    "NumberOfInstallments" INTEGER NOT NULL,
    "CreatedAtUtc" TIMESTAMP NOT NULL,
    CONSTRAINT "PK_PaymentConditions" PRIMARY KEY ("Id")
);

-- -----------------------------------------------------------------------------
-- Orders
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Orders" (
    "Id" SERIAL NOT NULL,
    "CustomerId" INTEGER NOT NULL,
    "PaymentConditionId" INTEGER NOT NULL,
    "OrderDate" TIMESTAMP NOT NULL,
    "TotalAmount" DECIMAL(18,2) NOT NULL,
    "Status" VARCHAR(30) NOT NULL,
    "RequiresManualApproval" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "IdempotencyKey" VARCHAR(64) NULL,
    "ApprovedBy" VARCHAR(20) NULL,
    "ApprovedAt" TIMESTAMP NULL,
    CONSTRAINT "PK_Orders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Orders_Customers_CustomerId" FOREIGN KEY ("CustomerId")
        REFERENCES "Customers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Orders_PaymentConditions_PaymentConditionId" FOREIGN KEY ("PaymentConditionId")
        REFERENCES "PaymentConditions" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_Orders_CustomerId" ON "Orders" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_Orders_PaymentConditionId" ON "Orders" ("PaymentConditionId");
CREATE INDEX IF NOT EXISTS "IX_Orders_Status" ON "Orders" ("Status");
CREATE INDEX IF NOT EXISTS "IX_Orders_OrderDate" ON "Orders" ("OrderDate");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Orders_IdempotencyKey" ON "Orders" ("IdempotencyKey") WHERE "IdempotencyKey" IS NOT NULL;

-- -----------------------------------------------------------------------------
-- OrderItems
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "OrderItems" (
    "Id" SERIAL NOT NULL,
    "OrderId" INTEGER NOT NULL,
    "ProductName" VARCHAR(150) NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "UnitPrice" DECIMAL(18,2) NOT NULL,
    "TotalPrice" DECIMAL(18,2) NOT NULL,
    CONSTRAINT "PK_OrderItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId")
        REFERENCES "Orders" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");

-- -----------------------------------------------------------------------------
-- DeliveryTerms
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "DeliveryTerms" (
    "Id" SERIAL NOT NULL,
    "OrderId" INTEGER NOT NULL,
    "EstimatedDeliveryDate" TIMESTAMP NOT NULL,
    "DeliveryDays" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    CONSTRAINT "PK_DeliveryTerms" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DeliveryTerms_Orders_OrderId" FOREIGN KEY ("OrderId")
        REFERENCES "Orders" ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_DeliveryTerms_OrderId" ON "DeliveryTerms" ("OrderId");
