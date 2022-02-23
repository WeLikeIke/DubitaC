using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UI;
using UnityEngine;


/// <summary>
/// Static class to maintain data between the scenes.
/// Stores settings, localization information and all gameplay related data.
/// The default time to solve a <see cref="codeQuestion"/> is stored in the readonly field <see cref="defaultTimer"/>.
/// </summary>
public static class DataManager {
    private static readonly int defaultTimer = 600;


    //Only useful to correctly parse ints from strings
    public static NumberStyles numberRules = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | 
                                             NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;

    #region GameplayVar
    public static codeQuestion currentCodeQuestion;

    public static List<databaseEntry[]> oldLeaderboard = new List<databaseEntry[]>(); //Server: Lobbies in use * clients in lobby | Client: myLobbySize
    public static List<databaseEntry[]> leaderboard = new List<databaseEntry[]>();    //Server: Lobbies in use * clients in lobby | Client: myLobbySize
    public static List<string[]> solutions = new List<string[]>();                    //Server: Lobbies in use * clients in lobby | Client: myLobbySize
    public static List<doubt[]> confirmedDoubts = new List<doubt[]>();                //Server: Lobbies in use * Matrix'          | Client: Matrix' 
    public static List<string[]> serverResults = new List<string[]>();                //Server: Lobbies in use * Matrix'          | Client: Matrix'
    public static List<string[]> userResults = new List<string[]>();                  //Server: Lobbies in use * Matrix'          | Client: Matrix'
    public static List<int[]> userPoints = new List<int[]>();                         //Server: Lobbies in use * clients in lobby | Client: N/A
                                                                                      //'Matrix = (clients in lobby * (clients in lobby -1))
    #endregion

    #region ClientLobbyVar
    public static databaseEntry myData;
    public static int myLobbyNumber = -1;
    public static int myLobbySize = -1;

    #endregion

    #region ServerLobbyVar
    public static int lobbiesInUse;
    public static List<int> allLobbySizes;
    public static List<ulong[]> allLobbyClients;

    #endregion

    #region SettingsVar
    public static float currentVolume = 100f;
    public static int currentTimeout = 2;
    public static string currentPath = Application.dataPath + "/Cpps";
    public static int currentTimer = defaultTimer;

    #endregion

    #region LocalizationVar
    public static string currentLanguage = "ENG";
    public static Dictionary<string, string> localization = null;


    public static string databaseFeedback = "";
    public static string currentBuild = "";

    #endregion

    #region SettingsSetters
    //The function takes a float because that is the return value of an interaction with an external slider
    //By construction value will always be an integer between 0 and 4, the multiplication by 25 is to have a percentage result
    public static void SetVolume(float value) { currentVolume = 25f * value; }

    //The function takes an int that is the return value of an interaction with a multiple choice box
    //By construction value will always be an integer between 0 and 5, the formula converts it into the corresponding seconds: 1, 2, 3, 5, 7, 9
    public static void SetTimeout(int value) { currentTimeout = 1 + value + value / 3 + value / 4 + value / 5; }

    //value should be a valid path into a directory, the assignation does not happen otherwise
    public static void SetPath(string value) { currentPath = value; }
    public static void SetFeedback(string value) { databaseFeedback = value; }

    //The function takes a string because that is the return value of an interaction with an input field
    //By construction value will always contain an integer or the empty string,
    //in case of the empty string or zero the default value is used, otherwise value is parsed into an int
    public static void SetTimer(string value) {
        if (value == "0" || string.IsNullOrEmpty(value)) {
            currentTimer = defaultTimer;
        }else{
            currentTimer = Mathf.Abs(int.Parse(value));
        }
    }

    #endregion


