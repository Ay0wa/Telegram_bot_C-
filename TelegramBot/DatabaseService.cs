using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace TeacherReviewBot
{
    public class DatabaseService
    {
        private const string ConnectionString = "Data Source=teachers.db";

        public DatabaseService()
        {
            InitializeDatabase();
            SeedData(); // Добавим начальные данные
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string createTeachersTable = @"CREATE TABLE IF NOT EXISTS Teachers (
                                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Name TEXT NOT NULL)";
                var command = new SQLiteCommand(createTeachersTable, connection);
                command.ExecuteNonQuery();

                string createReviewsTable = @"CREATE TABLE IF NOT EXISTS Reviews (
                                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                TeacherId INTEGER NOT NULL,
                                                Text TEXT NOT NULL,
                                                FOREIGN KEY(TeacherId) REFERENCES Teachers(Id))";
                command = new SQLiteCommand(createReviewsTable, connection);
                command.ExecuteNonQuery();
            }
        }

        private void SeedData()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("DELETE FROM Teachers; DELETE FROM Reviews;", connection);
                command.ExecuteNonQuery();
                // Проверка, если данные уже существуют
                var checkCommand = new SQLiteCommand("SELECT COUNT(*) FROM Teachers", connection);
                long count = (long)checkCommand.ExecuteScalar();

                if (count == 0) // Если данных нет, добавляем начальные данные
                {
                    var insertCommand = new SQLiteCommand("INSERT INTO Teachers (Name) VALUES (@name)", connection);

                    var teacherNames = new List<string>
                    {
                        "Бородачева Л.В.",
                        "Меньшикова Н.П.",
                        "Лещенкова Е.О.",
                        "Норин В.П",
                        "Рудяк Ю.В."
                    };

                    foreach (var name in teacherNames)
                    {
                        insertCommand.Parameters.Clear();
                        insertCommand.Parameters.AddWithValue("@name", name);
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<Teacher> GetTeachers()
        {
            var teachers = new List<Teacher>();
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT * FROM Teachers", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        teachers.Add(new Teacher
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }
            }
            return teachers;
        }

        public Teacher GetTeacherByName(string name)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT * FROM Teachers WHERE Name = @name", connection);
                command.Parameters.AddWithValue("@name", name);
                Console.WriteLine(name);
                Console.WriteLine(command);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Teacher
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        };
                    }
                }
            }
            return null;
        }

        public List<Review> GetReviewsByTeacher(int teacherId)
        {
            var reviews = new List<Review>();
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT * FROM Reviews WHERE TeacherId = @teacherId", connection);
                command.Parameters.AddWithValue("@teacherId", teacherId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reviews.Add(new Review
                        {
                            Id = reader.GetInt32(0),
                            TeacherId = reader.GetInt32(1),
                            Text = reader.GetString(2)
                        });
                    }
                }
            }
            return reviews;
        }
        public double GetAverageRateByTeacher(int teacherId)
        {
            double SumRates = 0;
            double CountRates = 0;
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT * FROM Reviews WHERE TeacherId = @teacherId", connection);
                command.Parameters.AddWithValue("@teacherId", teacherId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SumRates += reader.GetInt32(3);
                        CountRates += 1;
                    }
                }
            }
            return SumRates/CountRates;
        }

        

        public void AddReview(int teacherId, string text)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("INSERT INTO Reviews (TeacherId, Text) VALUES (@teacherId, @text)", connection);
                command.Parameters.AddWithValue("@teacherId", teacherId);
                command.Parameters.AddWithValue("@text", text);
                command.ExecuteNonQuery();
            }
        }
    }

    public class Teacher
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Review
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public string Text { get; set; }

        public int Rate {get; set;}
    }
}
