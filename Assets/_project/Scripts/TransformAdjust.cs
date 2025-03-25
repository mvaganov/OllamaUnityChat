using UnityEngine;

[ExecuteInEditMode]
public class TransformAdjust : MonoBehaviour {
	public enum State { None, Adjusting, Unadjusting }
	public enum Kind { None, Scale, EulerRotation, Position }

	public float Duration = 0.25f;
	public Kind AdjustKind = Kind.Scale;
	public Vector3 Start;
	public Vector3 End;
	protected int _timeAdjustmentStarted;
	protected State _state;
	[SerializeField] protected int _repetitions;
	public bool AlsoUndoAdjustment = false;
	public string label;

	public int ConsecutiveRepetitions {
		get => _repetitions;
		set => _repetitions = value;
	}

	public bool IsChangeHappening => _state != State.None;

	private void Reset() {
		Start = End = transform.localScale;
	}

	private void OnValidate() {
		AdditionalEditorSpecificRefresh();
	}

	/// <summary>
	/// will trigger state updates in the editor, even without UI activity
	/// </summary>
	private void AdditionalEditorSpecificRefresh() {
#if UNITY_EDITOR
		UnityEditor.EditorApplication.delayCall += ContinueAdjusting;
#endif
	}

	void Update() {
		ContinueAdjusting();
	}

	public void ContinueAdjusting() {
		if (!enabled) { return; }
		bool isActivating = UpdateOverTime();
#if UNITY_EDITOR
		if (isActivating) {
			AdditionalEditorSpecificRefresh();
		}
#endif
		if (_repetitions <= 0) {
			return;
		}
		if (!isActivating) {
			--_repetitions;
			StartAdjusting();
		}
	}

	public void StartAdjusting() {
		_timeAdjustmentStarted = System.Environment.TickCount;
		_state = State.Adjusting;
	}

	private bool UpdateOverTime() {
		if (_state == State.None) {
			return false;
		}
		UpdateValue(CalculateProgress());
		return true;
	}

	public float CalculateProgress() {
		int durationMs = (int)(Duration * 1000);
		int now = System.Environment.TickCount;
		int timePassed = now - _timeAdjustmentStarted;
		float progress = 0;
		if (timePassed >= durationMs) {
			_timeAdjustmentStarted = now - (timePassed - durationMs);
			timePassed = durationMs;
			if (_state == State.Adjusting) {
				progress = 1;
			}
			AdvanceState();
		} else {
			progress = (float)timePassed / durationMs;
		}
		if (_state == State.Unadjusting) {
			progress = 1 - progress;
		}
		return progress;
	}

	public void UpdateValue(float progress) {
		Vector3 value = Vector3.Lerp(Start, End, progress);
		switch (AdjustKind) {
			case Kind.Scale: transform.localScale = value; break;
			case Kind.Position: transform.localPosition = value; break;
			case Kind.EulerRotation: transform.localRotation = Quaternion.Euler(value); break;
		}
	}

	void AdvanceState() {
		switch (_state) {
			case State.None: _state = State.Adjusting; break;
			case State.Adjusting: _state = AlsoUndoAdjustment ? State.Unadjusting : State.None; break;
			case State.Unadjusting: _state = State.None; break;
		}
	}

	[ContextMenu(nameof(DoActivateTrigger))]
	public void DoActivateTrigger() {
		if (IsChangeHappening) {
			++_repetitions;
		} else {
			StartAdjusting();
		}
#if UNITY_EDITOR
		AdditionalEditorSpecificRefresh();
#endif
	}

	public void StopRepeating() {
		_repetitions = 0;
	}

	public void Stop() {
		_state = State.None;
		StopRepeating();
	}
}