    /// <summary>
    /// Loads the current localization dictionary by reading <see cref="currentLanguage"/>.
    /// </summary>
    public static void LoadDict() {
        localization = new Dictionary<string, string>();

        //Even though the localization is a .csv file, Resources.load forbids the use of the extension, treating every TextAsset as a .txt
        TextAsset localizationFile = Resources.Load("Localization/" + currentLanguage + "_localization") as TextAsset;

        //A valid localization file contains one entry: "_label","text" for each row
        string[] localizationLines = localizationFile.ToString().Split(new[] { '\n' },StringSplitOptions.RemoveEmptyEntries);

        //An underscore in front of the label is mandatory
        //The double quotes around _label and text are mandatory
        //The comma between "_label" and "text" is mandatory
        //Double quote inside "_label" or "text" are forbidden
        //Single quotes and commas inside "text" are allowed
        //Any label format different from: _lowercase_words_and_underscore_spaces is discouraged
        for (int i = 0;i < localizationLines.Length; i++) {
            bool betweenQuotes = false;
            for (int j = 0; j < localizationLines[i].Length; j++) {

                if (localizationLines[i][j] == '"') {
                    betweenQuotes = !betweenQuotes;
                }

                if (localizationLines[i][j] == ',' && !betweenQuotes) {
                    localization[localizationLines[i].Substring(1, j - 2)] = localizationLines[i].Substring(j + 2, localizationLines[i].Length - (j + 4));
                }
            }
        }
    }

    /// <summary>
    /// Extension method of the <see cref="Slider"/> class to update the value on the progress bar percentage wise.
    /// </summary>
    /// <param name="slider">The slider to update the value of.</param>
    /// <param name="newAmount">The current amount to represent on the bar.</param>
    /// <param name="maxAmount">The maximum expected amount that will need representation on the bar.</param>
    /// <param name="startAmount">The amount of progress bar that was already filled before calling this function.</param>
    /// <param name="finalAmount">The amount of progress bar that will be filled when <paramref name="newAmount"/> is equal to <paramref name="maxAmount"/>.</param>
    public static void UpdateProgressBar(this Slider slider, int newAmount, int maxAmount, float startAmount, float finalAmount) {
        if (newAmount < 0 || newAmount > maxAmount) { return; }
        if (startAmount < 0f || startAmount > finalAmount) { return; }
        if (finalAmount > 1f) { return; }

        float multiplier = finalAmount - startAmount;
        int ratio = newAmount / maxAmount;

        slider.value = startAmount + (multiplier * ratio);
    }

    #region GameplaySetters
    //Player Controller scripts update to their own DataManager their data after every modification of it
    public static void ConfirmData(databaseEntry userData) { myData = userData; }

    //Simply saving for later a client's own lobby number and size
    public static void UpdateLobbyInfo(int lobbyIdx, int lobbySize) {
        myLobbyNumber = lobbyIdx;
        myLobbySize = lobbySize;
    }

    //Simply saving for later all lobbies contents on the server
    public static void UpdateServerLobbyInfo(int lobbiesNumber, List<int> lobbiesSize, List<ulong[]> lobbiesClients) {
        lobbiesInUse = lobbiesNumber;
        allLobbySizes = lobbiesSize;
        allLobbyClients = lobbiesClients;
    }

    /// <summary>
    /// Initializes the correct length for <see cref="leaderboard"/> and <see cref="confirmedDoubts"/>.
    /// A leaderboard is as long as the number of players in the lobby and 
    /// the doubts are as long as the number of players times the number of players minus one.
    /// </summary>
    /// <param name="numberOfPlayers">The number of players in the lobby.</param>
    public static void SessionInit(int numberOfPlayers) {
        leaderboard.Add(new databaseEntry[numberOfPlayers]);
        confirmedDoubts.Add(new doubt[numberOfPlayers * (numberOfPlayers - 1)]);
    }

    //This method adds a whole leaderboard, used by the server to update the clients
    public static void AddToLeaderboard(int lobbyIdx, databaseEntry[] userDatas) { leaderboard[lobbyIdx] = userDatas; }
    //This method adds a single user to the leaderboard, used by the clients to ask to be added by the server
    public static void AddToLeaderboard(int lobbyIdx, databaseEntry userData, int idx) { leaderboard[lobbyIdx][idx] = userData;}

