using TMPro;
using UnityEngine;

/// <summary>
/// Class exclusively attached to a HintBox prefab.
/// Can be updated externally using <see cref="Setup"/>.
/// </summary>
public class HintBox : MonoBehaviour {

    public TextMeshProUGUI hintHead;
    public LocalizableText hintBody;

    /// <summary>
    /// Sets the 2 texts of an hint: the number at the top and the label of the body.
    /// </summary>
    /// <param name="head">Number of the hint, from 0 to how many are present in the codeQuestion.</param>
    /// <param name="body">Label to be assigned to the hint so that it will get localized at runtime.</param>
    public void Setup(int head, string body) {
        hintHead.SetText(head.ToString());
        hintBody.ChangeLabel(body);
    }

}