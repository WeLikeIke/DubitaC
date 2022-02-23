using System.Net;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using Unity.Netcode.Transports.UNET;

/// <summary>
/// Class to manage the retieval of the local Ipv4 address and the correct setup
/// of the connection address for server and clients so that they may communicate.
/// </summary>
public class IpManager : MonoBehaviour{
    private const string localhost = "127.0.0.1";


    public TextMeshProUGUI currentAddress;
    public TMP_InputField ipField;
    public LocalizableText feedbackText;
    public Button playButton;


    void Start() {
        string localIpv4 = FindLocalIpv4();

        SetServerIpv4(localIpv4);
        currentAddress.SetText(localIpv4); 
    }

    /// <summary>
    /// Set the ipv4 address of the online connection to the given address.
    /// </summary>
    /// <param name="ipv4Address">The valid ipv4 address of the server.</param>
    private void SetServerIpv4(string ipv4Address) {
        NetworkManager.Singleton.gameObject.GetComponent<UNetTransport>().ConnectAddress = ipv4Address;
    }

    /// <summary>
    /// After having inserted an Ip address.
    /// If it is valid, it becomes the new connection address for the client.
    /// If it is invalid, we fallback to localhost.
    /// </summary>
    public void IpSelection() {
        string ipString = ipField.text.Trim();

        if (ValidateIpAddress(ipString)) {
            SetServerIpv4(ipString);

            playButton.interactable = true;
            feedbackText.ChangeLabel("_selected_ip_address");
            feedbackText.ChangeColor(Cosmetics.correctColor);
        } else {
            playButton.interactable = false;
            feedbackText.ChangeLabel("_bad_ip_address");
            feedbackText.ChangeColor(Cosmetics.wrongColor);
        }
    }


    /// <summary>
    /// Utility function to check if a string contains a valid (and complete) Ipv4 address.
    /// Valid and complete refers to 4 numbers between 0 and 255 separated by one dot '.' each.
    /// </summary>
    /// <param name="ipString">The string to check for a valid Ipv4 address</param>
    /// <returns>true if the string contains a valid and complete Ipv4 address, false otherwise.</returns>
    private bool ValidateIpAddress(string ipString) {
        string[] arr = ipString.Split('.');
        if(arr.Length != 4) { return false; }

        for(int i = 0; i < arr.Length; i++) {
            if (arr[i].Length > 3 || !byte.TryParse(arr[i], out _)) { return false; }
        }
        return true;
    }

    /// <summary>
    /// Utility function to obtain the local Ipv4 address of the machine.
    /// </summary>
    /// <returns>The string containing the local Ipv4 address, or localhost if none were found.</returns>
    private string FindLocalIpv4() {
        foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList) {
            if(address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                return address.ToString();
            }
        }
        return localhost;
    }
}