    /// <summary>
    /// Saves the given string array into the known solutions for the relevant lobby.
    /// The server saves the solutions to its own <see cref="DataManager"/> after having received them,
    /// then it shares them with all the clients so that they can all have the same view of their own lobby's solutions 
    /// </summary>
    /// <param name="lobbyIdx">Lobby number from which the calls comes from.</param>
    /// <param name="solutionList">Array of solutions.</param>
    public static void AddSolutions(int lobbyIdx, string[] solutionList) {
        if (lobbyIdx > solutions.Count) { Debug.LogError("Error, trying to access lobby: " + lobbyIdx + " while there are only lobbies up to: " + (solutions.Count - 1)); }
        if (lobbyIdx == solutions.Count) {
            solutions.Add(new string[leaderboard[lobbyIdx].Length]);
        }
        solutions[lobbyIdx] = solutionList; 
    }

    /// <summary>
    /// Saves the given string array into the known server results for the relevant lobby.
    /// The server saves the server results to its own <see cref="DataManager"/> after having created them,
    /// then it shares them with all the clients so that they can all have the same view of their own lobby's server results 
    /// </summary>
    /// <param name="lobbyIdx">Lobby number from which the calls comes from.</param>
    /// <param name="resultsList">Array of server results.</param>
    public static void AddServerResults(int lobbyIdx, string[] resultsList) {
        if (lobbyIdx > serverResults.Count) { Debug.LogError("Error, trying to access lobby: " + lobbyIdx + " while there are only lobbies up to: " + (serverResults.Count - 1)); }
        if (lobbyIdx == serverResults.Count) {
            serverResults.Add(new string[resultsList.Length]);
        }
        serverResults[lobbyIdx] = resultsList; 
    }

    /// <summary>
    /// Saves the given string array into the known user results for the relevant lobby.
    /// The server saves the user results to its own <see cref="DataManager"/> after having received them,
    /// then it shares them with all the clients so that they can all have the same view of their own lobby's user results 
    /// </summary>
    /// <param name="lobbyIdx">Lobby number from which the calls comes from.</param>
    /// <param name="resultsList">Array of user results.</param>
    public static void AddUserResults(int lobbyIdx, string[] resultsList) {
        if (lobbyIdx > userResults.Count) { Debug.LogError("Error, trying to access lobby: " + lobbyIdx + " while there are only lobbies up to: " + (userResults.Count - 1)); }
        if (lobbyIdx == userResults.Count) {
            userResults.Add(new string[resultsList.Length]);
        }
        userResults[lobbyIdx] = resultsList;
    }

    public static void AddDoubts(int lobbyIdx, doubt[] doubts) { confirmedDoubts[lobbyIdx] = doubts; }

    #endregion

    #region GameplayGetters
    /// <summary>
    /// Function to return the solution of a specific client.
    /// This is ever only called by clients so we know that it refers to lobbymates.
    /// </summary>
    /// <param name="clientId">The id of the client to get the solution of.</param>
    /// <returns>The solution of the client with the same id as <paramref name="clientId"/>.</returns>
    public static string GetClientSolution(ulong clientId) {
        for (int i = 0; i < leaderboard[0].Length; i++) {
            if (leaderboard[0][i].owner == clientId) {
                return solutions[0][i];
            }
        }

        Debug.LogError("Error, could not find a client with id: " + clientId + " in my lobby (lobby " + myLobbyNumber + ")");
        return null;
    }

    /// <summary>
    /// Function to return the position in the leaderboard of a specific client.
    /// </summary>
    /// <param name="lobbyIdx">The index of the lobby where to serch for the <paramref name="clientId"/>.</param>
    /// <param name="clientId">The id of the client to get the ranking of.</param>
    /// <param name="useOldLeaderboard">Boolean to decide if the rank should be old or current.</param>
    /// <returns>The position in the leaderboard of the client with the same id as <paramref name="clientId"/>.</returns>
    public static int GetClientRank(int lobbyIdx, ulong clientId, bool useOldLeaderboard) {
        databaseEntry[] targetLeaderboard;

        if (useOldLeaderboard) {
            targetLeaderboard = oldLeaderboard[lobbyIdx];
        } else {
            targetLeaderboard = leaderboard[lobbyIdx];
        }

        for (int i = 0; i < targetLeaderboard.Length; i++) {
            if (targetLeaderboard[i].owner == clientId) {
                return i;
            }
        }
        Debug.LogError("Error, could not find a client with id: " + clientId + " in lobby " + lobbyIdx);
        return -1;
    }


