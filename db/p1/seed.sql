IF NOT EXISTS (SELECT 1 FROM clients WHERE email = 'ivan.petrenko@example.com')
BEGIN
    INSERT INTO clients (external_user_id, full_name, email, phone)
    VALUES (NEWID(), N'Іван Петренко', 'ivan.petrenko@example.com', '+380501112233');
END