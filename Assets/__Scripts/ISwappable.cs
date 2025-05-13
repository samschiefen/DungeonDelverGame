using UnityEngine;

public interface ISwappable {
    GameObject guaranteedDrop { get; set; }
    int tileNum { get; }
    void Init( int fromTileNum, int tileX, int tileY );
}
