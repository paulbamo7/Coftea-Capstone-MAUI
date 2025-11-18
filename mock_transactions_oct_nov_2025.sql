-- Mock Transaction Data for Sales Report
-- Date Range: October 1-31, 2025
-- 30 transactions with various products, sizes, and payment methods

-- ===================== TRANSACTIONS TABLE =====================
INSERT INTO transactions (userID, total, transactionDate, status, paymentMethod) VALUES
-- October 1st (3 transactions)
(1, 139.00, '2025-10-01 08:15:30', 'Completed', 'Cash'),
(1, 88.00, '2025-10-01 14:22:45', 'Completed', 'Cash'),
(1, 49.00, '2025-10-01 18:30:12', 'Completed', 'GCash'),

-- October 2nd (2 transactions)
(1, 117.00, '2025-10-02 09:45:28', 'Completed', 'Cash'),
(1, 78.00, '2025-10-02 16:12:55', 'Completed', 'Cash'),

-- October 3rd (2 transactions)
(1, 196.00, '2025-10-03 10:30:20', 'Completed', 'GCash'),
(1, 39.00, '2025-10-03 15:45:10', 'Completed', 'Bank'),

-- October 4th (1 transaction)
(1, 147.00, '2025-10-04 11:20:15', 'Completed', 'Cash'),

-- October 5th (2 transactions)
(1, 59.00, '2025-10-05 09:35:40', 'Completed', 'Cash'),
(1, 98.00, '2025-10-05 17:50:22', 'Completed', 'GCash'),

-- October 6th (1 transaction)
(1, 127.00, '2025-10-06 12:15:33', 'Completed', 'Cash'),

-- October 7th (1 transaction)
(1, 39.00, '2025-10-07 13:40:18', 'Completed', 'Cash'),

-- October 8th (2 transactions)
(1, 156.00, '2025-10-08 15:05:42', 'Completed', 'GCash'),
(1, 69.00, '2025-10-08 19:30:25', 'Completed', 'Bank'),

-- October 10th (1 transaction)
(1, 108.00, '2025-10-10 09:45:50', 'Completed', 'Cash'),

-- October 12th (1 transaction)
(1, 78.00, '2025-10-12 11:10:35', 'Completed', 'Cash'),

-- October 14th (1 transaction)
(1, 147.00, '2025-10-14 12:25:12', 'Completed', 'Bank'),

-- October 15th (2 transactions)
(1, 49.00, '2025-10-15 13:50:28', 'Completed', 'Cash'),
(1, 166.00, '2025-10-15 18:15:40', 'Completed', 'Cash'),

-- October 17th (1 transaction)
(1, 88.00, '2025-10-17 09:30:15', 'Completed', 'Cash'),

-- October 18th (1 transaction)
(1, 128.00, '2025-10-18 10:45:30', 'Completed', 'GCash'),

-- October 20th (1 transaction)
(1, 98.00, '2025-10-20 11:35:20', 'Completed', 'Bank'),

-- October 22nd (1 transaction)
(1, 137.00, '2025-10-22 14:50:15', 'Completed', 'Cash'),

-- October 24th (2 transactions)
(1, 49.00, '2025-10-24 10:15:40', 'Completed', 'GCash'),
(1, 78.00, '2025-10-24 17:30:25', 'Completed', 'Cash'),

-- October 25th (1 transaction)
(1, 167.00, '2025-10-25 12:45:30', 'Completed', 'Cash'),

-- October 27th (1 transaction)
(1, 58.00, '2025-10-27 15:20:10', 'Completed', 'Bank'),

-- October 29th (2 transactions)
(1, 108.00, '2025-10-29 09:40:55', 'Completed', 'GCash'),
(1, 87.00, '2025-10-29 16:15:20', 'Completed', 'Cash'),

-- October 31st (1 transaction)
(1, 149.00, '2025-10-31 13:30:45', 'Completed', 'Cash');

