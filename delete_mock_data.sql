-- Delete Mock Transaction Data
-- This script removes all mock transactions and their items
-- Run this BEFORE inserting new mock data

-- Delete transaction_items first (due to foreign key constraint)
-- ONLY deletes transactions from October 1-4, 2025 (the mock data period)
DELETE FROM transaction_items 
WHERE transactionID IN (
    SELECT transactionID 
    FROM transactions 
    WHERE transactionDate >= '2025-10-01' 
    AND transactionDate < '2025-10-05'
    AND userID = 1
    AND transactionID > 11
);

-- Delete transactions
-- ONLY deletes transactions from October 1-4, 2025 (the mock data period)
DELETE FROM transactions 
WHERE transactionDate >= '2025-10-01' 
AND transactionDate < '2025-10-31'
AND userID = 1
AND transactionID > 11;

-- Optional: Reset AUTO_INCREMENT if needed (uncomment if you want to reset transaction IDs)
-- ALTER TABLE transactions AUTO_INCREMENT = 12;
-- ALTER TABLE transaction_items AUTO_INCREMENT = 1;

