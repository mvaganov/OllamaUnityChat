using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class OllamaComponent : MonoBehaviour {

	[System.Serializable]
	public class UnityEvent_string : UnityEvent<string> { }
	[System.Serializable]
	public class WordTrigger {
		public string word;
		public UnityEvent trigger;
		public void Update(string text) {
			if (text.Contains(word)) {
				trigger.Invoke();
			}
		}
	}
	public string message;
	public bool sendMessage;
	public bool cancelMessage;
	[TextArea(1,10)]
	public string result;
	public string model;
	public TMP_Text textOutput;
	public UnityEvent_string onTextInput;
	public UnityEvent onTextFinished;
	public UnityEvent onWaitingForRequest;
	private Thread thread;
	private OllamaInterface ollamaInterface;
	private Task task;
	public WordTrigger[] wordTriggers = new WordTrigger[0];

	private void OnValidate() {
		if (sendMessage) {
			sendMessage = false;
			result = "";
			if (textOutput != null) {
				textOutput.text = "";
			}
			SendMessage();
		}
		if (cancelMessage && thread != null) {
			cancelMessage = false;
			ollamaInterface.client.Close();
			thread.Abort();
			thread.Join();
			thread = null;
		}
	}

	[ContextMenu(nameof(SendMessage))]
	public void SendMessage() {
		if (string.IsNullOrEmpty(message)) {
			return;
		}
		thread = new Thread(SeparateSocketThreadToPreventEditorCrash);
		thread.Start();
	}

	public void UnityEditorSafeAction(Action action) {
#if UNITY_EDITOR
		EditorApplication.delayCall += () => action.Invoke();
#else
		action.Invoke();
#endif
	}

	private void SeparateSocketThreadToPreventEditorCrash() {
		UnityEditorSafeAction(() => {
			onWaitingForRequest?.Invoke();
		});
		ollamaInterface = new OllamaInterface();
		task = ollamaInterface.Ask(message, model, AddToTextInput, FinishedText, Debug.LogError);
		while (!task.IsCompleted) {
			Task.Yield();
		}
	}
	
	public void AddToTextInput(string text) {
		onTextInput?.Invoke(text);
		result += text;
		UnityEditorSafeAction(() => {
			Array.ForEach(wordTriggers, t => t.Update(text));
		});
		if (textOutput != null) {
			UnityEditorSafeAction(() => {
				textOutput.text = result;
				textOutput.enabled = false;
				textOutput.enabled = true;
			});
		}
	}

	public void FinishedText() {
		onTextFinished?.Invoke();
		message = "";
	}
}
