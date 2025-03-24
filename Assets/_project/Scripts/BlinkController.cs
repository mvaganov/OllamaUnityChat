using System;
using UnityEngine;

public class BlinkController : MonoBehaviour {
	public bool blinkAll;
	public Blinker[] blinkers;

	private void OnValidate() {
		if (!blinkAll) { return; }
		blinkAll = false;
		BlinkAllBlinkers();
	}

	public void BlinkAllBlinkers() {
		ForEachBlinker(b => {
			b.BlinkAgain();
			b.ContinueBlinking();
		});
	}

	public void ForEachBlinker(Action<Blinker> action) {
		for (int i = 0; i < blinkers.Length; ++i) {
			Blinker b = blinkers[i];
			if (b == null) {
				continue;
			}
			action.Invoke(b);
		}
	}

	public void BlinkAgain() {
		ForEachBlinker(b => b.BlinkAgain());
	}

	public void StopBlinking() {
		ForEachBlinker(b => b.StopBlinking());
	}
}
