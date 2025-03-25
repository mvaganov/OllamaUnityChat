using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
public class Repeat : MonoBehaviour {
	public float MaxDurationWithoutActivation = 5;
	public UnityEvent WhatToDo;
	private int _timeOfActivation;

	private void OnValidate() {
		AdditionalEditorSpecificRefresh();
	}

	// Update is called once per frame, or during some UI actions in the Editor if ExecuteInEditMode
	void Update() {
		UpdateTimer();
	}

	void UpdateTimer() {
		int msSinceLastActivation = System.Environment.TickCount - _timeOfActivation;
		float secondsSinceLastActivation = msSinceLastActivation / 1000f;
		if (MaxDurationWithoutActivation > 0 && secondsSinceLastActivation >= MaxDurationWithoutActivation) {
			DoActivateTrigger();
		}
		if (enabled) {
			AdditionalEditorSpecificRefresh();
		}
	}

	[ContextMenu(nameof(DoActivateTrigger))]
	public void DoActivateTrigger() {
		_timeOfActivation = System.Environment.TickCount;
		WhatToDo.Invoke();
	}

	private void AdditionalEditorSpecificRefresh() {
#if UNITY_EDITOR
		UnityEditor.EditorApplication.delayCall += UpdateTimer;
#endif
	}
}
