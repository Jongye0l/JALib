using System;
using System.Collections.Generic;
using System.Linq;
using JALib.API;
using JALib.API.Packets;
using JALib.Core;
using JALib.Stream;
using UnityEngine;
using UnityModManagerNet;
using Object = UnityEngine.Object;

namespace JALib.Tools;

public static class ErrorUtils {
    private static string ErrorMessage => $"\n{JALib.Instance.Localization["ErrorUtils.ErrorMessage"]} ";
    private static Dictionary<Exception, byte> exceptions;
    private static ErrorCanvas errorCanvas;
    private static JAMod showingMod;
    private static Exception showingException;

    internal static void OnAdofaiStart() {
        try {
            GameObject errorObject = Object.Instantiate(RDConstants.data.prefab_errorCanvas);
            errorCanvas = errorObject.GetComponent<ErrorCanvas>();
            errorCanvas.btnSupport.onClick.RemoveAllListeners();
            errorCanvas.btnLog.onClick.RemoveAllListeners();
            errorCanvas.btnSubmit.onClick.RemoveAllListeners();
            errorCanvas.btnIgnore.onClick.RemoveAllListeners();
            errorCanvas.btnSupport.onClick.AddListener(BtnSupport);
            errorCanvas.btnLog.onClick.AddListener(BtnLog);
            errorCanvas.btnIgnore.onClick.AddListener(BtnIgnore);
            errorCanvas.txtFaq = null;
            errorCanvas.txtDiscord = null;
            errorCanvas.txtSteam = null;
            errorCanvas.txtGoBack = null;
            errorCanvas.btnBack = null;
            Object.Destroy(errorCanvas.supportPagesPanel);
            errorCanvas.supportPagesPanel = null;
            errorCanvas.gameObject.SetActive(false);
            Object.DontDestroyOnLoad(errorObject);
            exceptions = new Dictionary<Exception, byte>();
            if(showingException != null) ShowError0(null, showingException);
        } catch (Exception e) {
            JALib.Instance.Error("An error occurred while initializing the error canvas.");
            JALib.Instance.LogException(e);
        }
    }

    public static void ShowError(JAMod jaMod, Exception exception) {
        MainThread.Run(new JAction(jaMod, () => ShowError0(jaMod, exception)));
    }
    
    private static void ShowError0(JAMod jaMod, Exception exception) {
        try {
            if(!errorCanvas) {
                showingException = exception;
                showingMod = jaMod;
                return;
            }
            if(!CheckException(exception)) return;
            showingException = exception;
            showingMod = jaMod;
            JALocalization localization = JALib.Instance.Localization;
            errorCanvas.txtTitle.text = string.Format(localization["ErrorUtils.ErrorTitle"], jaMod.Name);
            errorCanvas.btnSubmit.gameObject.SetActive(false);
            if(UnityModManager.HasNetworkConnection()) {
                if(JApi.Connected) {
                    errorCanvas.txtErrorMessage.text = string.Format(localization["ErrorUtils.FoundError"]) + ErrorMessage + exception;
                    JApi.Send(new ExceptionInfo(jaMod, exception));
                } else errorCanvas.txtErrorMessage.text = string.Format(localization["ErrorUtils.ServerDisconnect"]) + ErrorMessage + exception;
            } else errorCanvas.txtErrorMessage.text = string.Format(localization["ErrorUtils.InternetDisconnect"]) + ErrorMessage + exception;
            errorCanvas.txtSupportPages.text = localization["ErrorUtils.GoDiscord"];
            errorCanvas.gameObject.SetActive(true);
        } catch (Exception e) {
            JALib.Instance.Error("An error occurred while showing an error.");
            JALib.Instance.LogException(e);
        }
    }
    
    private static bool CheckException(Exception exception) {
        foreach(Exception exceptionsKey in exceptions.Keys.Where(exceptionsKey => exceptionsKey.Message == exception.Message && exceptionsKey.StackTrace == exception.StackTrace)) {
            exceptions[exceptionsKey] = 0;
            return false;
        }
        exceptions[exception] = 0;
        return true;
    }

