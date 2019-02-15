// Manual https://docs.google.com/document/d/1s9Rj8qpaVihMYYPUzgfTG2DK7ZnapthX8sTVnGAga5A/

using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class RemoteLogger : MonoBehaviour
{
    public static RemoteLogger instance;

    static bool isEnable = false;

    public static void Enable()
    {
        if (isEnable) return;

        isEnable = true;

        PlayerPrefs.SetInt("remoteLogger", 1);

        Debug.Log("Remote Logger - Enabled");

#if UNITY_5
        Application.logMessageReceived += instance.HandleLog;
#else
        Application.RegisterLogCallback(instance.HandleLog);
#endif

        instance.StartCoroutine(instance.WaitForRequest());
    }

    public static void Disable()
    {
        isEnable = false;

        PlayerPrefs.SetInt("remoteLogger", 0);

#if UNITY_5
        Application.logMessageReceived -= instance.HandleLog;
#else
        Application.RegisterLogCallback(null);
#endif

        instance.StopAllCoroutines();
        Debug.Log("Remote Logger - Disabled");
    }

    public string googleScriptUrl; // https://script.google.com/macros/s/YYYY/exec

    Queue<string> logs = new Queue<string>();
    const int maxParams = 10;

    void Awake()
    {
        instance = this;

        if (string.IsNullOrEmpty(googleScriptUrl)) Debug.LogError("Remote Logger - Error: Google Script Url is empty");

        // if (Debug.isDebugBuild) RemoteLogger.Enable();

        // if (DateTime.Now.Year == 1985) RemoteLogger.Enable();
        // else if (DateTime.Now.Year == 1990) RemoteLogger.Disable();

        if (PlayerPrefs.GetInt("remoteLogger", 0) == 1) RemoteLogger.Enable();
    }

    const string warning = "Warning: ";
    const string error = "Error: ";
    const string exception = "Exception: ";
    const string n = "\n";
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Log) logs.Enqueue(logString);
        else if (type == LogType.Warning) logs.Enqueue(warning + logString);
        else if (type == LogType.Error) logs.Enqueue(error + logString + n + stackTrace);
        else if (type == LogType.Exception) logs.Enqueue(exception + logString + n + stackTrace);
    }

    const string deviceId = "?deviceId=";
    const string p0 = "&p0=";
    const string p = "&p";
    const string equal = "=";
    IEnumerator WaitForRequest()
    {
        while (true)
        {
            if (logs.Count > 0)
            {
                int count = logs.Count > maxParams ? maxParams : logs.Count;

                var url = new StringBuilder(googleScriptUrl).Append(deviceId).Append(SystemInfo.deviceUniqueIdentifier);
                for (int i = 0; i < count; ++i)
                    url.Append(p).Append(i).Append(equal).Append(UrlEncode(logs.Dequeue()));

                var www = new WWW(url.ToString());
                yield return www;

                // if (www.error != null) { }

                www.Dispose();
            }

            yield return null;
        }
    }

    string UrlEncode(string instring)
    {
        StringReader strRdr = new StringReader(instring);
        StringWriter strWtr = new StringWriter();
        int charValue = strRdr.Read();
        while (charValue != -1)
        {
            if (((charValue >= 48) && (charValue <= 57)) || ((charValue >= 65) && (charValue <= 90)) || ((charValue >= 97) && (charValue <= 122)))
                strWtr.Write((char)charValue);
            else if (charValue == 32)
                strWtr.Write("+");
            else
                strWtr.Write("%{0:x2}", charValue);

            charValue = strRdr.Read();
        }
        return strWtr.ToString();
    }
}