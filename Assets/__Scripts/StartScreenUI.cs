using UnityEngine;

public class StartScreenUI : MonoBehaviour {
    public void OnStartButtonPressed() {
        GameManager.S.StartGame();
    }
}
