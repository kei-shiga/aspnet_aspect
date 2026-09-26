SET NAMES utf8mb4;

CREATE TABLE account (
    id VARCHAR(16) NOT NULL PRIMARY KEY,
    name VARCHAR(64) NOT NULL,
    balance DECIMAL(18, 2) NOT NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE audit_log (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    message VARCHAR(512) NOT NULL,
    created_at DATETIME NOT NULL
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

INSERT INTO account (id, name, balance) VALUES
    ('A', '口座A', 1000.00),
    ('B', '口座B', 1000.00);
