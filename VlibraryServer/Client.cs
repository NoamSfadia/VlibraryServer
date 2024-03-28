using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading;
using System.CodeDom.Compiler;


namespace VlibraryServer
{
    class Client
    {
        // Store list of all clients connecting to the server
        // the list is static so all memebers of the chat will be able to obtain list
        // of current connected client
        public static Hashtable AllClients = new Hashtable();

        private static Dictionary<string, TimeObj> BlockedUsers = new Dictionary<string, TimeObj>();
        private static List<string> Libraries = new List<string>() { "Givataim", "Tel Aviv", "Ramat Gan", "Yavne", "Jerusalem", "Bat Yam"};
        private static List<string> Genres = new List<string>() { "Horror", "Fantasy", "Fiction", "Biogrpahy", "History", "Drama", "Mystery", "Cooking", "Self Help", "Travel", "Comedy", "Kids" };


        // information about the client
        private TcpClient _client;
        private string _clientIP;
        // used for sending and reciving data
        private byte[] data;

        private string userDetails;
        private string user;
        private string password;
        private string mail;
        private int LoginCount; //how many times the user tried to sign in.
        DateTime BlockEnd;

        //Encryption
        private RSA Rsa;
        private string ClientPublicKey;
        private string PrivateKey;
        private string SymmetricKey;

        //SMTP
        private string SmtpCode;

        //Capthca
        private string CaptchaAnswer;

        private string OverlapString;


        // the nickname being sent
        /// <summary>
        /// When the client gets connected to the server the server will create an instance of the ChatClient and pass the TcpClient
        /// </summary>
        /// <param name="client"></param>

        public Client(TcpClient client)
        {
            Rsa = new RSA();
            PrivateKey = Rsa.GetPrivateKey();

            TimeObj timeObj = new TimeObj();

            _client = client;

            // get the ip address of the client to register him with our client list
            _clientIP = client.Client.RemoteEndPoint.ToString();

            // Add the new client to our clients collection
            AllClients.Add(_clientIP, this);
            Console.WriteLine(AllClients.Count);


            if (BlockedUsers.ContainsKey(_clientIP))
            {
                if (BlockedUsers[_clientIP].getBanned())
                {
                    if (BlockEnd <= DateTime.Now)
                    {
                        BlockedUsers[_clientIP].ChangeBanned();
                        BlockedUsers[_clientIP].SetLoginTries(0);
                        SendMessage("UnBan");
                    }
                }
            }
            else
            {
                BlockedUsers.Add(_clientIP, timeObj);
            }


            // Read data from the client async
            data = new byte[_client.ReceiveBufferSize];

            // BeginRead will begin async read from the NetworkStream
            // This allows the server to remain responsive and continue accepting new connections from other clients
            // When reading complete control will be transfered to the ReviveMessage() function.
            _client.GetStream().BeginRead(data,
                                          0,
                                          System.Convert.ToInt32(_client.ReceiveBufferSize),
                                          ReceiveMessage,
                                          null);


        }

