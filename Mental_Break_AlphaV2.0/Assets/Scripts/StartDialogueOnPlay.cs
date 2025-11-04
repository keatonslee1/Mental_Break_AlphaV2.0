using UnityEngine;
using Yarn.Unity;

public class StartDialogueOnPlay : MonoBehaviour {
	public string startNode = "R1_Start";
	public DialogueRunner dialogueRunner;

	private void Awake() {
		if (dialogueRunner == null) {
			dialogueRunner = FindAnyObjectByType<DialogueRunner>();
		}
	}

	private void Start() {
		if (dialogueRunner != null && dialogueRunner.YarnProject != null) {
			dialogueRunner.StartDialogue(startNode);
		} else {
			Debug.LogError("StartDialogueOnPlay: DialogueRunner or YarnProject is missing.");
		}
	}
}