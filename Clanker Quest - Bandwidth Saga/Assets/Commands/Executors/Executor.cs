using System.Collections;
using UnityEngine;

public abstract class Executor : MonoBehaviour {
    public void Init(string[] inputWords) {
        Execute(inputWords);
        StartCoroutine(DestroyNextFrame());
    }

    IEnumerator DestroyNextFrame() {
        // Wait one frame so the EventSystem/GraphicRaycaster finishes raycasts
        yield return null;
        if (this) Destroy(gameObject);
    }

    protected abstract void Execute(string[] inputWords);
}