        /// <summary>
        /// allow the server to send message to the client.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message)
        {
            try
            {
                System.Net.Sockets.NetworkStream ns;

                // use lock to present multiple threads from using the networkstream object
                // this is likely to occur when the server is connected to multiple clients all of 
                // them trying to access to the networkstram at the same time.
                lock (_client.GetStream())
                {
                    ns = _client.GetStream();
                }
                if (!message.StartsWith("$") && !message.StartsWith("*"))
                {
                    byte[] Key = Encoding.UTF8.GetBytes(SymmetricKey);
                    byte[] IV = new byte[16];
                    message = AES.Encrypt(message, Key, IV);
                }

                byte[] bytesToSend = System.Text.Encoding.ASCII.GetBytes(message + "!");
                int chunkSize = 60000; // Chunk size in bytes
                int bytesSent = 0;
                while (bytesSent < bytesToSend.Length)
                {
                    int remainingBytes = bytesToSend.Length - bytesSent;
                    int currentChunkSize = Math.Min(remainingBytes, chunkSize);
                    ns.Write(bytesToSend, bytesSent, currentChunkSize);
                    bytesSent += currentChunkSize;
                }
                ns.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        /// <summary>
        /// reciev and handle incomming stream 
        /// Asynchrom
        /// </summary>
        /// <param name="ar">IAsyncResult Interface</param>
        public void ReceiveMessage(IAsyncResult ar)
        {
            int bytesRead;
            bool logged = false;
            try
            {
                lock (_client.GetStream())
                {
                    // call EndRead to handle the end of an async read.
                    bytesRead = _client.GetStream().EndRead(ar);
                }
                // if bytesread<1 -> the client disconnected
                if (bytesRead < 1)
                {
                    // remove the client from out list of clients
                    AllClients.Remove(_clientIP);
                    return;
                }
                else // client still connected
                {
                    string messageReceived = System.Text.Encoding.ASCII.GetString(data, 0, bytesRead);
                    if(messageReceived.EndsWith("!"))
                    {
                        messageReceived = messageReceived.Remove(messageReceived.Length - 1);
                        messageReceived = OverlapString + messageReceived;
                        OverlapString = "";
                    }
                    else
                    {
                        OverlapString += messageReceived;
                    }

                    if (messageReceived.StartsWith("public key:"))
                    {
                        messageReceived = messageReceived.Remove(0, 11);
                        ClientPublicKey = messageReceived;
                        SendMessage("*" + Rsa.GetPublicKey());

                        SymmetricKey = AES.RandomKey(32);
                        string EncryptedSymmerticKey = Rsa.Encrypt(SymmetricKey, ClientPublicKey);
                        SendMessage("$" + EncryptedSymmerticKey);

                    }
                    else if(OverlapString == "")
                    {
                        byte[] Key = Encoding.UTF8.GetBytes(SymmetricKey);
                        byte[] IV = new byte[16];
                        messageReceived = AES.Decrypt(messageReceived, Key, IV);
                    }
                    if (messageReceived.StartsWith("In")) //tries to sign in.
                    {
                        string userTriesToLog = messageReceived.Substring(2, messageReceived.IndexOf('#', 2) - 2);
                        BlockedUsers[_clientIP].AddTry();
                        if (DataHandler.isExist(messageReceived.Remove(0, 2))) //succescful login.
                        {
                            user = userTriesToLog; //saves the username.
                            SendMessage("In The Database");
                            mail = DataHandler.GetEmailAddress(user); //saves the user's mail.
                            SendCodeFromOutlook(mail);

                            BlockedUsers[_clientIP].SetLoginTries(0);
                        }
                        else
                        {
                            if (BlockedUsers[_clientIP].GetLoginTries() == 5)
                            {
                                SendMessage("ban");
                                DateTime now = DateTime.Now;
                                BlockEnd = now.AddMinutes(3); //ban for 3 minutes.
                                BlockedUsers[_clientIP].ChangeBanned();
                            }

                            SendMessage("Not In The Database");
                        }

                    }

                    if (messageReceived.StartsWith("CheckUser:"))
                    {
                        if (!DataHandler.isUsernameExist(messageReceived.Remove(0, 10)))//Trying to change username
                        {
                            SendMessage("CheckUserValid"); //if username is not existing.
                        }
                        else
                        {
                            SendMessage("CheckUserInvalid");
                        }
                    }
                    if (messageReceived.StartsWith("CheckUserSignUp:"))
                    {
                        if (!DataHandler.isUsernameExist(messageReceived.Remove(0, 16)))//trying to sign up
                        {
                            SendMessage("CheckUserValidSignUp"); //if username is not existing.
                        }
                        else
                        {
                            SendMessage("CheckUserInvalid");
                        }
                    }

                    if (messageReceived.StartsWith("Up")) //tries to sign up. 
                    {

                        userDetails = messageReceived.Remove(0, 2);
                        string[] details = userDetails.Split('#');
                        user = details[0];
                        password = details[1];
                        mail = details[2];
                        SendCodeFromOutlook(mail);
                    }

                    if (messageReceived.StartsWith("CodeUp") && SmtpCode.Equals(messageReceived.Remove(0, 6)) && !logged) //tries to pass the smtp test.
                    {
                        //captcha test
                        string[] Captcha = CaptchaTest();
                        if (Captcha.Length == 2) //to ensure out of range exception
                        {
                            SendMessage("captUp:" + Captcha[0]);
                            CaptchaAnswer = Captcha[1];
                        }
                    }
                    if (messageReceived.StartsWith("CaptchaUp:")) //for captcha answer in sign up
                    {
                        if (messageReceived.Remove(0, 10) == CaptchaAnswer)
                        {
                            DataHandler.InsertUser(userDetails);
                            SendMessage("Signed Up");
                        }
                        else
                        {
                            string[] Captcha = CaptchaTest();
                            if (Captcha.Length == 2) //to ensure out of range exception
                            {
                                SendMessage("captUp:" + Captcha[0]);
                                CaptchaAnswer = Captcha[1];
                            }
                        }
                    }
                    if (messageReceived.StartsWith("CodeIn") && SmtpCode.Equals(messageReceived.Remove(0, 6)) && !logged) //tries to pass the smtp test.
                    {
                        //captcha test
                        string[] Captcha = CaptchaTest();
                        if (Captcha.Length == 2) //to ensure out of range exception
                        {
                            SendMessage("captIn:" + Captcha[0]);
                            CaptchaAnswer = Captcha[1];
                        }
                    }
                    if (messageReceived.StartsWith("CaptchaIn:")) //for captcha answer in sign up
                    {
                        if (messageReceived.Remove(0, 10) == CaptchaAnswer)
                        {
                            SendMessage("Signed In");
                            logged = true;
                        }
                        else
                        {
                            string[] Captcha = CaptchaTest();
                            if (Captcha.Length == 2) //to ensure out of range exception
                            {
                                SendMessage("captIn:" + Captcha[0]);
                                CaptchaAnswer = Captcha[1];
                            }
                        }
                    }



                    if (messageReceived.StartsWith("CodeForNewPass") && SmtpCode.Equals(messageReceived.Remove(0, 14)))
                    {
                            SendMessage("Valid Code");
                    }


                    if(messageReceived == "Disconnection In proccess")
                    {
                        AllClients.Remove(_clientIP);
                    }

                    

                    if (messageReceived.StartsWith("Send Code")) // send code to mail.
                    {
                        mail = messageReceived.Remove(0, 9);
                        SendCodeFromOutlook(mail);
                    }

                    if (messageReceived.StartsWith("SendCodeSettings")) // send code to mail.
                    {
                        string mail = DataHandler.GetEmailAddress(messageReceived.Remove(0,16));
                        SendCodeFromOutlook(mail);
                    }
                    if (messageReceived.StartsWith("CodeForSettings") && SmtpCode.Equals(messageReceived.Remove(0, 15)))
                    {
                        SendMessage("Valid Code For Settings");
                    }


                    if (messageReceived.StartsWith("ChangePassMail")) // user passed the smtp test, tries to change password in forgot password form.
                    {
                        DataHandler.UpdatePassword(mail, messageReceived.Remove(0, 14));
                    }

                    if (messageReceived.StartsWith("ChangePassUser")) // user passed the smtp test, tries to change password in settings form.
                    {
                        DataHandler.UpdatePassword(mail, messageReceived.Remove(0, 14));
                    }
                    if (messageReceived.StartsWith("ChangeMail"))
                    {
                        DataHandler.UpdateMail(user, messageReceived.Remove(0, 9));
                    }
                    if (messageReceived.StartsWith("NewUsername:"))
                    {
                        DataHandler.UpdateUsername(mail, messageReceived = messageReceived.Remove(0, 12));
                    }


                    if (messageReceived.StartsWith("What Type"))
                    {
                        string UserType = DataHandler.GetUserType(user);
                        SendMessage("UserType:" + UserType);
                        Console.WriteLine(UserType);
                    }

                    if (messageReceived.StartsWith("GetUserName"))
                    {
                        SendMessage("UserName:" + user);
                    }
                    if (messageReceived == "GetLibraries")
                    {
                        SendMessage("lb:" + string.Join(",", Libraries));
                    }
                    

                    if (messageReceived.StartsWith("BookInsert:"))
                    {
                        messageReceived = messageReceived.Remove(0, 11);
                        DataHandler.InsertBook(messageReceived);
                    }

                    if (messageReceived.StartsWith("GetBooks:"))
                    {
                        SendMessage("BooksToPreview:" + DataHandler.GetAll());
                    }
                    if(messageReceived.StartsWith("getGenresForHome"))
                    {
                        SendMessage("genresForHome:" + string.Join("," , Genres));
                    }
                    if(messageReceived.StartsWith("GetGenres"))
                    {
                        SendMessage("Genres:" + string.Join(",", Genres));
                    }
                    if(messageReceived.StartsWith("Filter:"))
                    {
                         string filter = messageReceived.Remove(0, 7);
                         SendMessage("BooksToPreview:" + DataHandler.GetFilter(filter));
                    }


                    if(messageReceived.StartsWith("AddReadBook:"))
                    {
                        string bookName = messageReceived.Remove(0, 12);
                        string Readlist = DataHandler.GetReadlist(user);

                        if(Readlist == "") 
                        {
                            Readlist = bookName;
                        }
                        else if (!CheckForString(Readlist, bookName))
                        {
                            Readlist += ',' + bookName;
                        }
                        DataHandler.UpdateReadlist(user, Readlist);
                    }
                    if (messageReceived.StartsWith("AddWishBook:"))
                    {
                        string bookName = messageReceived.Remove(0, 12);
                        string Wishlist = DataHandler.GetWishlist(user);

                        if (Wishlist == "")
                        {
                            Wishlist = bookName;
                        }
                        else if (!CheckForString(Wishlist, bookName))
                        {
                            Wishlist += ',' + bookName;
                        }
                        DataHandler.UpdateWishlist(user, Wishlist);
                    }
                    if(messageReceived.StartsWith("SearchQuery:"))
                    {
                        string Query = messageReceived.Remove(0, 12);
                        string[] AllBooks = DataHandler.GetAll().Split('@');
                        string BooksToSend = "";
                        foreach(string Book in AllBooks) 
                        {
                            if (Query.Contains(Book) || Book.Contains(Query)) 
                            {
                                BooksToSend += '@' + Book; 
                            }
                        }
                        BooksToSend = BooksToSend.Remove(0, 1);
                        SendMessage("BooksToPreview:" + BooksToSend);
                    }
                    if(messageReceived.StartsWith("RateForBook:"))
                    {
                        messageReceived = messageReceived.Remove(0, 12);
                        string[] strings = messageReceived.Split(',');
                        string bookName = strings[0];
                        float rateToAdd = float.Parse(strings[1]);

                        float rate = float.Parse(DataHandler.GetBooksRating(bookName));
                        float floatedRatingNumber = float.Parse(DataHandler.GetBooksRatingCount(bookName));

                        rate = (rate *  Math.Abs(floatedRatingNumber- 1)) + rateToAdd;
                        rate = rate / (floatedRatingNumber);

                        DataHandler.UpdateRate(bookName, rate.ToString("F2"));
                        DataHandler.UpdateRateNum(bookName, (float.Parse(DataHandler.GetBooksRatingCount(bookName)) +1).ToString());
                        SendMessage("SuccesfulRating");
                    }


                    lock (_client.GetStream())
                    {
                            // continue reading form the client
                            _client.GetStream().BeginRead(data, 0, System.Convert.ToInt32(_client.ReceiveBufferSize), ReceiveMessage, null);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// send message to all the clients that are stored in the allclients hashtable
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message)
        {
            Console.WriteLine(message);
            foreach (DictionaryEntry c in AllClients)
            {
                ((Client)(c.Value)).SendMessage(message + Environment.NewLine);
            }
        }

        /// <summary>
        /// Send verification code to the user.  
        /// </summary>
        /// <param name="email"></param>
        public void SendCodeFromOutlook(string email)
        {
            MailMessage message = new MailMessage();
            message.From = new MailAddress("VirtualLibrary367@outlook.com");
            message.To.Add(email);
            message.Subject = "Verification Code";

            Random random = new Random();
            SmtpCode = (random.Next(100000, 999999)).ToString();
            message.Body = "The code is:    " + SmtpCode;
            message.IsBodyHtml = false;


            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.office365.com";
            smtp.Port = 587;
            smtp.Credentials = new NetworkCredential
                ("VirtualLibrary367@outlook.com", "cihwzvnmtcuycdgp");
            smtp.EnableSsl = true;

            smtp.Send(message);
        }
        /// <summary>
        /// Generates a new captcha test. returns an array of strings with the sentences - the moddified and the original.
        /// </summary>
        /// <returns></returns>
        public string[] CaptchaTest()
        {
            string[] sentences = new string[] { "THIS IS A TEST", "CAPTCHA TEST", "CYBER IS COOL", "TOP GEEK", "SEE YOU AGAIN", "LIVE AND LET DIE", "ANOTHER ONE BITES THE DUST", "HOTEL CALIFORNIA", "SMELLS LIKE TEEN SPIRIT", "DONT LOOK BACK IN ANGER", "WALKIN ON SUNSHINE", "PARTY IN THE USA", "SINCE YOU BEEN GONE", "ROLLING IN THE DEEP", "LIFE IS BEAUTIFUL", "SMILES LIGHT UP FACES", "COFFEE WARMS THE HEART", "DREAM BIG ACHIEVE MORE", "SUNSHINE BRINGS JOY", "LOVE CONQUERS ALL", "RAINY DAYS INSPIRE COZINESS", "CHERISH EVERY MOMENT", "FRIENDS MAKE LIFE SWEET", "ADVENTURE AWAITS YOU", "KINDNESS MATTERS ALWAYS", "LAUGHTER IS PURE MEDICINE", "WORK HARD STAY HUMBLE", "FAMILY FIRST ALWAYS", "BOOKS OPEN NEW WORLDS", "GOOD FOOD GOOD MOOD", "MUSIC SOOTHES THE SOUL", "BE TRUE TO YOURSELF", "NEW BEGINNINGS BRING EXCITEMENT", "HAPPINESS IS A CHOICE", "EMBRACE THE JOURNEY", "CREATE YOUR OWN SUNSHINE", "STAY POSITIVE STAY FIGHTING", "EXPLORE DISCOVER GROW", "NATURE HEALS THE HEART", "BE BRAVE TAKE RISKS", "HOME IS WHERE LOVE IS", "JOY IN SIMPLE PLEASURES", "LEARN FROM YESTERDAY", "DREAM BELIEVE ACHIEVE", "STARS SHINE IN DARKNESS", "FIND BEAUTY IN SIMPLICITY", "STAY CURIOUS STAY YOUNG", "CELEBRATE LITTLE VICTORIES", "TIME HEALS LET IT", "KEEP IT SIMPLE SILLY", "SEIZE THE DAY EVERYDAY", "FORGIVE LET GO MOVE ON", "BREATHE IN BREATHE OUT", "BELIEVE IN YOURSELF", "SMILE IT'S CONTAGIOUS", "SUNSET VIBES GOOD VIBES", "EVERY DAY IS A GIFT", "LOVE WHAT YOU DO", "LIFE'S A JOURNEY ENJOY", "WHAT'S GOING ON", "HOLD MY HAND", "I WILL SURVIVE", "LET IT BE", "JUST THE WAY YOU ARE", "HAPPY TOGETHER", "WALK THIS WAY", "LOVE ME TENDER", "ALL YOU NEED IS LOVE", "DREAM ON", "HEAL THE WORLD", "SWEET CHILD OF MINE", "SOMETHING JUST LIKE THIS", "HERE COMES THE SUN", "DONT STOP BELIEVIN", "I WANT TO BREAK FREE", "WITH OR WITHOUT YOU", "LEAN ON ME", "STAND BY ME", "WE WILL ROCK YOU", "IMAGINE", "WHAT A WONDERFUL WORLD", "HELLO GOODBYE", "HOLD ON FOREVER", "HAVE A NICE DAY", "LIVING ON A PRAYER", "DONT WORRY BE HAPPY", "JUST LIKE HEAVEN", "UNDER THE BRIDGE", "TAKE ME HOME", "SWEET HOME ALABAMA", "WONDERWALL", "BORN TO BE WILD", "WALK LIKE AN EGYPTIAN", "EVERY BREATH YOU TAKE", "STAND BY YOUR MAN", "LOVE ME DO", "FAITHFULLY", "WISH YOU WERE HERE", "UNCHAINED MELODY", "LIKE A ROLLING STONE", "WHAT'S LOVE GOT TO DO WITH IT", "WAKE ME UP BEFORE YOU GO-GO", "WON'T YOU BE MY NEIGHBOR", "DON'T LET THE SUN GO DOWN ON ME", "DON'T FEAR THE REAPER", "LIVIN' ON A PRAYER", "EVERYBODY WANTS TO RULE THE WORLD", "HAVE YOU EVER SEEN THE RAIN", "NEVER GONNA GIVE YOU UP", "SOMEBODY TO LOVE", "BLACKBIRD", "LOVE ME TENDER", "I'M A BELIEVER", "WONDERFUL TONIGHT", "THE SOUND OF SILENCE", "GOOD VIBRATIONS", "BAD MOON RISING", "LUCY IN THE SKY WITH DIAMONDS", "RING OF FIRE", "WATERLOO SUNSET", "HERE COMES YOUR MAN", "SWEET CHILD O' MINE", "WHEN DOVES CRY", "ANGIE", "LITTLE RED CORVETTE", "IT'S NOW OR NEVER", "THE WAY YOU MAKE ME FEEL", "WILD HORSES", "DON'T STOP BELIEVIN'", "TINY DANCER", "A DAY IN THE LIFE", "WALK THIS WAY", "BLUE SUEDE SHOES", "WONDERWALL", "SOMEWHERE OVER THE RAINBOW", "THE TIMES THEY ARE A-CHANGIN'", "LIFE ON MARS?", "THUNDERSTRUCK", "DANCING QUEEN", "MY GENERATION", "TWIST AND SHOUT", "I CAN'T GET NO SATISFACTION", "ALL YOU NEED IS LOVE", "UNDER PRESSURE", "SWEET CAROLINE", "LIVING ON A PRAYER", "WISH YOU WERE HERE", "BLACKBIRD", "HELLO GOODBYE", "PAINT IT BLACK", "THE THRILL IS GONE", "BOHEMIAN RHAPSODY", "SOMETHING", "COME TOGETHER", "BLOWIN' IN THE WIND", "BORN TO RUN", "LIKE A ROLLING STONE", "I WANT TO BREAK FREE", "HERE COMES THE SUN", "WON'T YOU BE MY NEIGHBOR", "JUST THE WAY YOU ARE", "CRAZY LITTLE THING CALLED LOVE", "KNOCKIN' ON HEAVEN'S DOOR", "SITTIN' ON THE DOCK OF THE BAY", "I WALK THE LINE", "LOVE STORY", "SWEET HOME ALABAMA", "CAN'T HELP FALLING IN LOVE", "I WILL ALWAYS LOVE YOU", "PURPLE HAZE", "WALK LIKE AN EGYPTIAN", "A HARD DAY'S NIGHT", "TAKE ME HOME, COUNTRY ROADS", "I WILL SURVIVE", "FIELDS OF GOLD", "SOMEONE LIKE YOU", "SOUND OF SILENCE", "EYE OF THE TIGER", "LET IT BE", "WHAT A WONDERFUL WORLD", "WHAT'S GOING ON", "CANDLE IN THE WIND", "DREAM ON", "STAND BY ME", "STAIRWAY TO HEAVEN", "YOU ARE MY SUNSHINE", "HOUSE OF THE RISING SUN", "YESTERDAY", "GOD ONLY KNOWS", "WALK ON THE WILD SIDE", "ALL YOU NEED IS LOVE", "DUST IN THE WIND", "BOHEMIAN RHAPSODY", "HOTEL CALIFORNIA", "HEY JUDE", "IMAGINE", "ANGIE", "LIKE A ROLLING STONE", "WHEN DOVES CRY", "AMERICAN PIE", "I WANT TO HOLD YOUR HAND" };

            Dictionary<char, char> ScrambleLetters = new Dictionary<char, char>();
            ScrambleLetters.Add('A', '4');
            ScrambleLetters.Add('E', '3');
            ScrambleLetters.Add('L', '1');
            ScrambleLetters.Add('O', '0');
            ScrambleLetters.Add('I', '!');
            ScrambleLetters.Add('C', '(');
            ScrambleLetters.Add('S', '5');
            ScrambleLetters.Add('G', '6');

            Random random = new Random();
            string OriginalSentence = sentences[random.Next(sentences.Length)];
            string modifiedSentence = OriginalSentence;
            int i = 0;
            while (i < 8/* || modifiedSentence == returnSentence*/)
            {
                KeyValuePair<char, char> randomKeyValuePair = ScrambleLetters.ElementAt(random.Next(0, ScrambleLetters.Count));
                modifiedSentence = modifiedSentence.Replace(randomKeyValuePair.Key, randomKeyValuePair.Value);
                i++;
            }
            return new string[] { modifiedSentence, OriginalSentence };
        }

        /// <summary>
        /// Receives a string in "string1, string2, ..., ..." format, and a string to check for.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>
        /// Returns true if the string is within the long string.
        /// </returns>
        public bool CheckForString(string stringChain, string StringToCheckFor)
        {
            string[] ChainArray = stringChain.Split(',');
            foreach (string s in ChainArray) 
            {
                if (s.Equals(StringToCheckFor))
                    {  return true; }
            }
            return false;
        }
    }
}
