using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//DO NOT REMOVE THIS, IT IS NEEDED!
using static SerializationExtensions;

/// <summary>
/// General manager class for the first "round", the scene with the creation of the user solutions.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class ReadyManager : NetworkBehaviour {

    public int doneCounter = 0;
    public MySceneManager MSM;
    public NotepadManager NM;
    public ExecutionManager EM;

    //The test button can only be pressed after the PreCompilation has finished
    public Button testButton;

    //Server control panel
    public Slider doneSlider;
    public GameObject serverPanel;
    public TextMeshProUGUI[] readyListText;

    private List<List<databaseEntry>> readyList = new List<List<databaseEntry>>();
    private List<string[]> solutionList = new List<string[]>();

    void Start() {
        if (NetworkManager.Singleton.IsServer) { ServerSetup(); }

        CheckAndPrecompile();
    }

    /// <summary>
    /// Utility function to setup the server interface.
    /// It also initializes the variables for this session.
    /// </summary>
    private void ServerSetup() {
        if (serverPanel != null) { serverPanel.SetActive(true); }

        for (int i = 0; i < DataManager.lobbiesInUse; i++) {
            readyListText[i].SetText("0/" + DataManager.allLobbySizes[i]);
            readyListText[i].rectTransform.parent.gameObject.SetActive(true);

            readyList.Add(new List<databaseEntry>());
            solutionList.Add(new string[DataManager.allLobbySizes[i]]);
            DataManager.SessionInit(DataManager.allLobbySizes[i]);
        }
    }


    /// <summary>
    /// Function that checks for the existence of the Catch2 main that is required for compilation.
    /// If it is not found, it will be created and the "Test" button will be activated once the main exists.
    /// The function is 'async' because the compilation takes time (between 10 and 30 seconds usually).
    /// The function is 'async void' because it adheres to the "fire and forget" pattern, its termination is signaled by a side effect (the button becoming active)
    /// </summary>
    private async void CheckAndPrecompile() {
        if (!File.Exists(Application.persistentDataPath + "/catch_main.o")) {
            string compilationResult = await EM.PreCompile();

            if (compilationResult.Trim().Length > 0) {
                //TODO Serious trouble, the user can't play because the catch main could not be created
                Debug.LogError("Fatal Error, the game cannot proceed! (Error in PreCompilation)");
                NetworkLog.LogInfoServer("Fatal Error, the game cannot proceed! (Error in PreCompilation)");
            }

        }
        testButton.interactable = true;
    }
    


    /// <summary>
    /// Function to trigger the ServerRpc to notify of a change in ready status: from not ready to ready and viceversa.
    /// The function is public void and parameterless on purpose so that it can be called by a button OnClick.
    /// </summary>
    public void SendReady() {
        SendReadyServerRpc(DataManager.myLobbyNumber, DataManager.myData);
    }

    /// <summary>
    /// External function called by <see cref="RoundTimer"/>.
    /// It triggers the ServerRpc to force the client to send its solution.
    /// </summary>
    public void SendSolution() {
        if (NetworkManager.Singleton.IsServer) { return; }
        SendSolutionServerRpc(DataManager.myLobbyNumber, DataManager.myData, NM.GetSolution());
    }

    /// <summary>
    /// Function to count all the received solutions, if all clients sent theirs, the server will share all solutions with a Remote Procedure Call.
    /// </summary>
    private void CheckAllAndShareData() {
        int totalReady = 0;
        for(int i = 0; i < readyList.Count; i++) { totalReady += readyList[i].Count; }
        int totalExpectedSolutions = 0;
        for (int i = 0; i < solutionList.Count; i++) { totalExpectedSolutions += solutionList[i].Length; }

        //totalReady and totalExpectedSolutions can be used to gauge where we are regarding the progress towards the next round
        doneSlider.UpdateProgressBar(totalReady, totalExpectedSolutions, 0f, 0.5f);

        //All solutions have been given, so the leaderboard should be updated following the readyList
        if(totalReady == totalExpectedSolutions) {
            for(int l = 0; l < readyList.Count; l++) { 
                for(int i = 0; i < readyList[l].Count; i++) { 
                    DataManager.AddToLeaderboard(l, readyList[l][i], i);
                }
            }
        }

        //If any solution is null, we have not filled all solutions' slots yet
        foreach (string[] lobbySolutions in solutionList) { 
            foreach(string singleSolution in lobbySolutions) { 
                if (singleSolution == null) { return; }
            }
        }

        for (int i = 0; i < solutionList.Count; i++) {
            DataManager.AddSolutions(i, solutionList[i]);

            ClientRpcParams oneLobbyRpcParams = new ClientRpcParams {
                Send = new ClientRpcSendParams {
                    TargetClientIds = DataManager.allLobbyClients[i]
                }
            };

            ShareSolutionsAndLeaderboardClientRpc(DataManager.leaderboard[i], DataManager.solutions[i], oneLobbyRpcParams);

        }
    }

    #region ServerRpcs

    /// <summary>
    /// Remote Procedure Call, from client to server.
    /// Triggers a change in the ready list, either by adding the new client or removing an old one.
    /// </summary>
    /// <param name="lobbyIdx">The lobby of client.</param>
    /// <param name="userData">The general data of the client.</param>
    [ServerRpc(RequireOwnership = false)]
    public void SendReadyServerRpc(int lobbyIdx, databaseEntry userData) {
        if (readyList[lobbyIdx].Contains(userData)) {
            readyList[lobbyIdx].Remove(userData);
        } else {
            readyList[lobbyIdx].Add(userData);
        }

        readyListText[lobbyIdx].SetText(readyList[lobbyIdx].Count + "/" + DataManager.allLobbySizes[lobbyIdx]);
    }

    /// <summary>
    /// Remote Procedure Call, from client to server.
    /// Sends the solution to the server that will store it accordingly.
    /// </summary>
    /// <param name="lobbyIdx">The lobby of the client.</param>
    /// <param name="userData">The general data of the client.</param>
    /// <param name="solution">The solution of the client.</param>
    [ServerRpc(RequireOwnership = false)]
    public void SendSolutionServerRpc(int lobbyIdx, databaseEntry userData, string solution) {
        //Clients that were forced by the timer to send solutions would not appear to be in the readyList
        int idx = readyList[lobbyIdx].IndexOf(userData);
        if (idx == -1) {
            idx = readyList[lobbyIdx].Count;
            readyList[lobbyIdx].Add(userData);
            readyListText[lobbyIdx].SetText(readyList[lobbyIdx].Count + "/" + DataManager.allLobbySizes[lobbyIdx]);
        }
        //Save the client's solution
        solutionList[lobbyIdx][idx] = solution;


        //DataManager.AddToLeaderboard(lobbyIdx, userData, idx);
        CheckAllAndShareData();
    }

    /// <summary>
    /// Remote Procedure Call, from client to server.
    /// Notifies the server that the client is ready for the next scene.
    /// When all clients are ready, the next scene is loaded.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ReadyForNextSceneServerRpc() {
        doneCounter++;

        doneSlider.UpdateProgressBar(doneCounter, NetworkManager.Singleton.ConnectedClientsList.Count, 0.5f, 1f);

        if (doneCounter == NetworkManager.Singleton.ConnectedClientsList.Count) {
            MSM.SetupAndLoadNextScene();
        }
    }

    #endregion

    #region ClientRpcs

    /// <summary>
    /// Remote Procedure Call, from server to client.
    /// Shares all the solutions and leaderboard of their own lobby to each client.
    /// </summary>
    /// <param name="lobbyLeaderboard">The leaderboard of the client's lobby.</param>
    /// <param name="lobbySolutions">The solutions of the client's lobby.</param>
    /// <param name="clientRpcParams">Necessary parameter to edit which clients will receive the Rpc, in this case all clients will receive a different Rpc depending on their lobby</param>
    [ClientRpc]
    public void ShareSolutionsAndLeaderboardClientRpc(databaseEntry[] lobbyLeaderboard, string[] lobbySolutions, ClientRpcParams clientRpcParams = default) {
        if (DataManager.leaderboard.Count == 0) { DataManager.SessionInit(lobbyLeaderboard.Length); }
        DataManager.AddToLeaderboard(0, lobbyLeaderboard);
        DataManager.AddSolutions(0, lobbySolutions);
        ReadyForNextSceneServerRpc();
    }

    #endregion

}

