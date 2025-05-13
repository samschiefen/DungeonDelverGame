using UnityEngine;

public class GameOverUI : MonoBehaviour {
    public void OnRetryButtonPressed() {
        GameManager.S.RestartGame();
    }
}
