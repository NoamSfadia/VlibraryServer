using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace VlibraryServer
{
    internal class DataHandler
    {
        //user properties
        static string Username;
        static string Password;
        static string Mail;

        //book properties
        static string BookName;
        static string Author;
        static string Genre;
        static string Rating;
        static string Image;
        static string Library;
        static string Summary;


        /// <summary>
        /// Represents the connection string used to connect to the SQL Server database
        /// (represents the database location)
        /// </summary>
        static string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\User\Source\Repos\VlibraryServer\VlibraryServer\UserDatabase.mdf;Integrated Security=True";

        /// <summary>
        /// Represents a connection object to a SQL Server database
        /// </summary>
        static SqlConnection connection = new SqlConnection(connectionString);

        /// <summary>
        /// Represents a SQL command object which is used to define and execute database commands
        /// </summary>
        static SqlCommand cmd = new SqlCommand();

        /// <summary>
        /// The InsertUser method inserts user details into a database table named UsersDetails
        /// It executes an SQL INSERT statement with the provided data and returns the number of affected rows
        /// If an exception occurs, it displays an error message
        /// </summary>
        /// <param name="userdetails"> Represents a string which contains the user details separated by #</param>
        /// <returns>It returns the number of affected rows. If there has been an exception, it returns 0</returns>
        public static int InsertUser(string userdetails)
        {
            try
            {
                string[] data = userdetails.Split('#');
                Username = data[0];
                Password = CreateMD5Hash(data[1]);
                Mail = data[2];


                cmd.Connection = connection;

                string sql = "INSERT INTO UserDetails ([User], Password, Mail, Type) VALUES (@Username, @Password, @Mail, @Type)";

                cmd.CommandText = sql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Username", Username);
                cmd.Parameters.AddWithValue("@Password", Password);
                cmd.Parameters.AddWithValue("@Mail", Mail);
                if (Username == "Manager")
                {
                    cmd.Parameters.AddWithValue("@Type", "Manager");
                }
                else if (Username == "librarian")
                {
                    cmd.Parameters.AddWithValue("@Type", "Librarian");
                }
                else
                {
                    cmd.Parameters.AddWithValue("@Type", "Customer");
                }

                connection.Open();
                int x = cmd.ExecuteNonQuery();
                connection.Close();
                return x;
            }
            catch (Exception ex)
            {

                Console.WriteLine("-" + ex.Message);
                return 0;
            }
        }


        /// <summary>
        /// The isExist method checks if a user with the provided username and password exists in the UsersDetails table of the database
        /// The method executes an SQL SELECT statement to count the number of rows matching the username and password
        /// If the count is greater than 0, it shows that the user exists in the database
        /// If an exception occurs, it displays an error message
        /// Otherwise, it returns false. If If an exception occurs, it displays an error message, it displays an error message and returns false.
        /// </summary>
        /// <param name="details">Represents a string which contains the username and password separated by #</param>
        /// <returns>It returns true if there is a user in the database with the same username and password. Otherwise, it returns false (and if an exception occurs)</returns>
        public static bool isExist(string details)
        {
            try
            {
                string[] data = details.Split('#');
                Username = data[0];
                Password = CreateMD5Hash(data[1]);


                cmd.Connection = connection;
                string sql = "SELECT COUNT(*) FROM UserDetails WHERE [User] = @USERNAME AND [Password] = @PASSWORD";


                cmd.CommandText = sql;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@USERNAME", Username);
                cmd.Parameters.AddWithValue("@PASSWORD", Password);

                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    connection.Open();
                }

                int c = (int)cmd.ExecuteScalar();
                if (c > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
        /// <summary>
        /// Checks if the user is in the database. if it is, return true, else returns false.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static bool isUsernameExist(string username)
        {
            try
            {
                Username = username;

                cmd.Connection = connection;
                string sql = "SELECT COUNT(*) FROM UserDetails WHERE [User] = '" + Username + "'";

                cmd.CommandText = sql;
                connection.Open();
                int c = (int)cmd.ExecuteScalar();
                connection.Close();
                if (c > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Encrypts the data in md5. returns the encrpyted string. 
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        static public string CreateMD5Hash(string Input)//crypts passwords
        {
            // Step 1, calculate MD5 hash from input
            System.Security.Cryptography.MD5 Md5 = System.Security.Cryptography.MD5.Create();
            byte[] InputBytes = System.Text.Encoding.ASCII.GetBytes(Input);
            byte[] HashBytes = Md5.ComputeHash(InputBytes);
            // Step 2, convert byte array to hex string
            StringBuilder StringBuilder = new StringBuilder();
            for (int i = 0; i < HashBytes.Length; i++)
            {
                StringBuilder.Append(HashBytes[i].ToString("X2"));
            }
            return StringBuilder.ToString();
        }



        /// <summary>
        /// Get the email address from the table. 
        /// </summary>
        /// <param name="Username"></param>
        /// <returns></returns>
        public static string GetEmailAddress(string Username)
        {
            try
            {
                string mailAddress = "";

                cmd.Connection = connection;
                string sql = "SELECT Mail FROM UserDetails WHERE [User] = '" + Username + "'";

                cmd.CommandText = sql;
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    connection.Open();
                }
                object result = (string)cmd.ExecuteScalar();


                if (result != null)
                {
                    mailAddress = (string)result;
                }
                return mailAddress;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Input the mail of the user and the new password. updates it.
        /// </summary>
        /// <param name="mail"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public static bool UpdatePassword(string mail, string newPassword)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    newPassword = CreateMD5Hash(newPassword);

                    // Define the SQL update query.
                    string query = "UPDATE UserDetails SET Password = @NewPassword WHERE Mail = @Mail";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@NewPassword", newPassword);
                        command.Parameters.AddWithValue("@Mail", mail);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();

                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        /// <summary>
        /// Input the username of the user and the new password. updates it.
        /// </summary>
        /// <param name="mail"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public static bool UpdatePasswordViaUser(string username, string newPassword)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    newPassword = CreateMD5Hash(newPassword);

                    // Define the SQL update query.
                    string query = "UPDATE UserDetails SET Password = @newPassword WHERE User = @User";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("newPassword", newPassword);
                        command.Parameters.AddWithValue("@User", username);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();

                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        /// <summary>
        /// updates the username in the database.
        /// </summary>
        /// <param name="CurrentUsername"></param>
        /// <param name="newUsername"></param>
        /// <returns></returns>
        public static bool UpdateUsername(string Mail, string newUsername)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Define the SQL update query.
                    string query = "UPDATE UserDetails SET [User] = @Username WHERE [Mail] = @mail";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@Username", newUsername);
                        command.Parameters.AddWithValue("@mail", Mail);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();

                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        /// <summary>
        /// Updates the mail in the database.
        /// </summary>
        /// <param name="newMail"></param> 
        /// <param name="Username"></param>
        /// <returns></returns>
        public static bool UpdateMail(string newMail, string Username)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Define the SQL update query.
                    string query = "UPDATE UserDetails SET [Mail] = @NewUsername WHERE [User] = @Username";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@NewMail", newMail);
                        command.Parameters.AddWithValue("@Username", Username);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();

                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        /// <summary>
        /// Returns the user type inputing the username.
        /// </summary>
        /// <param name="Username"></param>
        /// <returns></returns>
        public static string GetUserType(string Username)
        {
            try
            {
                string type = "";

                cmd.Connection = connection;
                string sql = "SELECT Type FROM UserDetails WHERE [User] = '" + Username + "'";

                cmd.CommandText = sql;
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    connection.Open();
                }
                object result = (string)cmd.ExecuteScalar();

                if (result != null)
                {
                    type = (string)result;
                }
                return type;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
        /// <summary>
        /// Updates the type of the user in the database.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool UpdateType(string username, string type)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Define the SQL update query.
                    string query = "UPDATE UserDetails SET [Type] = @Type WHERE [User] = @Username";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Type", type);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();
                        connection.Close();
                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }

                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        /// <summary>
        /// Returns the user's associated library.
        /// </summary>
        /// <param name="Username"></param>
        /// <returns></returns>
        public static string GetUserLibrary(string Username)
        {
            try
            {
                cmd.Connection = connection;
                cmd.Parameters.Clear();
                string sql = "SELECT Library FROM UserDetails WHERE [User] = @Username";
                cmd.Parameters.AddWithValue("@Username", Username);
                cmd.CommandText = sql;
                connection.Open();

                object result = cmd.ExecuteScalar();
                string UserType = "";
                // Check if a result was found
                if (result != null && result != DBNull.Value)
                {
                    UserType = (string)result;
                }
                connection.Close();
                return UserType;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }
        /// <summary>
        /// Updates the library of the user.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool UpdateLibrary(string username, string library)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Define the SQL update query.
                    string query = "UPDATE UserDetails SET [Library] = @Library WHERE [User] = @Username";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Library", library);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();
                        connection.Close();
                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }

                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        /// <summary>
        /// Returns all the users that exists in the database.
        /// </summary>
        /// <returns></returns>
        public static string GetAllUsernames()
        {
            string AllUsers = "";
            try
            {
                cmd.Connection = connection;
                string sql = "SELECT [User] FROM UserDetails";
                cmd.CommandText = sql;
                connection.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader["User"].ToString();
                        AllUsers += name + ",";
                    }
                }
                connection.Close();
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            return AllUsers.Substring(0, AllUsers.Length - 1);

        }
        public static int InsertBook(string bookdetails)
        {
            try
            {
                string[] data = bookdetails.Split('@');
                BookName = data[0];
                Author = data[1];
                Genre = data[2];
                Summary = data[3];
                Image = data[4]; //Image Here In Base64

                string ImageBookResourcesFolderPath = @"C:\Users\User\source\repos\VlibraryServer\VlibraryServer\ImageBookResources\";
                try
                {
                    string filePath = Path.Combine(ImageBookResourcesFolderPath, BookName + "_Image");
                    byte[] bytes = Convert.FromBase64String(Image);
                    System.IO.File.WriteAllBytes(filePath, bytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                }



                cmd.Connection = connection;

                string sql = "INSERT INTO BookDetails ([Name], Author, Genre, Summary) VALUES (@BookName, @author, @genre, @summary)";

                cmd.CommandText = sql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@BookName", BookName);
                cmd.Parameters.AddWithValue("@author", Author);
                cmd.Parameters.AddWithValue("@genre", Genre);
                cmd.Parameters.AddWithValue("@summary", Summary);

                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    connection.Open();
                }
                int x = cmd.ExecuteNonQuery();
                return x;
            }
            catch (Exception ex)
            {

                Console.WriteLine("-" + ex.Message);
                return 0;
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();

                }
            }
        }
        public static string GetBooksAuthor(string BookName)
        {
            try
            {
                cmd.Connection = connection;
                string sql = "SELECT Author FROM BookDetails WHERE Name = @Name";
                cmd.Parameters.AddWithValue("@Name", BookName);
                cmd.CommandText = sql;
                connection.Open();

                object result = cmd.ExecuteScalar();
                string UserType = "";
                // Check if a result was found
                if (result != null && result != DBNull.Value)
                {
                    UserType = (string)result;
                }
                connection.Close();
                return UserType;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }
        public static string GetBooksGenre(string BookName)
        {
            try
            {
                cmd.Connection = connection;
                string sql = "SELECT Genre FROM BookDetails WHERE Name = @book";
                cmd.Parameters.AddWithValue("@book", BookName);
                cmd.CommandText = sql;
                connection.Open();

                object result = cmd.ExecuteScalar();
                string UserType = "";
                // Check if a result was found
                if (result != null && result != DBNull.Value)
                {
                    UserType = (string)result;
                }
                connection.Close();
                return UserType;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }
        public static string GetBooksRating(string BookName)
        {
            try
            {
                cmd.Connection = connection;
                cmd.Parameters.Clear();
                string sql = "SELECT Rate FROM BookDetails WHERE Name = @BookName";
                cmd.Parameters.AddWithValue("@BookName", BookName);
                cmd.CommandText = sql;
                connection.Open();

                object result = cmd.ExecuteScalar();
                string UserType = "";
                // Check if a result was found
                if (result != null && result != DBNull.Value)
                {
                    UserType = (string)result;
                }
                connection.Close();
                return UserType;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }
        public static string GetBooksRatingCount(string BookName)
        {
            try
            {
                cmd.Connection = connection;
                cmd.Parameters.Clear();
                string sql = "SELECT RateNum FROM BookDetails WHERE Name = @BookName";
                cmd.Parameters.AddWithValue("@BookName", BookName);
                cmd.CommandText = sql;
                connection.Open();

                object result = cmd.ExecuteScalar();
                string UserType = "";
                // Check if a result was found
                if (result != null && result != DBNull.Value)
                {
                    UserType = (string)result;
                }
                connection.Close();
                return UserType;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }
        public static string GetAll()
        {
            string AllBooks = "";
            try
            {
                cmd.Connection = connection;
                string sql = "SELECT Name,Author,Genre,Rate,Summary FROM BookDetails";
                cmd.CommandText = sql;
                connection.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader["Name"].ToString();
                        string Author = reader["Author"].ToString();
                        string Genre = reader["Genre"].ToString();
                        string Rate = reader["Rate"].ToString();
                        string Summary = reader["Summary"].ToString();

                        string filePath = Path.Combine(@"C:\Users\User\source\repos\VlibraryServer\VlibraryServer\ImageBookResources\", name + "_Image");
                        byte[] bytes = System.IO.File.ReadAllBytes(filePath);
                        string Image = Convert.ToBase64String(bytes);



                        string bookDetails = name + "$" + Author + "$" + Genre + "$" + Rate + "$" + Summary + "$" + Image;
                        AllBooks += bookDetails + "@";
                    }
                }
                connection.Close();
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            return AllBooks.Substring(0, AllBooks.Length - 1);

        }
        public static string GetFilter(string filter)
        {
            string AllBooks = "";
            try
            {
                cmd.Connection = connection;

                string sql = "SELECT Name,Author,Genre,Rate,Summary FROM BookDetails WHERE Genre = @genre";

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@genre", filter);

                cmd.CommandText = sql;
                connection.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader["Name"].ToString();
                        string Author = reader["Author"].ToString();
                        string Genre = reader["Genre"].ToString();
                        string Rate = reader["Rate"].ToString();
                        string Summary = reader["Summary"].ToString();

                        string filePath = Path.Combine(@"C:\Users\User\source\repos\VlibraryServer\VlibraryServer\ImageBookResources\", name + "_Image");
                        byte[] bytes = System.IO.File.ReadAllBytes(filePath);
                        string Image = Convert.ToBase64String(bytes);



                        string bookDetails = name + "$" + Author + "$" + Genre + "$" + Rate + "$" + Summary + "$" + Image;
                        AllBooks += bookDetails + "@";
                    }
                }
                connection.Close();

            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            if (AllBooks == "")
            {
                return "None";
            }
            else
            {
                return AllBooks.Substring(0, AllBooks.Length - 1);
            }
        }
        public static string GetReadlist(string Username)

        {
            try
            {
                string type = "";

                cmd.Connection = connection;
                string sql = "SELECT Readlist FROM UserDetails WHERE [User] = '" + Username + "'";

                cmd.CommandText = sql;
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    connection.Open();
                }
                object result = (string)cmd.ExecuteScalar();

                if (result != null)
                {
                    type = (string)result;
                }
                return type;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
        public static string GetWishlist(string Username)
        {
            try
            {
                string type = "";

                cmd.Connection = connection;
                string sql = "SELECT Wishlist FROM UserDetails WHERE [User] = '" + Username + "'";

                cmd.CommandText = sql;
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    connection.Open();
                }
                object result = (string)cmd.ExecuteScalar();

                if (result != null)
                {
                    type = (string)result;
                }
                return type;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
        public static bool UpdateReadlist(string username, string readlist)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Define the SQL update query.
                    string query = "UPDATE UserDetails SET [Readlist] = @Readlist WHERE [User] = @Username";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Readlist", readlist);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();
                        connection.Close();
                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }

                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        public static bool UpdateWishlist(string username, string wishlist)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Define the SQL update query.
                    string query = "UPDATE UserDetails SET [Wishlist] = @Wishlist WHERE [User] = @Username";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Wishlist", wishlist);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();
                        connection.Close();
                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        public static bool UpdateRate(string book, string Rate)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Define the SQL update query.
                    string query = "UPDATE BookDetails SET [Rate] = @Rate WHERE [Name] = @BookName";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@BookName", book);
                        command.Parameters.AddWithValue("@Rate", Rate);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();
                        connection.Close();
                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        /// <summary>
        /// Updates the rate number for book details.
        /// </summary>
        /// <param name="book"></param>
        /// <param name="RateNum"></param>
        /// <returns></returns>
        public static bool UpdateRateNum(string book, string RateNum)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Define the SQL update query.
                    string query = "UPDATE BookDetails SET [RateNum] = @Rate WHERE [Name] = @BookName";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@BookName", book);
                        command.Parameters.AddWithValue("@Rate", RateNum);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();
                        connection.Close();
                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        /// <summary>
        /// Returns all the libraries in LibraryDetails.
        /// </summary>
        /// <returns></returns>
        public static string GetAllLibraries()
        {
            string AllLibraries = "";
            try
            {
                cmd.Connection = connection;

                string sql = "SELECT Library FROM LibraryDetails";

                cmd.Parameters.Clear();

                cmd.CommandText = sql;
                connection.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string Library = reader["Library"].ToString();

                        AllLibraries += Library + ",";
                    }
                }
                connection.Close();

            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            if (AllLibraries == "")
            {
                return "None";
            }
            else
            {
                return AllLibraries.Substring(0, AllLibraries.Length - 1);
            }
        }
        /// <summary>
        /// Inserts library in library details.
        /// </summary>
        /// <param name="LibraryName"></param>
        /// <returns></returns>
        public static int InsertLibrary(string LibraryName)
        {
            try
            {
                
                cmd.Connection = connection;

                string sql = "INSERT INTO LibraryDetails (Library) VALUES (@Library)";

                cmd.CommandText = sql;

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Library", LibraryName);
                connection.Open();
                int x = cmd.ExecuteNonQuery();
                connection.Close();
                return x;
            }
            catch (Exception ex)
            {

                Console.WriteLine("-" + ex.Message);
                return 0;
            }
        }
        /// <summary>
        /// Updates the Books And Quantity in a specific library.
        /// </summary>
        /// <param name="library"></param>
        /// <param name="bookAndQuantity"></param>
        /// <returns></returns>
        public static bool UpdateBooksQuantity(string library, string bookAndQuantity)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Define the SQL update query.
                    string query = "UPDATE LibraryDetails SET [BooksAndQuantity] = @books WHERE [Library] = @library";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@library", library);
                        command.Parameters.AddWithValue("@books", bookAndQuantity);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();
                        connection.Close();
                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        /// <summary>
        /// Retruns the book and quantity for set library.
        /// </summary>
        /// <param name="Username"></param>
        /// <returns>
        /// Books and Quantity.
        /// </returns>
        public static string GetBooksAndQuantity(string Library)
        {
            try
            {
                string type = "";

                cmd.Connection = connection;
                string sql = "SELECT BooksAndQuantity FROM LibraryDetails WHERE [Library] = '" + Library + "'";

                cmd.CommandText = sql;
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    connection.Open();
                }
                object result = (string)cmd.ExecuteScalar();

                if (result != null)
                {
                    type = (string)result;
                }
                return type;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
        /// <summary>
        /// Returns Orders from LibraryDetalis.
        /// </summary>
        /// <param name="Library"></param>
        /// <returns></returns>
        public static string GetOrders(string Library)
        {
            try
            {
                string type = "";

                cmd.Connection = connection;
                string sql = "SELECT Orders FROM LibraryDetails WHERE [Library] = '" + Library + "'";

                cmd.CommandText = sql;
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    connection.Open();
                }
                object result = (string)cmd.ExecuteScalar();

                if (result != null)
                {
                    type = (string)result;
                }
                return type;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
        /// <summary>
        /// Update Orders in LibraryDetails.
        /// </summary>
        /// <param name="library"></param>
        /// <param name="Orders"></param>
        /// <returns></returns>
        public static bool UpdateOrders(string library, string Orders)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Define the SQL update query.
                    string query = "UPDATE LibraryDetails SET [Orders] = @books WHERE [Library] = @library";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@library", library);
                        command.Parameters.AddWithValue("@books", Orders);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();
                        connection.Close();
                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        /// <summary>
        /// Updates the Order in UserDetails.
        /// </summary>
        /// <param name="User"></param>
        /// <param name="Order"></param>
        /// <returns></returns>
        public static bool UpdateOrdersForUser(string User, string Order)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Define the SQL update query.
                    string query = "UPDATE UserDetails SET [Order] = @Order WHERE [User] = @username";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@username", User);
                        command.Parameters.AddWithValue("@Order", Order);

                        // Execute the update query.
                        int rowsAffected = command.ExecuteNonQuery();
                        connection.Close();
                        // Check if any rows were affected. If > 0, the update was successful.
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during the database operation.
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        public static string GetOrderUser(string user)
        {
            try
            {
                string type = "";

                cmd.Connection = connection;
                string sql = "SELECT [Order] FROM UserDetails WHERE [User] = '" + user + "'";

                cmd.CommandText = sql;
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    connection.Open();
                }
                object result = (string)cmd.ExecuteScalar();

                if (result != null)
                {
                    type = (string)result;
                }
                return type;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }
}
