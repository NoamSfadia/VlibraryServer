using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        static string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""C:\Users\Noam Sfadia\source\repos\VlibraryServer\VlibraryServer\UserDatabase.mdf"";Integrated Security=True";

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

                cmd.Parameters.AddWithValue("@Username", Username);
                cmd.Parameters.AddWithValue("@Password", Password);
                cmd.Parameters.AddWithValue("@Mail", Mail);
                if(Username == "Manager")
                {
                    cmd.Parameters.AddWithValue("@Type", "Manager");
                }
                else if(Username == "librarian")
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
                string sql = "SELECT COUNT(*) FROM UserDetails WHERE [User] = '" + Username + "' AND [Password] = '" + Password + "'";

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
                cmd.Connection = connection;
                string sql = "SELECT Mail FROM UserDetails WHERE User = '" + Username + "'";
                cmd.CommandText = sql;
                connection.Open();

                object result = cmd.ExecuteScalar();
                string emailAddress = "";
                // Check if a result was found
                if (result != null && result != DBNull.Value)
                {
                    emailAddress = (string)result;
                }
                connection.Close();
                return emailAddress;

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                return "";
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
                    string query = "UPDATE UserDetails SET Password = @NewPassword WHERE User = @User";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@NewPassword", newPassword);
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
        public static bool UpdateUsername(string CurrentUsername,string newUsername)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Define the SQL update query.
                    string query = "UPDATE UserDetails SET [User] = @NewUsername WHERE [User] = @Username";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Set the parameters for the query.
                        command.Parameters.AddWithValue("@NewUsername", newUsername);
                        command.Parameters.AddWithValue("@Username", CurrentUsername);

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
        public static bool UpdateMail(string newMail , string Username)
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
        public static string GetUserType(string Username)
        {
            try
            {
                cmd.Connection = connection;
                string sql = "SELECT Type FROM UserDetails WHERE User = @Username";
                cmd.Parameters.AddWithValue("@Username", Username);
                cmd.CommandText = sql;
                connection.Open();

                object result = cmd.ExecuteScalar();
                string UserType = "";
                // Check if a result was found
                if (result != null && result != DBNull.Value)
                {
                   UserType  = (string)result;
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
        public static int InsertBook(string bookdetails)
        {
            try
            {
                string[] data = bookdetails.Split(',');
                BookName = data[0];
                Author = data[1];
                Genre = data[2];
                Rating = data[3];
                


                cmd.Connection = connection;

                string sql = "INSERT INTO BookDetails ([Name], Author, Genre, Rate) VALUES (@name, @author, @genre, @rate)";

                cmd.CommandText = sql;

                cmd.Parameters.AddWithValue("@name", BookName);
                cmd.Parameters.AddWithValue("@author", Author);
                cmd.Parameters.AddWithValue("@genre", Genre);
                cmd.Parameters.AddWithValue("@rate", Rating);

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
        public static string GetBooksAuthor(string BookName)
        {
            try
            {
                cmd.Connection = connection;
                string sql = "SELECT Author FROM BookDetails WHERE Name = @name";
                cmd.Parameters.AddWithValue("@name", BookName);
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
                string sql = "SELECT Genre FROM BookDetails WHERE Name = @name";
                cmd.Parameters.AddWithValue("@name", BookName);
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
                string sql = "SELECT Rate FROM BookDetails WHERE Name = @name";
                cmd.Parameters.AddWithValue("@name", BookName);
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
    }
}
