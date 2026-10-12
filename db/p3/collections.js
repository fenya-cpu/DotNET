db = db.getSiblingDB('fitpulse_reviews_db');

db.createCollection('reviews');
db.createCollection('client_activity_logs');
db.createCollection('trainer_ratings');

db.reviews.createIndex({ "trainer_id": 1, "created_at": -1 });