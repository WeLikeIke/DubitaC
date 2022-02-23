using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Class responsible for spawning and managing the lobbies.
/// See <see cref="LobbyUI"/>.
/// The maximum amount of possible lobbies is stored in constant <see cref="maxNumberOfLobbies"/>.
/// </summary>
public class LobbyManager : MonoBehaviour {
    private const int maxNumberOfLobbies = 4;


    public RectTransform lobbyHolder;
    public GameObject lobbyPrefab;
    public GameObject userHolderPrefab;
    public List<GameObject> listOfLobbies = new List<GameObject>();

    void Start() {
        //Just to be sure
        if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsListening) {
            SpawnLobbies();
        }
    }


    /// <summary>
    /// Function to spawn the lobbies in the server interface.
    /// Each lobby is saved in a list and setup as needed.
    /// Notice how their <see cref="NetworkObject"/> component needs to be spawned,
    /// so that other <see cref="NetworkObject"/>s might be parented to the lobby.
    /// </summary>
    public void SpawnLobbies() {
        for (int i = 0; i < maxNumberOfLobbies; i++) {

            //Instantiate the prefab
            GameObject lobby = Instantiate(lobbyPrefab, lobbyHolder);

            //Save the reference
            listOfLobbies.Add(lobby);

            //Spawn over the network
            lobby.GetComponent<NetworkObject>().Spawn();

            //Setup the details
            lobby.GetComponent<LobbyUI>().Setup(i, userHolderPrefab);
        }

    }

    /// <summary>
    /// Uility function to return the first lobby containing the given Id.
    /// When given 0, it will return the first lobby with an empty spot.
    /// </summary>
    /// <param name="val">Value to be found in all lobbies.</param>
    /// <returns>The index of the lobby containing <paramref name="val"/>, or -1 if it was not found.</returns>
    private int FirstLobby(ulong val) {
        for (int i = 0; i < listOfLobbies.Count; i++) {
            if (listOfLobbies[i].GetComponent<LobbyUI>().FirstSeat(val) >= 0) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Utility function to assign a player prefab to the first available spot, returns if the operation was successfull or not.
    /// </summary>
    /// <param name="playerTransform">The player prefab transform of the client to assign.</param>
    /// <param name="clientId">The id client to assign.</param>
    /// <returns></returns>
    public bool AssignLobby(RectTransform playerTransform, ulong clientId) {
        int lobbyId = FirstLobby(0);

        if (lobbyId == -1) { 
            //Not an error, just a message
            Debug.Log("All lobbies are full!");
            return false;
        }

        listOfLobbies[lobbyId].GetComponent<LobbyUI>().AddClient(playerTransform, clientId);
        return true;
    }

    /// <summary>
    /// Utility function to deassign a player from its lobby.
    /// </summary>
    /// <param name="clientId">The id of the client to deassign.</param>
    public void DeassignLobby(ulong clientId) {
        int lobbyId = FirstLobby(clientId);

        if (lobbyId >= 0) {
            listOfLobbies[lobbyId].GetComponent<LobbyUI>().RemoveClient(clientId);
        }
    }

    /// <summary>
    /// Function to rebalance the lobbies using the following scheme:
    /// Divide the number of clients by the maximum capacity of a lobby to get the number of needed lobbies.
    /// Divide the number of clients by the needed lobbies to get the minimum guaranteed number of clients in each lobby.
    /// Distribute the minimum guarantedd number of clients to the needed lobbies.
    /// The remaining clients are distributed in round-robin order to all lobbies.
    /// To reduce computation, clients that are already assigned to a lobby correctly are not moved. 
    /// </summary>
    public void RebalanceLobbies() {
        //Take the positions of all current clients
        List<ulong[]> clientsInLobbies = GetClientsInLobbies();
        int totalClients = CountTotalClients(clientsInLobbies);
        int lobbyCapacity = listOfLobbies[0].GetComponent<LobbyUI>().GetMaxCapacityOfLobby();

        //Calculate the sizes that we would like the lobbies to have
        List<int> targetSizes = CalculateBalancedLobbySizes(totalClients, lobbyCapacity);

        //Create a pool of clients that must move
        List<ulong> clientPool = FillClientPool(clientsInLobbies, targetSizes);

        EmptyClientPool(clientPool, clientsInLobbies, targetSizes);

    }

    /// <summary>
    /// Utility function to return the amount of clients for each lobby to make them as balanced as possible.
    /// </summary>
    /// <param name="totalClients">Total number of clients to distribute along the lobbies.</param>
    /// <param name="lobbyCapacity">Maximum amount of clients that can fit into a lobby.</param>
    /// <returns></returns>
    private List<int> CalculateBalancedLobbySizes(int totalClients, int lobbyCapacity) {
        List<int> targetSizes = new List<int>();
        int lobbiesNeeded = Mathf.CeilToInt(totalClients / (float)lobbyCapacity);
        int temp = totalClients;

        //Starting from the most balanced baseline
        for (int i = 0; i < lobbiesNeeded; i++) {
            int balancedLobby = (int)(totalClients / lobbiesNeeded);
            targetSizes.Add(balancedLobby);
            temp -= balancedLobby;
        }

        //And then distributing the remaining clients starting from the first lobby
        for (int i = 0; i < temp; i++) {
            targetSizes[i % lobbiesNeeded] += 1;
            temp -= 1;
        }

        return targetSizes;
    }

    /// <summary>
    /// Utility function to remove all the clients that are exceeding the expected lobby size and insert it into a client pool.
    /// </summary>
    /// <param name="clientsInLobbies">List containing all the clients for each lobby.</param>
    /// <param name="targetSizes">List of expected lobby sizes.</param>
    /// <returns>A list of client ids that need to be relocated.</returns>
    private List<ulong> FillClientPool(List<ulong[]> clientsInLobbies, List<int> targetSizes) {
        List<ulong> clientPool = new List<ulong>();

        //If we are occupying more lobbies than needed, empty as many lobbies as needed completely
        int removedCounter = 0;
        while (clientsInLobbies.Count - targetSizes.Count > 0) {

            foreach(ulong singleClient in clientsInLobbies[clientsInLobbies.Count - 1]) {
                clientPool.Add(singleClient);
            }

            clientsInLobbies.RemoveAt(clientsInLobbies.Count - 1);
            removedCounter++;
        }

        //Refill the lobbies with empty arrays
        for (int i = 0; i < removedCounter; i++) {
            clientsInLobbies.Add(new ulong[0]);
        }

        //If the lobby contains more clients than needed, remove them and add them to the pool
        for (int i = 0; i < targetSizes.Count; i++) {
            if (clientsInLobbies[i].Length > targetSizes[i]) {

                int counter = 0;
                for (int j = clientsInLobbies[i].Length - 1; j >= targetSizes[i]; j--) {
                    clientPool.Add(clientsInLobbies[i][j]);
                    counter++;
                }
                clientsInLobbies[i] = ResizeArray(clientsInLobbies[i], clientsInLobbies[i].Length - counter);

            }
        }

        return clientPool;
    }

    /// <summary>
    /// Utility function to assign the clients from the clientPool to the correct lobbies, thus emptying it.
    /// </summary>
    /// <param name="clientPool">The list of clients that need to be relocated.</param>
    /// <param name="clientsInLobbies">List containing all the clients for each lobby.</param>
    /// <param name="targetSizes">List of expected lobby sizes.</param>
    private void EmptyClientPool(List<ulong> clientPool, List<ulong[]> clientsInLobbies, List<int> targetSizes) {
        //Fill every lobby accordingly, removing all clients from the pool
        for (int i = 0; i < targetSizes.Count; i++) {
            while (targetSizes[i] != clientsInLobbies[i].Length) {
                MoveClientToDifferentLobby(clientPool[0], i);
                clientPool.RemoveAt(0);
                clientsInLobbies[i] = ResizeArray(clientsInLobbies[i], clientsInLobbies[i].Length + 1);
            }
        }

        if (clientPool.Count > 0) { Debug.LogError("Error, something went wrong during the rebalancing of the lobbies, " + clientPool.Count + " clients have no lobby!"); }
    }

    /// <summary>
    /// Utility function to move a client from one lobby to another.
    /// </summary>
    /// <param name="clientId">The id of the client that needs to be moved.</param>
    /// <param name="targetLobby">The new lobby to move the client to.</param>
    private void MoveClientToDifferentLobby(ulong clientId, int targetLobby) {
        int lobbyId = FirstLobby(clientId);
        if (lobbyId == -1 || lobbyId == targetLobby) { return; }

        //Get the RectTransform from the NetworkManager
        RectTransform playerTransform = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<RectTransform>();

        //Remove first, add later
        listOfLobbies[lobbyId].GetComponent<LobbyUI>().RemoveClient(clientId);
        listOfLobbies[targetLobby].GetComponent<LobbyUI>().AddClient(playerTransform, clientId);

    }

    /// <summary>
    /// Utility function to resize an array.
    /// If <paramref name="newLength"/> is smaller bigger than the original, then the new cells are filled with default values.
    /// </summary>
    /// <param name="input">Original array to be resized.</param>
    /// <param name="newLength">New length of the returned array.</param>
    /// <returns>An array containing as much elements of the original array as possible and with length equal to <paramref name="newLength"/>.</returns>
    private ulong[] ResizeArray(ulong[] input, int newLength) {
        ulong[] result = new ulong[newLength];
        int safeLength = Mathf.Min(input.Length, newLength);
        for (int i = 0; i < safeLength; i++) {
            result[i] = input[i];
        }

        return result;
    }

    /// <summary>
    /// Utility function to return all the clients inside all the lobbies in a list.
    /// </summary>
    /// <returns>List containing the arrays of the client ids inside all lobbies.</returns>
    public List<ulong[]> GetClientsInLobbies() {
        List<ulong[]> lobbiesClients = new List<ulong[]>();

        foreach(GameObject lobby in listOfLobbies) {
            lobbiesClients.Add(lobby.GetComponent<LobbyUI>().GetClientsInLobby());
        }

        return lobbiesClients;
    }

    /// <summary>
    /// Utility function to return the number of all clients in all lobbies.
    /// </summary>
    /// <param name="lobbiesClients">List of all clients in all lobbies.</param>
    /// <returns>Total number of clients in all lobbies.</returns>
    private int CountTotalClients(List<ulong[]> lobbiesClients) {
        int total = 0;
        foreach(ulong[] lobby in lobbiesClients) {
            total += lobby.Length;
        }

        return total;
    }

}
