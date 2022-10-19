using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace ApiEndPoints.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EndpointController : ControllerBase
    {
        private readonly IConfiguration _config;

        public EndpointController(IConfiguration cfg)
        {
            _config = cfg;
        }


        // getHash function for Encryption, returns the 64 bit SHA512 hash
        public static byte[] getHash(string pass)
        {
            using (HashAlgorithm hash = SHA512.Create()) return hash.ComputeHash(Encoding.UTF8.GetBytes(pass));
        }

        // Encryption function for password safety
        public static string Encrypt(string pass)
        {
            StringBuilder builder = new StringBuilder();

            foreach (byte x in getHash(pass)) builder.Append(x.ToString("X2"));

            return builder.ToString();
        }


        int checkUserData(userData ud, int type) // 1 - Create, 2 - Update
        {
            if (type == 1)
            {
                if (string.IsNullOrEmpty(ud.Password) ||
                string.IsNullOrEmpty(ud.Email)) return -1; // DATA ERROR
            }
            if (string.IsNullOrEmpty(ud.Name) ||
            string.IsNullOrEmpty(ud.Surname)) return -1;  // DATA ERROR for all types

            if (ud.Name.Length < 3) return 1; else if (ud.Name.Length > 30) return 2; // 1 - short name, 2 - long name

            if (ud.Name.Any(ch => !Char.IsLetterOrDigit(ch))) return 3; // Name contains special characters

            if (ud.Surname.Length < 3) return 4; else if (ud.Surname.Length > 30) return 5; // 4 - short surname, 5 - long surname

            if (ud.Surname.Any(ch => !Char.IsLetterOrDigit(ch))) return 6; // Surname contains special characters

            if (type == 1)
            {
                if (ud.Password.Length < 8) return 7; else if (ud.Password.Length > 30) return 8; // 7 - password too short, 8 - password too long

                if (!new EmailAddressAttribute().IsValid(ud.Email)) return 9; // Email not valid
            }

            if (string.IsNullOrEmpty(ud.Gender)) ud.Gender = "unknown";
            else
            if (ud.Gender != "male" && ud.Gender != "female" && ud.Gender != "other") return 10; // Gender is incorrect

            return 0; // Check passed
        }

        string getErrorString(int result)
        {
            switch(result)
            {
                case -1: return "BŁĄD, DANE NIE ISTNIEJĄ";
                case 1: return "Imie jest za krótkie!";
                case 2: return "Imie jest za długie!"; // Name
                case 3: return "Imie zawiera specjalne znaki!"; // Name /w special chars
                case 4: return "Nazwisko jest za krótkie!";
                case 5: return "Nazwisko jest za długie!"; // Surname
                case 6: return "Nazwisko zawiera specjalne znaki!"; // Surname /w special chars
                case 7: return "Hasło jest za słabe!";
                case 8: return "Hasło jest za długie!"; // Password
                case 9: return "Email nie jest poprawny!"; // Email validation
                case 10: return "Płeć jest niepoprawna!"; //  Gender
                default: break; // Check pass
            }
            return "";
        }

        int checkAppData(applicationData ad, int type) // type = 1 - create, type = 2 - update
        {
            if(type == 1)
            {
                if (ad.userId == null ||
                string.IsNullOrEmpty(ad.createdBy) ||
                string.IsNullOrEmpty(ad.name) ||
                string.IsNullOrEmpty(ad.category) ||
                ad.reward == null ||
                ad.latitude == null ||
                ad.longitude == null) return 1;
            }
            else if(type == 2)
            {
                if (
                string.IsNullOrEmpty(ad.name) ||
                string.IsNullOrEmpty(ad.category) ||
                ad.reward == null) return 1;
            }


            return 0;
        }

        // Database connection
        MySqlConnection getConnection()
        {
            return new(_config.GetConnectionString("LogReg").ToString());
        }


        // Register endpoint
        [HttpPost]
        [Route("register")]

        public string Register(userData register)
        {
            if (register != null)
            {
                // Checking if data is correctly assigned

                int errorId = checkUserData(register, 1);
                string errorString = getErrorString(errorId);
                if (!string.IsNullOrEmpty(errorString)) return errorString;


                // Database connection
                using (MySqlConnection db = getConnection())
                {
                    db.Open();

                    // Checking if email already exists in database
                    MySqlDataAdapter emailcheck = new($"SELECT * FROM userData WHERE email = '{register.Email}'", db);
                    DataTable check = new();
                    emailcheck.Fill(check);
                    if (check.Rows.Count > 0) return "Konto z tym emailem już istnieje";

                    // MySql query
                    MySqlCommand query = new MySqlCommand($"INSERT INTO userData(uuid, name, surname, email, password, gender, city, adress, phone) VALUES (UUID(), '{register.Name}', '{register.Surname}', '{register.Email}', '{Encrypt(register.Password)}', '{register.Gender}','{register.City}', '{register.Address}', '{register.Phone}')", db);
                    int queryResult = query.ExecuteNonQuery();
                    if (queryResult == 1) return "Pomyślnie zarejstrowano";
                    else return "Nastąpił nieoczekiwany błąd podczas rejestracji. Spróbuj później";

                }
            }
            else return "BŁĄD, DANE NIE ISTNIEJĄ"; // Happens when data passed from front-end doesn't exist




        }

        // Login endpoint
        [HttpPost]
        [Route("login")]

        public Tuple<bool, userData> Login(userData login)
        {
            userData output = new userData();

            if (login != null)
            {
                if (string.IsNullOrEmpty(login.Password)) return new Tuple<bool, userData>(false, output); ;
                //Database connection
                using (MySqlConnection db = getConnection())
                {
                    db.Open();
                    //MySql query
                    MySqlCommand loginQuery = new($"SELECT * FROM userData WHERE email = '{login.Email}' AND password = '{Encrypt(login.Password)}'", db);
                    MySqlCommand count = new($"SELECT count(*) FROM userData WHERE email = '{login.Email}' AND password = '{Encrypt(login.Password)}'", db);
                    //DataTable loginDT = new();
                    //loginQuery.Fill(loginDT);

                    output = new userData();
                    MySqlDataReader dr;
                    dr = loginQuery.ExecuteReader();
                    //int result = (int)count.ExecuteScalar();

                    while (dr.Read())
                    {
                        output.userId = dr.GetString(0);
                        output.Name = dr.GetString(1);
                        output.Surname = dr.GetString(2);
                        output.Email = dr.GetString(3);
                        output.Password = dr.GetString(4);
                        output.Gender = dr.GetString(5);
                        output.City = dr.GetString(6);
                        output.Address = dr.GetString(7);
                        output.Phone = dr.GetInt32(8);
                    }


                    if (dr.HasRows) return new Tuple<bool, userData>(true, output);
                    else return new Tuple<bool, userData>(false, output);

                }
            }
            return new Tuple<bool, userData>(false, output); //  Happens when data passed from front-end doesn't exist
        }

        // Password update endpoint
        [HttpPut]
        [Route("updatePassword/{userId}")]

        public string updatePassword(otherData updatePassword, string userId)
        {
            if (updatePassword != null)
            {
                if (string.IsNullOrEmpty(updatePassword.newPassword)) return "Nie wprowadziłeś hasła";
                using (MySqlConnection db = getConnection())
                {
                    db.Open();
                    //MySql query
                    MySqlDataAdapter findQuery = new($"SELECT * FROM userData WHERE uuid = '{userId}' AND password = '{Encrypt(updatePassword.oldPassword)}'", db);
                    DataTable findDT = new();
                    findQuery.Fill(findDT);

                    if (findDT.Rows.Count > 0)
                    {
                        MySqlCommand query = new($"UPDATE userData SET password = '{Encrypt(updatePassword.newPassword)}'");
                        int queryResult = query.ExecuteNonQuery();

                        if (queryResult == 1) return "Hasło zmienione";
                        else return "Wystąpił nieoczekiwany błąd podczas zmieniania hasła. Spróbuj ponownie później";
                    }
                    else return "Incorrect email or password";

                }
            }
            return "BŁĄD, DANE NIE ISTNIEJĄ";
        }

        // Password update endpoint
        [HttpPut]
        [Route("updateEmail/{userId}")]

        public string updateEmail(otherData updateEmail, string userId)
        {
            if (updateEmail != null)
            {
                if (string.IsNullOrEmpty(updateEmail.newEmail)) return "Nie wprowadziłeś adresu email";
                using (MySqlConnection db = getConnection())
                {
                    db.Open();
                    //MySql query
                    MySqlDataAdapter findQuery = new($"SELECT * FROM userData WHERE uuid = '{userId}' AND email = '{updateEmail.oldEmail}'", db);
                    DataTable findDT = new();
                    findQuery.Fill(findDT);

                    if (findDT.Rows.Count > 0)
                    {
                        MySqlCommand query = new($"UPDATE userData SET email = '{updateEmail.newEmail}'");
                        int queryResult = query.ExecuteNonQuery();

                        if (queryResult == 1) return "Adres email został zmieniony";
                        else return "Wystąpił nieoczekiwany błąd podczas zmieniania adresu email. Spróbuj ponownie później";
                    }
                    else return "Incorrect email or password";

                }
            }
            return "BŁĄD, DANE NIE ISTNIEJĄ";
        }

        // UserData update endpoint
        [HttpPut]
        [Route("updateUserData/{userId}")]

        public string updateUserData(userData changeUD, string userId)
        {
            if(changeUD != null)
            {

                // Checking if data is ok
                int errorId = checkUserData(changeUD, 2);
                string errorString = getErrorString(errorId);
                if (!string.IsNullOrEmpty(errorString)) return errorString;

                using (MySqlConnection db = getConnection())
                {
                    MySqlCommand query = new($"UPDATE userData SET(name, surname, gender, city, adress, phone) VALUES ('{changeUD.Name}', '{changeUD.Surname}', '{changeUD.Gender}','{changeUD.City}', '{changeUD.Address}', '{changeUD.Phone}' WHERE uuid = '{userId}')", db);
                    int queryResult = query.ExecuteNonQuery();

                }

            }
            return "BŁĄD, DANE NIE ISTNIEJĄ";
        }

        // Application creation endpoint
        [HttpPost]
        [Route("createApplication")]

        public string CreateApplication(applicationData create)
        {
            if (create != null)
            {
                // Data check if any values that aren't intended to, are null
               int checkData = checkAppData(create, 1);

               if (checkData == 1) return "Dane nie są poprawnie uzupełnione";

                using (MySqlConnection db = getConnection())
                {
                    db.Open();

                    MySqlCommand count = new($"SELECT COUNT(*) FROM application WHERE userId = '{create.userId}'", db);
                    Int64 result = (Int64)count.ExecuteScalar(); // Counts how many rows there are
                        
                    if (result > 3) return "Osiągnięto limit zgłoszeń";



                    // MySql query
                    MySqlCommand query = new MySqlCommand($"INSERT INTO application(date_created, name, userId, applicationId, createdBy, category, reward, stake, finished, description, latitude, longitude, localizationName) VALUES (NOW(),'{create.name}', '{create.userId}',UUID() , '{create.createdBy}', '{create.category}', '{Convert.ToInt16(create.reward)}','{create.stake}', 'idle', '{create.description}', '{create.latitude}', '{create.longitude}', '{create.localizationName}')", db);
                    int queryResult = query.ExecuteNonQuery();

                    if (queryResult == 1) return "Pomyślnie stworzono zgłoszenie";
                    else return "Wystąpił nieoczekiwany błąd podczas tworzenia zgłoszeń. Spróbuj ponownie później";

                }

            }
            return "BŁĄD, DANE NIE ISTNIEJĄ"; //  Happens when data passed from front-end doesn't exist
        }

        // Application update endpoint
        [HttpPut]
        [Route("updateApplication/{id}")]

        public string updateApplication(applicationData update, string id)
        {
            if (update != null)
            {
                // Data check 
                int checkData = checkAppData(update, 2);

                if (checkData == 1) return "Dane nie są poprawnie uzupełnione";

                using (MySqlConnection db = getConnection())
                {
                    db.Open();

                    // Replaces all data with new data
                    // MySql query
                    MySqlCommand query = new MySqlCommand($"UPDATE application SET name = '{update.name}', category = '{update.category}', reward = {update.reward}, stake = '{update.stake}', finished = '{update.finished}',  description = '{update.description}' WHERE applicationId = '{id}'", db);
                    int queryResult = query.ExecuteNonQuery();

                    if (queryResult == 1) return "Zgłoszenie zaktualizowane";
                    else return "Wystąpił nieoczekiwany błąd podczas aktualizowania zgłoszenia. Spróbuj ponownie później";

                }
            }
            return "BŁĄD, DANE NIE ISTNIEJĄ"; //  Happens when data passed from front-end doesn't exist
        }

        // Application delete endpoint
        [HttpDelete]
        [Route("deleteApplication/{id}")]

        public string deleteApplication(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                using (MySqlConnection db = getConnection())
                {
                    db.Open();
                    // MySql query
                    MySqlCommand query = new MySqlCommand($"DELETE FROM application WHERE applicationId = '{id}'", db);
                    int queryResult = query.ExecuteNonQuery();

                    if (queryResult == 1) return "Usunięto zgłoszenie";
                    else return "Wystąpił nieoczekiwany błąd podczas usuwania zgłoszenia. Spróbuj ponownie później";
                }
            }
            return "Żadno zgłoszenie nie ma takiego id";
                
        }

        // Get all applications endpoint
        [HttpGet]
        [Route("getApplications")]
        [Route("getApplications/{id?}")] // Returns an application with that id
        [Route("getApplications/user/{userId?}")] // Returns all applications assigned to one user

        public applicationData[] getApplications(string? id = "", string? userId = "")
        {
            using (MySqlConnection db = getConnection())
            {
                db.Open();
                Int64 result;
                MySqlCommand query;
                if (string.IsNullOrEmpty(id))
                {
                    MySqlCommand count;

                    if (string.IsNullOrEmpty(userId))
                    {
                        count = new("SELECT COUNT(*) FROM application WHERE finished = 'idle' OR finished = 'during'", db); // Count how many applications there are
                        query = new($"SELECT * FROM application WHERE finished = 'idle' OR finished = 'during'", db); // Show all applications
                    }
                    else
                    {
                        count = new($"SELECT COUNT(*) FROM application WHERE userId = '{userId}'", db); // Count how many aplications the user has made
                        query = new($"SELECT * FROM application WHERE userId = '{userId}'", db); // Show all applications made by the user
                    }
                    result = (Int64)count.ExecuteScalar(); // Counts how many rows there are
                }
                else
                {
                    result = 1; // Only one application
                    query = new($"SELECT * FROM application WHERE applicationId = '{id}'", db);
                }

                MySqlDataReader dr;
                dr = query.ExecuteReader(); // Data reader

                applicationData[] array = new applicationData[result]; // Array objects


                if (dr.FieldCount > 0)
                {
                    int iteration = 0;
                    while (dr.Read())
                    {
                        array[iteration] = new applicationData();

                        for(int i = 0; i < array[iteration].GetType().GetProperties().Length; i++)
                        {
                            Console.WriteLine(i);
                        }

                        array[iteration].date_created = dr.GetMySqlDateTime(0);
                        array[iteration].name = dr.GetString(1);
                        array[iteration].userId = dr.GetString(2);
                        array[iteration].applicationId = dr.GetString(3);
                        array[iteration].createdBy = dr.GetString(4);
                        array[iteration].category = dr.GetString(5);
                        array[iteration].reward = dr.GetBoolean(6);
                        array[iteration].stake = dr.GetFloat(7);
                        array[iteration].finished = dr.GetString(8);
                        array[iteration].description = dr.GetString(9);
                        array[iteration].latitude = dr.GetDouble(10);
                        array[iteration].longitude = dr.GetDouble(11);
                        array[iteration].localizationName = dr.GetString(12);

                        

                        iteration++;
                    }
                }
                return array;
            }
        }

    }
}
