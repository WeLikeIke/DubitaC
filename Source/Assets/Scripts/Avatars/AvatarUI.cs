using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prefab class managing the details of an Avatar and exposing the functions to interact with it.
/// </summary>
public class AvatarUI : MonoBehaviour {

    private string spriteId;
    public bool isSceneObject;
    public Image backgroundPanel;
    public Button selectionButton;
    public Image spriteArea;
    public TextMeshProUGUI points;

    void Start() {

        //For the loading screen, the avatar is a scene object, it is not spawned by the AvatarManager,
        //so the sprite needs to be setup manually.
        if (isSceneObject && NetworkManager.Singleton.IsClient) {
            spriteArea.sprite = Cosmetics.GetAvatar(DataManager.myData.avatar.ToString(), 0);
        }

        TryWrapperSetup();
        
    }

    /// <summary>
    /// Function to initialize an Avatar prefab, it takes care of the visal UI and button OnClick delegate.
    /// </summary>
    /// <param name="AM">AvatarManager that spawned the prefab.</param>
    /// <param name="spriteName">Name of the avatar.</param>
    /// <param name="pointsThreshold">Points that the user currently has.</param>
    public void Setup(AvatarManager AM, string spriteName, int pointsThreshold) {
        string[] arr = spriteName.Split('_');

        //Bad spriteName
        if (arr.Length != 2 || !int.TryParse(arr[1], out int val)) {
            Debug.LogError("Error, the Avatar Sprite name " + spriteName + " was badly formatted, the correct format is: 'name_positiveNumber'.");
            return;
        }

        int state = 0;
        if (val > pointsThreshold) {
            state = 1;
            selectionButton.interactable = false;
        }

        spriteId = spriteName;
        spriteArea.sprite = Cosmetics.GetAvatar(spriteName, state);
        points.SetText(arr[1]);

        selectionButton.onClick.AddListener(delegate{AM.SelectThis(this);});
    }


    /// <summary>
    /// Utility function to add an additional delegate to the Avatar button.
    /// Only in scenes where there is a <see cref="NetworkWrapper"/>, does nothing otherwise.
    /// </summary>
    private void TryWrapperSetup() {
        GameObject networkWrapperObject = GameObject.FindWithTag("wrapper");
        if (networkWrapperObject == null) { return; }
        selectionButton.onClick.AddListener(
            delegate {
                networkWrapperObject.GetComponent<NetworkWrapper>().CosmeticChoiceServerRpc(NetworkManager.Singleton.LocalClientId, spriteId);
            });
    }


    /// <summary>
    /// Utility function to change the color of the background to a given <see cref="Color"/>.
    /// </summary>
    /// <param name="c">Color to change the background to.</param>
    public void SetPanelColor(Color c) { backgroundPanel.color = c; }

}
