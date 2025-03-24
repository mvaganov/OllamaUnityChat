using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
public class Repeat : MonoBehaviour {
	public float MaxDurationWithoutBlink = 5;
	public UnityEvent WhatToDo;
	private int _timeAdjustmentStarted;

	private void OnValidate() {
		AdditionalEditorSpecificRefresh();
	}

	// Update is called once per frame, or during some UI actions in the Editor if ExecuteInEditMode
	void Update() {
		UpdateTimer();
	}

	void UpdateTimer() {
		int msSinceLastBlink = System.Environment.TickCount - _timeAdjustmentStarted;
		float secondsSinceLastBlink = msSinceLastBlink / 1000f;
		if (MaxDurationWithoutBlink > 0 && secondsSinceLastBlink >= MaxDurationWithoutBlink) {
			DoActivateTrigger();
		}
		if (enabled) {
			AdditionalEditorSpecificRefresh();
		}
	}

	public void DoActivateTrigger() {
		_timeAdjustmentStarted = System.Environment.TickCount;
		WhatToDo.Invoke();
	}

	private void AdditionalEditorSpecificRefresh() {
#if UNITY_EDITOR
		UnityEditor.EditorApplication.delayCall += UpdateTimer;
#endif
	}
}
