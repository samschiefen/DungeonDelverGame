using UnityEngine;

public interface IKeyMaster {
    int keyCount { get; set; }
    Vector2 pos { get; }
    int GetFacing();
}
