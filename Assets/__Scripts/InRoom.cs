using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InRoom : MonoBehaviour
{
    static public float ROOM_W = 16;
    static public float ROOM_H = 11;
    static public float WALL = 2;
    static Vector2 GRID_OFFSET = new Vector2( 0.5f, 0.5f );
    static public int MAX_RM_X = 9;
    static public int MAX_RM_Y = 9;

    static public Vector2[] DOORS = new Vector2[] {
        new Vector2(14.5f, 5.5f), // Right door
        new Vector2(8.0f, 9.5f), // Up door
        new Vector2(1.5f, 5.5f), // Left door
        new Vector2(8.0f, 1.5f) // Down door
    };

    [Header("Inscribed")]
    public bool keepInRoom = true;
    public float gridMult = 1;
    public float radius = 0.5f;

    void LateUpdate() {
        if (!keepInRoom) return;
        if (isInRoom) return;

        Vector2 posIR = posInRoom;
        posIR.x = Mathf.Clamp(posIR.x, WALL + radius, ROOM_W - WALL - radius);
        posIR.y = Mathf.Clamp(posIR.y, WALL + radius, ROOM_H - WALL - radius);
        posInRoom = posIR;
    }

    public bool isInRoom {
        get {
            Vector2 posIR = posInRoom;
            if (     posIR.x < WALL + radius 
                  || posIR.x > ROOM_W - WALL - radius
                  || posIR.y < WALL + radius
                  || posIR.y > ROOM_H - WALL - radius ) {
                return false;
            } 
            return true;
        }
    }

    // Where is the character in local room coordinates?
    public Vector2 posInRoom {
        get {
            Vector2 tPos = transform.position;
            Vector2 posIR = new Vector2();
            posIR.x = tPos.x % ROOM_W;
            posIR.y = tPos.y % ROOM_H;
            return posIR;
        }
        set {
            Vector2 rNum = roomNum;
            Vector2 tPos = new Vector2();
            tPos.x = roomNum.x * ROOM_W;
            tPos.y = roomNum.y * ROOM_H;
            transform.position = tPos + value;
        }
    }

    // Which room is this character in?
    public Vector2 roomNum {
        get {
            Vector2 tPos = (Vector2) transform.position;
            Vector2 rNum = new Vector2();
            rNum.x = Mathf.Floor( tPos.x / ROOM_W );
            rNum.y = Mathf.Floor( tPos.y / ROOM_H );
            return rNum;
        }
        set {
            Vector2 rNum = value;
            Vector2 tPos = new Vector2();
            tPos.x = rNum.x * ROOM_W;
            tPos.y = rNum.y * ROOM_H;
            transform.position = tPos + posInRoom;
        }
    }

    // What is the closest grid location to this character?
    public Vector2 GetGridPosInRoom(float mult = -1) {
        if (mult == -1) {
            mult = gridMult;
        }
        Vector2 posIR = posInRoom - GRID_OFFSET;
        posIR /= mult;
        posIR.x = Mathf.Round( posIR.x );
        posIR.y = Mathf.Round( posIR.y );
        posIR *= mult;
        posIR += GRID_OFFSET;
        return posIR;
    }
}
