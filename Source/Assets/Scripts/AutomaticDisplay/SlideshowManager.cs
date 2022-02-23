using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// General manager class for the slideshow scene.
/// The amount of seconds given to the players to check each doubt is stored in constant <see cref="waitTime"/>.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class SlideshowManager : NetworkBehaviour {
    private const float waitTime = 6f;

    private int doneCounter = 0;
    private List<int[]> finalScores = new List<int[]>();
    private readonly string[] possibleLabels = new string[10]{"_return", "_timeout", "_not_compile", "_crash", "_correct_doubt",
                                                "_half_doubt", "_wrong_doubt", "_terrible_doubt", "_no_doubt", "_unknown"};

    public TextMeshProUGUI doubterUsername;
    public LocalizableText expectedOrNoDoubtText;
    public TextMeshProUGUI targetUsername;

    public LocalizableText expectedResult;
    public TextMeshProUGUI expectedResultValue;

    public TextMeshProUGUI predictedCorrectValue;

    public TextMeshProUGUI givenInput;

    public LocalizableText solutionResult;
    public TextMeshProUGUI correctResult;

    public LocalizableText finalEvaluation;

    public List<TextMeshProUGUI> hideableText;

    public PlayerSpawner PS;
    public ExecutionManager EM;
    public MySceneManager MSM;
    public AccountManager AM;

    public ScrollRect doubtersScrollView;
    public ScrollRect targetsScrollView;

    public GameObject leaderboardPanel;
    public RectTransform playerHolder;

    public Slider doneSlider;
    public GameObject serverPanel;

    void Start() {
        if (leaderboardPanel != null) { leaderboardPanel.SetActive(false); }

        if (NetworkManager.Singleton.IsServer) {
            
            serverPanel.SetActive(true);

            for (int i = 0; i < DataManager.lobbiesInUse; i++) {
                finalScores.Add(DataManager.userPoints[i]);
            }
        } else {

            (doubt[] sortedDoubts, string[] sortedServerResults, string[] sortedUserResults) = SortDoubts();
            StartCoroutine(Performance(sortedDoubts, sortedServerResults, sortedUserResults));
        }
    }

    #region Coroutines
    /// <summary>
    /// Coroutine to execute the whole slideshow.
    /// </summary>
    /// <param name="sortedDoubts">The list of doubts to show, already ordered.</param>
    /// <param name="sortedServerResults">The list of server results, already ordered.</param>
    /// <param name="sortedUserResults">The list of user results, already ordered.</param>
    /// <returns>As a coroutine, it just yields the execution.</returns>
    IEnumerator Performance(doubt[] sortedDoubts, string[] sortedServerResults, string[] sortedUserResults) {

        for (int i = 0; i < sortedDoubts.Length; i++) {
            
            //Do not wait in the very first iterations, or the placeholder labels are exposed
            if (i > 0) { yield return new WaitForSeconds(waitTime); }

            //Ordering of the playerBoxes in the scroll views
            (int idx1, int idx2) = CalculateDoubtIndexes(i);
            
            targetsScrollView.verticalNormalizedPosition = (float)idx1 / DataManager.myLobbySize;
            doubtersScrollView.horizontalNormalizedPosition = (float)idx2 / DataManager.myLobbySize;

            PS.HighlightPlayerBoxServerRpc(DataManager.myLobbyNumber, DataManager.oldLeaderboard[0][idx2].owner, DataManager.oldLeaderboard[0][idx1].owner);

            SingleSlideshowStep(sortedDoubts[i], sortedServerResults[i], sortedUserResults[i], idx2, idx1);
            
        }

        yield return new WaitForSeconds(waitTime);
        leaderboardPanel.SetActive(true);
        PerformanceFinishedServerRpc();
        
    }

    /// <summary>
    /// Coroutine used to wait <paramref name="secondsToWait"/> seconds before loading the next scene.
    /// </summary>
    /// <param name="secondsToWait">Total amount of seconds to wait.</param>
    /// <param name="numberOfSplits">Number of splits to use so that the coroutine can yield and update the progress <see cref="Slider"/>.</param>
    /// <returns>As a coroutine, it just yield the execution.</returns>
    IEnumerator WaitAndLoad(float secondsToWait, int numberOfSplits) {
        WaitForSeconds waitTime = new WaitForSeconds(secondsToWait / (float) numberOfSplits);

        for (int i = 0; i < numberOfSplits; i++) {
            yield return waitTime;
            doneSlider.UpdateProgressBar(i, 3, 0.85f, 1f);
        }

        MSM.SetupAndLoadNextScene();
    }

    #endregion

    /// <summary>
    /// Utility function to execute a single slideshow step.
    /// </summary>
    /// <param name="currentDoubt">The doubt of this step.</param>
    /// <param name="currentServerResult">The result of the server execution against the <paramref name="currentDoubt"/>.</param>
    /// <param name="currentUserResult">The result of the target user execution against the <paramref name="currentDoubt"/>.</param>
    /// <param name="doubterRank">The rank of the doubter in the leaderboard.</param>
    /// <param name="targetRank">The rank of the target in the leaderboard.</param>
    private void SingleSlideshowStep(doubt currentDoubt, string currentServerResult, string currentUserResult, int doubterRank, int targetRank) {
        string[] usernamesByRank = DataManager.GetUsernameArray();

        if (currentDoubt.clientId == currentDoubt.targetId) {
            doubterUsername.SetText(usernamesByRank[doubterRank]);
            expectedOrNoDoubtText.ChangeLabel("_not_expected");
            targetUsername.SetText(usernamesByRank[targetRank]);

            ClearBulkText();

        } else {

            doubterUsername.SetText(DataManager.GetClientUsername(currentDoubt.clientId));
            expectedOrNoDoubtText.ChangeLabel("_expected");
            targetUsername.SetText(DataManager.GetClientUsername(currentDoubt.targetId));

            expectedResult.ChangeLabel(DecideLabel(currentDoubt.doubtType));
            expectedResultValue.SetText(currentDoubt.expected.ToString());

            predictedCorrectValue.SetText(currentDoubt.output.ToString());

            givenInput.SetText(currentDoubt.input.ToString());

            solutionResult.ChangeLabel(DecideLabel(currentUserResult));

            correctResult.SetText(currentServerResult);

            RestoreBulkText();
        }

        (string label, Color textColor) = DecideLabel(currentDoubt.currentStatus);
        finalEvaluation.ChangeLabel(label);
        finalEvaluation.ChangeColor(textColor);
    }

    /// <summary>
    /// Utility function to return an almost exact 2 dimensional division of the one dimensional array containing doubts.
    /// The weird division follows the sorting of the <see cref="SortDoubts"/> function.
    /// </summary>
    /// <param name="serialIdx">The target position in the one dimensional doubt array.</param>
    /// <returns>Tuple containing row and column of the given target doubt position.</returns>
    private (int, int) CalculateDoubtIndexes(int serialIdx) {
        int side = DataManager.myLobbySize - 1;

        int idx1 = serialIdx / side;
        int idx2 = serialIdx % side;
        if (idx2 >= idx1) { idx2 += 1; }

        return (idx1, idx2);
    }

    /// <summary>
    /// Utility function to return the correct position for the given doubt.
    /// The weird calculation follows the sorting of the <see cref="SortDoubts"/> function.
    /// </summary>
    /// <param name="currentDoubt">The target doubt to position in the doubt array.</param>
    /// <returns>Index containing the correct position for the given doubt.</returns>
    private int CalculateDoubtIndex(doubt currentDoubt) {
        int side = DataManager.myLobbySize - 1;

        int clientRank = DataManager.GetClientRank(0, currentDoubt.clientId, true);
        int targetRank = DataManager.GetClientRank(0, currentDoubt.targetId, true);
        int offset = 0;
        if (targetRank < clientRank) { offset = -1; }


        return offset + clientRank + (side * targetRank);
    }

    /// <summary>
    /// Utility function to transform an input <see cref="DOUBTTYPE"/> into a corresponding label from the <see cref="possibleLabels"/>.
    /// </summary>
    /// <param name="t">Input <see cref="DOUBTTYPE"/> to decide the label for.</param>
    /// <returns>The correct label from the <see cref="possibleLabels"/> string array.</returns>
    private string DecideLabel(DOUBTTYPE t) {
        switch (t) {
            case DOUBTTYPE.Timeout:         return possibleLabels[1]; 
            case DOUBTTYPE.NoCompilation:   return possibleLabels[2]; 
            case DOUBTTYPE.Crash:           return possibleLabels[3];
        }
        return possibleLabels[0];
    }

    /// <summary>
    /// Utility function to transform an input string into a corresponding label from the <see cref="possibleLabels"/>.
    /// </summary>
    /// <param name="s">Input string to decide the label for.</param>
    /// <returns>The correct label from the <see cref="possibleLabels"/> string array, 
    /// or the original string if it does not correspond to the predetermined ones.</returns>
    private string DecideLabel(string s) {
        switch (s) {
            case "Timeout":         return possibleLabels[1];
            case "NoCompilation":   return possibleLabels[2];
            case "Crash":           return possibleLabels[3];
            case "":                return possibleLabels[9];
        }
        return s;
    }

    /// <summary>
    /// Utility function to transform an input <see cref="STATUS"/> into a corresponding label from the <see cref="possibleLabels"/>
    /// and the correct color for the text displaying the label.
    /// </summary>
    /// <param name="currentStatus">Input <see cref="STATUS"/> to decide the label for.</param>
    /// <returns>The tuple containing the correct label from the <see cref="possibleLabels"/> string array and 
    /// the correct color for the text.</returns>
    private (string, Color) DecideLabel(STATUS currentStatus) {
        switch (currentStatus) {
            case STATUS.None:       return (possibleLabels[8], Color.black);
            case STATUS.Correct:    return (possibleLabels[4], Cosmetics.correctColor);
            case STATUS.Lost:       return (possibleLabels[6], Cosmetics.wrongColor);
            case STATUS.Lucky:      return (possibleLabels[5], Cosmetics.buttonsColor);
            case STATUS.Worst:      return (possibleLabels[7], Color.black);
            case STATUS.Angry:      return (possibleLabels[6], Cosmetics.wrongColor);
            case STATUS.Scared:     return (possibleLabels[5], Cosmetics.buttonsColor);
            case STATUS.Regret:     return (possibleLabels[6], Cosmetics.wrongColor);
        }

        Debug.LogError("Error, STATUS: " + currentStatus + " should never reach this point.");
        return ("Error", Color.black);
    }


    /// <summary>
    /// Function to sort the doubts in the correct sequence.
    /// At the same time the known lists of server and user results are ordered the same way.
    /// The ordering follows these rules:
    /// Each row contains the same target in order of appearence in the leaderboard from 0 to <see cref="DataManager.myLobbySize"/> -1.
    /// Each column contains the same doubter in order of appearence in the leaderboard from 0 to <see cref="DataManager.myLobbySize"/> -1.
    /// With the exception of the indexes where row number is equal to the column number, 
    /// in those cases the order of doubter and target are swapped.
    /// </summary>
    /// <returns>The new order of the <see cref="doubt"/>, serverResults and userResults arrays.</returns>
    private (doubt[], string[], string[]) SortDoubts() {
        doubt[]  unsortedDoubts =       DataManager.confirmedDoubts[0];
        string[] unsortedServerResults= DataManager.serverResults[0];
        string[] unsortedUserResults =  DataManager.userResults[0];

        doubt[]  sortedDoubts =         new  doubt[unsortedDoubts.Length];
        string[] sortedServerResults =  new string[unsortedServerResults.Length];
        string[] sortedUserResults =    new string[unsortedUserResults.Length];

        //        Number of users in lobby
        //     2      3      4      5      6
        // ┌──────┬──────┬──────┬──────┬──────┐
        // │ 1->0 │ 2->0 │ 3->0 │ 4->0 │ 5->0 │
        // │      │      │      │      │      │
        // │ 0->1 │ 2->1 │ 3->1 │ 4->1 │ 5->1 │
        // ├──────┘      │      │      │      │      
        // │ 0->2   1->2 │ 3->2 │ 4->2 │ 5->2 │
        // ├─────────────┘      │      │      │
        // │ 0->3   1->3   2->3 │ 4->3 │ 5->3 │
        // ├────────────────────┘      │      │
        // │ 0->4   1->4   2->4   3->4 │ 5->4 │
        // ├───────────────────────────┘      │
        // │ 0->5   1->5   2->5   3->5   4->5 │
        // └──────────────────────────────────┘

        for (int i = 0; i < unsortedDoubts.Length; i++) {
            if (unsortedDoubts[i].clientId == unsortedDoubts[i].targetId) { continue; }

            int idx = CalculateDoubtIndex(unsortedDoubts[i]);

            sortedDoubts[idx] =         unsortedDoubts[i];
            sortedServerResults[idx] =  unsortedServerResults[i];
            sortedUserResults[idx] =    unsortedUserResults[i];
        }

        return (sortedDoubts, sortedServerResults, sortedUserResults);
    }

    /// <summary>
    /// Function that executes the last test battery on the user solution and then notifies the server of the result.
    /// The function is 'async' because the test takes time (between 1 and 10 seconds usually).
    /// The function is 'async void' because it adheres to the "fire and forget" pattern, its termination is signaled by a side effect (the call to <see cref="SendFinalResultServerRpc(int, int, string)"/>)
    /// </summary>
    public async void FinalExecution() {
        string finalResult = null;
        if (File.Exists(Application.persistentDataPath + "/Client" + DataManager.myData.owner + ".exe")) {
            finalResult = await EM.Test("Client" + DataManager.myData.owner, "[final]");
        }

        SendFinalResultServerRpc(DataManager.myLobbyNumber, DataManager.GetClientRank(0, DataManager.myData.owner, false), finalResult);
    }

    /// <summary>
    /// Function to parse the Catch2 test execution.
    /// </summary>
    /// <param name="finalResult">Complete string from the test execution.</param>
    /// <returns>The number of points given to this execution.</returns>
    public int ParseFinalResult(string finalResult) {
        int finalPoints = 0;
        string[] arr = finalResult.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        //The last 2 strings are ignored because they are not tests
        for (int i = 0; i < arr.Length - 2; i++) {
            //A fatal error is heavily penalized
            if (arr[i].IndexOf("fatal error condition") >= 0) { finalPoints -= 200; }

            //An exception is penalized just a bit less
            if (arr[i].IndexOf("unexpected exception") >= 0) { finalPoints -= 150; }

            string[] parts = arr[i].Split(new[] { EM.functionName + "(" }, StringSplitOptions.RemoveEmptyEntries);
            bool passed = (parts[0].IndexOf(" passed: ") >= 0);
            int helpingIdx = -1;

            for (int j = 0; j < parts[1].Length; j++) {
                if (parts[1][j] == ')') {
                    helpingIdx = j;
                    break;
                }
            }

            //Each passed test is a net positive and every failed one a net negative
            if (parts[1][helpingIdx + 1] == ' ') {
                if (passed) {
                    finalPoints += 50;
                } else {
                    finalPoints -= 50;
                }
            }
        }
        return finalPoints;
    }

    /// <summary>
    /// Utility function to clear the slideshow interface.
    /// </summary>
    private void ClearBulkText() {
        expectedResult.ChangeLabel("");
        expectedResultValue.SetText("");
        predictedCorrectValue.SetText("");
        givenInput.SetText("");
        solutionResult.ChangeLabel("");
        correctResult.SetText("");

        foreach(TextMeshProUGUI t in hideableText) {
            t.enabled = false;
        }
    }

    /// <summary>
    /// Utility function to restore the slideshow interface.
    /// </summary>
    private void RestoreBulkText() {
        foreach (TextMeshProUGUI t in hideableText) {
            t.enabled = true;
        }
    }


    #region ServerRpcs

    /// <summary>
    /// Remote Procedure Call, from client to server.
    /// Sends the results of the users' final execution to the server, after which the server calculates the final scores and 
    /// updates the clients leaderboard.
    /// </summary>
    /// <param name="lobbyIdx">The lobby of the client.</param>
    /// <param name="clientRank">The position of the client in the current leaderboard.</param>
    /// <param name="userResult">The result of the final execution of the user solution.</param>
    [ServerRpc(RequireOwnership = false)]
    public void SendFinalResultServerRpc(int lobbyIdx, int clientRank, string userResult) {
        doneCounter++;
        doneSlider.UpdateProgressBar(doneCounter, NetworkManager.Singleton.ConnectedClientsList.Count, 0.5f, 0.65f);

        if (userResult == null) {
            //Was not compiled
            finalScores[lobbyIdx][clientRank] -= 300;
        } else {
            finalScores[lobbyIdx][clientRank] += ParseFinalResult(userResult);
        }

        if (doneCounter == NetworkManager.Singleton.ConnectedClientsList.Count) {
            doneCounter = 0;

            for (int i = 0; i < DataManager.lobbiesInUse; i++) {
                DataManager.UpdateLeaderboard(i, finalScores[i]);

                ClientRpcParams oneLobbyRpcParams = new ClientRpcParams {
                    Send = new ClientRpcSendParams {
                        TargetClientIds = DataManager.allLobbyClients[i]
                    }
                };

                UpdateLeaderboardClientRpc(finalScores[i], oneLobbyRpcParams);
            }
        }

    }

    /// <summary>
    /// Remote Procedure Call, from client to server.
    /// Keeps track of all players that have finished watching the slideshow and are ready to start the final execution.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void PerformanceFinishedServerRpc() {
        doneCounter++;
        doneSlider.UpdateProgressBar(doneCounter, NetworkManager.Singleton.ConnectedClientsList.Count, 0f, 0.5f);

        if (doneCounter == NetworkManager.Singleton.ConnectedClientsList.Count) {
            doneCounter = 0;

            PS.SpawnAllLobbyLeaderboards(new RectTransform[] { playerHolder }, true, false, false);
            FinalExecutionClientRpc();
        }
    }

    /// <summary>
    /// Remote Procedure Call, from client to server.
    /// Keeps track of all players that have finished the final execution and are ready to load the next scene.
    /// </summary>
    /// <param name="userData">The user data of the current client.</param>
    [ServerRpc(RequireOwnership = false)]
    public void ReadyForNextSceneServerRpc(databaseEntry userData) {
        doneCounter++;
        doneSlider.UpdateProgressBar(doneCounter, NetworkManager.Singleton.ConnectedClientsList.Count, 0.65f, 0.8f);

        //Save the points earned by the users in the database
        AM.StorePoints(userData.username.ToString(), userData.progress);

        //TODO watchout for disconnections
        if (doneCounter == NetworkManager.Singleton.ConnectedClientsList.Count) {
            StartCoroutine(WaitAndLoad(2 * waitTime, 4));
        }
    }

    #endregion

    #region ClientRpcs
    /// <summary>
    /// Remote Procedure Call, from server to client.
    /// Starts the <see cref="FinalExecution"/> to execute the last final battery on the client's own solution.
    /// </summary>
    [ClientRpc]
    public void FinalExecutionClientRpc() {
        FinalExecution();
    }

    /// <summary>
    /// Remote Procedure Call, from server to client.
    /// Updates the clients leaderboard.
    /// </summary>
    /// <param name="pointDeltas">Points for all the clients in the lobby of this round.</param>
    /// <param name="clientRpcParams">Necessary parameter to edit which clients will receive the Rpc, in this case all clients in the same lobby will receive a different Rpc</param>
    [ClientRpc]
    public void UpdateLeaderboardClientRpc(int[] pointDeltas, ClientRpcParams clientRpcParams = default) {
        DataManager.UpdateLeaderboard(0, pointDeltas);
        DataManager.UpdateClientProgress();
        ReadyForNextSceneServerRpc(DataManager.myData);
    }

    #endregion
}
