using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JALib.Core.Setting.GUI.Notification;

public class JANotification : MonoBehaviour {
    public TMP_Text title;
    public TMP_Text text;
    public Button closeButton;
    
    public void SetMessage(string message) {
        text.text = message;
    }
    
    public void SetTitle(string title) {
        this.title.text = title;
    }
    
    public void Close() {
        NotificationRoom.Instance.RemoveNotification(this);
    }
}