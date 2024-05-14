using System.Collections.Generic;
using UnityEngine;

namespace JALib.Core.Setting.GUI.Notification;

public class NotificationRoom : MonoBehaviour {
    public static NotificationRoom Instance;
    public List<Notification> notifications = new();
    
    private void Awake() {
        Instance ??= this;
    }
    
    public void AddNotification(Notification notification) {
        notifications.Add(notification);
        notification.transform.SetParent(transform);
    }
    
    public void RemoveNotification(Notification notification) {
        notifications.Remove(notification);
        Destroy(notification.gameObject);
    }
}