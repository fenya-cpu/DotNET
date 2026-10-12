CREATE TABLE IF NOT EXISTS trainers (
    trainer_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    specialization VARCHAR(100) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS trainer_details (
    trainer_id BIGINT PRIMARY KEY REFERENCES trainers(trainer_id) ON DELETE CASCADE,
    tax_number VARCHAR(20) NOT NULL UNIQUE,
    experience_years INT NOT NULL CHECK (experience_years >= 0),
    biography TEXT NULL
);

CREATE TABLE IF NOT EXISTS training_types (
    training_type_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255) NULL
);

CREATE TABLE IF NOT EXISTS trainer_training_types (
    trainer_id BIGINT NOT NULL REFERENCES trainers(trainer_id) ON DELETE CASCADE,
    training_type_id INT NOT NULL REFERENCES training_types(training_type_id) ON DELETE CASCADE,
    assigned_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    qualification_level VARCHAR(50) NOT NULL DEFAULT 'Master',
    PRIMARY KEY (trainer_id, training_type_id)
);

CREATE TABLE IF NOT EXISTS gym_halls (
    gym_hall_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    capacity INT NOT NULL CHECK (capacity > 0)
);

CREATE TABLE IF NOT EXISTS schedule_slots (
    schedule_slot_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    trainer_id BIGINT NOT NULL REFERENCES trainers(trainer_id),
    gym_hall_id INT NOT NULL REFERENCES gym_halls(gym_hall_id),
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    end_time TIMESTAMP WITH TIME ZONE NOT NULL,
    price NUMERIC(18,2) NOT NULL CHECK (price >= 0),
    max_participants INT NOT NULL CHECK (max_participants > 0)
);

CREATE INDEX IF NOT EXISTS ix_schedule_slots_trainer_time ON schedule_slots(trainer_id, start_time);