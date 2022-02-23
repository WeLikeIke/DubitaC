using UnityEngine;


/// <summary>
/// The <see cref="InvokableDataManager"/> class exists solely for the purpose of exposing public void and value parametrized functions
/// that will then called by external UI elements.
/// The functions have purposefully the same name that they have in the <see cref="DataManager"/> class.
/// </summary>
public class InvokableDataManager : MonoBehaviour {
    public void SetVolume(float value) {    DataManager.SetVolume(value);   }
    public void SetTimeout(int value) {     DataManager.SetTimeout(value);  }
    public void SetPath(string value) {     DataManager.SetPath(value);     }
    public void SetFeedback(string value) { DataManager.SetFeedback(value); }
    public void SetTimer(string value) {    DataManager.SetTimer(value);    }
}
