using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class that manages the notepads, implements autocentering and Undo-Redo.
/// The maximum amount of zoom possible is stored in constant <see cref="maxAllowedZoom"/>.
/// The minimum amount of zoom possible is stored in constant <see cref="minAllowedZoom"/>.
/// The zoom granularity is stored in constant <see cref="zoomStepSize"/>.
/// The offset required to avoid that the cursor goes off screen while typing is stored in constant <see cref="typingOffset"/>.
/// </summary>
public class NotepadManager : MonoBehaviour {
    private const float maxAllowedZoom = 2f;
    private const float minAllowedZoom = 0.2f;
    private const float zoomStepSize   = 0.2f;
    private const float typingOffset   = 150f;

    public ScrollRect scrollView;
    public RectTransform content;
    public bool readOnly;

    private TMP_InputField notepad;

    private float originalHeight;
    private float originalWidth;

    private float currentZoom = 1f;

    private List<string> pastText = new List<string>();
    private List<string> futureText = new List<string>();
    private bool ignoreContentChange = false;


    void Awake() {
        notepad = GetComponent<TMP_InputField>();
        originalWidth = notepad.GetComponent<RectTransform>().rect.width;
        originalHeight = notepad.GetComponent<RectTransform>().rect.height;
    }

    void Start() {
        if (!readOnly) {
            notepad.Select();
            notepad.ActivateInputField();
        }
    }

    void Update() {
        if (notepad.isFocused) { ManageKeys(); }
    }

    /// <summary>
    /// Utility function to manage all function calls that are tied to a keyboard combination.
    /// Currently managing:
    /// Ctrl+Z -> Undo
    /// Ctrl+Y -> Redo
    /// Arrow kyes -> Move view in the <see cref="notepad"/> content following the cursor.
    /// </summary>
    private void ManageKeys() {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
            if (Input.GetKeyDown(KeyCode.Z)) { UndoText(); }
            if (Input.GetKeyDown(KeyCode.Y)) { RedoText(); }
        }

