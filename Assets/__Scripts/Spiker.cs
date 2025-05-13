using UnityEngine;
using System.Collections;

[RequireComponent(typeof(InRoom))]
public class Spiker : Enemy, IFacingMover {
    enum eMode { search, attack, retract };

    [Header("Inscribed: Spiker")]
    public float sensorRange = 1f;
    public float attackSpeed = 10;
    public float retractSpeed = 15;
    public float radius = 0.4f;
    public int speed = 2;
    public float timeThinkMin = 1f;
    public float timeThinkMax = 4f;

    [Header("Dynamic: Spiker")]
    [Range(0,4)]
    public int facing = 0;
    public float timeNextDecision = 0;
    public float attackTimeout = 1.5f;

    private eMode mode = eMode.search;
    private InRoom inRm;
    private Dray dray;
    private SphereCollider drayColld;
    private Vector3 p0, p1;
    private DamageEffect dEf;
    private float attackStartTime;


    protected override void Awake() {
        base.Awake();
        inRm = GetComponent<InRoom>();

        GameObject go = GameObject.Find("Dray");
        dray = go.GetComponent<Dray>();
        drayColld = go.GetComponent<SphereCollider>();
        dEf = GetComponent<DamageEffect>();
    }

    protected override void Update () {
        base.Update();
        if ( knockback ) mode = eMode.search;

        switch (mode) {
            case eMode.search:
                // Check if Dray is in the same room
                if (dray.roomNum != inRm.roomNum) break;


                Vector2 toDray = dray.posInRoom - inRm.posInRoom;
                Vector2 dir = directions[facing];

                float alignment = Vector2.Dot(toDray.normalized, dir);

                // Check if Dray is within sensor range, regardless of alignment
                if (Vector2.Distance(transform.position, dray.transform.position) < sensorRange) {
                    if ( knockback ) return;
                    // Attack towards Dray's current position
                    p0 = transform.position;  // Store current position (initial position)
                    p1 = dray.transform.position; // Dray's current position
                    Debug.Log("Spiker is attacking!");
                    mode = eMode.attack;
                    attackStartTime = Time.time;
                    return;
                }

                // Movement behavior
                if (Time.time >= timeNextDecision) {
                    if ( knockback ) return;
                    DecideDirection(); // Change direction occasionally
                    Debug.Log("Spiker is moving...");
                }

                // Move in facing direction
                rigid.linearVelocity = dir * speed;
                break;

            case eMode.attack:
                // Move towards Dray's position
                Vector3 attackDir = (p1 - transform.position).normalized;
                rigid.linearVelocity = attackDir * attackSpeed;
                anim.speed = 3;

                // Auto-retract if too much time has passed
                if (Time.time - attackStartTime > attackTimeout) {
                    Debug.Log("Spiker attack timed out — switching to retract mode.");
                    mode = eMode.retract;
                    break;

                }

                // If close enough to Dray's position, switch to retract mode
                if (Vector3.Distance(transform.position, p1) < 0.1f) {
                    transform.position = p1; // Ensure we are exactly at the Dray's position
                    Debug.Log("Spiker reached Dray's position!");
                    mode = eMode.retract;
                }
                break;

            case eMode.retract:
                // Move back to the original position (p0)
                Vector3 retractDir = (p0 - transform.position).normalized;
                rigid.linearVelocity = retractDir * retractSpeed;
                anim.speed = 2;

                // If close enough to the original position, go back to search mode
                if (Vector3.Distance(transform.position, p0) < 0.1f) {
                    transform.position = p0; // Ensure we are exactly at the original position
                    Debug.Log("Spiker returned to original position!");
                    mode = eMode.search;
                }
                break;
        }
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