    /// <summary>
    /// Function to return the username of a specific client.
    /// This is ever only called by clients so we know that it refers to lobbymates.
    /// </summary>
    /// <param name="clientId">The id of the client to get the username of.</param>
    /// <returns>The username of the client with the same id as <paramref name="clientId"/>.</returns>
    public static string GetClientUsername(ulong clientId) {
        for (int i = 0; i < oldLeaderboard[0].Length; i++) {
            if (oldLeaderboard[0][i].owner == clientId) {
                return oldLeaderboard[0][i].username.ToString();
            }
        }
        Debug.LogError("Error, could not find a client with id: " + clientId + " in my lobby (lobby " + myLobbyNumber + ")");
        return null;
    }

    /// <summary>
    /// Function to return all the usernames of a client's lobby.
    /// This is ever only called by clients so we know that it refers to lobbymates.
    /// </summary>
    /// <returns>The array of usernames of the client in the same lobby as the caller.</returns>
    public static string[] GetUsernameArray() {
        List<string> usernameList = new List<string>();

        for(int i = 0; i < oldLeaderboard[0].Length; i++) {
            usernameList.Add(oldLeaderboard[0][i].username.ToString());
        }

        return usernameList.ToArray();
    }

    #endregion

    /// <summary>
    /// Updates the final score of the client to its progress property so that it may be saved into the database later.
    /// </summary>
    public static void UpdateClientProgress() {
        //10 is the minimum reward for partecipation
        ushort prize = 10;

        //This contains a positive number, starting from the normal amount of reward and reduced by each bought hint
        prize += myData.points;

        //Getting in the top 3 gives much more points
        switch (GetClientRank(0, myData.owner, false)) {
            case 0: prize += 200; break;
            case 1: prize += 100; break;
            case 2: prize += 50; break;
        }

        //Points are capped at 9999 because of the 4 digits restriction in the database, if the restriction is increased then other 9s can be added
        myData.progress += prize;
    }

    /// <summary>
    /// Updates how many points does the client have as a baseline.
    /// The baseline is decreased by every hint bought.
    /// </summary>
    public static void UpdateClientPoints() {
        //Base number of points that are lost when buying an hint
        myData.points -= 30;
    }


    /// <summary>
    /// Function to update the current leaderboard by using an array of corresponding points.
    /// </summary>
    /// <param name="lobbyIdx">The relevant lobby to update.</param>
    /// <param name="pointsList">The array of points that will be used for the sorting.</param>
    public static void UpdateLeaderboard(int lobbyIdx, int[] pointsList) {

        //Copy the points list
        int[] tempPointsList = new int[pointsList.Length];
        for (int i = 0; i < pointsList.Length; i++) {
            tempPointsList[i] = pointsList[i];
        }

        //Copy the leaderboard
        databaseEntry[] newLeaderboard = new databaseEntry[leaderboard[lobbyIdx].Length];
        for (int i = 0; i < leaderboard[lobbyIdx].Length; i++) {
            newLeaderboard[i] = new databaseEntry(leaderboard[lobbyIdx][i]);
        }

        //Use key-values sorting by using the points as keys
        Array.Sort(tempPointsList, newLeaderboard);

        //Reverse both since a bigger score means being closer to th 0th rank
        Array.Reverse(tempPointsList);
        Array.Reverse(newLeaderboard);

        //Like calling AddServerResult but with ints instead of strings
        if (lobbyIdx > userPoints.Count) { Debug.LogError("Error, trying to access lobby: " + lobbyIdx + " while there are only lobbies up to: " + (userPoints.Count - 1)); }
        if (lobbyIdx == userPoints.Count) {
            userPoints.Add(new int[tempPointsList.Length]);
        }
        userPoints[lobbyIdx] = tempPointsList;

        //Call the other overload, that will now sort the use solutions
        UpdateLeaderboard(lobbyIdx, newLeaderboard);
    }


