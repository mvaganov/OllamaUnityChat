using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class OllamaComponent : MonoBehaviour {
	[System.Serializable]
	public class UnityEvent_string : UnityEvent<string> { }
	public string message;
	public bool sendMessage;
	[TextArea(1,10)]
	public string result;
	public string model;
	public TMP_Text textOutput;
	public UnityEvent_string onTextInput;
	public UnityEvent onTextFinished;

	private void OnValidate() {
		if (!sendMessage) { return; }
		sendMessage = false;
		result = "";
		if (textOutput != null) {
			textOutput.text = "";
		}
		SendMessage();
	}

	[ContextMenu(nameof(SendMessage))]
	public void SendMessage() {
		Thread t = new Thread(SeparateSocketThreadToPreventEditorCrash);
		t.Start();
	}

	private void SeparateSocketThreadToPreventEditorCrash() {
		Task task = OllamaInterface.Ask(message, model, AddToTextInput, FinishedText, Debug.LogError);
		while (!task.IsCompleted) {
			Task.Yield();
		}
	}
	
	public void AddToTextInput(string text) {
		onTextInput?.Invoke(text);
		result += text;
		if (textOutput != null) {
#if UNITY_EDITOR
			EditorApplication.delayCall += () => {
				textOutput.text += text;
				textOutput.enabled = false;
				textOutput.enabled = true;
			};
#else
			textOutput.text = text;
#endif
		}
	}

	public void FinishedText() {
		onTextFinished?.Invoke();
		message = "";
	}
}
