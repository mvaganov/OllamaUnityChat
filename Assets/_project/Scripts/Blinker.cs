using UnityEngine;

[ExecuteInEditMode]
public class Blinker : MonoBehaviour {
	public enum State { None, Scaling, Unscaling }

	public float Duration = 0.25f;
	public Vector3 StartScale;
	public Vector3 EndScale;
	private int timeBlinkStarted;
	private State state;
	[SerializeField] protected int _consecutiveBlinksLeftToDo;
	public float MaxDurationWithoutBlink = 5;
	private float waitedWithoutBlink;

	public int ConsecutiveBlinks {
		get => _consecutiveBlinksLeftToDo;
		set => _consecutiveBlinksLeftToDo = value;
	}

	public bool IsBlinkHappening => state != Blinker.State.None;

	private void Reset() {
		StartScale = EndScale = transform.localScale;
	}

	private void OnValidate() {
		EditorOnlyRefresh();
	}

	private void EditorOnlyRefresh() {
#if UNITY_EDITOR
		UnityEditor.EditorApplication.delayCall += ContinueBlinking;
#endif
	}

	void Update() {
		ContinueBlinking();
	}

	public void ContinueBlinking() {
		bool isBlinking = UpdateScaleOverTime();
#if UNITY_EDITOR
		if (isBlinking) {
			EditorOnlyRefresh();
		}
#endif
		if (_consecutiveBlinksLeftToDo <= 0) {
			int msSinceLastBlink = System.Environment.TickCount - timeBlinkStarted;
			float secondsSinceLastBlink = msSinceLastBlink / 1000f;
			if (MaxDurationWithoutBlink > 0 && secondsSinceLastBlink > MaxDurationWithoutBlink) {
				BlinkAgain();
				EditorOnlyRefresh();
			} else {
				return;
			}
		}
		if (!isBlinking) {
			--_consecutiveBlinksLeftToDo;
			StartBlinking();
		}
	}

	public void StartBlinking() {
		timeBlinkStarted = System.Environment.TickCount;
		state = State.Scaling;
		waitedWithoutBlink = 0;
	}

	bool UpdateScaleOverTime() {
		if (state == State.None) {
			return false;
		}
		int durationMs = (int)(Duration * 1000);
		int now = System.Environment.TickCount;
		int timePassed = now - timeBlinkStarted;
		float progress = 0;
		if (timePassed >= durationMs) {
			AdvanceState();
			timeBlinkStarted = now - (timePassed - durationMs);
			timePassed = durationMs;
			if (state == State.Scaling) {
				progress = 1;
			}
		} else {
			progress = (float)timePassed / durationMs;
		}
		if (state == State.Unscaling) {
			progress = 1 - progress;
		}
		transform.localScale = Vector3.Lerp(StartScale, EndScale, progress);
		return true;
	}

	void AdvanceState() {
		switch (state) {
			case State.None: state = State.Scaling; break;
			case State.Scaling: state = State.Unscaling; break;
			case State.Unscaling: state = State.None; break;
		}
	}

	public void BlinkAgain() {
		++_consecutiveBlinksLeftToDo;
	}

	public void StopBlinking() {
		_consecutiveBlinksLeftToDo = 0;
	}
}
