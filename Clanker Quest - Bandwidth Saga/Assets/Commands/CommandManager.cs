using TMPro;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;

public class CommandManager : MonoBehaviour {
    [Header("Input")]
    [Tooltip("The text mesh pro input field used for commands.")]
    [SerializeField] TMP_InputField inputField;

    [Header("Behaviour")]
    [Tooltip("Minimum percentage of a command's keyword that must be present for it to execute.")]
    [Range(0, 100)]
    [SerializeField] float minPercentageThreshold = 50;
    [Tooltip("List of all possible commands.")]
    [SerializeField] Command[] commands;

    private void Start() {
        if (!inputField) {
            Debug.LogError("Command Manager has no input field assigned.");
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Return)) {
            ParseTextForCommands(inputField.text);
            inputField.text = "";
        }
    }

    private void ParseTextForCommands(string textToParse) {
        string[] words = textToParse.ToLower().Split(" ");

        Command currentCommand = null;
        float bestPercentage = 0f;

        foreach (Command command in commands) {
            int matchCount = 0;

            foreach (string word in words) {
                foreach (string keyword in command.keywords) {
                    if (word == keyword) {
                        matchCount++;
                        continue;
                    }
                }
            }

            float matchPercentage = matchCount / command.keywords.Length * 100;

            if (matchPercentage > 0 && matchPercentage > bestPercentage && matchPercentage >= minPercentageThreshold) {
                bestPercentage = matchPercentage;
                currentCommand = command;
            }
        }

        if (!currentCommand) { return; } // TODO: Add robot confused logic

        GameObject executor = Instantiate(currentCommand.executor.gameObject);
        executor.GetComponent<Executor>().Init(words);
    }
}
