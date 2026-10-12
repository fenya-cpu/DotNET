db = db.getSiblingDB('fitpulse_reviews_db');

db.reviews.updateOne(
  { review_id: "rev-101" },
  {
    $setOnInsert: {
      review_id: "rev-101",
      trainer_id: 1,
      client_id: 1,
      rating: 5,
      comment_text: "Чудове тренування, тренер дуже уважний!",
      comments: [
        { author: "Адміністратор", text: "Дякуємо за відгук!", created_at: new Date() }
      ],
      created_at: new Date()
    }
  },
  { upsert: true }
);

db.client_activity_logs.updateOne(
  { log_id: "log-501" },
  {
    $setOnInsert: {
      log_id: "log-501",
      client_id: 1,
      activity_type: "Strength",
      duration_minutes: 60,
      calories_burned: 450,
      details: { workout_name: "Powerlifting Heavy", max_weight_kg: 140 },
      logged_at: new Date()
    }
  },
  { upsert: true }
);

db.trainer_ratings.updateOne(
  { trainer_id: 1 },
  {
    $setOnInsert: {
      trainer_id: 1,
      avg_rating: 4.9,
      total_reviews: 25,
      last_updated: new Date()
    }
  },
  { upsert: true }
);