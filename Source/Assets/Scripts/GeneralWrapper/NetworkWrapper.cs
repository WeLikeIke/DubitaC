using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

//DO NOT REMOVE THIS, IT IS NEEDED!
using static SerializationExtensions;

/// <summary>
/// General manager class for the main menu.
/// The minimum number of clients required before being able to start a session is saved in constant <see cref="minNumberOfPlayers"/>.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkWrapper : NetworkBehaviour {
    private const int minNumberOfPlayers = 2;

    public bool isServerBuild;
    public Button continueButton;
    public LocalizeStringEvent continueButtonText;

    public MySceneManager MSM;

    public GameObject serverPanel;
    public LocalizeStringEvent numberOfPlayers;
    public LocalizeStringEvent codeQuestionSolved;

    //Only relevant for the clients
    public GameObject cosmeticPanel;
    public GameObject mainMenuPanel;

    //Only relevant for the server
    public Button startButton;
    public LobbyManager LM;
    private Dictionary<ulong, databaseEntry> acceptedUsers = new Dictionary<ulong, databaseEntry>();
    private int doneCounter = 0;

    void Awake() { SetupBuildDifference(isServerBuild); }

    void Start() {

        // Only possible in the last scene
        if (NetworkManager.Singleton.IsServer){ ServerFinalSetup(); }

        //Both server and clients need to load the Avatars
        Cosmetics.Init();

        //In case the scene was reloaded by a bad connection attempt, do not bother the user with the main menu again, go straight to the credentials interface
        if (DataManager.wasRejected) { 
            mainMenuPanel.SetActive(false);
            DataManager.wasRejected = false;
        }
    }

    /// <summary>
    /// Utility function to aid the developers in the creation of a different build between client and server
    /// </summary>
    /// <param name="isServerBuild">true if the build should be for the server, false if the build should be for the client.</param>
    private void SetupBuildDifference(bool isServerBuild) {
        //Do not try to setup the continue button in the last scene
        if(continueButton == null) { return; }

        string currentBuild = "";

        if (isServerBuild) {
            //Change the label that the continue button will have
            currentBuild = "_server";

            //Change the behaviour that the continue button will have
            continueButton.onClick.AddListener(delegate {
                serverPanel.SetActive(true);
                Server();
            });

        } else {
            //Change the label that the continue button will have
            currentBuild = "_player";

            //Change the behaviour that the continue button will have
            continueButton.onClick.AddListener(delegate {
                mainMenuPanel.SetActive(false);
            });

        }

        //Required so that a localization local variable is set at runtime
        ((LocalizedString)continueButtonText.StringReference["currentBuild"]).TableEntryReference = currentBuild;
    }

    /// <summary>
    /// Utility function to setup the server interface in the final scene.
    /// </summary>
    private void ServerFinalSetup() {
        serverPanel.SetActive(true);
        int totalClientsNumber = 0;
        foreach (int i in DataManager.allLobbySizes) {
            totalClientsNumber += i;
        }

        ((IntVariable)numberOfPlayers.StringReference["numberOfClients"]).Value = totalClientsNumber;
        numberOfPlayers.RefreshString();

        ((StringVariable)codeQuestionSolved.StringReference["solvedQuestion"]).Value = DataManager.currentCodeQuestion.name;
        codeQuestionSolved.RefreshString();
    }

    /// <summary>
    /// External function to start the Server and subscribe to the relevant callbacks.
    /// The function is public void and parameterless on purpose so that it could be called from a button OnClick
    /// </summary>
    public void Server() {
        NetworkManager.Singleton.StartServer();

        NetworkManager.Singleton.OnClientConnectedCallback += RequirementsCheck;

        NetworkManager.Singleton.OnClientDisconnectCallback += CleanLobby;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientsDisconnectionCheck;
    }

    /// <summary>
    /// Function that responds to a client connecting to the server or the codeQuestion being chosen.
    /// Makes sure that the codeQuestion had been chosen and the minimum amount of players is connected.
    /// </summary>
    /// <param name="clientId">The id of the client that just connected, not needed.</param>
    public void RequirementsCheck(ulong clientId) {
        if (startButton == null) { return; }
        startButton.interactable = (DataManager.currentCodeQuestion.name != null && NetworkManager.Singleton.ConnectedClientsList.Count >= minNumberOfPlayers);
    }

    /// <summary>
    /// Function that responds to a client disconnecting from the server.
    /// Makes sure that the codeQuestion had been chosen and the minimum amount of players is still connected.
    /// </summary>
    /// <param name="clientId">The id of the client that just disconnected, not needed.</param>
    private void OnClientsDisconnectionCheck(ulong clientId) {
        if (startButton == null) { return; }
        startButton.interactable = (DataManager.currentCodeQuestion.name != null && NetworkManager.Singleton.ConnectedClientsList.Count > minNumberOfPlayers);
    }

    /// <summary>
    /// Function that responds to a client disconnecting from the server.
    /// Removes the client from the dictionary of confirmed clients and from its lobby.
    /// </summary>
    /// <param name="clientId">The id of the client that just disconnected.</param>
    private void CleanLobby(ulong clientId) {
        //Remove the user from the dictionary
        if (acceptedUsers.ContainsKey(clientId)) {
            acceptedUsers.Remove(clientId);
        }

        //Remove the user from the lobbies
        LM.DeassignLobby(clientId);
    }

    /// <summary>
    /// External function to execute the operations after a connection has been confirmed as valid.
    /// In case that the maximum amount of players has been reached, the client is disconnected.
    /// Otherwise it is assigned a spot in the lobbies and the cosmetic panel is opened.
    /// </summary>
    /// <param name="clientId">The id of the client that completed the login.</param>
    /// <param name="clientRpcParams">Necessary parameter to edit which clients will receive the Rpc, in this case all clients will receive a different Rpc</param>
    public void ValidLogin(ulong clientId, ClientRpcParams clientRpcParams = default) {

        //Get the RectTransform from the corresponding playerObject taken from the NetworkManager
        RectTransform playerTransform = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<RectTransform>();

        if (LM.AssignLobby(playerTransform, clientId)) {
            //If the client was assigned to a lobby, save its data and open the cosmetic panel
            playerTransform.GetComponent<PlayerController>().myData.Value = acceptedUsers[clientId];
            OpenCosmeticsClientRpc(clientRpcParams);
        } else {
            //If the client could not be assigned to a lobby, disconnect the client
            NetworkManager.Singleton.DisconnectClient(clientId);
        }
    }

    /// <summary>
    /// Function to add the user into the dictionary if the credentials are not already in the session.
    /// If they are, the connection is refused.
    /// </summary>
    /// <param name="clientId">The id of the client that just connected.</param>
    /// <param name="userData">The data (user creadentials) of the client that just connected.</param>
    /// <returns></returns>
    public bool NewConfirmedClient(ulong clientId, databaseEntry userData) {
        foreach (databaseEntry e in acceptedUsers.Values) {
            if (e.username == userData.username) { return false; }
        }
        acceptedUsers[clientId] = userData;
        return true;
    }

    /// <summary>
    /// Function to start the last necessary steps before starting a game session.
    /// Rebalance lobbies, give Avatars to clients without one, 
    /// share the lobby information and session information with the clients
    /// The function is public void and parameterless on purpose so that it can be called by a button OnClick.
    /// </summary>
    public void LoadNotepadScene() {
        LM.RebalanceLobbies();

        ForceAvatars();

        UpdateGeneralLobbyInfo();

        UpdateStaticDataClientRpc(DataManager.currentTimer, DataManager.currentCodeQuestion.name, DataManager.currentCodeQuestion.description, DataManager.currentCodeQuestion.content, DataManager.currentCodeQuestion.tags);
    }

    /// <summary>
    /// Function to give a random Avatar to the clients that did not choose one in time.
    /// </summary>
    private void ForceAvatars() {
        List<ulong> targetKeys = new List<ulong>();
        foreach (KeyValuePair<ulong, databaseEntry> entry in acceptedUsers) {
            if (entry.Value.avatar.IsEmpty) { targetKeys.Add(entry.Key); }
        }

        foreach (ulong clientId in targetKeys) {
            UpdateClientSprite(clientId, Cosmetics.GetRandomAvatarName());
        }
    }

    /// <summary>
    /// Function to share the correct lobby information to all clients. 
    /// </summary>
    private void UpdateGeneralLobbyInfo() {
        int numberOfUsedLobbies = 0;
        List<int> lobbiesSize = new List<int>();
        List<ulong[]> lobbiesClients = LM.GetClientsInLobbies();


        for (int i = 0; i < lobbiesClients.Count; i++) {
            if(lobbiesClients[i].Length > 0) { 
                lobbiesSize.Add(lobbiesClients[i].Length);
                numberOfUsedLobbies++;
            }

            ClientRpcParams oneLobbyRpcParams = new ClientRpcParams {
                Send = new ClientRpcSendParams {
                    TargetClientIds = lobbiesClients[i]
                }
            };

            AddLobbiesClientRpc(i, lobbiesClients[i].Length, oneLobbyRpcParams);
        }

        DataManager.UpdateServerLobbyInfo(numberOfUsedLobbies, lobbiesSize, lobbiesClients);
    }


    /// <summary>
    /// Function to change the Avatar of a client that requested it.
    /// </summary>
    /// <param name="clientId">The id of the client that wants to change the Avatar.</param>
    /// <param name="spriteName">The name of the new Avatar.</param>
    private void UpdateClientSprite(ulong clientId, string spriteName) {
        databaseEntry oldData = acceptedUsers[clientId];
        databaseEntry updatedData = new databaseEntry(oldData.username, oldData.progress, oldData.points, spriteName, oldData.owner);
        acceptedUsers[clientId] = updatedData;
        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerController>().myData.Value = updatedData;
    }

    #region ServerRpcs

    /// <summary>
    /// Remote Procedure Call, from client to server.
    /// Signals the server that the user has selected a different Avatar and updates it over the network accordingly.
    /// </summary>
    /// <param name="clientId">The id of the client that called it.</param>
    /// <param name="spriteName">The name of the new Avatar.</param>
    [ServerRpc(RequireOwnership = false)]
    public void CosmeticChoiceServerRpc(ulong clientId, string spriteName) { UpdateClientSprite(clientId, spriteName); }

    
    /// <summary>
    /// Remote Procedure Call, from client to server.
    /// Signals the server that whatever operation the client had to perform has finished.
    /// When all clients have terminated all their operations, the next scene is loaded.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CheckAllAndNextSceneServerRpc() {
        doneCounter++;
        if (doneCounter == 2 * NetworkManager.Singleton.ConnectedClientsList.Count) {
            //We unsubscribe from the event to avoid wrong calls in the last scene
            NetworkManager.Singleton.OnClientDisconnectCallback -= CleanLobby;
            MSM.SetupAndLoadNextScene();
        }
    }

    #endregion

    #region ClientRpcs

    /// <summary>
    /// Remote Procedure Call, from server to client.
    /// Updates the clients data.
    /// </summary>
    /// <param name="userData">The new client data.</param>
    /// <param name="clientRpcParams">Necessary parameter to edit which clients will receive the Rpc, in this case all clients will receive a different Rpc</param>
    [ClientRpc]
    public void FillDataClientRpc(databaseEntry userData, ClientRpcParams clientRpcParams = default) { DataManager.ConfirmData(userData); }

    /// <summary>
    /// Remote Procedure Call, from server to client.
    /// Opens the client's cosmetics panel.
    /// <param name="clientRpcParams">Necessary parameter to edit which clients will receive the Rpc, in this case all clients will receive a different Rpc</param>
    /// </summary>
    [ClientRpc]
    public void OpenCosmeticsClientRpc(ClientRpcParams clientRpcParams = default) { cosmeticPanel.SetActive(true); }

    /// <summary>
    /// Remote Procedure Call, from server to client.
    /// Updates the lobby data for each client, then signals the server that it has completed the operation.
    /// </summary>
    /// <param name="lobbyIdx">The client's lobby.</param>
    /// <param name="lobbySize">The size of the lobby.</param>
    /// <param name="clientRpcParams">Necessary parameter to edit which clients will receive the Rpc, in this case all clients in the same lobby will receive a different Rpc</param>
    [ClientRpc]
    public void AddLobbiesClientRpc(int lobbyIdx, int lobbySize, ClientRpcParams clientRpcParams = default) {
        DataManager.UpdateLobbyInfo(lobbyIdx, lobbySize);
        CheckAllAndNextSceneServerRpc();
    }

    /// <summary>
    /// Remote Procedure Call, from server to client.
    /// Updates the static data decided by the server for each client, the it signals the server that it has completed the operation..
    /// Specifically: how much time is available and which <see cref="codeQuestion"/> was selected.
    /// The <see cref="codeQuestion"/> is sent piece by piece and then reconstructed on the client because of 2 limitation:
    /// A struct property cannot be a reference type (like string) if it is to be sent over the network.
    /// The content of a <see cref="codeQuestion"/> could be arbitrarely large, so converting the struct's strings into fixed byte strings would not work.
    /// </summary>
    /// <param name="currentTimer">Amount of available time to create a solution.</param>
    /// <param name="questionName">Name of the selected codeQuestion.</param>
    /// <param name="questionLabel">Description (in label format) of the selected codeQuestion.</param>
    /// <param name="questionContent">Content of the selected codeQuestion.</param>
    /// <param name="questionTags">Tags of the selected codeQuestion.</param>
    [ClientRpc]
    public void UpdateStaticDataClientRpc(int currentTimer, string questionName, string questionLabel, string questionContent, string[] questionTags) {
        DataManager.currentTimer = currentTimer;
        DataManager.currentCodeQuestion = new codeQuestion(questionName, questionLabel, questionContent, questionTags);
        CheckAllAndNextSceneServerRpc();
    }

    #endregion


}
