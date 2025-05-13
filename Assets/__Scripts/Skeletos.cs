using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent( typeof(InRoom) )]
public class Skeletos : Enemy, IFacingMover
{
    [Header("Inscribed: Skeletos")]
    public int speed = 2;
    public float timeThinkMin = 1f;
    public float timeThinkMax = 4f;

    [Header("Dynamic: Skeletos")]
    [Range(0,4)]
    public int facing = 0;
    public float timeNextDecision = 0;

    private InRoom inRm;

    protected override void Awake() {
        base.Awake();
        inRm = GetComponent<InRoom>();
    }

    protected override void Update()
    {
        base.Update();
        if ( knockback ) return;
        
        if (Time.time >= timeNextDecision) {
            DecideDirection();
        }
        // rigid is inherited from Enemy, which defines it in Enemy.Awake()
        rigid.linearVelocity = directions[facing] * speed;
    }

    void DecideDirection() {
        facing = Random.Range(0, 5);
        timeNextDecision = Time.time + Random.Range(timeThinkMin, timeThinkMax);
    }

    // -------------- Implementation of IFacingMover --------------
    public int GetFacing() { 
        return facing % 4; 
    }

    public float GetSpeed() { return speed; }

    public bool moving { 
        get { return (facing < 4); } 
    }

    public float gridMult { get { return inRm.gridMult; } }

    public bool isInRoom { get { return inRm.isInRoom; } }

    public Vector2 roomNum {
        get { return inRm.roomNum; }
        set { inRm.roomNum = value; }
    }

    public Vector2 posInRoom {
        get { return inRm.posInRoom; }
        set { inRm.posInRoom = value; }
    }

    public Vector2 GetGridPosInRoom( float mult = -1 ) {
        return inRm.GetGridPosInRoom( mult );
    }
}