    /// <summary>
    /// Internal overload of <see cref="UpdateLeaderboard(int, int[])"/>, performs the same sorting on the user solutions
    /// to avoid another key-value sort, the leaderboard is passed already sorted
    /// </summary>
    /// <param name="lobbyIdx">The relevant lobby to update.</param>
    /// <param name="newLeaderboard">The already sorted array that will be used to match the sorting.</param>
    private static void UpdateLeaderboard(int lobbyIdx, databaseEntry[] newLeaderboard) {
        oldLeaderboard.Add(new databaseEntry[leaderboard[lobbyIdx].Length]);
        for (int i = 0; i < leaderboard[lobbyIdx].Length; i++) {
            oldLeaderboard[lobbyIdx][i] = new databaseEntry(leaderboard[lobbyIdx][i]);
        }

        string[] newSolutions = new string[solutions[lobbyIdx].Length];

        for (int i = 0; i < newLeaderboard.Length; i++) {
            for (int j = 0; j < leaderboard[lobbyIdx].Length; j++) {
                if (leaderboard[lobbyIdx][j].owner == newLeaderboard[i].owner) {
                    newSolutions[i] = solutions[lobbyIdx][j];
                    break;
                }
            }
        }

        leaderboard[lobbyIdx] = newLeaderboard;
        solutions[lobbyIdx] = newSolutions;
    }

    #region StringUtilities

    /// <summary>
    /// Utility function that returns a substring of the string given containing all its characters that are found inbetween 
    /// the first instance of a string and the next instance of a character (both excluded).
    /// </summary>
    /// <param name="input">String to extract from.</param>
    /// <param name="from">String used to determine the starting point of the extraction, can be any length > 0, it will be excluded from the result.</param>
    /// <param name="to">Char used to determine the end point of the extraction, it will be excluded from the result.</param>
    /// <returns>The extracted string between <paramref name="from"/> and <paramref name="to"/>, 
    /// if <paramref name="input"/> does not contain <paramref name="from"/> it returns the empty string "",
    /// if <paramref name="input"/> does not contain <paramref name="to"/> it returns from <paramref name="from"/> (excluded) to the end of <paramref name="input"/>.</returns>
    public static string ExtractFromStringToChar(string input, string from, char to) {
        string result = "";
        if (input.IndexOf(from) == -1) { return result; }

        bool singleQuotes = false;
        bool doubleQuotes = false;
        bool active = true;

        if (to != '\'') { singleQuotes = true; }
        if (to != '"') { doubleQuotes = true; }


        for (int i = input.IndexOf(from) + from.Length; i < input.Length; i++) {
            if (input[i] == '\'' && singleQuotes) { active = !active; }
            if (input[i] == '"' && doubleQuotes) { active = !active; }

            if (input[i] == to && active) { break; }

            result += input[i];
        }
        return result;
    }

    /// <summary>
    /// Utility function to identify if the given string is of the correct type.
    /// </summary>
    /// <param name="givenInput">String to check the contents of.</param>
    /// <param name="expectedType">String with the name of the target type.</param>
    /// <returns>true if the given string only contains a value of type <paramref name="expectedType"/>, false otherwise.</returns>
    public static bool IsCorrectType(string givenInput, string expectedType) {
        string trimmed = givenInput.Trim();
        switch (expectedType) {
            case "float":
            case "double":
                if (!double.TryParse(trimmed, out _)) {
                    return false;
                }
                break;
            case "int":
                if (!int.TryParse(trimmed, numberRules, new CultureInfo("en-US"), out _)) {
                    return false;
                }
                break;
            case "char":
                if (trimmed.Length != 3 || trimmed[0] != '\'' || trimmed[2] != '\'' || trimmed[1] == '\'' || trimmed[1] == '"') {
                    return false;
                }
                break;
            case "string":
                if (trimmed.Length < 2 || trimmed[0] != '"' || trimmed[trimmed.Length - 1] != '"') {
                    return false;
                }
                string[] temp = trimmed.Split(new[] { "//", "/*", "*/", "\"", "'" }, StringSplitOptions.None);
                if (temp.Length > 3) {
                    return false;
                }
                break;
            case "bool":
                if (trimmed != "true" && trimmed != "false") {
                    return false;
                }
                break;
            default: return false;
        }
        return true;

    }

