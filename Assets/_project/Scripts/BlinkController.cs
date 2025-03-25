using System;
using UnityEngine;

public class BlinkController : MonoBehaviour {
	public bool activate;
	public TransformAdjust[] blinkers;

	private void OnValidate() {
		if (!activate) { return; }
		activate = false;
		BlinkAllBlinkers();
	}

	public void BlinkAllBlinkers() {
		ForEachBlinker(b => {
			b.DoActivateTrigger();
			b.ContinueAdjusting();
		});
	}

	public void ForEachBlinker(Action<TransformAdjust> action) {
		for (int i = 0; i < blinkers.Length; ++i) {
			TransformAdjust b = blinkers[i];
			if (b == null) {
				continue;
			}
			action.Invoke(b);
		}
	}

	public void BlinkAgain() {
		ForEachBlinker(b => b.DoActivateTrigger());
	}

	public void StopBlinking() {
		ForEachBlinker(b => b.StopRepeating());
	}
}
