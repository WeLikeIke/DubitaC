using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

//DO NOT REMOVE THIS, IT IS NEEDED!
using static SerializationExtensions;

/// <summary>
/// General manager of the doubting round.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class DoubtManager : NetworkBehaviour {

    //Server only
    public int doneCounter = 0;
    public int totalMatrixSize = 0;

    public List<int>      matrixSize    = new List<int>();
    public List<string[]> serverResults = new List<string[]>();
    public List<string[]> userResults   = new List<string[]>();
    public List<int[]>    pointDeltas   = new List<int[]>();

    public MySceneManager MSM;
    public ExecutionManager EM;
    public ulong targetId;
    public List<doubt[]> doubtList = new List<doubt[]>();


    //Server control panel
    public Slider doneSlider;
    public GameObject serverPanel;
    public TextMeshProUGUI[] readyListText;

    //Loading panel
    public GameObject loadingPanel;

    public Button openDoubtPanelButton;

    public TMP_InputField givenInput;
    public TMP_InputField correctOutput;

    public TMP_InputField expectedOutput;
    public Button noCompileButton;
    public Button timeoutButton;
    public Button crashButton;

    public Button doubtButton;
    public Button removeButton;

    //We are using Awake instead of Start because of the dependency that PlayerSpawner has on DoubtManager
    void Awake() { DoubtDataInit(); }

    void Start() {
        if (NetworkManager.Singleton.IsServer) {
            serverPanel.SetActive(true);

            for (int i = 0; i < DataManager.lobbiesInUse; i++) {
                readyListText[i].text = "0/" + matrixSize[i];
                readyListText[i].rectTransform.parent.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Function to initialize all required data collections, 1 for each client, all of them for the server.
    /// Initializes: <see cref="matrixSize"/>, <see cref="doubtList"/>, <see cref="serverResults"/> and <see cref="userResults"/> for both clients and server.
    /// Initializes: <see cref="totalmatrixSize"/> and <see cref="pointDeltas"/> for the server only.
    /// </summary>
    private void DoubtDataInit() {
        if (NetworkManager.Singleton.IsServer) {
            for (int i = 0; i < DataManager.lobbiesInUse; i++) {
                int targetLobbySize = DataManager.allLobbySizes[i];


                matrixSize.Add(targetLobbySize * (targetLobbySize - 1));
                totalMatrixSize += matrixSize[i];

                pointDeltas.Add(new int[targetLobbySize]);
                for (int j = 0; j < pointDeltas[i].Length; j++) {
                    pointDeltas[i][j] = pointDeltas[i].Length - 1 - j;
                }

                doubtList.Add(new doubt[matrixSize[i]]);

                serverResults.Add(new string[matrixSize[i]]);
                for (int j = 0; j < serverResults[i].Length; j++) {
                    serverResults[i][j] = "";
                }

                userResults.Add(new string[matrixSize[i]]);
                for (int j = 0; j < userResults[i].Length; j++) {
                    userResults[i][j] = "";
                }

            }

        } else {
            //Clients can doubt only their lobbyMates
            matrixSize.Add(DataManager.myLobbySize - 1);
            doubtList.Add(new doubt[matrixSize[0]]);
            serverResults.Add(new string[matrixSize[0]]);
            userResults.Add(new string[matrixSize[0]]);

        }

    }

    #region DoubtPanelManagement
    /// <summary>
    /// Utility function to reset the doubt panel to the default state.
    /// </summary>
    private void ResetDoubtPanel() {
        givenInput.text = "";
        correctOutput.text = "";

        expectedOutput.text = "";
        expectedOutput.interactable = true;

        noCompileButton.interactable = true;
        timeoutButton.interactable = true;
        crashButton.interactable = true;
        doubtButton.interactable = false;
    }

    /// <summary>
    /// Function to select the clicked PlayerBox as the current target.
    /// </summary>
    /// <param name="clientId">The id of the client corresponding to the clicked PlayerBox.</param>
    public void SetTarget(ulong clientId) {
        //Save the new target
        targetId = clientId;

        //Can only doubt if the target is not yourself
        openDoubtPanelButton.interactable = (targetId != NetworkManager.Singleton.LocalClientId);

        //Can only remove doubts if it was doubted before
        removeButton.interactable = (FindDoubtTargeting(targetId) >= 0);

        //Reset the doubt panel
        ResetDoubtPanel();
    }

    /// <summary>
    /// External function to enforce exclusivity between the 4 options and to notify that the user has pressed a button.
    /// The function is public void and single parametrized on purpose so that it can be called by a button OnClick.
    /// </summary>
    /// <param name="b">The button that has just been pressed.</param>
    public void SelectButton(Button b) {
        bool interaction = !expectedOutput.interactable;
        expectedOutput.interactable = interaction;

        if (b == noCompileButton) {
            timeoutButton.interactable = interaction;
            crashButton.interactable = interaction;
        }

        if (b == timeoutButton) {
            noCompileButton.interactable = interaction;
            crashButton.interactable = interaction;
        }

        if (b == crashButton) {
            noCompileButton.interactable = interaction;
            timeoutButton.interactable = interaction;
        }

        doubtButton.interactable = AllSetForDoubt();
    }

    /// <summary>
    /// External function to notify that new text has been entered in one of the 3 input fields.
    /// The function is public void and parameterless on purpose so that it can be called by an inputfield OnValueChanged.
    /// </summary>
    public void NewText() { doubtButton.interactable = AllSetForDoubt(); }

    /// <summary>
    /// External function to enforce the exclusivity between the 4 options and to notify that the user has entered a string.
    /// The function is public void and value parametrized on purpose so that it can be called by an inputfield OnValueChanged.
    /// </summary>
    /// <param name="value"></param>
    public void SelectExpected(string value) {
        noCompileButton.interactable = (value.Length == 0);
        timeoutButton.interactable = (value.Length == 0);
        crashButton.interactable = (value.Length == 0);

        NewText();
    }

    /// <summary>
    /// Utility function to check for all requirements of the doubt panel.
    /// The user must enter 2 strings, one for input and one for expected "perfect" output.
    /// The user must select one of the 4 "expected" options.
    /// Inputs and outputs must respect the function signature's typings.
    /// Expected output and expected "perfect" output must differ.
    /// The doubt cannot be exaclty the same as a previously created doubt on the same target.
    /// </summary>
    /// <returns>true if the requirements are all met, false otherwise.</returns>
    public bool AllSetForDoubt() {

        if (givenInput.text.Length == 0) { return false; }
        //The inputs should respect all typings and length
        if (!CorrectInputSequence(givenInput.text.Trim(), EM.argumentsType, EM.argumentsLimits)) { return false; }

        if (correctOutput.text.Length == 0) { return false; }
        //The correct output should respect the typing of the function
        if (!CorrectInputSequence(correctOutput.text.Trim(), EM.functionType)) { return false; }

        //When present, the expected output should respect the typing of the function
        if (expectedOutput.text.Length > 0 && !CorrectInputSequence(expectedOutput.text, EM.functionType)) { return false; }

        //It is impossible to think the user made a mistake and then propose that the correct solution outputs the same
        if (correctOutput.text.Trim() == expectedOutput.text.Trim()) { return false; }

        Button atLeastOneOn = null;
        bool atLeastOneOff = false;
        bool differentDoubt = true;

        if (noCompileButton.interactable) {
            atLeastOneOn = noCompileButton;
        } else {
            atLeastOneOff = true;
        }

        if (timeoutButton.interactable) {
            atLeastOneOn = timeoutButton;
        } else {
            atLeastOneOff = true;
        }

        if (crashButton.interactable) {
            atLeastOneOn = crashButton;
        } else {
            atLeastOneOff = true;
        }

        //Checks if the current inserted doubt is exactly the same to a previously created doubt
        int idx = FindDoubtTargeting(targetId);
        if (idx >= 0) {
            if (doubtList[0][idx].input.ToString() == givenInput.text &&
                doubtList[0][idx].output.ToString() == correctOutput.text && (
                (doubtList[0][idx].expected.ToString() == expectedOutput.text && !string.IsNullOrEmpty(expectedOutput.text) && atLeastOneOn == null) ||
                (doubtList[0][idx].doubtType == DOUBTTYPE.NoCompilation && atLeastOneOn == noCompileButton) ||
                (doubtList[0][idx].doubtType == DOUBTTYPE.Timeout && atLeastOneOn == timeoutButton) ||
                (doubtList[0][idx].doubtType == DOUBTTYPE.Crash && atLeastOneOn == crashButton))) {
                differentDoubt = false;
            }
        }

        return ((atLeastOneOn != null || expectedOutput.text.Length > 0) && atLeastOneOff && differentDoubt);
    }

    /// <summary>
    /// Utility function to check if a given input string contains comma separated values
    /// that follow the expected type patterns and are within the given limits. 
    /// </summary>
    /// <param name="input">String containing comma separated values.</param>
    /// <param name="expectedType">String array containing the types of the expected values.</param>
    /// <param name="limitRules"><see cref="inputLimits"/> array to apply on each expected value.</param>
    /// <returns>true if the requirements are all met, false otherwise.</returns>
    private bool CorrectInputSequence(string input, string[] expectedType, inputLimits[] limitRules) {
        string[] splitInputs = DataManager.CommaSplitPreservingQuotes(input, expectedType.Length);
        if (splitInputs == null) { return false; }

        for (int i = 0; i < expectedType.Length; i++) {
            if (!DataManager.IsCorrectType(splitInputs[i], expectedType[i])) { return false; }
        }

        if (limitRules == null) { return true; }


        for (int i = 0; i < expectedType.Length; i++) {
            if (!DataManager.IsWithinLimits(splitInputs[i], expectedType[i], limitRules[i])) { return false; }
        }

        return true;
    }

    /// <summary>
    /// Overload of <see cref="CorrectInputSequence(string, string[], inputLimits[])"/>, 
    /// only on one type and without any limit to respect.
    /// </summary>
    /// <param name="input">String containing comma separated values.</param>
    /// <param name="expectedType">String containing the type of all the expected values.</param>
    /// <returns>true if the requirements are all met, false otherwise.</returns>
    private bool CorrectInputSequence(string input, string expectedType) {
        string[] temp = new string[1] { expectedType };
        return CorrectInputSequence(input, temp, null);
    }
    #endregion

    #region DoubtOperations
    /// <summary>
    /// External function to remove the <see cref="doubt"/> against the current target.
    /// The function is public void and parameterless on purpose so that it can be called by a button OnClick.
    /// </summary>
    public void RemoveDoubt() {
        int idx = FindDoubtTargeting(targetId);
        //this return should never happen
        if (idx == -1) { return; }

        doubtList[0][idx] = new doubt();
        removeButton.interactable = false;
        ResetDoubtPanel();
    }

    /// <summary>
    /// External function to create a <see cref="doubt"/> against the current target.
    /// The function is public void and parameterless on purpose so that it can be called by a button OnClick.
    /// </summary>
    public void CreateDoubt() {
        /*
		TEST_CASE("clientId", "[user]"){
			CHECK(fun(TIMEOUT, givenInput.text) != correctOutput.text);
			CHECK(fun(TIMEOUT, givenInput.text) == expectedOutput.text);
			CHECK_THROWS_WITH(fun(TIMEOUT, givenInput.text), "Timeout");
            CHECK_THROWS_WITH(fun(TIMEOUT, givenInput.text), !Contains("Timeout"));
		};
		*/
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        string userClientDoubt = "";

        /*RESTRICTIONS: 
			the user inserted strings cannot be longer than 20 bytes
			the admin chosen function names cannot be longer than 20 bytes
		*/

        givenInput.text = givenInput.text.Trim();
        expectedOutput.text = expectedOutput.text.Trim();
        correctOutput.text = correctOutput.text.Trim();

        //Doubt on no compilation
        if (noCompileButton.interactable) {
            userClientDoubt += "TEST_CASE(\"" + clientId;                       //12-13  bytes
            userClientDoubt += "\", \"[user]\"){\nCHECK(" + EM.functionName;    //21-40  bytes
            userClientDoubt += "(TIMEOUT, " + givenInput.text;                  //11-30  bytes
            userClientDoubt += ") != " + correctOutput.text + ");\n};\n\n";     //13-32  bytes
        }                                                                       //57-115 total

        //Doubt on a crash
        if (crashButton.interactable) {
            userClientDoubt += "TEST_CASE(\"" + clientId;                                               //12-13  bytes
            userClientDoubt += "\", \"[user]\"){\nCHECK_THROWS_WITH(" + EM.functionName;                //33-52  bytes
            userClientDoubt += "(TIMEOUT, " + givenInput.text + "), !Contains(\"Timeout\"));\n};\n\n";  //41-60  bytes
        }                                                                                               //86-125 total

        //Doubt on timeout
        if (timeoutButton.interactable) {
            userClientDoubt += "TEST_CASE(\"" + clientId;                                   //12-13  bytes
            userClientDoubt += "\", \"[user]\"){\nCHECK_THROWS_WITH(" + EM.functionName;    //33-52  bytes
            userClientDoubt += "(TIMEOUT, " + givenInput.text + "), \"Timeout\");\n};\n\n"; //30-49  bytes
        }                                                                                   //75-114 total

        //Doubt on a value
        if (expectedOutput.text.Length > 0) {
            userClientDoubt += "TEST_CASE(\"" + clientId;                       //12-13  bytes
            userClientDoubt += "\", \"[user]\"){\nCHECK(" + EM.functionName;    //21-40  bytes
            userClientDoubt += "(TIMEOUT, " + givenInput.text;                  //11-30  bytes
            userClientDoubt += ") == " + expectedOutput.text + ");\n};\n\n";    //13-32  bytes
        }                                                                       //57-115 total

        //Server doubt, always on a value
        string userServerDoubt = "";
        userServerDoubt += "TEST_CASE(\"" + clientId + "->" + targetId;         //15-17  bytes
        userServerDoubt += "\", \"[user]\"){\nCHECK(" + EM.functionName;        //21-40  bytes
        userServerDoubt += "(TIMEOUT, " + givenInput.text;                      //11-30  bytes
        userServerDoubt += ") == " + correctOutput.text + ");\n};\n\n";         //13-32  bytes
                                                                                //60-119 total

        DOUBTTYPE doubtType = DecideDoubtType(noCompileButton.interactable, timeoutButton.interactable, crashButton.interactable);
        doubt doubtData = new doubt(clientId, targetId, givenInput.text, correctOutput.text, expectedOutput.text,
                                    doubtType, userClientDoubt, userServerDoubt);

        UpdateIfAlreadyDoubted(doubtData);

        doubtButton.interactable = false;
        removeButton.interactable = true;
    }


    /// <summary>
    /// Utility function to overwrite an already existing doubt.
    /// If no already existing <see cref="doubt"/> is found, the <see cref="doubt"/> is saved in the first available spot.
    /// </summary>
    /// <param name="doubtData">New doubt.</param>
    private void UpdateIfAlreadyDoubted(doubt doubtData) {
        int idx = FindDoubtTargeting(doubtData.targetId);
        if (idx == -1) { idx = FindEmptyDoubtSpot(0); }
        if (idx != -1) { doubtList[0][idx] = doubtData; }
    }


    /// <summary>
    /// External function called at the end of the available time.
    /// Sends all doubts to the server, one by one.
    /// Also opens the loading panel until the next scene is loaded.
    /// </summary>
    public void SendDoubts() {
        if (NetworkManager.Singleton.IsServer) { return; }
        loadingPanel.SetActive(true);

        foreach (doubt singleDoubt in doubtList[0]) {
            SendDoubtsServerRpc(DataManager.myLobbyNumber, singleDoubt);
        }
    }
    #endregion

    /// <summary>
    /// Function to start the server and client executions when all the clients are ready.
    /// The function is 'async' because the compilation and execution takes time (over 30 seconds usually).
    /// The function is 'async void' because it adheres to the "fire and forget" pattern, its termination is signaled by a side effect (the progressbar reaching 50%)
    /// </summary>
    public async void CheckAllAndStartExecution() {

        if (doneCounter < totalMatrixSize) { return; }
        doneCounter = 0;

        //Convert user solutions in readyCpps final test batteries
        List<string[]> cppContents = new List<string[]>();

        for (int l = 0; l < DataManager.lobbiesInUse; l++) {

            (string serverSolution, string[] userSolutions) = CreateLobbySolutions(l);
            cppContents.Add(userSolutions);

            string correctResult = await EM.TestReadySolution(serverSolution, "Solution");

            doneSlider.UpdateProgressBar(1 + (l * DataManager.lobbiesInUse), 2 * DataManager.lobbiesInUse, 0f, 0.5f);

            //It is possible for the solution to not run any test when no one partecipated
            if (correctResult.IndexOf("No tests ran.") == -1) {
                ParseServerDoubt(l, correctResult);
                DataManager.AddServerResults(l, serverResults[l]);
            }

            //Each client will execute its own solution
            for (int i = 0; i < cppContents[l].Length; i++) {
                ClientRpcParams oneClientRpcParam = new ClientRpcParams {
                    Send = new ClientRpcSendParams {
                        TargetClientIds = new ulong[] { DataManager.leaderboard[l][i].owner }
                    }
                };

                OffloadExecutionClientRpc(l, i, cppContents[l][i], "Client" + DataManager.leaderboard[l][i].owner, oneClientRpcParam);
            }

            doneSlider.UpdateProgressBar(2 + (l * DataManager.lobbiesInUse), 2 * DataManager.lobbiesInUse, 0f, 0.5f);
        }
    }

    /// <summary>
    /// Utility function to create all the executables of a lobby, the perfect one of the server and the doubted ones for the clients
    /// </summary>
    /// <param name="lobbyIdx">Index of the lobby of the client.</param>
    /// <returns>Tuple containing the server solution and the array of users solutions of the <paramref name="lobbyIdx"/> lobby.</returns>
    private (string, string[]) CreateLobbySolutions(int lobbyIdx) {
        //One perfect cpp needs to be used to make sure that the expectations of the users are correct
        string serverSolution = EM.GetIntendedSolution();

        string[] userSolutions = new string[DataManager.solutions[lobbyIdx].Length];

        //Obtain the user solutions with the required boilerplate
        for (int i = 0; i < userSolutions.Length; i++) {
            userSolutions[i] = EM.Boilerplate(EM.GetUserSolution(DataManager.solutions[lobbyIdx][i]), false);
            userSolutions[i] += EM.GetFinalTests();
        }

        //Add all the doubts that the user gave to the correct readyCpps
        foreach (doubt singleDoubt in doubtList[lobbyIdx]) {

            for (int j = 0; j < userSolutions.Length; j++) {
                if (DataManager.leaderboard[lobbyIdx][j].owner == singleDoubt.targetId) {
                    userSolutions[j] += singleDoubt.clientDoubt.ToString();
                }
            }

            serverSolution += singleDoubt.serverDoubt.ToString();
        }

        return (serverSolution, userSolutions);
    }

    #region GeneralParsing

    /// <summary>
    /// Utility function to determine the <see cref="DOUBTTYPE"/> of a <see cref="doubt"/>.
    /// No more than one input should be true.
    /// </summary>
    /// <param name="noCompilation">true if the <see cref="doubt"/> expects no compilation, false otherwise.</param>
    /// <param name="timeout">true if the <see cref="doubt"/> expects timeout, false otherwise.</param>
    /// <param name="crash">true if the <see cref="doubt"/> expects a crash, false otherwise.</param>
    /// <returns>The correct <see cref="DOUBTTYPE"/> depending on the input booleans.</returns>
    private DOUBTTYPE DecideDoubtType(bool noCompilation, bool timeout, bool crash) {
        int val = (noCompilation ? 1 : 0) + (timeout ? 1 : 0) + (crash ? 1 : 0);
        if (val > 1) {
            Debug.LogError("Error, trying to create a doubt that is expecting a combination of: no compilation, timeout and crash, and this is not allowed.");
            return DOUBTTYPE.Regular;
        }

        if (noCompilation) { return DOUBTTYPE.NoCompilation; }
        if (timeout) { return DOUBTTYPE.Timeout; }
        if (crash) { return DOUBTTYPE.Crash; }

        return DOUBTTYPE.Regular;
    }

    /// <summary>
    /// Utility function to decide how many points to assign depending on the doubt result.
    /// </summary>
    /// <param name="typeOfResult">The string representing a doubt result to decide for.</param>
    /// <returns>The amount of points that the user should get.</returns>
    private int AssignPoints(string typeOfResult) {
        switch (typeOfResult) {
            case "noCompilation": return -200;
            case "yesCrash": return -200;

            case "wrongDoubt": return -100;
            case "wrongSolution": return -50;

            case "correctDoubt": return 100;
            case "correctSolution": return 50;
            default: return 0;
        }
    }

    /// <summary>
    /// Utility function to identify if the given <see cref="STATUS"/> is in an appropriate state.
    /// </summary>
    /// <param name="currentStatus"><see cref="STATUS"/> to check.</param>
    /// <param name="statusLevel">Level requirement, 0 for None, 1 for Server 2 for user (can be extended to 3 for total info, if necessary).</param>
    /// <returns>true if <paramref name="currentStatus"/> is at the appropriate level, false otherwise.</returns>
    private bool CheckStatusLevel(STATUS currentStatus, int statusLevel) {
        switch (currentStatus) {
            case STATUS.None: return (statusLevel == 0);

            case STATUS.ServerOk:
            case STATUS.ServerNo: return (statusLevel == 1);

            case STATUS.BothOk:
            case STATUS.UserOk:
            case STATUS.UserNo:
            case STATUS.BothNo: return (statusLevel == 2);

            default: return false;
        }
    }

    /// <summary>
    /// Utility function to store the relevant informations of a test result in a <see cref="testLineContents"/> struct.
    /// </summary>
    /// <param name="testLine">String containing one test line.</param>
    /// <returns>The <see cref="testLineContents"/> with the relevant information.</returns>
    private testLineContents TestLineParse(string testLine) {

        testLineContents result = new testLineContents();

        bool start = false;
        for (int j = 0; j < testLine.Length; j++) {
            if (testLine[j] == ')') {
                result.endOfInputIdx = j;
                break;
            }
            if (start) { result.input += testLine[j]; }

            if (testLine[j] == ' ') { start = true; }

        }
        result.followingChar = testLine[result.endOfInputIdx + 1];

        return result;
    }

    /// <summary>
    /// Utility function to retrieve the value returned by an execution for <see cref="doubts"/> expecting a wrong value.
    /// </summary>
    /// <param name="testTail">The very final part of a test line.</param>
    /// <param name="currentLineParse"><see cref="testLineContents"/> returned by <see cref="TestLineParse(string)"/>.</param>
    /// <returns>Tuple containing the updated <paramref name="currentLineParse"/> and the string of the returned execution.</returns>
    private (testLineContents, string) RetrieveSolutionResult(string testTail, testLineContents currentLineParse) {
        string returned = "";

        string[] smallerParts;

        if (testTail.IndexOf(" != ") == -1) {
            smallerParts = testTail.Split(new[] { " == " }, StringSplitOptions.RemoveEmptyEntries);

            //the -1 from the length is there to remove a ghost char at the end of the line
            for (int j = 0; j < smallerParts[2].Length - 1; j++) {
                currentLineParse.expected += smallerParts[2][j];
            }

        } else {
            smallerParts = testTail.Split(new[] { " != " }, StringSplitOptions.RemoveEmptyEntries);

            //the -1 from the length is there to remove a ghost char at the end of the line
            for (int j = 0; j < smallerParts[2].Length - 1; j++) {
                currentLineParse.correct += smallerParts[2][j];
            }
        }

        for (int j = smallerParts[1].IndexOf(":") + 2; j < smallerParts[1].Length; j++) {
            returned += smallerParts[1][j];
        }

        return (currentLineParse, returned);
    }

    /// <summary>
    /// Function to correctly assign points to solutions and <see cref="doubt"/>s when something went wrong.
    /// Either the solution could not be compiled or a fatal error occurred.
    /// </summary>
    /// <param name="lobbyIdx">Index of the lobby of the client.</param>
    /// <param name="idx">Index of the target solution.</param>
    /// <param name="fatalType"><see cref="DOUBTTYPE"/> regarding which doubts should be considered as correct, only accepts 
    /// <see cref="DOUBTTYPE.NoCompilation"/> and <see cref="DOUBTTYPE.Crash"/>.</param>
    private void FatalExecution(int lobbyIdx, int idx, DOUBTTYPE fatalType) {
        //FatalExecution should only be called for no compilations or fatal crashes
        switch (fatalType) {
            case DOUBTTYPE.NoCompilation: {
                pointDeltas[lobbyIdx][idx] += AssignPoints("noCompilation");
                break;
            }
            case DOUBTTYPE.Crash: {
                pointDeltas[lobbyIdx][idx] += AssignPoints("yesCrash");
                break;
            }
            default: return;
        }

        doubt[] currentDoubtList = doubtList[lobbyIdx];

        for (int i = 0; i < currentDoubtList.Length; i++) {
            bool correctTarget = currentDoubtList[i].targetId == DataManager.leaderboard[lobbyIdx][idx].owner;

            if (correctTarget) {
                //Save that the user solution did not compile
                if (string.IsNullOrEmpty(userResults[lobbyIdx][i])) {
                    switch (fatalType) {
                        case DOUBTTYPE.NoCompilation: {
                            userResults[lobbyIdx][i] = "NoCompilation";
                            break;
                        }
                        case DOUBTTYPE.Crash: {
                            userResults[lobbyIdx][i] = "Crash";
                            break;
                        }
                        default: return;
                    }
                }

                int doubterIdx = DataManager.GetClientRank(lobbyIdx, currentDoubtList[i].clientId, false);

                if (CheckStatusLevel(currentDoubtList[i].currentStatus, 1)) {

                    if (currentDoubtList[i].doubtType == fatalType) {
                        pointDeltas[lobbyIdx][doubterIdx] += AssignPoints("correctDoubt");
                        currentDoubtList[i].ProgressStatus(true);
                        currentDoubtList[i].ProgressStatus(false);
                    } else {
                        pointDeltas[lobbyIdx][doubterIdx] += AssignPoints("wrongDoubt");
                        currentDoubtList[i].ProgressStatus(false);
                        currentDoubtList[i].ProgressStatus(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Function to parse completely a test result fn the server.
    /// </summary>
    /// <param name="lobbyIdx">Index of the lobby currently examining.</param>
    /// <param name="testResults">String containing all test results of a server execution.</param>
    public void ParseServerDoubt(int lobbyIdx, string testResults) {
        string[] arr = testResults.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        //The -2 on the length removes an empty line and summary line from the returned string
        for (int i = 0; i < arr.Length - 2; i++) {
         
            string[] parts = arr[i].Split(new[] { EM.functionName + "(" }, StringSplitOptions.RemoveEmptyEntries);
            bool passed = parts[0].IndexOf(" passed: ") >= 0;

            testLineContents currentLineParse = TestLineParse(parts[1]);

            //Received a value when betting on a wrong value
            if (currentLineParse.followingChar == ' ') {
                string[] smallerParts = parts[1].Split(new[] { " == " }, StringSplitOptions.RemoveEmptyEntries);
                string returned = "";

                //What the server returned
                for (int j = smallerParts[1].IndexOf(":") + 2; j < smallerParts[1].Length; j++) {
                    returned += smallerParts[1][j];
                }

                //the -1 from the length is there to remove a ghost char at the end of the line
                for (int j = 0; j < smallerParts[2].Length - 1; j++) {
                    //expected += smallerParts[2][j];
                    currentLineParse.correct += smallerParts[2][j];
                }

                //What needs to be found now is a doubt, containing the same input and expected output
                (int doubtIdx, int clientIdx) = FindRelevantDoubt(lobbyIdx, 0, 0, currentLineParse);

                serverResults[lobbyIdx][doubtIdx] = returned;

                if (CheckStatusLevel(doubtList[lobbyIdx][doubtIdx].currentStatus, 0)) {
                    if (passed) {
                        pointDeltas[lobbyIdx][clientIdx] += AssignPoints("correctDoubt");
                        doubtList[lobbyIdx][doubtIdx].ProgressStatus(true);
                    } else {
                        pointDeltas[lobbyIdx][clientIdx] += AssignPoints("wrongDoubt");
                        doubtList[lobbyIdx][doubtIdx].ProgressStatus(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Function to parse completely a test result of a client.
    /// </summary>
    /// <param name="lobbyIdx">Index of the lobby of the client.</param>
    /// <param name="solutionIdx">Index of the client solution that was executed.</param>
    /// <param name="testResults">String containing all test results of a client execution.</param>
    public void ParseClientDoubt(int lobbyIdx, int solutionIdx, string testResults) {
        int[]    lobbyPointDeltas   = pointDeltas[lobbyIdx];
        string[] lobbyUserResults   = userResults[lobbyIdx];
        string[] lobbyServerResults = serverResults[lobbyIdx];
        doubt[]  lobbyDoubtList     = doubtList[lobbyIdx];


        string[] arr = testResults.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < arr.Length - 2; i++) {
            if (arr[i].IndexOf("fatal error condition") >= 0) {
                //The doubt doesn't matter, the program crashed
                FatalExecution(lobbyIdx, solutionIdx, DOUBTTYPE.Crash);
                continue;
            }

            string[] parts = arr[i].Split(new[] { EM.functionName + "(" }, StringSplitOptions.RemoveEmptyEntries);
            bool passed = (parts[0].IndexOf(" passed: ") >= 0);

            testLineContents currentLineParse = TestLineParse(parts[1]);

            if (arr[i].IndexOf("expected exception, got none;") >= 0) {
                //What needs to be found now is a doubt towards this target solution, containing the same input and having expectedTimeout = true or expectedCrash = true
                (int doubtIdx, int clientIdx) = FindRelevantDoubt(lobbyIdx, 1, DataManager.leaderboard[lobbyIdx][solutionIdx].owner, currentLineParse.input, true, true);
                //Don't save any user solution because it is unknown (so the string stays null)
                //Because it is impossible to reliably capture the result of a test when exceptions are involved
                //We do not give any points to the solution, but change the status as if the solution was correct
                lobbyPointDeltas[clientIdx] += AssignPoints("wrongDoubt");
                lobbyDoubtList[doubtIdx].ProgressStatus(false);
                lobbyDoubtList[doubtIdx].ProgressStatus(true);
                continue;

            }

            if (arr[i].IndexOf("unexpected exception") >= 0) {

                for (int j = currentLineParse.endOfInputIdx + 5; j < parts[1].Length - 1; j++) {
                    currentLineParse.expected += parts[1][j];
                }

                (int doubtIdx, int clientIdx) = FindRelevantDoubt(lobbyIdx, 1, DataManager.leaderboard[lobbyIdx][solutionIdx].owner, currentLineParse);

                //The exception received is NOT Timeout
                if (arr[i].IndexOf("\'Timeout\'") == -1) {
                    //Save that the user solution crashed
                    lobbyUserResults[doubtIdx].AssignIfEmpty("Crash");
                } else {
                    //Save that the user solution timed out
                    lobbyUserResults[doubtIdx].AssignIfEmpty("Timeout");
                }

                //Tecnically an unexpected exception can be considered under the "crash" doubts, otherwise there would be no way to spot it
                if (lobbyDoubtList[doubtIdx].doubtType == DOUBTTYPE.Crash || lobbyDoubtList[doubtIdx].doubtType == DOUBTTYPE.Timeout) {
                    lobbyPointDeltas[clientIdx] += AssignPoints("correctDoubt");
                    lobbyDoubtList[doubtIdx].ProgressStatus(true);
                    lobbyDoubtList[doubtIdx].ProgressStatus(false);
                } else {
                    lobbyPointDeltas[clientIdx] += AssignPoints("wrongDoubt");
                    lobbyDoubtList[doubtIdx].ProgressStatus(false);
                    lobbyDoubtList[doubtIdx].ProgressStatus(false);
                }
                continue;

            }

            if (arr[i].IndexOf("!Contains(\"Timeout\") for: ") >= 0) {

                //What needs to be found now is a doubt towards this target solution, containing the same input and having expectedCrash = true
                (int doubtIdx, int clientIdx) = FindRelevantDoubt(lobbyIdx, 1, DataManager.leaderboard[lobbyIdx][solutionIdx].owner, currentLineParse.input, false, true);

                if (passed) {
                    //Save that the user solution crashed
                    lobbyUserResults[doubtIdx].AssignIfEmpty("Crash");
                    lobbyPointDeltas[clientIdx] += AssignPoints("correctDoubt");
                    lobbyDoubtList[doubtIdx].ProgressStatus(true);
                    lobbyDoubtList[doubtIdx].ProgressStatus(false);
                } else {
                    //Save that the user solution timed out
                    lobbyUserResults[doubtIdx].AssignIfEmpty("Timeout");
                    lobbyPointDeltas[clientIdx] += AssignPoints("wrongDoubt");
                    //Because it is impossible to reliably capture the result of a test when exceptions are involved
                    //We do not give any points to the solution, but change the status as if the solution was correct
                    lobbyDoubtList[doubtIdx].ProgressStatus(false);
                    lobbyDoubtList[doubtIdx].ProgressStatus(true);
                }
                continue;

            }

            //Received a wrong exception when betting on timeout
            if (currentLineParse.followingChar == ',') {
                //What needs to be found now is a doubt towards this target soltution, containing the same input and having expectedTimeout = true
                (int doubtIdx, int clientIdx) = FindRelevantDoubt(lobbyIdx, 1, DataManager.leaderboard[lobbyIdx][solutionIdx].owner, currentLineParse.input, true, false);

                if (passed) {
                    //Save that the user solution timed out
                    lobbyUserResults[doubtIdx].AssignIfEmpty("Timeout");
                    lobbyPointDeltas[clientIdx] += AssignPoints("correctDoubt");
                    lobbyDoubtList[doubtIdx].ProgressStatus(true);
                    lobbyDoubtList[doubtIdx].ProgressStatus(false);
                } else {
                    //Save that the user solution crashed
                    lobbyUserResults[doubtIdx].AssignIfEmpty("Crash");
                    lobbyPointDeltas[clientIdx] += AssignPoints("wrongDoubt");
                    lobbyDoubtList[doubtIdx].ProgressStatus(false);
                    lobbyDoubtList[doubtIdx].ProgressStatus(false);
                }
                continue;
            }

            //Received the wrong value when betting on another wrong value
            if (currentLineParse.followingChar == ' ') {
                string returned;
                (currentLineParse, returned) = RetrieveSolutionResult(parts[1], currentLineParse);

                //What needs to be found now is a doubt towards this target solution, containing the same input and expected output
                (int doubtIdx, int clientIdx) = FindRelevantDoubt(lobbyIdx, 1, DataManager.leaderboard[lobbyIdx][solutionIdx].owner, currentLineParse);
                //Save the user result
                lobbyUserResults[doubtIdx].AssignIfEmpty(returned);

                if (parts[1].IndexOf(" != ") == -1 && passed) {
                    lobbyPointDeltas[clientIdx] += AssignPoints("correctDoubt");
                    lobbyDoubtList[doubtIdx].ProgressStatus(true);
                } else {
                    //It is always wrong to bet on a compilation and obtain a result
                    lobbyPointDeltas[clientIdx] += AssignPoints("wrongDoubt");
                    lobbyDoubtList[doubtIdx].ProgressStatus(false);
                }

                if (CheckStatusLevel(lobbyDoubtList[doubtIdx].currentStatus, 2)) {
                    //Rewarding when the client solution is equals to the server solution
                    if (lobbyServerResults[doubtIdx] == returned) {
                        lobbyPointDeltas[solutionIdx] += AssignPoints("correctSolution");
                        lobbyDoubtList[doubtIdx].ProgressStatus(true);
                    } else {
                        lobbyPointDeltas[solutionIdx] += AssignPoints("wrongSolution");
                        lobbyDoubtList[doubtIdx].ProgressStatus(false);
                    }
                }
            }
        }
    }

    #endregion

    #region DoubtSearching

    /// <summary>
    /// Utility function to find the first spot in the <see cref="doubtList"/> that is consirdered "free".
    /// A "free" spot is currently represented by havin the clientId and targetId be the same. 
    /// </summary>
    /// <param name="lobbyIdx">The index of the target lobby.</param>
    /// <returns>The index of the first empty spot in the <see cref="doubtList"/>, -1 is none were found.</returns>
    private int FindEmptyDoubtSpot(int lobbyIdx) {
        for (int i = 0; i < doubtList[lobbyIdx].Length; i++) {
            if (doubtList[lobbyIdx][i].clientId == doubtList[lobbyIdx][i].targetId) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Utility function to return the index of the <see cref="doubt"/> against client with id equal to <paramref name="targetId"/>.
    /// Only ever called by the clients, so we can safely search only in the first <see cref="doubtList"/>.
    /// </summary>
    /// <param name="targetId">The Id of the client that we are searching for a doubt against.</param>
    /// <returns>The index of the <see cref="doubt"/> against <paramref name="targetId"/> or -1 if no <see cref="doubt"/> was found.</returns>
    private int FindDoubtTargeting(ulong targetId) {
        for (int i = 0; i < doubtList[0].Length; i++) {
            if (doubtList[0][i].targetId == targetId) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Utility function to retrieve which client created a <see cref="doubt"/> just from the strings returned to the terminal.
    /// This Overload checks for the contents of <paramref name="contents"/> to retrieve the doubter.
    /// </summary>
    /// <param name="lobbyIdx">Index of the lobby of the client.</param>
    /// <param name="statusLevel">The <see cref="STATUS"/> level of the expected <see cref="doubt"/>.</param>
    /// <param name="targetId">Id of the client targeted by the expected <see cref="doubt"/>.</param>
    /// <param name="contents">An initially parsed test result line.</param>
    /// <returns>Tuple containing the index of the found <see cref="doubt"/> 
    /// and the position of the doubter on the leaderboard, (-1,-1) if it is not found.</returns>
    //public (int, int) FindRelevantDoubt(int lobbyIdx, int statusLevel, ulong targetId, string input, string expected, string correct) {
    public (int, int) FindRelevantDoubt(int lobbyIdx, int statusLevel, ulong targetId, testLineContents contents) {
        if (string.IsNullOrEmpty(contents.input)) { return (-1, -1); }
        if (string.IsNullOrEmpty(contents.expected) && string.IsNullOrEmpty(contents.correct)) {Debug.LogError("Error, FindRelevantDoubt is being used incorrectly, use the overload with booleans instead.");}

        for (int i = 0; i < doubtList[lobbyIdx].Length; i++) {
            if (!CheckStatusLevel(doubtList[lobbyIdx][i].currentStatus, statusLevel)) { continue; }

            bool correctInput =  (doubtList[lobbyIdx][i].input.ToString() == contents.input);
            bool correctTarget = (doubtList[lobbyIdx][i].targetId == targetId || targetId == 0);

            //!!!
            if (correctInput && correctTarget) {

                //There is no correct part of the doubt, it means that we are checking for a wrong doubt
                if (string.IsNullOrEmpty(contents.correct) && doubtList[lobbyIdx][i].expected.ToString() == contents.expected) {
                    return (i, DataManager.GetClientRank(lobbyIdx, doubtList[lobbyIdx][i].clientId, false));
                }

                //There is no expected part of the doubt, it means that we are checking for a wrong prediction of the solution
                if (string.IsNullOrEmpty(contents.expected) && doubtList[lobbyIdx][i].output.ToString() == contents.correct) {
                     return (i, DataManager.GetClientRank(lobbyIdx, doubtList[lobbyIdx][i].clientId, false));
                }
            }

        }
        //Failsafe
        Debug.LogError("Error, Could not find a doubt tageting: "+ targetId + "  with input: " + contents.input + "\nexpecting a WRONG output: " + contents.expected + "\nand expecting the correct server input to be: " + contents.correct);
        return (-1, -1);
    }

    /// <summary>
    /// Utility function to retrieve which client created a <see cref="doubt"/> just from the strings returned to the terminal.
    /// This Overload checks for <paramref name="includeTimeout"/> and <paramref name="includeCrash"/> strings to retieve the doubter.
    /// </summary>
    /// <param name="lobbyIdx">Index of the lobby of the client.</param>
    /// <param name="statusLevel">The <see cref="STATUS"/> level of the expected <see cref="doubt"/>.</param>
    /// <param name="targetId">Id of the client targeted by the expected <see cref="doubt"/>.</param>
    /// <param name="input">String containing the input given to the expected <see cref="doubt"/>.</param>
    /// <param name="includeTimeout">Bool representing a timeout from the expected <see cref="doubt"/>.</param>
    /// <param name="includeCrash">Bool representing a crash  from the expected <see cref="doubt"/>.</param>
    /// <returns>Tuple containing the index of the found <see cref="doubt"/> 
    /// and the position of the doubter on the leaderboard, (-1,-1) if it is not found.</returns>
    public (int, int) FindRelevantDoubt(int lobbyIdx, int statusLevel, ulong targetId, string input, bool includeTimeout, bool includeCrash) {
        if (string.IsNullOrEmpty(input)) { return (-1, -1); }
        if (!includeTimeout && !includeCrash) { Debug.LogError("Error, FindRelevantDoubt is being used incorrectly, use the overload with strings instead."); }

        for (int i = 0; i < doubtList[lobbyIdx].Length; i++) {
            if (!CheckStatusLevel(doubtList[lobbyIdx][i].currentStatus, statusLevel)) { continue; }

            bool correctInput =  (doubtList[lobbyIdx][i].input.ToString() == input);
            bool correctTarget = (doubtList[lobbyIdx][i].targetId == targetId || targetId == 0);

            if (correctInput && correctTarget) {

                //We are checking for a doubt with expected timeout
                if (includeTimeout && doubtList[lobbyIdx][i].doubtType == DOUBTTYPE.Timeout) {
                       return (i, DataManager.GetClientRank(lobbyIdx, doubtList[lobbyIdx][i].clientId, false));
                }

                //We are checking for a doubt with expected crash
                if (includeCrash && doubtList[lobbyIdx][i].doubtType == DOUBTTYPE.Crash) {
                       return (i, DataManager.GetClientRank(lobbyIdx, doubtList[lobbyIdx][i].clientId, false));
                }
            }

        }
        //Failsafe
        Debug.LogError("Could not find a doubt targeting: " + targetId + " with input: " + input + "\nexpecting a TIMEOUT: " + includeTimeout + "\nor expecting a CRASH: " + includeCrash);
        return (-1, -1);
    }
    #endregion

    #region ServerRpcs
    /// <summary>
    /// Remote Procedure Call, from client to server.
    /// Notifies the server that the client is ready for the next scene, when all the clients are ready, the next scene is loaded.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ReadyForNextSceneServerRpc() {
        doneCounter++;
        doneSlider.UpdateProgressBar(doneCounter, (NetworkManager.Singleton.ConnectedClientsList.Count * 2), 0.75f, 1f);

        //TODO watchout for disconnections
        if (doneCounter == NetworkManager.Singleton.ConnectedClientsList.Count * 2) {
            MSM.SetupAndLoadNextScene();
        }
    }

    /// <summary>
    /// Remote Procedure Call, from client to server.
    /// Sends to the server a client <see cref="doubt"/>, must be called more than once so 
    /// that every client sends all of its local <see cref="doubt"/>s.
    /// </summary>
    /// <param name="lobbyIdx">Index of the lobby of the client.</param>
    /// <param name="singleDoubt"><see cref="doubt"/> to be sent to the server.</param>
    [ServerRpc(RequireOwnership = false)]
    public void SendDoubtsServerRpc(int lobbyIdx, doubt singleDoubt) {
        doneCounter++;

        if (singleDoubt.clientId != singleDoubt.targetId) {
            int idx = FindEmptyDoubtSpot(lobbyIdx);
            if (idx != -1) { doubtList[lobbyIdx][idx] = singleDoubt; }
        }

        
        readyListText[lobbyIdx].SetText(doneCounter + "/" + matrixSize[lobbyIdx]);
        CheckAllAndStartExecution();
    }

    /// <summary>
    /// Remote Procedure Call, from client to server.
    /// Sends to the server the result of the local execution of the client's own solution.
    /// When all solutions have been parsed, the leaderboards are updated and all doubt
    /// are shared for the slideshow in the next scene.
    /// </summary>
    /// <param name="lobbyIdx">Index of the lobby of the client.</param>
    /// <param name="clientRank">Position in the leaderboard of the client.</param>
    /// <param name="userResult">Result of the user solution's execution, null if it did not compile.</param>
    [ServerRpc(RequireOwnership = false)]
    public void SendExecutionResultServerRpc(int lobbyIdx, int clientRank, string userResult) {
        doneCounter++;
        doneSlider.UpdateProgressBar(doneCounter, NetworkManager.Singleton.ConnectedClientsList.Count, 0.5f, 0.75f);

        //In case of no compilation, call the no compilation function
        if (userResult == null) {
            FatalExecution(lobbyIdx, clientRank, DOUBTTYPE.NoCompilation);
        } else {

            //In case some tests were run, call the parsing function
            if (userResult.IndexOf("No tests ran.") == -1) {
                ParseClientDoubt(lobbyIdx, clientRank, userResult);
            }
        }


        if (doneCounter == NetworkManager.Singleton.ConnectedClientsList.Count) {
            doneCounter = 0;

            for (int l = 0; l < DataManager.lobbiesInUse; l++) {
                DataManager.AddUserResults(l, userResults[l]);

                //Last pass over the doubts to make sure that doubts that bet against compilation are punished because the solution actually compiled
                for (int i = 0; i < doubtList[l].Length; i++) {
                    if (doubtList[l][i].currentStatus == STATUS.ServerOk) {
                        doubtList[l][i].currentStatus = STATUS.Angry;
                    } else {
                        if (doubtList[l][i].currentStatus == STATUS.ServerNo) { 
                            doubtList[l][i].currentStatus = STATUS.Regret;
                        } else {
                            continue;
                        }
                    }
                    pointDeltas[l][DataManager.GetClientRank(l, doubtList[l][i].clientId, false)] += AssignPoints("wrongDoubt");
                }



                ClientRpcParams oneLobbyRpcParams = new ClientRpcParams {
                    Send = new ClientRpcSendParams {
                        TargetClientIds = DataManager.allLobbyClients[l]
                    }
                };

                DataManager.UpdateLeaderboard(l, pointDeltas[l]);
                UpdateLeaderboardClientRpc(pointDeltas[l], oneLobbyRpcParams);

                DataManager.AddDoubts(l, doubtList[l]);
                UpdateDoubtsClientRpc(doubtList[l], serverResults[l], userResults[l], oneLobbyRpcParams);

            }
        }

    }
    #endregion

    #region ClientRpcs
    /// <summary>
    /// Remote Procedure Call, from server to client.
    /// Gives to each client the required strings to execute compilation and execution of the user solution locally.
    /// The result is sent back to the server with <see cref="SendExecutionResultServerRpc(int, int, string)"/>.
    /// </summary>
    /// <param name="lobbyIdx">Index of the lobby of the client.</param>
    /// <param name="clientRank">Position of the client in the leaderboard.</param>
    /// <param name="cppContent">Full user solution, with all the tests already attached.</param>
    /// <param name="fileName">Name of the file with which to call the temporary .cpp.</param>
    /// <param name="clientRpcParams">Necessary parameter to edit which clients will receive the Rpc, in this case all clients in the same lobby will receive a different Rpc</param>
    [ClientRpc]
    public async void OffloadExecutionClientRpc(int lobbyIdx, int clientRank, string cppContent, string fileName, ClientRpcParams clientRpcParams = default) {
        EM.Create(cppContent, fileName);
        string userResult = await EM.Compile(fileName);

        if (userResult.Length > 0) {
            userResult = null;
        } else {
            userResult = await EM.Test(fileName, "[user]");
        }

        SendExecutionResultServerRpc(lobbyIdx, clientRank, userResult);
    }

    /// <summary>
    /// Remote Procedure Call, from server to client.
    /// Updates the <see cref="doubt"/>s and string relative to the result of server and user execution of each doubt. 
    /// </summary>
    /// <param name="allDoubts">Array of all <see cref="doubt"/>s of the lobby.</param>
    /// <param name="serverResults">Array of all results of the server executions.</param>
    /// <param name="userResults">Array of all results of the user executions.</param>
    /// <param name="clientRpcParams">Necessary parameter to edit which clients will receive the Rpc, in this case all clients in the same lobby will receive a different Rpc</param>
    [ClientRpc]
    public void UpdateDoubtsClientRpc(doubt[] allDoubts, string[] serverResults, string[] userResults, ClientRpcParams clientRpcParams = default) {
        DataManager.AddDoubts(0, allDoubts);
        DataManager.AddServerResults(0, serverResults);
        DataManager.AddUserResults(0, userResults);
        ReadyForNextSceneServerRpc();
    }

    /// <summary>
    /// Remote Procedure Call, from server to client.
    /// Updates the leaderboard of the lobby.
    /// </summary>
    /// <param name="pointDeltas">New leaderboard order.</param>
    /// <param name="clientRpcParams">Necessary parameter to edit which clients will receive the Rpc, in this case all clients in the same lobby will receive a different Rpc</param>
    [ClientRpc]
    public void UpdateLeaderboardClientRpc(int[] pointDeltas, ClientRpcParams clientRpcParams = default) {
        DataManager.UpdateLeaderboard(0, pointDeltas);
        ReadyForNextSceneServerRpc();
    }
    #endregion


}