-- ===================== TRANSACTION_ITEMS TABLE =====================
-- Transaction 1 (Oct 1)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(12, 1, 'Latte', 1, 39.00, 39.00, 0.00, 0.00, 0.00, 'No add-ons', 'Small: 1'),
(12, 11, 'Chocolate Latte', 2, 78.00, 78.00, 0.00, 0.00, 0.00, 'No add-ons', 'Small: 2'),
(12, 23, 'Caramel Macchiato', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 2 (Oct 1)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(13, 6, 'Hazelnut Brew', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(13, 22, 'Kiwi', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 3 (Oct 1)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(14, 4, 'Americano', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 4 (Oct 2)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(15, 1, 'Latte', 1, 39.00, 39.00, 0.00, 0.00, 0.00, 'No add-ons', 'Small: 1'),
(15, 11, 'Chocolate Latte', 2, 78.00, 78.00, 0.00, 0.00, 0.00, 'No add-ons', 'Small: 2');

-- Transaction 5 (Oct 2)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(16, 9, 'Green Apple', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1'),
(16, 8, 'Hokkaido', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(16, 7, 'Okinawa', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 6 (Oct 3)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(17, 12, 'Passionfruit', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(17, 11, 'Blueberry', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1'),
(17, 10, 'Kiwi', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1'),
(17, 6, 'Hazelnut Brew', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1');

-- Transaction 7 (Oct 3)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(18, 6, 'Hazelnut Brew', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1');

-- Transaction 8 (Oct 4)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(19, 1, 'Latte', 1, 39.00, 39.00, 0.00, 0.00, 0.00, 'No add-ons', 'Small: 1'),
(19, 15, 'Green Apple', 1, 59.00, 0.00, 0.00, 59.00, 0.00, 'No add-ons', 'Large: 1'),
(19, 14, 'Lychee', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1');

-- Transaction 9 (Oct 5)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(20, 13, 'Strawberry', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 10 (Oct 5)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(21, 18, 'Lychee', 1, 49.00, 0.00, 49.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(21, 17, 'Kiwi', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 11 (Oct 6)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(22, 17, 'Kiwi', 1, 59.00, 0.00, 0.00, 59.00, 0.00, 'No add-ons', 'Large: 1'),
(22, 16, 'Blueberry', 1, 59.00, 0.00, 0.00, 59.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 12 (Oct 7)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(23, 21, 'Wintermelon', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1'),
(23, 20, 'Passionfruit', 1, 59.00, 0.00, 0.00, 59.00, 0.00, 'No add-ons', 'Large: 1'),
(23, 19, 'Strawberry', 1, 49.00, 0.00, 49.00, 0.00, 0.00, 'No add-ons', 'Medium: 1');

-- Transaction 13 (Oct 8)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(24, 24, 'Cookies & Cream', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1'),
(24, 23, 'Matcha', 1, 49.00, 0.00, 49.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(24, 22, 'Taro', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 14 (Oct 8)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(25, 27, 'Original', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 15 (Oct 10)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(26, 26, 'Brown Sugar', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(26, 25, 'Chocolate', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1'),
(26, 30, 'Java Chip', 1, 69.00, 0.00, 0.00, 69.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 16 (Oct 12)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(27, 26, 'Brown Sugar', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1'),
(27, 25, 'Chocolate', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1');

-- Transaction 17 (Oct 14)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(28, 26, 'Brown Sugar', 2, 78.00, 0.00, 78.00, 0.00, 0.00, 'No add-ons', 'Medium: 2'),
(28, 30, 'Java Chip', 1, 69.00, 0.00, 0.00, 69.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 18 (Oct 15)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(29, 30, 'Java Chip', 2, 128.00, 0.00, 59.00, 69.00, 0.00, 'No add-ons', 'Multiple');

-- Transaction 19 (Oct 15)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(30, 29, 'Cookies And Cream', 1, 59.00, 0.00, 59.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(30, 28, 'Cheesecake', 1, 69.00, 0.00, 0.00, 69.00, 0.00, 'No add-ons', 'Large: 1'),
(30, 1, 'Latte', 1, 39.00, 39.00, 0.00, 0.00, 0.00, 'No add-ons', 'Small: 1');

-- Transaction 20 (Oct 17)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(31, 33, 'Vanilla Coffee', 1, 59.00, 0.00, 59.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(31, 32, 'Strawberry', 1, 69.00, 0.00, 0.00, 69.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 21 (Oct 18)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(32, 11, 'Chocolate Latte', 1, 39.00, 39.00, 0.00, 0.00, 0.00, 'No add-ons', 'Small: 1'),
(32, 4, 'Americano', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1'),
(32, 6, 'Hazelnut Brew', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1');

-- Transaction 23 (Oct 20)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(34, 8, 'Hokkaido', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(34, 7, 'Okinawa', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 24 (Oct 22)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(35, 12, 'Passionfruit', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(35, 11, 'Blueberry', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1'),
(35, 10, 'Kiwi', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 25 (Oct 24)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(36, 15, 'Green Apple', 1, 59.00, 0.00, 0.00, 59.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 26 (Oct 24)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(37, 14, 'Lychee', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(37, 13, 'Strawberry', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 27 (Oct 25)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(38, 18, 'Lychee', 1, 49.00, 0.00, 49.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(38, 17, 'Kiwi', 1, 59.00, 0.00, 0.00, 59.00, 0.00, 'No add-ons', 'Large: 1'),
(38, 16, 'Blueberry', 1, 59.00, 0.00, 0.00, 59.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 28 (Oct 27)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(39, 21, 'Wintermelon', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');

-- Transaction 29 (Oct 29)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(40, 20, 'Passionfruit', 1, 59.00, 0.00, 0.00, 59.00, 0.00, 'No add-ons', 'Large: 1'),
(40, 19, 'Strawberry', 1, 49.00, 0.00, 49.00, 0.00, 0.00, 'No add-ons', 'Medium: 1');

-- Transaction 30 (Oct 29)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(41, 24, 'Cookies & Cream', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1'),
(41, 23, 'Matcha', 1, 49.00, 0.00, 49.00, 0.00, 0.00, 'No add-ons', 'Medium: 1');

-- Transaction 31 (Oct 31)
INSERT INTO transaction_items (transactionID, productID, productName, quantity, price, smallPrice, mediumPrice, largePrice, addonPrice, addOns, size) VALUES
(42, 22, 'Taro', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1'),
(42, 27, 'Original', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1'),
(42, 26, 'Brown Sugar', 1, 39.00, 0.00, 39.00, 0.00, 0.00, 'No add-ons', 'Medium: 1'),
(42, 25, 'Chocolate', 1, 49.00, 0.00, 0.00, 49.00, 0.00, 'No add-ons', 'Large: 1');
