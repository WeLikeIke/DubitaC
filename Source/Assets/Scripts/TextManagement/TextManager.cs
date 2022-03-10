using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;

/// <summary>
/// Class to manage text in general.
/// Maintains: localization initialization and visuals, hints initializaion and visuals, 
/// tutorial initialization and visuals, codeQuestion visuals and the log panel.
/// </summary>
public class TextManager : MonoBehaviour {

    public TextMeshProUGUI log;
    public TMP_InputField selectCppsPath;

    #region CodeQuestionVar
    public string codeQuestionLabel;
    public LocalizeStringEvent codeQuestionHintText;
    public LocalizeStringEvent codeQuestionGoalText;

    #endregion

    #region HintsVar
    public GameObject hintsPanel;
    public GameObject hintBoxPrefab;
    public LocalizeStringEvent startingPrize;
    public Button buyHintButton;
    private string[] hints;
    private int nextHint = 0;

    #endregion

    #region LocalizationVar
    public int languageIdx = 0;
    public LocalizeStringEvent feedbackText;

    #endregion

    #region TutorialVar
    public int tutorialIdx = 0;
    public Texture[] tutorialTextures;
    public RawImage tutorialImage;

    private readonly string[] tutorialLabels = new string[6] { "_tutorial_0",  "_tutorial_1", "_tutorial_2", "_tutorial_3", "_tutorial_4", "_tutorial_5" };
    public LocalizeStringEvent tutorialText;

    #endregion


    public void Start() {
        //Make sure to have the correct locale loaded for the localization
        languageIdx = DataManager.currentLanguage;
        UpdateLanguage();

        if (selectCppsPath != null) {
            Directory.CreateDirectory(DataManager.currentPath);
            selectCppsPath.text = DataManager.currentPath;
        }

        if (hintsPanel != null) { hintsPanel.SetActive(false); }

        if (tutorialImage != null) {
            tutorialImage.texture = tutorialTextures[0];
        }

        if (feedbackText != null && !string.IsNullOrEmpty(DataManager.databaseFeedback)) {
            feedbackText.StringReference.SetReference("Strings",DataManager.databaseFeedback);
        }

        if(codeQuestionGoalText != null) {
            codeQuestionGoalText.StringReference.SetReference("Strings", DataManager.currentCodeQuestion.description);
        }

        if (codeQuestionHintText != null) {
            codeQuestionHintText.StringReference.SetReference("Strings", DataManager.currentCodeQuestion.description);
        }

    }


    /// <summary>
    /// Function called externally by buttons, pressing the left one will decrease the tutorialIdx, the right one will increase it,
    /// then the tutorial data is updated using <see cref="UpdateTutorial"/>.
    /// </summary>
    /// <param name="dir">A negative number to reduce the tutorialIdx, a positive one to increase it.</param>
    public void ChangeTutorial(int dir) {
        dir = Math.Sign(dir);
        tutorialIdx = (tutorialTextures.Length + tutorialIdx + dir) % tutorialTextures.Length;
        UpdateTutorial();
    }

    /// <summary>
    /// Utility function to update the tutorial <see cref="RawImage"/> to the correct one following <see cref="tutorialIdx"/>.
    /// </summary>
    private void UpdateTutorial() {
        //Update texture
        tutorialImage.texture = tutorialTextures[tutorialIdx];

        //Update description text
        tutorialText.StringReference.SetReference("Strings", tutorialLabels[tutorialIdx]);
    }

    /// <summary>
    /// Loads the correct locale for the localization using <see cref="languageIdx"/>;
    /// </summary>
    public void UpdateLanguage() {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[languageIdx];
    }

    /// <summary>
    /// Changes the current language by increasing <see cref="languageIdx"/> and then calling <see cref="UpdateLanguage"/>
    /// </summary>
    public void ChangeLanguage() {
        languageIdx = (languageIdx + 1) % LocalizationSettings.AvailableLocales.Locales.Count;
        DataManager.currentLanguage = languageIdx;
        UpdateLanguage();
    }

    /// <summary>
    /// Adds a message to the log, it implicitly adds a 2 newlines after it.
    /// </summary>
    /// <param name="message">Message to add to the log<./param>
    public void AddToLog(string message) {
        string oldText = log.text;
        log.SetText(oldText + message + "\n\n");
    }

    /// <summary>
    /// External function to change the path of the permanent cpps and to keep the <see cref="TMP_InputField"/> synchronized with the variable.
    /// The function is public void and with a value parameter on purpose so that it could be called externally by an <see cref="TMP_InputField"/> OnEndEdit.
    /// </summary>
    /// <param name="value">New path inserted.</param>
    public void SelectCurrentPath(string value) {
        if (Directory.Exists(value)) { DataManager.SetPath(value); }
        selectCppsPath.text = DataManager.currentPath;
    }

    /// <summary>
    /// Function called externally to pass the hints that have been extracted in the ExecutionManager.
    /// </summary>
    /// <param name="hintsFromExecutionManager">The hint labels in a string array.</param>
    public void PassHints(string[] hintsFromExecutionManager) {hints = hintsFromExecutionManager;}

    /// <summary>
    /// Function called externally to pass the codeQuestion request that has been extracted in the ExecutionManager.
    /// </summary>
    /// <param name="label">The label to display (before localization).</param>
    public void PassLabel(string label) { codeQuestionLabel = label; }

    /// <summary>
    /// Function called externally by a button click (from the "Buy" button).
    /// Creates a new HintBox prefab carrying all the information of a <see cref="HintBox"/>,
    /// additionally disables the buyHintButton if there are no more hints for this codeQuestion
    /// and updates the Client data appropriately for the final scoring.
    /// </summary>
    /// <param name="fromHintShop">The GameObject representing the HintShop.</param>
    public void CreateHintBox(GameObject fromHintShop) {

        GameObject hintBox = Instantiate(hintBoxPrefab, fromHintShop.transform.parent);
        hintBox.transform.SetSiblingIndex(fromHintShop.transform.GetSiblingIndex());
        hintBox.GetComponent<HintBox>().Setup(nextHint + 1, hints[nextHint]);
        nextHint += 1;
        if (nextHint == hints.Length) {
            buyHintButton.interactable = false;
        }

        DataManager.UpdateClientPoints();

        ((UShortVariable)startingPrize.StringReference["points"]).Value = DataManager.myData.points;
        startingPrize.RefreshString();
    }

}
