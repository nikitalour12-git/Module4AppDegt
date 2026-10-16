using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Module4AppDegt.Model;
using Module4AppDegt.View;


namespace Module4AppDegt.Model
{
    public class Database
    {
        private static SqlConnection SqlConnection = new SqlConnection(
            "Data Source=510EC9\\SQLEXPRESS;Initial Catalog=DEMO1;Integrated Security=True;"
        );

        /// <summary>
        /// Проверка логина/пароля. Возвращает объект User при успехе и null при неудаче
        /// или если пользователь заблокирован.
        /// </summary>
        public static User GetUser(string login, string password)
        {
            try
            {
                // Проверяем сначала блокировку — чтобы пользователь сразу понял причину
                if (IsUserBlocked(login))
                {
                    MessageBox.Show("Вы заблокированы. Обратитесь к администратору.",
                        "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                string query = "SELECT Login, Password, Role, IsBlocked FROM Users " +
                               "WHERE Login = @login AND Password = @password";

                using (SqlConnection connection = new SqlConnection(SqlConnection.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Параметры вместо конкатенации — защита от SQL-инъекций
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", password);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable table = new DataTable();
                            adapter.Fill(table);

                            if (table.Rows.Count == 1)
                            {
                                DataRow row = table.Rows[0];
                                return new User(
                                    row.Field<string>("Login"),
                                    row.Field<string>("Password"),
                                    row.Field<string>("Role"))
                                {
                                    IsBlocked = row.Field<bool>("IsBlocked")
                                };
                            }
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка работы с базой данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Проверка: заблокирован ли пользователь с указанным логином.
        /// </summary>
        public static bool IsUserBlocked(string login)
        {
            try
            {
                const string query = "SELECT IsBlocked FROM Users WHERE Login = @login";

                using (SqlConnection connection = new SqlConnection(SqlConnection.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        object result = command.ExecuteScalar();

                        // Если пользователя нет — считаем, что он не заблокирован,
                        // его просто нет в БД, и это отдельная ошибка ввода.
                        if (result == null || result == DBNull.Value) return false;
                        return Convert.ToBoolean(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки блокировки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Блокировка пользователя в БД.
        /// </summary>
        public static void BlockUser(string login)
        {
            try
            {
                const string query = "UPDATE Users SET IsBlocked = 1 WHERE Login = @login";

                using (SqlConnection connection = new SqlConnection(SqlConnection.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось заблокировать пользователя: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
