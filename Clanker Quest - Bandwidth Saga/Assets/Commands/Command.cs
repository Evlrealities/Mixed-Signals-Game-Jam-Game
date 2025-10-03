using UnityEngine;

[CreateAssetMenu(menuName = "Robot/Command", fileName = "NewCommand")]
public class Command : ScriptableObject {
    [Header("Config")]
    [Tooltip("List of keywords to be parsed for IN ORDER")]
    [SerializeField] public string[] keywords;
    [Tooltip("The executor object to be instantiated in the world.")]
    [SerializeField] public Executor executor;
}
