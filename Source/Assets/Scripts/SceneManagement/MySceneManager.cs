using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// The <see cref="MySceneManager"/> class exists to expose <see cref="UnityEngine.SceneManagement"/> methods.
/// Because of the synchronized nature of the project, the actual scene management is handled mostly by the <see cref="NetworkManager.SceneManager"/>,
/// which we interface in <see cref="SetupAndLoadNextScene"/>.
/// </summary>
public class MySceneManager : MonoBehaviour {

    /// <summary>
    /// Utility function to Invoke the client disconnection and termination of the application.
    /// The function is purposefully public void and parameterless so that it can be used by external buttons.
    /// </summary>
    public void Quit() {
        if (NetworkManager.Singleton.IsConnectedClient) { NetworkManager.Singleton.Shutdown(); }
        Application.Quit(); 
    }

    /// <summary>
    /// Utility function to return the name of the next scene.
    /// Required because the <see cref="NetworkManager.SceneManager"/> does not work with indexes.
    /// </summary>
    /// <returns>The name of the next scene in the build scene order.</returns>
    private string GetNextSceneByName() {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        string path = SceneUtility.GetScenePathByBuildIndex(currentSceneIndex + 1);
        string[] arr = path.Split('/');
        return arr[arr.Length - 1].Split('.')[0];
    }

    /// <summary>
    /// Take care of despawning everything that has been spawned on the network and then load the next scene.
    /// The function is purposefully public void and parameterless so that it can be used by external buttons.
    /// </summary>
    public void SetupAndLoadNextScene() {
        //Despawn all objects that the server had spawned in the scene
        List<NetworkObject> allObjects = new List<NetworkObject>(NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values);
        int idx = 0;
        while (idx < allObjects.Count) {
            allObjects[idx].Despawn(true);
            idx++;
        }

        //Load the next scene using the NetworkManager LoadScene
        NetworkManager.Singleton.SceneManager.LoadScene(GetNextSceneByName(), LoadSceneMode.Single);

    }

    /// <summary>
    /// Turns off and destroys the <see cref="NetworkManager"/>, then loads the first scene.
    /// Note that the <see cref="NetworkManager"/> is a scene object in the main menu, 
    /// meaning that the destruction is consistent with its Singleton pattern.
    /// The function is purposefully public void and parameterless so that it can be used by external buttons.
    /// </summary>
    public void LoadSceneZero() {
        if (NetworkManager.Singleton.IsListening) { NetworkManager.Singleton.Shutdown(); }

        Destroy(NetworkManager.Singleton.gameObject);
        SceneManager.LoadScene(0);
    }
}