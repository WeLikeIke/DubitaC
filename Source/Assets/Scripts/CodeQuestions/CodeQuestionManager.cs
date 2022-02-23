using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class responsible for spawning and managing the codeQuestions.
/// See <see cref="CodeQuestionUI"/>.
/// </summary>
public class CodeQuestionManager : MonoBehaviour {

    public GameObject codeQuestionPrefab;
    public RectTransform codeQuestionHolder;

    private TextAsset[] codeQuestionsFiles;
    private Dictionary<string, List<codeQuestion>> tagsDictionary = new Dictionary<string, List<codeQuestion>>();

    private string selectedCodeQuestionName;
    private List<CodeQuestionUI> shownCodeQuestionUI = new List<CodeQuestionUI>();


    void Start() { 
        RetrieveQuestions(); 
        DisplayFilteredView("");
    }

    /// <summary>
    /// Function to retrieve all the possible <see cref="codeQuestion"/>s that are saved in <see cref="codeQuestionFiles"/>.
    /// </summary>
    private void RetrieveQuestions() {
        //Load
        codeQuestionsFiles = Resources.LoadAll<TextAsset>("CodeQuestions");

        foreach (TextAsset question in codeQuestionsFiles) {
            string content = question.ToString();

            //Split in parts
            string[] parts = content.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);

            //Save the name
            string label = parts[0];

            //Find the tags
            string tags = "";
            for (int i = 2; i < parts.Length; i++) {
                if (parts[i - 1] == "_tags_") {
                    tags = parts[i];
                    break;
                }
            }

            //Save the tags
            string[] tagArray = tags.ToUpper().Split(',');
            for (int i = 0; i < tagArray.Length; i++) {
                tagArray[i] = tagArray[i].Trim();
            }

            //Create the codeQuestion
            codeQuestion dictionaryEntry = new codeQuestion(question.name, label, content, tagArray);

            //Add the entries to an inverse-index dictionary
            foreach(string tag in tagArray) {
                if (tagsDictionary.ContainsKey(tag)) {
                    tagsDictionary[tag].Add(dictionaryEntry);
                } else {
                    tagsDictionary[tag] = new List<codeQuestion>() { dictionaryEntry };
                }
            }
        }

    }

    /// <summary>
    /// Utility function to filter the codeQuestions based on the tags.
    /// </summary>
    /// <param name="tags">The tags to filter by.</param>
    /// <returns>The list of codeQuestions that fulfill the tag requirements.</returns>
    private List<codeQuestion> FilterCodeQuestions(string tags) {
        tags = tags.Trim().ToUpper();

        List<codeQuestion> filtered = new List<codeQuestion>();

        if (tags.Length == 0) {
            //With no tag, display all the codeQuestions
            foreach (List<codeQuestion> val in tagsDictionary.Values) {
                foreach (codeQuestion dictionaryEntry in val) {
                    if (!filtered.Contains(dictionaryEntry)) {
                        filtered.Add(dictionaryEntry);
                    }
                }
            }
        } else { 
            string[] tagArray = tags.Split(',');
            for (int i = 0; i < tagArray.Length; i++) {
                tagArray[i] = tagArray[i].Trim();
            }

            foreach (string tag1 in tagArray) {
                //If the tag is valid
                if (tagsDictionary.ContainsKey(tag1)) {
                    //If it is the first one, add all codeQuestions with this tag
                    if (filtered.Count == 0) {
                        filtered.AddRange(tagsDictionary[tag1]);
                    } else {
                        //If it is not the first one, keep only the codeQuestions that contain both tags.
                        foreach (codeQuestion tag2 in tagsDictionary[tag1]) {
                            filtered.RemoveAll(x => Array.IndexOf(x.tags, tag1) == -1);
                        }
                    }
                }
            } 
        }

        //Sort the results alphabetically
        filtered.Sort((cq1,cq2) => cq1.name.CompareTo(cq2.name));
        return filtered;
    }

    /// <summary>
    /// Function to populate the codeQuestions to be shown.
    /// </summary>
    /// <param name="codeQuestionsToShow">The list of codeQuestions to populate.</param>
    private void PopulateQuestionView(List<codeQuestion> codeQuestionsToShow) {
        //Clean already existing display
        CleanDisplay();

        //Repopulate it with the input list
        foreach (codeQuestion question in codeQuestionsToShow) {
            GameObject codeQuestionObject = Instantiate(codeQuestionPrefab, codeQuestionHolder);
            codeQuestionObject.GetComponent<CodeQuestionUI>().Setup(this, question, (selectedCodeQuestionName == question.name));
            shownCodeQuestionUI.Add(codeQuestionObject.GetComponent<CodeQuestionUI>());
        }
    }

    /// <summary>
    /// Utility function to remove all the shown codeQuestions.
    /// </summary>
    private void CleanDisplay() {
        foreach (RectTransform child in codeQuestionHolder) {
            Destroy(child.gameObject);
        }
        shownCodeQuestionUI.Clear();
    }

    /// <summary>
    /// Function called externally to display the spawned codeQuestions according to the filter on the tags.
    /// The function is public void and with parameter value on purpose so that it could be called on an input field change.
    /// </summary>
    /// <param name="value">The tags to filter by.</param>
    public void DisplayFilteredView(string value) {
        PopulateQuestionView(FilterCodeQuestions(value));
    }

    /// <summary>
    /// External function called by <see cref="CodeQuestionUI"/>.
    /// Triggers the background change of the caller into "selected", while changing all the others into the defaul color.
    /// </summary>
    /// <param name="selectedCodeQuestion">The caller of function that wants to change its background color.</param>
    public void SelectThis(CodeQuestionUI selectedCodeQuestion) {
        selectedCodeQuestionName = selectedCodeQuestion.myCodeQuestionName;
        for (int i = 0; i < shownCodeQuestionUI.Count; i++) {
            if (shownCodeQuestionUI[i] == selectedCodeQuestion) {
                shownCodeQuestionUI[i].SetPanelColor(Cosmetics.selectedColor);
            } else {
                shownCodeQuestionUI[i].SetPanelColor(Cosmetics.buttonsColor);
            }
        }
    }

}