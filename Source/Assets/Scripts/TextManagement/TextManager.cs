using System;
using System.IO;
using TMPro;
using UnityEngine;
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
    public LocalizableText codeQuestionHintText;
    public LocalizableText codeQuestionGoalText;

    #endregion

    #region HintsVar
    public GameObject hintsPanel;
    public GameObject hintBoxPrefab;
    public TextMeshProUGUI startingPrize;
    public Button buyHintButton;
    private string[] hints;
    private int nextHint = 0;

    #endregion

    #region LocalizationVar
    public int languageIdx = 0;
    public Sprite[] languageSprites;
    public Image languageFlag;
    public LocalizableText[] mainMenuVisibleText;

    #endregion

    #region TutorialVar
    public int tutorialIdx = 0;
    public Texture[] tutorialTextures;
    public RawImage tutorialImage;

    private readonly string[] tutorialLabels = new string[6] { "_tutorial_0",  "_tutorial_1", "_tutorial_2", "_tutorial_3", "_tutorial_4", "_tutorial_5" };
    public LocalizableText tutorialText;

    #endregion

    public void Awake() { DataManager.LoadDict(); }

    public void Start() {
        if (hintsPanel != null) { hintsPanel.SetActive(false); }

        if (selectCppsPath != null) {
            Directory.CreateDirectory(DataManager.currentPath);
            selectCppsPath.text = DataManager.currentPath;
        }

        if (languageFlag != null) {
            languageIdx = GetLanguageIdx();
            UpdateLanguage();
        }

        if(tutorialImage != null) {
            tutorialImage.texture = tutorialTextures[0];
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
        tutorialImage.texture = tutorialTextures[tutorialIdx];
        tutorialText.ChangeLabel(tutorialLabels[tutorialIdx]);
    }

    /// <summary>
    /// Reads the <see cref="DataManager.currentLanguage"/> and returns its index.
    /// </summary>
    /// <returns>Returns the index in the <see cref="languageSprites"/> array of the current language</returns>
    public int GetLanguageIdx() {
        string languageName = DataManager.currentLanguage;

        for(int i = 0; i < languageSprites.Length; i++) {
            if (languageSprites[i].name.Split('_')[0] == languageName) {
                return i;
            }
        }

        //Should never happen, since we get the language from the DataManager
        Debug.LogError("Error, unable to retieve the Sprite corresponding to the language: " + languageName);
        return -1;
    }

    /// <summary>
    /// Executes all necessary steps for a correct language selection:
    /// Updates the flag <see cref="Sprite"/>, Updates the language in the static class <see cref="DataManager"/>,
    /// Triggers a reloading of the new dictionary and forces already spawned <see cref="LocalizableText"/> to relocalize.
    /// </summary>
    public void UpdateLanguage() {
        //Show the correct flag 
        languageFlag.sprite = languageSprites[languageIdx];

        //Make sure that the static class knows then new preference
        DataManager.currentLanguage = languageSprites[languageIdx].name.Split('_')[0];

        //Trigger the loading of the new dictionary
        DataManager.LoadDict();
        
        //Force the localization on all LocalizableText that already run their Start method
        foreach (LocalizableText t in mainMenuVisibleText) {
            t.LocalizeSelf();
        }
    }

    /// <summary>
    /// Changes the current language by increasing <see cref="languageIdx"/> and then calling <see cref="UpdateLanguage"/>
    /// </summary>
    public void ChangeLanguage() {
        languageIdx = (languageIdx + 1) % languageSprites.Length;
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
        startingPrize.SetText(DataManager.myData.points.ToString());
    }

}
