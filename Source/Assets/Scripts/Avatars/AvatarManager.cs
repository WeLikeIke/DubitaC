using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Class responsible for spawning and managing the Avatars.
/// See <see cref="AvatarUI"/>.
/// </summary>
public class AvatarManager : MonoBehaviour {

    private bool spawnedOnce = false;
    public GameObject avatarPrefab;

    public RectTransform avatarHolder;
    public TextMeshProUGUI currentPoints;

    private List<AvatarUI> allAvatarUI = new List<AvatarUI>();

    void Update() {TrySetup();}

    /// <summary>
    /// Utility function to setup the details of the <see cref="AvatarManager"/>.
    /// Updates the number of points the user has currently and spawns all Avatars in order.
    /// Uses <see cref="spawnedOnce"/> as a guard, so it only gets executed once.
    /// </summary>
    private void TrySetup() {
        if (spawnedOnce) { return; }
            
        spawnedOnce = true;
        currentPoints.SetText(DataManager.myData.progress.ToString());

        string[] orderedNames = SortByPoints(Cosmetics.GetAvatarNames());

        SpawnAvatars(orderedNames);
    }

    /// <summary>
    /// Function to spawn the Avatars in the order given.
    /// </summary>
    /// <param name="names">The names of the Avatars to spawn.</param>
    private void SpawnAvatars(string[] orderedNames) {
        foreach (string name in orderedNames) {
            GameObject singleAvatar = Instantiate(avatarPrefab, avatarHolder);
            allAvatarUI.Add(singleAvatar.GetComponent<AvatarUI>());

            singleAvatar.GetComponent<AvatarUI>().Setup(this, name, DataManager.myData.progress);
        }
    }


    /// <summary>
    /// Utility function that, given a list of avatar names, returns them ordered from lowest point requirement to highest.
    /// </summary>
    /// <param name="input">The list of avatr names to order.</param>
    /// <returns></returns>
    private string[] SortByPoints(string[] input) {
        int[] pointRequirements = new int[input.Length];

        for (int i = 0; i < input.Length; i++) {
            string[] arr = input[i].Split('_');

            //Bad string Name
            if (arr.Length != 2 || !int.TryParse(arr[1], out pointRequirements[i])) {
                Debug.LogError("Error, the Avatar Sprite name " + input[i] + " was badly formatted, the correct format is: 'name_positiveNumber'.");
                return null; 
            }
        }

        //Sort the input array by using the point array that we created as keys
        Array.Sort(pointRequirements, input);

        return input;
    }

    /// <summary>
    /// Function used by the <see cref="AvatarUI"/> class to change the color of the background panel of all displayed <see cref="AvatarUI"/>.
    /// </summary>
    /// <param name="selectedAvatar">The Avatar calling the function, the one that should be selected.</param>
    public void SelectThis(AvatarUI selectedAvatar) {
        for (int i = 0; i < allAvatarUI.Count; i++) {
            if (allAvatarUI[i] == selectedAvatar) {
                allAvatarUI[i].SetPanelColor(Cosmetics.selectedColor);
            } else {
                allAvatarUI[i].SetPanelColor(Cosmetics.selectableColor);
            }
        }
    }

}
