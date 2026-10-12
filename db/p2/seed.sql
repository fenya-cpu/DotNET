INSERT INTO training_types (name, description) VALUES
('CrossFit', 'Силове тренування'),
('Yoga', 'Розтяжка та медитація')
ON CONFLICT (name) DO NOTHING;

INSERT INTO gym_halls (name, capacity) VALUES
('Зал A', 20)
ON CONFLICT DO NOTHING;

INSERT INTO trainers (full_name, email, specialization) 
VALUES ('Олексій Ткаченко', 'oleksiy@fitpulse.com', 'Силовий тренінг')
ON CONFLICT (email) DO NOTHING;