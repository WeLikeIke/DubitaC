using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Class responsible for spawning and managing the userBoxes.
/// See <see cref="PlayerController"/>.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerSpawner : NetworkBehaviour {

    public DoubtManager DM;
    public NotepadManager NM;
    public GameObject playerPrefab;
    public RectTransform[] playerHolders;

    public bool isDoubtScene;

    private List<List<GameObject>> doubtersList = new List<List<GameObject>>();
    private List<List<GameObject>> targetsList = new List<List<GameObject>>();
    private List<ulong> currentDoubterList = new List<ulong>();
    private List<ulong> currentTargetList = new List<ulong>();

    void Start() {

        if (NetworkManager.Singleton.IsClient) {
            //Only during the doubt scene we can set the targets for the players
            if (isDoubtScene) { SetInitialTarget(); }
            return;
        }

        SpawnByScene();
        

    }

    /// <summary>
    /// Function to decide which version of the spawn to trigger.
    /// Since each scene has a different number of <see cref="RectTransform"/>s assigned to <see cref="playerHolders"/>,
    /// and because getting the scene index would require interfacing with <see cref="UnityEngine.SceneManagement"/>,
    /// we used the length of the array as a way to know the correct function to use.
    /// </summary>
    private void SpawnByScene() {

        switch (playerHolders.Length) {

            //Doubt scene, on the left side there is a column with all the players
            case 1:
                SpawnAllLobbyLeaderboards(playerHolders, false, false, false);
                break;

            //Slideshow scene, on the left and top side there is the same identical list with all the players
            case 2:
                SavePlayerReferences();
                SpawnAllLobbyLeaderboards(playerHolders, true, true, false);
                break;

            //Final scene, staggered on three lists there are all the players
            case 3:
                SpawnAllLobbyLeaderboards(playerHolders, true, false, true);
                break;
        }
    }

    /// <summary>
    /// Utility function to spawn the player prefabs using the same function for all lobbies.
    /// </summary>
    /// <param name="holdersTransform">Array of transforms that the players will be parented to.</param>
    /// <param name="serverOwnership">true if the player prefab represents the local client, false if the prefab represents a different client.</param>
    /// <param name="useOldLeaderboard">true if the order should follow the not yet updated leaderboard, false to use the updated leaderboard.</param>
    /// <param name="staggeredSpawning">true if the player prefabs should be spread over many <see cref="RectTransform"/>s, false if they should be duplicated instead.</param>
    public void SpawnAllLobbyLeaderboards(RectTransform[] holdersTransform, bool serverOwnership, bool useOldLeaderboard, bool staggeredSpawning) {
        for(int i = 0; i < DataManager.lobbiesInUse; i++) {
            SpawnLeaderboard(i, serverOwnership, useOldLeaderboard, DataManager.allLobbyClients[i], holdersTransform, staggeredSpawning);
        }
    }


    /// <summary>
    /// Function to spawn and initialize correctly the player prefabs.
    /// </summary>
    /// <param name="lobbyIdx">The lobby of the clients.</param>
    /// <param name="serverOwnership">true if the player prefab represents the local client, false if the prefab represents a different client.</param>
    /// <param name="useOldLeaderboard">true if the order should follow the not yet updated leaderboard, false to use the updated leaderboard.</param>
    /// <param name="playersInLobby">List of all the clientIds, used to determine how many and how to initialize the <see cref="PlayerController"/> </param>
    /// <param name="staggeredSpawning">true if the player prefabs should be spread over many <see cref="RectTransform"/>s, false if they should be duplicated instead.</param>
    private void SpawnLeaderboard(int lobbyIdx, bool serverOwnership, bool useOldLeaderboard, ulong[] playersInLobby, RectTransform[] holdersTransform, bool staggeredSpawning) {
        int staggeredOffset = 0;
        int avatarState = 0;
        //During the slideshow the Avatars start in their "happy" state
        if (staggeredSpawning) { avatarState = 3; }

        for (int i = 0; i < playersInLobby.Length; i++) {
            for (int j = 0; j < holdersTransform.Length; j++) {
                GameObject player = Instantiate(playerPrefab);

                //During the slideshow we keep track of the PlayerBoxes to being able to change their background
                if (serverOwnership != staggeredSpawning) {
                    if(j == 0) {
                        targetsList[lobbyIdx].Add(player);
                    } else {
                        doubtersList[lobbyIdx].Add(player);
                    }
                }

                //Obtain the correct leaderboardSpot
                databaseEntry leaderboardSpot;
                if (useOldLeaderboard) {
                    leaderboardSpot = DataManager.oldLeaderboard[lobbyIdx][i];
                } else {
                    leaderboardSpot = DataManager.leaderboard[lobbyIdx][i];
                }

                //Setup the PlayerController
                if (staggeredSpawning) {
                    player.GetComponent<PlayerController>().disableLayout.Value = true;
                    player.GetComponent<PlayerController>().myAvatarState.Value = avatarState;
                }
                player.GetComponent<PlayerController>().canBeClicked.Value = !serverOwnership;
                player.GetComponent<PlayerController>().myData.Value = leaderboardSpot;

                //Set visibility, a PlayerController is visible ONLY to other clients in the same lobby
                player.GetComponent<NetworkObject>().CheckObjectVisibility = (clientId) => { 
                    for(int i = 0; i < playersInLobby.Length; i++) {
                        if(playersInLobby[i] == clientId) { return true; }
                    }
                    return false; 
                };

                //Spawn the prefab all over the network
                if (serverOwnership) {
                    player.GetComponent<NetworkObject>().Spawn();
                } else {
                    player.GetComponent<NetworkObject>().SpawnAsPlayerObject(leaderboardSpot.owner);
                }

                //Parent it to the correct RectTransform
                player.GetComponent<NetworkObject>().TrySetParent(holdersTransform[j + staggeredOffset], false);

                //Calculations for the staggering over 3 RectTransforms
                if (staggeredSpawning) {
                    staggeredOffset = 1 + (int)(i / 2) - (int)(i / 4);
                    avatarState = 4 - staggeredOffset;
                    break;
                }
            }
        }
    }


    /// <summary>
    /// Utility function to create the setup for the slideshow scene.
    /// The lists keep a reference of which players need to be spawned an which is currently highlighted (at setup noone).
    /// </summary>
    private void SavePlayerReferences() {
        for (int i = 0; i < DataManager.lobbiesInUse; i++) {
            doubtersList.Add(new List<GameObject>());
            targetsList.Add(new List<GameObject>());
            currentDoubterList.Add(0);
            currentTargetList.Add(0);
        }
    }


    /// <summary>
    /// Function to set the first target in the "doubt" round to the own client, so that the first solution that they see is their own.
    /// </summary>
    private void SetInitialTarget() {
        DM.SetTarget(NetworkManager.Singleton.LocalClientId);
        NM.SetSolution(DataManager.GetClientSolution(NetworkManager.Singleton.LocalClientId));
    }

    /// <summary>
    /// Utility function to trigger the background panel highlighting of a playerBox prefab only if the owner corresponds to the <paramref name="targetId"/>.
    /// </summary>
    /// <param name="playerBox">The playerBoc prefab.</param>
    /// <param name="targetId">The id of the target client.</param>
    private void HighlightPlayerBox(GameObject playerBox, ulong targetId) {
        PlayerController PC = playerBox.GetComponent<PlayerController>();
        PC.isSelected.Value = (PC.myData.Value.owner == targetId);
    }

    #region ServerRpcs

    /// <summary>
    /// Remote Procedure Call, from client to server.
    /// Triggers the change in background color of the player prefabs, both in <see cref="doubtersList"/> and <see cref="targetsList"/>.
    /// To reduce the computation, the background changes only if the ids are different from the last time the function was called.
    /// </summary>
    /// <param name="lobbyIdx">The lobby of the clients.</param>
    /// <param name="doubterId">The id of the client that made the doubt (top list).</param>
    /// <param name="targetId">The id of the client that was doubted (left list).</param>
    [ServerRpc(RequireOwnership = false)]
    public void HighlightPlayerBoxServerRpc(int lobbyIdx, ulong doubterId, ulong targetId) {

        if (doubterId != currentDoubterList[lobbyIdx]) {
            currentDoubterList[lobbyIdx] = doubterId;

            foreach (GameObject playerBox in doubtersList[lobbyIdx]) {
                HighlightPlayerBox(playerBox, doubterId);
            }
        }

        if (targetId != currentTargetList[lobbyIdx]) {
            currentTargetList[lobbyIdx] = targetId;

            foreach (GameObject playerBox in targetsList[lobbyIdx]) {
                HighlightPlayerBox(playerBox, targetId);
            }
        }

    }

    #endregion
}
