// Uncomment this if you have the Touch controller classes in your project
//#define USE_OVRINPUT

using System;
using Oculus.Platform;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * This class shows a very simple way to integrate the Reporting Callback
 * by registering a listener and responding to the notification
 */
public class ReportingCallbackSample : MonoBehaviour
{
  public Text InVRConsole;
  public Text DestinationsConsole;

  // Start is called before the first frame update
  void Start()
  {
    UpdateConsole("Init Oculus Platform SDK...");
    Core.AsyncInitialize().OnComplete(message => {
      if (message.IsError)
      {
        // Init failed, nothing will work
        UpdateConsole(message.GetError().Message);
      }
      else
      {
        UpdateConsole("Init complete!");

        /**
         * Listen for when user clicks AUI report button
         */
        AbuseReport.SetReportButtonPressedNotificationCallback(OnReportButtonIntentNotif);
        UpdateConsole("Registered callback");
      }
    });
  }

  // User has interacted with the AUI outside this app
  void OnReportButtonIntentNotif(Message<string> message)
  {
    if (message.IsError)
    {
      UpdateConsole(message.GetError().Message);
    } else
    {
      UpdateConsole("Send that response is handled");
      AbuseReport.ReportRequestHandled(ReportRequestResponse.Handled);
      UpdateConsole("Response has been handled!");
    }
  }

  #region Helper Functions

  private void UpdateConsole(string value)
  {
    Debug.Log(value);

    InVRConsole.text =
      "Welcome to the Sample Reporting Callback App\n\n" + value;
  }
  #endregion
}