    /// <summary>
    /// Utility function to identify if the given string is correctly within the given limits.
    /// </summary>
    /// <param name="givenInput">String to check the contents of.</param>
    /// <param name="expectedType">String with the name of the target type.</param>
    /// <param name="currentLimit"><see cref="inputLimits"/> to check against.</param>
    /// <returns>true if the given string is within the given limits, false otherwise.</returns>
    public static bool IsWithinLimits(string givenInput, string expectedType, inputLimits currentLimit) {

        if(expectedType == "bool") { return true; }

        if (currentLimit.setValues == null) {

            if (expectedType == "char") { return true; }

            if (expectedType == "string") {
                if (currentLimit.leftIncluded) {
                    if (givenInput.Length < currentLimit.leftValue) { return false; }
                } else {
                    if (givenInput.Length <= currentLimit.leftValue) { return false; }
                }

                if (currentLimit.rightIncluded) {
                    if (givenInput.Length > currentLimit.rightValue) { return false; }
                } else {
                    if (givenInput.Length >= currentLimit.rightValue) { return false; }
                }
            }

            if (expectedType == "int") {
                int.TryParse(givenInput, numberRules, new CultureInfo("en-US"), out int limitVal);
                if (currentLimit.leftIncluded) {
                    if (limitVal < currentLimit.leftValue) { return false; }
                } else {
                    if (limitVal <= currentLimit.leftValue) { return false; }
                }

                if (currentLimit.rightIncluded) {
                    if (limitVal > currentLimit.rightValue) { return false; }
                } else {
                    if (limitVal >= currentLimit.rightValue) { return false; }
                }

            }

            if (expectedType == "float" || expectedType == "double") {
                double.TryParse(givenInput, out double limitVal);
                if (currentLimit.leftIncluded) {
                    if (limitVal < currentLimit.leftValue) { return false; }
                } else {
                    if (limitVal <= currentLimit.leftValue) { return false; }
                }

                if (currentLimit.rightIncluded) {
                    if (limitVal > currentLimit.rightValue) { return false; }
                } else {
                    if (limitVal >= currentLimit.rightValue) { return false; }
                }
            }

        } else {
            if (!currentLimit.setValues.Contains(givenInput)) { return false; }
        }

        return true;

    }

    /// <summary>
    /// Utility function to split the contents of a string on commas, ignoring commas inside single and double quotes.
    /// Useful for parsing a string of arguments.
    /// It does NOT support escaping single and double quotes characters (' & ").
    /// </summary>
    /// <param name="input">String that needs to be split on commas.</param>
    /// <param name="expectedLength">Number of expected splits.</param>
    /// <returns>String array of the characters between commas, null if the expectedLength requirement was not met.</returns>
    public static string[] CommaSplitPreservingQuotes(string input, int expectedLength) {
        List<string> output = new List<string>() { "" };

        bool singleQuotes = false;
        bool doubleQuotes = false;
        int idx = 0;
        foreach(char singleChar in input) {
            if (singleChar == ',' && !(singleQuotes || doubleQuotes)) {
                output.Add("");
                idx += 1;
                continue;
            }

            if (singleChar == '\'') { singleQuotes = !singleQuotes; }
            if (singleChar == '"') { doubleQuotes = !doubleQuotes; }

            output[idx] += singleChar;
        }

        //Reject a string that could not be split into the correct length
        if (expectedLength > 0 && output.Count != expectedLength) { return null; }

        //Reject a string that contained 2 consecutive commas
        foreach(string singleString in output) {
            if (String.IsNullOrEmpty(singleString)) { return null; }
        }

        return output.ToArray();
    }

    /// <summary>
    /// Extension method to avoid string override.
    /// </summary>
    /// <param name="inputString">The string to which assign <paramref name="newValue"/>.</param>
    /// <param name="newValue">The new value to assign to the <paramref name="inputString"/>.</param>
    /// <returns><paramref name="inputString"/> if null or empty, <paramref name="newValue"/> otherwise.</returns>
    public static string AssignIfEmpty(this string inputString, string newValue) {
        if(string.IsNullOrEmpty(inputString)) { 
            return newValue; 
        } else { 
            return inputString; 
        }
    }

    #endregion

}
