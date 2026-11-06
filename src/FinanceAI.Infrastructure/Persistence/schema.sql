-- Idempotent: runs on every startup.
CREATE TABLE IF NOT EXISTS categories (
    id    SERIAL       PRIMARY KEY,
    name  VARCHAR(60)  NOT NULL UNIQUE,
    type  INTEGER      NOT NULL CHECK (type IN (1, 2)) -- 1 = income, 2 = expense
);

CREATE TABLE IF NOT EXISTS transactions (
    id                        BIGSERIAL      PRIMARY KEY,
    description               VARCHAR(200)   NOT NULL,
    amount                    NUMERIC(14, 2) NOT NULL CHECK (amount > 0),
    date                      DATE           NOT NULL,
    category_id               INTEGER        NOT NULL REFERENCES categories (id),
    category_suggested_by_ai  BOOLEAN        NOT NULL DEFAULT FALSE,
    created_at                TIMESTAMPTZ    NOT NULL DEFAULT NOW()
);

-- Covers the date-range filter and the monthly summary GROUP BY.
CREATE INDEX IF NOT EXISTS ix_transactions_date_category ON transactions (date, category_id);

INSERT INTO categories (name, type) VALUES
    ('Salário', 1),
    ('Rendimentos', 1),
    ('Alimentação', 2),
    ('Transporte', 2),
    ('Moradia', 2),
    ('Saúde', 2),
    ('Educação', 2),
    ('Lazer', 2),
    ('Assinaturas', 2),
    ('Compras', 2),
    ('Outros', 2)
ON CONFLICT (name) DO NOTHING;
