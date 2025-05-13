using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[System.Serializable]
public class MapStage {
    public string sceneName;
    public Vector3 drayStartPosition;
}

public class GameManager : MonoBehaviour {
    public static GameManager S;

    [Header("Game Map Sequence")]
    public List<MapStage> mapStages;
    public string endScene = "_GameOver_Scene";
    public string startScene = "_Start_Scene";

    private int currentMapIndex = 0;

    void Awake() {
        if (S == null) {
            S = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public void StartGame() {
        currentMapIndex = 0;
        LoadCurrentMap();
    }

    public void LoadNextMap() {
        currentMapIndex++;
        if (currentMapIndex < mapStages.Count) {
            LoadCurrentMap();
        } else {
            SceneManager.LoadScene(endScene);
        }
    }

    public void RestartGame() {
        SceneManager.LoadScene(startScene);
    }

    void LoadCurrentMap() {
        SceneManager.LoadScene(mapStages[currentMapIndex].sceneName);
    }

    // Allow access to map assets if needed
    public MapStage GetCurrentMapStage() {
        return mapStages[currentMapIndex];
    }
}

