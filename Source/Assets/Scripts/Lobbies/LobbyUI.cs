using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization.Components;

/// <summary>
/// Prefab class managing the details of a lobby and exposing the functions to interact with it.
/// The maximum number of players that can be in a lobby is stored in constant <see cref="maxCapacityOfLobby"/>.
/// </summary>
public class LobbyUI : MonoBehaviour {
    private const int maxCapacityOfLobby = 6;

    //Localization variables
    public int lobbyNumber;
    public int lobbyCount = 0;
    public int lobbyCapacity = maxCapacityOfLobby;
    public LocalizeStringEvent lobbyInfoText;


    private RectTransform usersLobby;

    //It is needed that freeSeats be initialized with zeros, but that is the default value of a ulong so no further action is required
    public ulong[] freeSeats = new ulong[maxCapacityOfLobby];

    /// <summary>
    /// Function add given player to this lobby.
    /// The <see cref="RectTransform"/> of the player is needed for easy parenting.
    /// </summary>
    /// <param name="playerTransform">The transform of the player spawned prefab.</param>
    /// <param name="clientId">The id of the player to add.</param>
    public void AddClient(RectTransform playerTransform, ulong clientId) {
        //Find the first empty seat and fill it with the new clientId
        int seatId = FirstSeat(0);
        if (seatId != -1) {
            freeSeats[seatId] = clientId;
        } else {
            Debug.LogError("Error, Trying to add client " + clientId + " into a full lobby (lobby " + lobbyNumber + ")");
        }

        //Parent the client object to the user area and normalized its scale
        playerTransform.SetParent(usersLobby);
        playerTransform.localScale = Vector3.one;

        lobbyCount += 1;
        lobbyInfoText.RefreshString();
    }

    /// <summary>
    /// Function to remove the given player from this lobby.
    /// Because this function is called for player disconnection or lobby rebalancing,
    /// there is no need to keep track of the clients' <see cref="RectTransform"/>,
    /// it either gets despawned or moved with <see cref="AddClient(RectTransform, ulong)"/>.
    /// </summary>
    /// <param name="clientId">The id of the player to remove.</param>
    public void RemoveClient(ulong clientId) {
        //Find the seat of the client and empty it
        int seatId = FirstSeat(clientId);
        if (seatId != -1) {
            freeSeats[seatId] = 0;
        } else {
            Debug.LogError("Error, Trying to remove client " + clientId + " from lobby " + lobbyNumber + " but it could not be found.");
        }

        lobbyCount -= 1;
        lobbyInfoText.RefreshString();
    }

    /// <summary>
    /// Uility function to return the first spot in the lobby that corresponds to the given Id.
    /// When given 0, it will return the first empty spot.
    /// </summary>
    /// <param name="val">Value to be found in the lobby.</param>
    /// <returns>The index of the position in the lobby for <paramref name="val"/>, or -1 if it was not found.</returns>
    public int FirstSeat(ulong val) {
        for (int i = 0; i < freeSeats.Length; i++) {
            if (freeSeats[i] == val) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Function called externally by <see cref="LobbyManager"/> to setup the details of a lobby.
    /// </summary>
    /// <param name="n">Number of the lobby.</param>
    /// <param name="usersHolderPrefab">Prefab of the area where the player prefabs will be parented.</param>
    public void Setup(int n, GameObject usersHolderPrefab) {
        lobbyNumber = n;
        lobbyInfoText.RefreshString();

        SpawnUserArea(usersHolderPrefab);
    }


    /// <summary>
    /// Utility function to spawn correctly the user area of this lobby.
    /// The area needs to call its <see cref="NetworkObject"/> spawn because the lobby
    /// itself is a <see cref="NetworkObject"/> and nesting must be carefully managed
    /// to make sure that everything can be parented correctly.
    /// </summary>
    /// <param name="usersLobbyPrefab">Prefab of the area clients are parented to.</param>
    private void SpawnUserArea(GameObject usersLobbyPrefab) {
        //Just to be sure
        if (NetworkManager.Singleton.IsServer) {

            //Instantiate the GameObject where clients will be parented
            usersLobby = Instantiate(usersLobbyPrefab).GetComponent<RectTransform>();

            //Spawn it over the network
            usersLobby.GetComponent<NetworkObject>().Spawn();

            //Parent the clients area to this lobby
            usersLobby.SetParent(GetComponent<RectTransform>());

            //Normalize size and scale
            usersLobby.offsetMin = Vector2.zero;
            usersLobby.offsetMax = new Vector2(0, -80f);
            usersLobby.localScale = Vector2.one;
        }
    }

    /// <summary>
    /// Utility function to return all Ids of the clients in this lobby. 
    /// Very useful to extract the targets for <see cref="ClientRpcParams"/>.
    /// </summary>
    /// <returns>The array of the ids of the clients in this lobby.</returns>
    public ulong[] GetClientsInLobby() {
        List<ulong> clientsInLobby = new List<ulong>();

        for (int i = 0; i < freeSeats.Length; i++) {
            if (freeSeats[i] != 0) {
                clientsInLobby.Add(freeSeats[i]);
            }
        }

        //Easiest way to have an array of unknown size
        return clientsInLobby.ToArray();
    }

    /// <summary>
    /// Getter of the constant for the maximum amount of client that a lobby can handle.
    /// </summary>
    /// <returns><see cref="maxCapacityOfLobby"/>.</returns>
    public int GetMaxCapacityOfLobby() { return maxCapacityOfLobby; }
}
