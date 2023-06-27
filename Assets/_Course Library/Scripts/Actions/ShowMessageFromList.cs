using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Shows an ordered list of messages via a text mesh
/// </summary>
public class ShowMessageFromList : MonoBehaviour
{
    [Tooltip("The text mesh the message is output to")]
    public TextMeshProUGUI messageOutput = null;

    // What happens once the list is completed
    public UnityEvent OnComplete = new UnityEvent();

    [Tooltip("The list of messages that are shown")]
    [TextArea] public List<string> messages = new List<string>();

    private int index = 0;

    private void Start()
    {
        ShowMessage();
    }

    public void NextMessage()
    {
        int newIndex = ++index % messages.Count;

        if (newIndex < index)
        {
            OnComplete.Invoke();
        }
        else
        {
            ShowMessage();
        }
    }

    public void PreviousMessage()
    {
        index = --index % messages.Count;
        ShowMessage();
    }

    private void ShowMessage()
    {
        messageOutput.text = messages[Mathf.Abs(index)];
    }

    public void ShowMessageAtIndex(int value)
    {
        index = value;
        ShowMessage();
    }
}
