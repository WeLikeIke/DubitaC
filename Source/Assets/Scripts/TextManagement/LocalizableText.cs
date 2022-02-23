using TMPro;
using UnityEngine;

/// <summary>
/// <see cref="LocalizableText"/> is used to create a localizable UI text using labels, 
/// it is hooked to a forced <see cref="TextMeshProUGUI"/> component on the same <see cref="GameObject"/>.
/// Requires an external class to hold the localization dictionary (here <see cref="DataManager"/>) and
/// Must run its <see cref="Start"/> after the dictionary loading has already been completed.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizableText : MonoBehaviour {

    public string currentLabel;
    private TextMeshProUGUI myText;
    public int initializedFromStatic;

    void Awake() { myText = GetComponent<TextMeshProUGUI>(); }

    void Start() {
        if (DataManager.localization == null) {
            Debug.LogError("No localization loaded");
            return;
        }

        //Initialized from static 1 means that the LocalizableText refers to the databaseFeedback
        if (initializedFromStatic == 1) {
            ChangeLabel(DataManager.databaseFeedback);
        }

        //Initialized from static 2 means that the LocalizableText refers to the description of the current codeQuestion
        if (initializedFromStatic == 2) {
            ChangeLabel(DataManager.currentCodeQuestion.description);
        }

        //Initialized from static 3 means that the LocalizableText refers to the continue as serer or player button
        if (initializedFromStatic == 3) {
            ChangeLabel(DataManager.currentBuild);
        }

        LocalizeSelf();
    }

    /// <summary>
    /// Function to localize the current label, if the label is not found in the localization dictionary,
    /// then the string is treated as plaintext (useful for non translatable symbols like numbers for example).
    /// </summary>
    public void LocalizeSelf() {
        if(currentLabel == null) { myText.SetText("");}

        if (DataManager.localization.TryGetValue(currentLabel, out string val)) {
            myText.SetText(val);
        } else {
            myText.SetText(currentLabel);
        }
    }

    /// <summary>
    /// Utility function to change the label of this LocalizableText at runtime.
    /// </summary>
    /// <param name="newLabel">The new label.</param>
    public void ChangeLabel(string newLabel) {
        currentLabel = newLabel;
        LocalizeSelf();
    }

    /// <summary>
    /// Utility function to change the color of the text.
    /// </summary>
    /// <param name="newColor">The new color.</param>
    public void ChangeColor(Color newColor) {
        myText.color = newColor;
    }

}
