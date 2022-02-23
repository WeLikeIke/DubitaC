using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Prefab class managing the details of a codeQuestion and exposing the functions to interact with it.
/// </summary>
public class CodeQuestionUI : MonoBehaviour{

    public string myCodeQuestionName;
    public Image backgroundPanel;
    public Button selectionButton;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI tagsText;
    
    void Start() {TryWrapperSetup();}

    /// <summary>
    /// Function to initialize a codeQuestion prefab, it takes care of the UI visuals and button OnClick delegate.
    /// </summary>
    /// <param name="CQM">CodeQuestionManager that spawned the prefab..</param>
    /// <param name="myQuestion">codeQuestion associated with this prefab.</param>
    /// <param name="spawnSelected">boolean to decide if the background color should be selected or selectable.</param>
    public void Setup(CodeQuestionManager CQM, codeQuestion myQuestion, bool spawnSelected) {

        myCodeQuestionName = myQuestion.name;
        nameText.SetText(myQuestion.name);
        tagsText.SetText(string.Join("<br>", myQuestion.tags));

        //If it has been previously selected, the background should have the selected color
        if (spawnSelected) { SetPanelColor(Cosmetics.selectedColor); }

        //By clicking on the prefab, the server saves statically which codeQuestion is selected
        selectionButton.onClick.AddListener(
            delegate {
                DataManager.currentCodeQuestion = myQuestion;
                CQM.SelectThis(this);
            });

    }

    /// <summary>
    /// Utility function to add an additional delegate to the codeQuestion button.
    /// Only in scenes where there is a <see cref="NetworkWrapper"/>, does nothing otherwise.
    /// </summary>
    private void TryWrapperSetup() {
        GameObject networkWrapperObject = GameObject.FindWithTag("wrapper");
        if (networkWrapperObject == null) { return; }
        selectionButton.onClick.AddListener(
            delegate {
                networkWrapperObject.GetComponent<NetworkWrapper>().RequirementsCheck(0);
            });
    }


    /// <summary>
    /// Utility function to change the color of the background to a given <see cref="Color"/>.
    /// </summary>
    /// <param name="c">Color to change the background to.</param>
    public void SetPanelColor(Color c) { backgroundPanel.color = c;}

}
