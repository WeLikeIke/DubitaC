using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Class responsible to manage the interaction with the database and the validation of the connections with the server.
/// The maximum amount of points a user can have is stored in constant <see cref="maxPointsPossible"/>.
/// The string that is used during the clientside salting is stored in constant <see cref="sharedSalt"/>.
/// The number of iterations for the secure hashing algorithm is stored in constant <see cref="secureHashIterations"/>.
/// The size in bytes of the hashed user password is stored in constant <see cref="hashSize"/>.
/// The Encoding that has been chosen is stored in the readonly field <see cref="currentEncoding"/>.
/// The separator in the database file is stored in constant <see cref="separator"/>.
/// The relative path to the database folder is stored in constant <see cref="relativeDatabasePath"/>.
/// The path to the database file is stored in constant <see cref="databaseFile"/>.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class AccountManager : NetworkBehaviour {
    private const ushort maxPointsPossible = 9999;
    private const string sharedSalt = "11/09/2021-11/12/2021-28/03/2022";
    private const int secureHashIterations = 1000;
    private const int hashSize = 32;
    //Not allowed to save classes as a const, so we make the Encoding readonly
    private readonly Encoding currentEncoding = new UTF8Encoding(true);
    private const string separator = ",";
    private const string relativeDatabasePath = "/Database";
    private const string databaseFile = "/accounts.csv";


    public TMP_InputField username;
    public TMP_InputField password;
    public bool newAccount = false;

    public NetworkWrapper NW;
    public MySceneManager MSM;

    private int saltSize;
    private string databasePath;
    private string pointsFieldFormat;

    
    
    //We subscribe to the connection approval callback to execute custom code when a client tries to connect to the server
    void Start() {

        //Length recommended online: as long, or longer, than the hash length and divisible by 3
        saltSize = hashSize + (2 * hashSize % 3);

        //Finding the absolute path of the database by checking the relativeDatabasePath constant.
        databasePath = Application.dataPath + relativeDatabasePath + databaseFile;

        //Only the server should have a valid database
        if (NW != null && NW.isServerBuild) { DatabaseCheck(); }

        //Finding out how many leading zeros might be needed by checking the maxPointsPossible constant
        pointsFieldFormat = "D" + maxPointsPossible.ToString().Length.ToString();

        //User authentication only happens in the first scene
        if (username != null && password != null) {
            NetworkManager.Singleton.ConnectionApprovalCallback += ValidateLogin;
        }

    }

    /// <summary>
    /// External function to change the status of the next connection request, from known credentials to new credentials and viceversa.
    /// The function is public void and value parametrized on purpose so that it could be called from a toggle OnClick.
    /// </summary>
    /// <param name="value">true if the credentials should be considered new, false if the credentials should be considered known.</param>
    public void SetNewAccount(bool value) { newAccount = value; }

    /// <summary>
    /// Utility function to convert plaintext into a byte array.
    /// </summary>
    /// <param name="plainText">Plaintext string (username or password).</param>
    /// <returns>The corresponding byte array using the <see cref="currentEncoding"/>.</returns>
    private byte[] FromPlaintextToBytes(string plainText) {
        return currentEncoding.GetBytes(plainText);
    }

    /// <summary>
    /// Utility function to conver a byte array into a database ready string.
    /// It uses base64 to ensure that no commas or whitespace ends up in the database
    /// </summary>
    /// <param name="bytes">Array of bytes to convert.</param>
    /// <returns>A string in database ready format.</returns>
    private string FromBytesToDBString(byte[] bytes) {
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Utility function to covert a database string into a byte array.
    /// </summary>
    /// <param name="dbString">String extracted from the database.</param>
    /// <returns>The corresponding byte array.</returns>
    private byte[] FromDBStringToBytes(string dbString) {
        return Convert.FromBase64String(dbString);
    }

    /// <summary>
    /// Utility function to convert a subset of a byte array into plaintext.
    /// </summary>
    /// <param name="bytes">Original array of bytes to convert.</param>
    /// <param name="start">Beginning index of the subset to cnvert.</param>
    /// <param name="length">Length of the subset to convert.</param>
    /// <returns>A string in plainttext using the <see cref="currentEncoding"/>.</returns>
    private string FromBytesToPlainText(byte[] bytes, int start, int length) {
        return currentEncoding.GetString(bytes, start, length);
    }

    /// <summary>
    /// Function required to validate the connection to the server.
    /// The connection will be accepted if:
    /// The sent username and password are valid OR
    /// The sent username is new and the newAccount boolean is true.
    /// And refused otherwise, if someone with the same username is already connected to the server then the newest client is rejected.
    /// </summary>
    /// <param name="connectionData">Array of bytes representing the data sent from client to server to validate the connection.</param>
    /// <param name="clientId">Id of the client that requests a connection with the server.</param>
    /// <param name="callback">Mandatory callback parameter, it signals to the server the outcome of the validation.</param>
    private void ValidateLogin(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback) {

        (bool newRegistration, string username, byte[] password) = ParseConnectionData(connectionData);

        //Default values
        bool approved = false;
        databaseEntry userData = new databaseEntry(100);

        //Finding the account in the database
        (bool isPasswordCorrect, databaseEntry foundUser) = CheckAccount(username, password);

        //Found username and correct password
        if (isPasswordCorrect) {
            approved = true;
            userData = foundUser;
            userData.owner = clientId;
        } else {
            //New account and username available
            if (foundUser.username.IsEmpty && newRegistration) {
                CreateNewLogin(username, password);
                approved = true;
                userData.username = username;
                userData.owner = clientId;
            }
        }

        //A user cannot be shared between clients, if a client with the same user credentials is already connected, the connection is refused
        if (approved) {
            if (!NW.NewConfirmedClient(clientId, userData)) {
                approved = false;
            }
        }

        //Emit the callback, approved contains the acceptation or refusal
        callback(true, null, approved, null, null);

        //After having confirmed that the connection has been accepted
        if (approved) {

            //The array of clients that need to change their data because a new client connected
            //it consists only of the id of the connected client itself, otherwise there would be data that is overritten and lost
            ulong[] currentClientId = new ulong[] { clientId };

            ClientRpcParams oneClientRpcParams = new ClientRpcParams {
                Send = new ClientRpcSendParams {
                    TargetClientIds = currentClientId
                }
            };

            //Setup userdata with the NetworkWrapper
            NW.FillDataClientRpc(userData, oneClientRpcParams);
            NW.ValidLogin(clientId, oneClientRpcParams);
        }
    }

    /// <summary>
    /// Utility function to parse the connectionData.
    /// <paramref name="connectionData"/> follows the format:
    /// First byte containing 0 or 1, representing known or new credentials respectively.
    /// Next series of bytes of variable length contain the username.
    /// The last <see cref="hashSize"/> bytes always represent the salted and hashed password.
    /// </summary>
    /// <param name="connectionData">Array of bytes representing the data sent from client to server to validate the connection.</param>
    /// <returns>The tuple containing the data parsed in their correct format.</returns>
    private (bool, string, byte[]) ParseConnectionData(byte[] connectionData) {
        //First byte, is the request for a known (0) or a new (1) account
        bool newRegistration = (connectionData[0] == 1);

        //Unknown Length, plainText of the username
        string username = FromBytesToPlainText(connectionData, 1, connectionData.Length - (hashSize + 1));

        //Last hashSize bytes, Hash of salted user password
        byte[] password = new byte[hashSize];
        Array.Copy(connectionData, connectionData.Length - hashSize, password, 0, hashSize);

        return (newRegistration, username, password);
    }

    /// <summary>
    /// Function to start a client and submit a login request.
    /// Checks username and password requirements on client side,
    /// If they are ok, the password is salted and hashed and then sent to the server to check if it is a valid login.
    /// The server will validate the sent data and accept or refuse the connection accordingly.
    /// The function is public void and parameterless on purpose so that it an be call from a button OnClick.
    /// </summary>
    public void Submit() {
        //Checks for username and password requirements, execution proceeds only if they are both valid
        RequirementsCheck();

        byte[] byteUsername = FromPlaintextToBytes(username.text);
        byte[] bytePassword = FromPlaintextToBytes(password.text);

        //Salts the password with the username and a long shared salt
        byte[] hashedPassword = SaltAndHash(bytePassword, Salt(byteUsername, FromPlaintextToBytes(sharedSalt)));

        //Create and set the connectionData
        byte[] customConnectionData = Salt(newAccount, Salt(byteUsername, hashedPassword));
        NetworkManager.Singleton.NetworkConfig.ConnectionData = customConnectionData;

        //Starts the client so that the connectiondData can be used for the validation
        NetworkManager.Singleton.StartClient();
    }

    /// <summary>
    /// Utility function to chek if the requirements of username and password are met.
    /// If they are not, the feedback text is updated accordingly and the main menu is reloaded.
    /// </summary>
    private void RequirementsCheck() {
        //Check for the username requirements
        if (!CheckStringLength(username.text, 1, 20)) {
            DataManager.databaseFeedback = "_bad_username";
            DataManager.wasRejected = true;
            MSM.LoadSceneZero();
        }

        //Check for the password requirements
        if (!CheckStringLength(password.text, 8, 20)) {
            DataManager.databaseFeedback =  "_bad_password";
            DataManager.wasRejected = true;
            MSM.LoadSceneZero();
        }
    }

    /// <summary>
    /// Utility function to check if a string's length is between the given range
    /// </summary>
    /// <param name="input">String to check.</param>
    /// <param name="minLength">Minimum length that the string can have (inclusive).</param>
    /// <param name="maxLength">Maximum length that the string can have (inclusive).</param>
    /// <returns>true if the string's length is inside the range, false if it is outside.</returns>
    private bool CheckStringLength(string input, int minLength, int maxLength) {
        return (input.Length >= minLength && input.Length <= maxLength);
    }

    /// <summary>
    /// Utility function to generate a random sequence of <see cref="saltSize"/> bytes.
    /// To ensure cryptographically secure randomness we use <see cref="RNGCryptoServiceProvider"/> from <see cref="System.Security.Cryptography"/>.
    /// </summary>
    /// <returns>Returns a cryptographically secure random byte array.</returns>
    private byte[] GenerateSalt() {
        byte[] salt = new byte[saltSize];
        RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();
        rand.GetBytes(salt);

        return salt;
    }

    /// <summary>
    /// Utility function that appends one byte array to another, also known as "salting".
    /// </summary>
    /// <param name="dish">Original byte array, it will be stored on the left of the result.</param>
    /// <param name="salt">Salting byte array, it will be stored on the right of the result.</param>
    /// <returns>A new byte array containing both of the input arrays in sequence.</returns>
    private byte[] Salt(byte[] dish, byte[] salt) {
        byte[] salted = new byte[dish.Length + salt.Length];

        //Store the first array
        for (int i = 0; i < dish.Length; i++) { salted[i] = dish[i]; }

        //Store the second array
        for (int i = 0; i < salt.Length; i++) { salted[dish.Length + i] = salt[i]; }

        return salted;
    }

    /// <summary>
    /// Utility function that appends a byte array on a byte containing 0 or 1 depending on a boolean.
    /// More specific overload of <see cref="Salt(byte[], byte[])"/>.
    /// </summary>
    /// <param name="dish">Boolean that will be converted into a size 1 byte array, it will be stored on the left of the result.</param>
    /// <param name="salt">Salting byte array, it will be stored on the right of the result.</param>
    /// <returns>A new byte array containing both of the input arrays in sequence.</returns>
    private byte[] Salt(bool dish, byte[] salt) {
        //Convert the bool to a byte array
        byte[] byteDish = new byte[1];
        if (dish) {
            byteDish[0] = 1;
        } else {
            byteDish[0] = 0;
        }
        //Call the more generic Salt function
        return Salt(byteDish, salt);
    }

    /// <summary>
    /// Utility function that creates a salted and hashed byte array.
    /// The result is then ready to be converted to string and stored in the database.
    /// </summary>
    /// <param name="password">Original byte array containing a password in plaintext.</param>
    /// <param name="salt">Salt to be used to salt the <paramref name="password"/> byte array.</param>
    /// <returns>A salted and hashed array of the original password.</returns>
    private byte[] SaltAndHash(byte[] password, byte[] salt) {
        return new Rfc2898DeriveBytes(password, salt, secureHashIterations).GetBytes(hashSize);
    }

    /// <summary>
    /// Function to create a new user account.
    /// A new salt is generated anytime a new account is created.
    /// The server never receives nor stores a plain text password.
    /// </summary>
    /// <param name="username">Plaintext username.</param>
    /// <param name="password">Salted and hashed byte array of the password.</param>
    private void CreateNewLogin(string username, byte[] password) {
        byte[] salt = GenerateSalt();
        byte[] readyPassword = SaltAndHash(password, salt);
        Store(username, readyPassword, salt);
    }

    /// <summary>
    /// Utility function to retrieve an account from the database.
    /// To check that the login information is correct, 
    /// the process of salting and hashing is repeated with the information in the database.
    /// </summary>
    /// <param name="username">Plaintext username.</param>
    /// <param name="password">Salted and hashed byte array of the password.</param>
    /// <returns>Tuple containing a bool representing if the credentials are valid and a databaseEntry with the corresponding match (a default databaseEntry if none found).</returns>
    private (bool, databaseEntry) CheckAccount(string username, byte[] password) {
        //Default values
        databaseEntry userData = new databaseEntry(100);

        //Read the csv database by splitting on newlines (\n)
        string[] database = File.ReadAllLines(databasePath);

        foreach(string row in database) {
            //Find the columns by splitting on the decided separator
            string[] columns = row.Split(new[] { separator }, StringSplitOptions.None);

            //Plaintext check of the username in the first column
            if (columns[0] == username) {
                userData.username = username;

                if (!ushort.TryParse(columns[1], out userData.progress)) {
                    Debug.LogError("User " + username + " does not have a valid database entry, assuming default points.");
                }

                //Check if it the given hash corresponds to the hash of the password stored in the database
                bool isPasswordCorrect = CompareStoredHash(password, columns[2], columns[3]);

                return (isPasswordCorrect, userData);
            }
        }

        //Known credentials not found
        return (false, userData);
    }

    /// <summary>
    /// Utility function to check if a given hash corresponds to the hash stored in the database.
    /// </summary>
    /// <param name="inputHash">Byte array representing the hash to test.</param>
    /// <param name="storedHash">Database string of the stored hash.</param>
    /// <param name="storedSalt">Database string of the stored salt.</param>
    /// <returns>true if the hash in the database corresponds to the given one, false otherwise.</returns>
    private bool CompareStoredHash(byte[] inputHash, string storedHash, string storedSalt) {
        return FromBytesToDBString(SaltAndHash(inputHash, FromDBStringToBytes(storedSalt))) == storedHash;
    }

    /// <summary>
    /// Function to store in the database the triple:
    /// username, salted and hashed password, salt used serverside.
    /// </summary>
    /// <param name="username">Plaintext username.</param>
    /// <param name="password">Salted and hashed byte array of the password.</param>
    /// <param name="salt">Salt used in serverside salting of the password.</param>
    private void Store(string username, byte[] password, byte[] salt) {
        //Only possible during a runtime deletion of the database
        DatabaseCheck();

        //A new account starts with 0 points
        string paddedZeroPoints = 0.ToString(pointsFieldFormat);

        //Create the database entry
        string content = username + separator + paddedZeroPoints + separator + FromBytesToDBString(password) + separator + FromBytesToDBString(salt) + "\n";

        //Store it on the file
        File.AppendAllText(databasePath, content);
    }

    /// <summary>
    /// Utility function to check for the existence of the database path and create it with an empty database if it is not found.
    /// </summary>
    private void DatabaseCheck() {
        Directory.CreateDirectory(Application.dataPath + relativeDatabasePath);
        if (!File.Exists(databasePath)) {
            File.AppendAllText(databasePath, " username, points, password, salt\n");
        }
    }

    /// <summary>
    /// Function to update in the database the points of the given username.
    /// To facilitate the overwriting of the file without having to read all of it in memory,
    /// we store the points as a fixed length string padded with leading zeros.
    /// </summary>
    /// <param name="username">Plaintext username.</param>
    /// <param name="points">Amount of points to be saved in the database.</param>
    public void StorePoints(string username, ushort points) {

        //The points that can be saved are capped at the maximum
        points = Math.Min(points, maxPointsPossible);

        //Fixed length string, as many leading zeros as needed to be the chosen length
        byte[] pointsBytes = currentEncoding.GetBytes(points.ToString(pointsFieldFormat));

        //The usernames are always preceded by a newline in a valid database
        byte[] target = currentEncoding.GetBytes("\n" + username + separator);

        //Using to dispose of the file correctly
        using FileStream database = new FileStream(databasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        long seekAmount = 1 + FindInFile(database, target);
        if (seekAmount > 1) {
            Debug.LogError("Error, could not find username: " + username + " in the database.");
        } else {
            database.Seek(seekAmount, SeekOrigin.Current);
            database.Write(pointsBytes, 0, pointsBytes.Length);
        }

    }

    /// <summary>
    /// Utility function to find a target sequence of bytes in a <see cref="FileStream"/>.
    /// </summary>
    /// <param name="file">The <see cref="FileStream"/> to explore.</param>
    /// <param name="target">The byte array that must be found in the <see cref="FileStream"/>.</param>
    /// <returns>The amount needed to be given to <see cref="FileStream.Seek(long, SeekOrigin)"/> to 
    /// return at the position at the end of the target, or 1 if <paramref name="target"/> could not be found.</returns>
    private long FindInFile(FileStream file, byte[] target) {
        byte[] buffer = new byte[target.Length];
        int idx = 0;

        //Read as long as there is something to read
        while (file.Read(buffer, 0, buffer.Length) > 0) {
            //Check what has been read
            for (int i = 0; i < buffer.Length; i++) {
                //If the character corresponds to the expected character in the target array we might have found our target
                if (buffer[i] == target[idx]) {
                    idx += 1;

                    //If the character is in the target and the target is finished it means that we found the target
                    if (idx == target.Length) {
                        return (i - buffer.Length);
                    }

                } else {
                    //The moment one character does not correspond to the target,
                    //we have to restart the check on the target from the beginning
                    idx = 0;
                }

            }

        }

        return 1;
    }

}
