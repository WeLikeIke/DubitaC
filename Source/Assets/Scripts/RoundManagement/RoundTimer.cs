using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;
using Unity.Netcode;

/// <summary>
/// Class to keep track of the time passing during a round.
/// The amount of additional time that the players receive during the doubting "round" is stored in constant <see cref="percentageTimeIncreaseForEachClient"/>.
/// </summary>
public class RoundTimer : MonoBehaviour {
    private const float percentageTimeIncreaseForEachClient = 0.15f;

    public ReadyManager RM;
    public DoubtManager DM;

    public Image timerImage;
    public bool isDoubtScene;

    //Localization
    public LocalizeStringEvent timerText;
    public int displayTime = 0;

    private float roundTimer;
    private bool primed = true;
    private float currentTime = 0f;

    void Start() {
        //Retrieving the maximum amount of time
        roundTimer = DataManager.currentTimer;

        //Giving some more time for the doubting part
        if (isDoubtScene) { IncreaseAvailableTime(); }
    }

    void Update() {
        Tick();
    }

    /// <summary>
    /// Utility function to increase the available time during the doubt round.
    /// The final time depends on the number of clients in the lobby: 115%, 130%, 145%, 160% and 175%.
    /// </summary>
    private void IncreaseAvailableTime() {
        int maxLobby = 0;

        if (NetworkManager.Singleton.IsServer) {
            foreach (int lobbySize in DataManager.allLobbySizes) {
                if (lobbySize > maxLobby) { maxLobby = lobbySize; }
            }
        } else {
            maxLobby = DataManager.myLobbySize;
        }

        roundTimer *= 1 + ((maxLobby - 1) * percentageTimeIncreaseForEachClient);
    }

    /// <summary>
    /// Function to advance the timer's time.
    /// When the available time finishes, it calles <see cref="TimesUp"/>.
    /// Updates the visuals (both client stopwatch and server number display).
    /// </summary>
    private void Tick() {
        if (!primed) { return; }

        //Ticking up
        currentTime += Time.deltaTime;

        //Radial fill of the timer indicator
        if (timerImage != null) { timerImage.fillAmount = currentTime / roundTimer; }

        //When the available time finishes
        if (currentTime >= roundTimer) {
            displayTime = 0;
            TimesUp();
        } else {
            displayTime = (int)(roundTimer - currentTime);
        }
        //Refresh the timer server text every tick
        timerText.RefreshString();

    }

    /// <summary>
    /// Function called at the end of the available time, triggers the appropriate function depending on which reference is not null.
    /// <see cref="ReadyManager"/> requires that the clients send their solution to the server.
    /// <see cref="DoubtManager"/> requires that the clients send their doubts to the server.
    /// </summary>
    private void TimesUp() {
        primed = false;

        if (RM != null) { RM.SendSolution(); }

        if (DM != null) { DM.SendDoubts(); }

    }

}