        //Moving using the arrow keys and Home/End shortcuts should behave like if the content needed to be recentered
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.RightArrow) ||
           Input.GetKey(KeyCode.Home) || Input.GetKey(KeyCode.End)) {
            ContentChanged(false);
        }
    }

    /// <summary>
    /// External function to save the current text into the stack of past texts.
    /// The function is public void and value parametrized on purpose so that it can be called from a inputfield OnValueChanged.
    /// </summary>
    /// <param name="value">The new text of the notepad.</param>
    public void DoText(string value) {

        if (!ignoreContentChange) { 
            futureText.Clear();
            pastText.Add(value);
        }

        ignoreContentChange = false;
        
    }

    /// <summary>
    /// External function to restore a previous text, only triggered by pressing Ctrl+Z.
    /// </summary>
    public void UndoText() {
        if (pastText.Count <= 1) { return; }

        //Move the last text in the past to the last text in the future
        futureText.Add(pastText[pastText.Count - 1]);
        pastText.RemoveAt(pastText.Count - 1);

        //The text on the notepad is always equal to the last in the past
        ignoreContentChange = true;
        SetSolution(pastText[pastText.Count - 1]);
    }

    /// <summary>
    /// External function to restore a previously undone text, only triggered by pressing Ctrl+Y.
    /// </summary>
    public void RedoText() {
        if (futureText.Count == 0) { return; }

        //Move the last text in the future to the last text in the past
        pastText.Add(futureText[futureText.Count - 1]);
        futureText.RemoveAt(futureText.Count - 1);

        //The text on the notepad is always equal to the last in the past
        ignoreContentChange = true;
        SetSolution(pastText[pastText.Count - 1]);

    }

    /// <summary>
    /// Getter of the content of the <see cref="notepad"/>.
    /// </summary>
    /// <returns>The current string in the notepad.</returns>
    public string GetSolution() { return notepad.text; }

    /// <summary>
    /// Setter of the content of the notepad.
    /// </summary>
    /// <param name="solution">The new string to put inside the <see cref="notepad"/>.</param>
    public void SetSolution(string solution) {
        notepad.text = solution;
        ContentChanged(true);
    }

    /// <summary>
    /// External function to set the <see cref="notepad"/> as interactable or viceversa.
    /// The function is public void and parameterless on purpose so that it can be called by a button OnClick.
    /// </summary>
    public void SwapInteractability() { notepad.interactable = !notepad.interactable; }

    /// <summary>
    /// Adjusts the size of the <see cref="notepad"/> on the text inside of it.
    /// If <see cref="notepad"/> contains more text that it can show, the scrollbars will activate.
    /// It also automatically calls <see cref="MoveView(bool, bool, bool)"/> so that the content is centered where the text is being modified.
    /// The function is public void and single parameter so it can be called by the inputfield OnValueChanged.
    /// </summary>
    /// <param name="focusOnBeginning">true if the view should be set at the beginning, false otherwise.</param>
    public void ContentChanged(bool focusOnBeginning) {
        Vector2 newSize = notepad.textComponent.GetPreferredValues(notepad.text);

        bool verticalMove   = ExpandNotepadHeight(newSize.y);
        bool horizontalMove = ExpandNotepadWidth( newSize.x);

        MoveView(focusOnBeginning, verticalMove, horizontalMove);
    }

    /// <summary>
    /// Utility function to expand the <see cref="notepad"/> only if the content is bigger than the original available space.
    /// </summary>
    /// <param name="preferredWidth">New preferred width based on the content.</param>
    /// <returns>true if the width has been expanded, false otherwise.</returns>
    private bool ExpandNotepadWidth(float preferredWidth) {

        float originalZoomedWidth = originalWidth / currentZoom;
        float finalPreferredWidth = preferredWidth + typingOffset / currentZoom; 

        float newWidth = Mathf.Max(originalZoomedWidth, finalPreferredWidth);
        
        notepad.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);

        return (newWidth > originalZoomedWidth);
    }

    /// <summary>
    /// Utility function to expand the <see cref="notepad"/> only if the content is bigger than the original available space.
    /// </summary>
    /// <param name="preferredHeight">New preferred height based on the content.</param>
    /// <returns>true if the height has been expanded, false otherwise.</returns>
    private bool ExpandNotepadHeight(float preferredHeight) {

        float originalZoomedHeight = originalHeight / currentZoom;
        float finalPreferredHeight = preferredHeight + typingOffset / currentZoom;

        float newHeight = Mathf.Max(originalZoomedHeight, finalPreferredHeight);

        notepad.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);

        return newHeight > (originalHeight / currentZoom);
    }

    /// <summary>
    /// Utility function to correctly center the scrollbars on the current caret position.
    /// </summary>
    /// <param name="focusOnBeginning">true if the scrollbars should be at the beginning, false if they should be dynamic.</param>
    /// <param name="verticalMove">true, if the vertical scrollbar should move, false if it should be locked.</param>
    /// <param name="horizontalMove">true, if the horizontal scrollbar should move, false if it should be locked.</param>
    private void MoveView(bool focusOnBeginning, bool verticalMove, bool horizontalMove) {
        //If both axis are locked, nothing can be done
        if (!verticalMove && !horizontalMove) { return; }

        //The beginning is static so no need for further calculations
        if (focusOnBeginning) {
            MoveScrollbars(1f, 0f);
            return;
        }

        (float vertical, float horizontal) = CalculateCursorCenteringOffsets();

        if (verticalMove) {   MoveScrollbars(vertical, null);   }
        if (horizontalMove) { MoveScrollbars(null, horizontal); }

        
    }

    /// <summary>
    /// Utility function to calculate the percentage of screen that the cursor in a text is curerntly occupying.
    /// </summary>
    /// <returns>Tuple containing the vertical normalized and horizontal normalized scrollbar position.</returns>
    private (float, float) CalculateCursorCenteringOffsets() {
        int caretPos = notepad.caretPosition;
        string[] lines = notepad.text.Split('\n');

        int longestLine = 0;
        int caretLine = 0;
        bool caretNeeded = true;

        for (int i = 0; i < lines.Length; i++) {
            if (lines[i].Length > longestLine) {
                longestLine = lines[i].Length;
                if (i == lines.Length - 1) { longestLine -= 1; }
            }

            if (caretNeeded) {
                if (caretPos >= lines[i].Length + 1) {
                    caretPos -= (lines[i].Length + 1);
                } else {
                    caretLine = i;
                    caretNeeded = false;
                }
            }
        }
        longestLine += 1;

        float verticalOffset   = (float)(lines.Length - caretLine) / lines.Length;
        float horizontalOffset = (float) caretPos / longestLine;

        return (verticalOffset, horizontalOffset);
    }

    /// <summary>
    /// Utility function to move the scrollbars to the given normalized positions.
    /// </summary>
    /// <param name="vertical">The new vertical normalized position, or null if no change.</param>
    /// <param name="horizontal">The new horizontal normalized position, or null if no change.</param>
    private void MoveScrollbars(float? vertical, float? horizontal) {
        if(vertical != null)   { scrollView.verticalNormalizedPosition   = (float) vertical;   }
        if(horizontal != null) { scrollView.horizontalNormalizedPosition = (float) horizontal; }
    }

    /// <summary>
    /// Increases the zoom on of the notepad to a maximum of 200%.
    /// The function is public void and parameterless on purpose so that it can be called by a button OnClick.
    /// </summary>
    public void ZoomIn() { Zoom(Mathf.Min(maxAllowedZoom, currentZoom + zoomStepSize)); }

    /// <summary>
    /// Decreases the zoom on of the notepad to a minimum of 20%.
    /// The function is public void and parameterless on purpose so that it can be called by a button OnClick.
    /// </summary>
    public void ZoomOut() { Zoom(Mathf.Max(minAllowedZoom, currentZoom - zoomStepSize)); }

    /// <summary>
    /// Utility function to zoom in or out of the notepad.
    /// It is achieved by scaling the content GameObject on both axis.
    /// It also automatically invokes <see cref="ContentChanged(bool)"/> since the size of the content has to be recalculated.
    /// </summary>
    /// <param name="newZoom">New zoom level.</param>
    private void Zoom(float newZoom) {
        content.localScale = new Vector3(newZoom, newZoom, 1f);

        currentZoom = newZoom;

        ContentChanged(true);
    }
}
