using JALib.Core.Setting.GUI.Notification;
using UnityEngine;

namespace JALib.Core.GUI;

public class NotificationCanvas {
    private static NotificationRoom room;
    
    public static void Initialize() {
        GameObject ob = Object.Instantiate(JABundle.JASettings);
        ob.SetActive(false);
        Object.DontDestroyOnLoad(ob);
        room = ob.GetComponent<NotificationRoom>();
    }
    
    public static void AddNotification(JANotification notification) {
        room.AddNotification(notification);
    }
    
    public static void RemoveNotification(JANotification notification) {
        room.RemoveNotification(notification);
    }

    public static NotificationInfo AddInfo(string title, string message) {
        NotificationInfo info = Object.Instantiate(JABundle.NotificationInfo, room.transform);
        info.SetTitle(title);
        info.SetMessage(message);
        room.AddNotification(info);
        return info;
    }
    
    public static NotificationWarning AddWarning(string title, string message) {
        NotificationWarning warning = Object.Instantiate(JABundle.NotificationWarning, room.transform);
        warning.SetTitle(title);
        warning.SetMessage(message);
        room.AddNotification(warning);
        return warning;
    }
    
    public static NotificationError AddError(string title, string message) {
        NotificationError error = Object.Instantiate(JABundle.NotificationError, room.transform);
        error.SetTitle(title);
        error.SetMessage(message);
        room.AddNotification(error);
        return error;
    }
}