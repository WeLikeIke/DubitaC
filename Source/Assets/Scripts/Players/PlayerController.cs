using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///  Prefab class managing the details of a UserBox and exposing the functions to interact with it.
///  This class is the <see cref="NetworkManager"/>'s player class, so it should be spawned by it to be propagated on the network.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour {

    private RectTransform rectTransform;
    public Image backgroundPanel;
    public Button selectionButton;
    public TextMeshProUGUI nametag;
    public Image spriteArea;

    public bool retrySettingClicks = false;

    public NetworkVariable<bool> disableLayout = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isSelected = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> canBeClicked = new NetworkVariable<bool>();

    public NetworkVariable<int> myAvatarState = new NetworkVariable<int>(0);
    public NetworkVariable<databaseEntry> myData = new NetworkVariable<databaseEntry>();

    void Awake() {
        rectTransform = GetComponent<RectTransform>();

        //Subscribe to thw network event of a change in the network variable
        myData.OnValueChanged += DataHasChanged;

        disableLayout.OnValueChanged += UpdateLayoutState;
        isSelected.OnValueChanged += UpdateBackgroundColor;
    }

    void Update() {
        //Maintain the scale normalized
        if (NetworkManager.Singleton.IsClient && transform.localScale != Vector3.one) {
            rectTransform.localScale = Vector3.one;
        }

        //Try to get the value of the button click if it failed during setup
        if (NetworkManager.Singleton.IsClient && retrySettingClicks) {
            TryGetManagers();
        }

    }

    /// <summary>
    /// Function that responds to the change in value of <see cref="myData"/>.
    /// Calls <see cref="SetPlayerData(databaseEntry)"/> to update the client data.
    /// </summary>
    /// <param name="oldData">Data before the change, not needed.</param>
    /// <param name="newData">Data after the change.</param>
    public void DataHasChanged(databaseEntry oldData, databaseEntry newData) {
        SetPlayerData(newData);
    }

    /// <summary>
    /// Function that responds to the change in value of <see cref="disableLayout"/>.
    /// Removes or reintroduces the restriction on the <see cref="LayoutElement"/> component that makes it stretch for all the space available.
    /// </summary>
    /// <param name="oldData">Data before the change, not needed.</param>
    /// <param name="newData">Data after the change.</param>
    public void UpdateLayoutState(bool oldData, bool newData) {
        if (newData) {
            GetComponent<LayoutElement>().flexibleWidth = 0;
        } else {
            GetComponent<LayoutElement>().flexibleWidth = 1;
        }
    }

    /// <summary>
    /// Function that responds to the change in value of <see cref="isSelected"/>.
    /// Changes the color of the background panel.
    /// </summary>
    /// <param name="oldData">Data before the change, not needed.</param>
    /// <param name="newData">Data after the change.</param>
    public void UpdateBackgroundColor(bool oldData, bool newData) {
        if (newData) {
            backgroundPanel.color = Cosmetics.selectedColor;
        } else {
            backgroundPanel.color = Cosmetics.selectableColor;
        }
    }

    /// <summary>
    /// Function to setup the delegates on the prefab's button.
    /// We hook from the references <see cref="DoubtManager.SetTarget(ulong)"/> and <see cref="NotepadManager.SetSolution(string)"/>.
    /// These delegates will be used during the "doubt" round.
    /// </summary>
    private void TryGetManagers() {
        GameObject referenceHolder = GameObject.FindWithTag("references");
        if (referenceHolder == null) {
            retrySettingClicks = true;
            return;
        }

        retrySettingClicks = false;

        selectionButton.onClick.AddListener(
            delegate {
                referenceHolder.GetComponent<PlayerSpawner>().DM.SetTarget(myData.Value.owner);
                referenceHolder.GetComponent<PlayerSpawner>().NM.SetSolution(DataManager.GetClientSolution(myData.Value.owner));
            });
    }

    /// <summary>
    /// Function to update the visuals and data of the current client's player prefab.
    /// </summary>
    /// <param name="userData">New user data.</param>
    private void SetPlayerData(databaseEntry userData) {
        //Set the username
        nametag.SetText(userData.username.ToString());

        //If the Avatars are not loaded, load them
        if (!Cosmetics.IsReady()) { Cosmetics.Init(); }

        //Set the Avatar
        spriteArea.sprite = Cosmetics.GetAvatar(userData.avatar.ToString(), myAvatarState.Value);

        //Saving the userData in my DataManager should only happen for the real playerController
        if (IsOwner && userData.owner == NetworkManager.Singleton.LocalClientId) {
            DataManager.ConfirmData(userData);
        }

        //Try to assign the delegates to the prefab's button
        if (canBeClicked.Value) { TryGetManagers(); }
    }

}