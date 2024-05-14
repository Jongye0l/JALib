using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JALib.Core.Setting.GUI.Notification;

public class Notification : MonoBehaviour {
    public TMP_Text text;
    public Button closeButton;
    
    public void SetMessage(string message) {
        text.text = message;
    }
    
    public void Close() {
        NotificationRoom.Instance.RemoveNotification(this);
    }
}