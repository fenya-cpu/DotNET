INSERT INTO clients (external_user_id, full_name, email, phone) 
VALUES ('a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', 'Тарас Шевченко', 'taras@test.com', '+380991234567')
ON CONFLICT (email) DO NOTHING;

INSERT INTO subscriptions (title, price, duration_days)
VALUES ('Безліміт 1 Місяць', 1200.00, 30)
ON CONFLICT DO NOTHING;