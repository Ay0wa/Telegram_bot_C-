using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace TeacherReviewBot
{
    public class DatabaseService
    {
        private const string ConnectionString = "Server=localhost; Database=rtbot; Uid=root; Pwd=[root];";
        //комментарий

        public DatabaseService()
        {
            InitializeDatabase();
            SeedData();
        }

        private void InitializeDatabase()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();

                string createTablesSql = @"
                SET SQL_MODE = 'NO_AUTO_VALUE_ON_ZERO';
                START TRANSACTION;
                SET time_zone = '+00:00';

                CREATE TABLE IF NOT EXISTS `added_prof` (
                    `id_add` INT(11) NOT NULL AUTO_INCREMENT,
                    `add_name` VARCHAR(128) NOT NULL,
                    `add_surname` VARCHAR(128) NOT NULL,
                    `add_patronymic` VARCHAR(128) NOT NULL,
                    `add_phone` VARCHAR(128) NOT NULL,
                    `add_email` VARCHAR(128) NOT NULL,
                    `add_img` VARCHAR(128) NOT NULL,
                    `id_auth` INT(11) NOT NULL,
                    PRIMARY KEY (`id_add`),
                    KEY `R_5` (`id_auth`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8;

                CREATE TABLE IF NOT EXISTS `admin` (
                    `id_admin` INT(11) NOT NULL AUTO_INCREMENT,
                    `admin_login` VARCHAR(128) NOT NULL,
                    `admin_password` VARCHAR(128) NOT NULL,
                    PRIMARY KEY (`id_admin`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8;

                CREATE TABLE IF NOT EXISTS `authentication` (
                    `id_auth` INT(11) NOT NULL AUTO_INCREMENT,
                    `auth_name` CHAR(18) DEFAULT NULL,
                    `auth_time` CHAR(18) DEFAULT NULL,
                    PRIMARY KEY (`id_auth`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8;

                CREATE TABLE IF NOT EXISTS `professor` (
                    `id_prof` INT(11) NOT NULL AUTO_INCREMENT,
                    `prof_name` VARCHAR(128) NOT NULL,
                    `prof_surname` VARCHAR(128) NOT NULL,
                    `prof_patronymic` VARCHAR(128) NOT NULL,
                    `prof_phone` VARCHAR(128) NOT NULL,
                    `prof_email` VARCHAR(128) NOT NULL,
                    `prof_img` VARCHAR(128) NOT NULL,
                    PRIMARY KEY (`id_prof`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8;

                CREATE TABLE IF NOT EXISTS `review` (
                    `id_review` CHAR(18) NOT NULL,
                    `rev_content` TEXT NOT NULL,
                    `id_admin` INT(11) NOT NULL,
                    `id_auth` INT(11) NOT NULL,
                    `id_prof` INT(11) NOT NULL,
                    `rev_rating` INT(11) NOT NULL,
                    PRIMARY KEY (`id_review`),
                    KEY `R_2` (`id_admin`),
                    KEY `R_3` (`id_auth`),
                    KEY `R_4` (`id_prof`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8;

                COMMIT;";
            }
        }

        private void SeedData()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();

                var checkExistenceCommand = new MySqlCommand("SELECT COUNT(*) FROM professor", connection);
                var count = Convert.ToInt32(checkExistenceCommand.ExecuteScalar());
                if (count > 0)
                {
                    // Данные уже существуют, не будем добавлять снова
                    return;
                }

                var command = new MySqlCommand(@"INSERT INTO professor (prof_name, prof_surname, prof_patronymic, prof_phone, prof_email, prof_img) VALUES
                ('John', 'Doe', 'A.', '1234567890', 'john.doe@example.com', 'aaa'),
                ('Jane', 'Smith', 'B.', '0987654321', 'jane.smith@example.com', 'aaa')", connection);
                command.ExecuteNonQuery();
            }
        }

        // Остальные методы для работы с данными
        public List<Teacher> GetTeachers()
        {
            var teachers = new List<Teacher>();
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new MySqlCommand("SELECT * FROM professor", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        
                        teachers.Add(new Teacher(reader.GetString("prof_name"), reader.GetString("prof_surname"),
                            reader.GetString("prof_patronymic"),reader.GetString("prof_email"),
                            reader.GetString("prof_phone"), reader.GetString("prof_img"))
                        {
                            Id = reader.GetInt32("id_prof"),
                        });
                    }
                }
            }
            return teachers;
        }

        public Teacher GetTeacherByName(string name)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new MySqlCommand("SELECT * FROM professor WHERE CONCAT(prof_name, ' ', prof_surname) = @name", connection);
                command.Parameters.AddWithValue("@name", name);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Teacher(reader.GetString("prof_name"), reader.GetString("prof_surname"),
                            reader.GetString("prof_patronymic"),reader.GetString("prof_email"),
                            reader.GetString("prof_phone"), reader.GetString("prof_img"))
                        {
                            Id = reader.GetInt32("id_prof"),
                        };
                    }
                }
            }
            return null;
        }

        public List<Review> GetReviewsByTeacher(int teacherId)
        {
            var reviews = new List<Review>();
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new MySqlCommand("SELECT * FROM review WHERE id_prof = @teacherId", connection);
                command.Parameters.AddWithValue("@teacherId", teacherId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reviews.Add(new Review
                        {
                            Id = reader.GetString("id_review"),
                            TeacherId = reader.GetInt32("id_prof"),
                            Text = reader.GetString("rev_content"),
                            
                        });
                    }
                }
            }
            return reviews;
        }
        public void AddTeacher(Teacher teacher)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new MySqlCommand(@"INSERT INTO professor (prof_name, prof_surname, prof_patronymic, prof_phone, prof_email, prof_img) VALUES
                (@name, @surname, @patronymic, @phonenumber, @email, @img)", connection);
                command.Parameters.AddWithValue("@name", teacher.Name);
                command.Parameters.AddWithValue("@surname", teacher.Surname);
                command.Parameters.AddWithValue("@patronymic", teacher.Patronymic);
                command.Parameters.AddWithValue("@phonenumber", teacher.PhoneNumber);
                command.Parameters.AddWithValue("@email", teacher.Email);
                command.Parameters.AddWithValue("@img", teacher.Image);

                command.ExecuteNonQuery();
            }
        }
        public void AddReview(int teacherId, string text)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                var shortGuid = Guid.NewGuid().ToString().Substring(0, 18);
                var command = new MySqlCommand("INSERT INTO review (id_review, rev_content, id_admin, id_auth, id_prof, rev_rating) VALUES (@idReview, @text, @idAdmin, @idAuth, @teacherId, @rating)", connection);
                command.Parameters.AddWithValue("@idReview", shortGuid);
                command.Parameters.AddWithValue("@text", text);
                command.Parameters.AddWithValue("@idAdmin", 1); // Пример: ID администратора, заменить на фактическое значение
                command.Parameters.AddWithValue("@idAuth", 1); // Пример: ID авторизованного пользователя, заменить на фактическое значение
                command.Parameters.AddWithValue("@teacherId", teacherId);
                command.Parameters.AddWithValue("@rating", 5); // Пример: рейтинг, заменить на фактическое значение
                command.ExecuteNonQuery();
            }
        }
        
    }

    public class Teacher
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname {get; set;}
        public string Patronymic {get; set;}
        public string PhoneNumber {get; set;}
        public string Email {get; set;}
        public string Image {get; set;}

        public Teacher(string name, string surname, string patronymic, string phoneNumber, string email, string image)
        {
            Name = name;
            Surname = surname;
            Patronymic = patronymic;
            PhoneNumber = phoneNumber;
            Email = email;
            Image = image;
        }
    }

    public class Review
    {
        public string Id { get; set; }
        public int TeacherId { get; set; }
        public string Text { get; set; }
    }
}
