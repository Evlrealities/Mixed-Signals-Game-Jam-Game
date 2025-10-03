using TMPro.EditorUtilities;
using UnityEngine;

public abstract class Executor : MonoBehaviour {
    public void Init(string[] inputWords) {
        Execute(inputWords);
        Destroy(gameObject);
    }

    protected abstract void Execute(string[] inputWords);
}