    internal static void ErrorInfo(byte[] data) {
        using ByteArrayDataInput input = new(data, JALib.Instance);
        if(showingException.GetHashCode() != input.ReadInt()) return;
        JALocalization localization = JALib.Instance.Localization;
        MainThread.Run(new JAction(JALib.Instance, () => {
            switch(input.ReadByte()) {
                case 0: // Report: Unknown error
                    SetupReport(localization);
                    errorCanvas.txtErrorMessage.text = string.Format(localization["ErrorUtils.UnknownError"]) + ErrorMessage + showingException;
                    break;
                case 1: // Report: Need more report
                    SetupReport(localization);
                    errorCanvas.txtErrorMessage.text = string.Format(localization["ErrorUtils.NeedMoreReport"]) + ErrorMessage + showingException;
                    break;
                case 2: // Report: Custom Message with exception message
                    SetupReport(localization);
                    errorCanvas.txtErrorMessage.text = input.ReadUTF() + ErrorMessage + showingException;
                    break;
                case 3: // Report: Custom Message
                    SetupReport(localization);
                    errorCanvas.txtErrorMessage.text = input.ReadUTF() + ErrorMessage + showingException;
                    break;
                case 4: // Report: Custom Localization with exception message
                    SetupReport(localization);
                    errorCanvas.txtErrorMessage.text = localization[input.ReadUTF()] + ErrorMessage + showingException;
                    break;
                case 5: // Report: Custom Localization
                    SetupReport(localization);
                    errorCanvas.txtErrorMessage.text = localization[input.ReadUTF()];
                    return;
                case 6: // Update: Not Message
                    SetupUpdate(localization);
                    errorCanvas.txtErrorMessage.text = string.Format(localization["ErrorUtils.NeedUpdate"]);
                    break;
                case 7: // Update: Custom Message with exception message
                    SetupUpdate(localization);
                    errorCanvas.txtErrorMessage.text = input.ReadUTF() + ErrorMessage + showingException;
                    break;
                case 8: // Update: Custom Message
                    SetupUpdate(localization);
                    errorCanvas.txtErrorMessage.text = input.ReadUTF();
                    break;
                case 9: // Update: Custom Localization with exception message
                    SetupUpdate(localization);
                    errorCanvas.txtErrorMessage.text = localization[input.ReadUTF()] + ErrorMessage + showingException;
                    break;
                case 10: // Update: Custom Localization
                    SetupUpdate(localization);
                    errorCanvas.txtErrorMessage.text = localization[input.ReadUTF()];
                    break;
                case 11: // Disable Feature: Not Message
                    SetupDisableFeature(localization, input.ReadUTF());
                    errorCanvas.txtErrorMessage.text = string.Format(localization["ErrorUtils.NeedDisableFeature"]);
                    break;
                case 12: // Disable Feature: Custom Message with exception message
                    SetupDisableFeature(localization, input.ReadUTF());
                    errorCanvas.txtErrorMessage.text = input.ReadUTF() + ErrorMessage + showingException;
                    break;
                case 13: // Disable Feature: Custom Message
                    SetupDisableFeature(localization, input.ReadUTF());
                    errorCanvas.txtErrorMessage.text = input.ReadUTF();
                    break;
                case 14: // Disable Feature: Custom Localization with exception message
                    SetupDisableFeature(localization, input.ReadUTF());
                    errorCanvas.txtErrorMessage.text = localization[input.ReadUTF()] + ErrorMessage + showingException;
                    break;
                case 15: // Disable Feature: Custom Localization
                    SetupDisableFeature(localization, input.ReadUTF());
                    errorCanvas.txtErrorMessage.text = localization[input.ReadUTF()];
                    break;
                case 16: // Disable Mod: Not Message
                    SetupDisableMod(localization);
                    errorCanvas.txtErrorMessage.text = string.Format(localization["ErrorUtils.NeedDisableMod"]);
                    break;
                case 17: // Disable Mod: Custom Message with exception message
                    SetupDisableMod(localization);
                    errorCanvas.txtErrorMessage.text = input.ReadUTF() + ErrorMessage + showingException;
                    break;
                case 18: // Disable Mod: Custom Message
                    SetupDisableMod(localization);
                    errorCanvas.txtErrorMessage.text = input.ReadUTF();
                    break;
                case 19: // Disable Mod: Custom Localization with exception message
                    SetupDisableMod(localization);
                    errorCanvas.txtErrorMessage.text = localization[input.ReadUTF()] + ErrorMessage + showingException;
                    break;
                case 20: // Disable Mod: Custom Localization
                    SetupDisableMod(localization);
                    errorCanvas.txtErrorMessage.text = localization[input.ReadUTF()];
                    break;
                case 21: // Open URL: Not Message
                    SetupOpenURL(localization, input.ReadUTF());
                    errorCanvas.txtErrorMessage.text = string.Format(localization["ErrorUtils.NeedMoreReportURL"]);
                    break;
                case 22: // Open URL: Custom Message with exception message
                    SetupOpenURL(localization, input.ReadUTF());
                    errorCanvas.txtErrorMessage.text = input.ReadUTF() + ErrorMessage + showingException;
                    break;
                case 23: // Open URL: Custom Message
                    SetupOpenURL(localization, input.ReadUTF());
                    errorCanvas.txtErrorMessage.text = input.ReadUTF();
                    break;
                case 24: // Open URL: Custom Localization with exception message
                    SetupOpenURL(localization, input.ReadUTF());
                    errorCanvas.txtErrorMessage.text = localization[input.ReadUTF()] + ErrorMessage + showingException;
                    break;
                case 25: // Open URL: Custom Localization
                    SetupOpenURL(localization, input.ReadUTF());
                    errorCanvas.txtErrorMessage.text = localization[input.ReadUTF()];
                    break;
                case 26: // Close: Custom Message with exception message
                    SetupClose(localization);
                    errorCanvas.txtErrorMessage.text = input.ReadUTF() + ErrorMessage + showingException;
                    break;
                case 27: // Close: Custom Message
                    SetupClose(localization);
                    errorCanvas.txtErrorMessage.text = input.ReadUTF();
                    break;
                case 28: // Close: Custom Localization with exception message
                    SetupClose(localization);
                    errorCanvas.txtErrorMessage.text = localization[input.ReadUTF()] + ErrorMessage + showingException;
                    break;
                case 29: // Close: Custom Localization
                    SetupClose(localization);
                    errorCanvas.txtErrorMessage.text = localization[input.ReadUTF()];
                    break;
            }
        }));
        return;

        static void SetupReport(JALocalization localization) {
            errorCanvas.txtSubmit.text = localization["ErrorUtils.ErrorReport"];
            errorCanvas.btnSubmit.onClick.AddListener(SubmitException);
            errorCanvas.btnSubmit.gameObject.SetActive(true);
        }

        static void SetupUpdate(JALocalization localization) {
            errorCanvas.txtSubmit.text = localization["ErrorUtils.Update"];
            errorCanvas.btnSubmit.onClick.AddListener(UpdateMod);
            errorCanvas.btnSubmit.gameObject.SetActive(true);
        }
        
        static void SetupDisableFeature(JALocalization localization, string featureName) {
            errorCanvas.txtSubmit.text = localization["ErrorUtils.DisableFeature"];
            errorCanvas.btnSubmit.onClick.AddListener(() => DisableFeature(featureName));
            errorCanvas.btnSubmit.gameObject.SetActive(true);
        }
        
        static void SetupDisableMod(JALocalization localization) {
            errorCanvas.txtSubmit.text = localization["ErrorUtils.DisableMod"];
            errorCanvas.btnSubmit.onClick.AddListener(DisableMod);
            errorCanvas.btnSubmit.gameObject.SetActive(true);
        }

        static void SetupOpenURL(JALocalization localization, string url) {
            errorCanvas.txtSubmit.text = localization["ErrorUtils.OpenURL"];
            errorCanvas.btnSubmit.onClick.AddListener(() => Application.OpenURL(url));
            errorCanvas.btnSubmit.gameObject.SetActive(true);
        }

        static void SetupClose(JALocalization localization) {
            errorCanvas.txtSubmit.text = localization["ErrorUtils.Close"];
            errorCanvas.btnSubmit.onClick.AddListener(BtnIgnore);
            errorCanvas.btnSubmit.gameObject.SetActive(true);
        }
    }

