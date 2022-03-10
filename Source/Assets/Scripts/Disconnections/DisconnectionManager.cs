using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;


/// <summary>
/// The <see cref="DisconnectionManager"/> handles the disconnection callback of clients in the general sense.
/// It also exposes a public void parameterless function to disconnect either clients or server using an external buttonpress.
/// Notice how it is VERY important that <see cref="DisconnectionManager"/> extends <see cref="MonoBehaviour"/> and NOT <see cref="NetworkBehaviour"/>,
/// During a disconnection caused by a server rejection (wrong credentials), all scene <see cref="NetworkObject"/>s are despawned,
/// making their functions unreachable, this is why this class exists.
/// </summary>
public class DisconnectionManager : MonoBehaviour {

    public MySceneManager MSM;
    public GameObject cosmeticPanel;

    void Start() { NetworkManager.Singleton.OnClientDisconnectCallback += LastRites; }


    /// <summary>
    /// Function to graciously handle a client disconnection.
    /// When a cosmeticPanel GameObject is referenced from the inspector, it should activate it.
    /// Otherwise, we are in the case of a server rejection, we update a static label informing the user that the server connection failed
    /// and then load the first scene.
    /// </summary>
    /// <param name="clientId">The Id of the client that received this event (automatically passed by the <see cref="NetworkManager"/>)</param>
    private void LastRites(ulong clientId) {
        if (!NetworkManager.Singleton.IsServer) {
            if (cosmeticPanel == null) {
                DataManager.databaseFeedback =  "_bad_request";
                DataManager.wasRejected = true;
                MSM.LoadSceneZero();
            } else {
                cosmeticPanel.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Function to disconnect a client and laod the first scene for a server.
    /// The function is purposefully public void and parameterless so that it can be used by external buttons.
    /// </summary>
    public void Disconnect() {
        if (NetworkManager.Singleton.IsServer) {
            MSM.LoadSceneZero();
        } else {
            NetworkManager.Singleton.Shutdown();
        }
        
    }

}
