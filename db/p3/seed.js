db = db.getSiblingDB('fitpulse_reviews_db');

db.reviews.deleteMany({});
db.client_activity_logs.deleteMany({});

db.reviews.insertOne({
  trainer_id: 1,
  client_id: 1,
  client_name: "Тарас Шевченко",
  rating: 5,
  text: "Чудовий тренер!",
  tags: ["професіонал", "силові"],
  comments: [
    {
      author_name: "Адміністратор",
      text: "Дякуємо за відгук!",
      created_at: new Date()
    }
  ],
  created_at: new Date()
});

db.client_activity_logs.insertOne({
  client_id: 1,
  activity_type: "STRENGTH",
  title: "Тренування спини",
  exercises: [{ name: "Тяга блоку", sets: 4, reps: 12 }],
  date: new Date()
});