    internal static void ReportComplete(int hashCode, string url) {
        if(showingException.GetHashCode() != hashCode) return;
        JALocalization localization = JALib.Instance.Localization;
        MainThread.Run(new JAction(JALib.Instance, () => {
            errorCanvas.txtTitle.text = localization["ErrorUtils.ReportCompleteTitle"];
            errorCanvas.txtErrorMessage.text = localization["ErrorUtils.ReportComplete"];
            errorCanvas.txtSubmit.text = localization["ErrorUtils.GoMessage"];
            errorCanvas.btnSubmit.onClick.AddListener(() => Application.OpenURL(url));
            errorCanvas.btnSubmit.gameObject.SetActive(true);
        }));
    }

    internal static void ReportFail(int hashCode) {
        if(showingException.GetHashCode() != hashCode) return;
        JALocalization localization = JALib.Instance.Localization;
        MainThread.Run(new JAction(JALib.Instance, () => {
            errorCanvas.txtTitle.text = localization["ErrorUtils.ReportFailTitle"];
            errorCanvas.txtErrorMessage.text = localization["ErrorUtils.ReportFail"];
        }));
    }

    private static void BtnSupport() => Application.OpenURL(showingMod.Discord);

    private static void BtnLog() => RDEditorUtils.OpenLogDirectory();
    
    private static void SubmitException() {
        string reporting = JALib.Instance.Localization["ErrorUtils.Reporting"];
        errorCanvas.txtTitle.text = reporting;
        errorCanvas.txtErrorMessage.text = reporting;
        errorCanvas.btnSubmit.gameObject.SetActive(false);
        JApi.Send(new ExceptionReport(showingMod, showingException));
    }

    private static void UpdateMod() {
        JAWebApi.DownloadMod(showingMod, true);
        BtnIgnore();
    }

    private static void DisableFeature(string featureName) {
        showingMod.Features.Find(feature => feature.Name == featureName).Disable();
        BtnIgnore();
    }
    
    private static void DisableMod() {
        showingMod.Disable();
        BtnIgnore();
    }
    
    private static void BtnIgnore() {
        errorCanvas.gameObject.SetActive(false);
        showingException = null;
        showingMod = null;
    }

    internal static void Dispose() {
        if(errorCanvas) Object.DestroyImmediate(errorCanvas);
        errorCanvas = null;
        exceptions = null;
        showingException = null;
        showingMod = null;
    }

    internal static void OnUpdate() {
        if(exceptions == null) return;
        List<Exception> keys = new(exceptions.Keys);
        foreach(Exception exception in keys.Where(exception => exceptions[exception]++ >= 120)) exceptions.Remove(exception);
    }
}