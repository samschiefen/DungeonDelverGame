using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent( typeof(InRoom) )]
public class Dray : MonoBehaviour, IFacingMover, IKeyMaster
{
    static private Dray S;
    static public IFacingMover IFM;
    public enum eMode { idle, move, attack, roomTrans, knockback, gadget, dodge }

    [Header("Inscribed")]
    public float speed = 5;
    public float attackDuration = 0.25f; // Number of seconds to attack
    public float attackDelay = 0.5f; // Delay between attacks
    public float roomTransDelay = 0.5f; // Room transition delay
    public int maxHealth = 10;
    public float knockbackSpeed = 10;
    public float dodgeSpeed = 20;
    public float knockbackDuration = 0.25f;
    public float invincibleDuration = 0.5f;
    public float dodgeDuration = 0.2f;
    public int healthPickupAmount = 2;
    public KeyCode keyAttack = KeyCode.Z;
    public KeyCode keyGadget = KeyCode.X;
    public KeyCode keyDodge = KeyCode.Space;
    [SerializeField]
    private bool startWithGrappler = true;

    [Header("Dynamic")]
    public int dirHeld = -1; // Direction of the held movement key
    public int facing = 1; // Direction Dray is facing
    public eMode mode = eMode.idle;
    public bool invincible = false;
    [SerializeField] [Range(0,20)]
    private int _numKeys = 0;

    [SerializeField] [Range(0,10)]
    private int _health;
    public int health {
        get { return _health; }
        set { _health = value; }
    }

    private float timeAtkDone = 0;
    private float timeAtkNext = 0;
    private float roomTransDone = 0;
    private Vector2 roomTransPos;
    private float dodgeDone = 0;
    private float knockbackDone = 0;
    private float invincibleDone = 0;
    private Vector2 knockbackVel;
    private Vector3 lastSafeLoc;
    private int lastSafeFacing;
    private int lastDirPressed = -1;

    private Collider2D colld;
    private Grappler grappler;
    private SpriteRenderer sRend;
    private Rigidbody2D rigid;
    private Animator anim;
    private InRoom inRm;

    private Vector2[] directions = new Vector2[] {
        Vector2.right, Vector2.up, Vector2.left, Vector2.down };

    private KeyCode[] keys = new KeyCode[] {
        KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow,
        KeyCode.D,          KeyCode.W,       KeyCode.A,         KeyCode.S };

    void Awake() {
        S = this;
        IFM = this;
        sRend = GetComponent<SpriteRenderer>();
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        inRm = GetComponent<InRoom>();
        health = maxHealth;
        grappler = GetComponentInChildren<Grappler>();
        if ( startWithGrappler ) currentGadget = grappler;
        colld = GetComponent<Collider2D>();

        // Subscribe to scene loading event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() {
        // Unsubscribe from the scene loading event when the object is destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ResetDray() {
        _health = maxHealth;
        startWithGrappler = false;
        _numKeys = 0;
        facing = 1;
        mode = eMode.idle;
    }

    // Called when a new scene is loaded
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (GameManager.S != null) {
            transform.position = GameManager.S.GetCurrentMapStage().drayStartPosition;
        }

        S = this;
        IFM = this;

        ResetDray();
    }

    void Start() {
        lastSafeLoc = transform.position;
        lastSafeFacing = facing;
    }

    void Update()
    {
        if ( isControlled ) return;

        // Check knockback and invincibility
        if (invincible && Time.time > invincibleDone) invincible = false;
        sRend.color = invincible ? Color.red : Color.white;
        if ( mode == eMode.knockback ) {
            rigid.linearVelocity = knockbackVel;
            if (Time.time < knockbackDone) return;
            // The following is only reached if Time.time >= knockbackDone
            mode = eMode.idle;
        }

        if ( mode == eMode.roomTrans ) {
            rigid.linearVelocity = Vector3.zero;
            anim.speed = 0;
            posInRoom = roomTransPos; // Keeps Dray in place
            if (Time.time < roomTransDone) return;
            // The following line is only reached if Time.time >= transitionDone
            mode = eMode.idle;
        }

        if ( health == 0 ) {
            SceneManager.LoadScene("_GameOver_Scene");
        }

       // Finishing the attack when it's over
       if (mode == eMode.attack && Time.time >= timeAtkDone) {
        mode = eMode.idle;
       }

       // Finishing the dodge when it's over
       if (mode == eMode.dodge && Time.time >= dodgeDone) {
        mode = eMode.idle;
       }

       // -------------- Handle Keyboard Input in idle or move Modes --------------
       if (mode == eMode.idle || mode == eMode.move) {
        dirHeld = -1;
        for (int i = 0; i < keys.Length; i++) {
            if ( Input.GetKey(keys[i]) ) dirHeld = i % 4;
            if ( Input.GetKeyDown(keys[i]) ) lastDirPressed = i % 4;
        }

        // Choosing the proper movement or idle mode based on dirHeld
        if (dirHeld == -1) {
            mode = eMode.idle;
        } else {
            facing = dirHeld;
            mode = eMode.move;
        }

        if (Input.GetKeyDown(keyDodge)) {
            int dodgeDir = (lastDirPressed != -1) ? lastDirPressed : dirHeld;
            if (dodgeDir != -1) {
                facing = dodgeDir;
                dirHeld = dodgeDir;
                mode = eMode.dodge;
                dodgeDone = Time.time + dodgeDuration;
            }
        }

        // Pressing the gadget button
        if ( Input.GetKeyDown( keyGadget ) ) {
            if ( currentGadget != null ) {
                if ( currentGadget.GadgetUse( this, GadgetIsDone ) ) {
                    mode = eMode.gadget;
                    rigid.linearVelocity = Vector2.zero;
                }
            }
        }

        // Pressing the attack button
        if (Input.GetKeyDown(keyAttack) && Time.time >= timeAtkNext) {
            mode = eMode.attack;
            timeAtkDone = Time.time + attackDuration;
            timeAtkNext = Time.time + attackDelay;
        }
       }

       // ----------------- Act on the current mode -----------------
       Vector2 vel = Vector2.zero;
       switch (mode) {
        case eMode.attack: // Show the Attack pose in the correct direction
            anim.Play( "Dray_Attack_" + facing );
            anim.speed = 0;
            rigid.linearVelocity = vel * speed;
            break;

        case eMode.idle: // Show frame 1 in the correct direction
            anim.Play( "Dray_Walk_" + facing );
            anim.speed = 0;
            rigid.linearVelocity = vel * speed;
            break;

        case eMode.move: // Play walking animation in the correct direction
            vel = directions[dirHeld];
            anim.Play( "Dray_Walk_" + facing );
            anim.speed = 1;
            rigid.linearVelocity = vel * speed;
            break;

        case eMode.gadget: // Show Attack pose & wait for IGadget to be done
            anim.Play( "Dray_Attack_" + facing );
            anim.speed = 0;
            rigid.linearVelocity = vel * speed;
            break;

        case eMode.dodge:
            vel = directions[dirHeld];
            anim.Play("Dray_Attack_" + facing); 
            anim.speed = 0;
            rigid.linearVelocity = vel * dodgeSpeed;
            break;
       }
    }

    void LateUpdate() {
        if ( isControlled ) return; 

        // Get the nearest quarter-grid position to Dray
        Vector2 gridPosIR = GetGridPosInRoom( 0.25f );

        // Check to see whether we're in a Door tile
        int doorNum;
        for (doorNum = 0; doorNum < 4; doorNum++) {
            if (gridPosIR == InRoom.DOORS[doorNum]) {
                break;
            }
        }

        if ( doorNum > 3 || doorNum != facing ) return;

        // Move to the next room
        Vector2 rm = roomNum;
        switch (doorNum) {
            case 0:
                rm.x += 1;
                break;
            case 1:
                rm.y += 1;
                break;
            case 2: 
                rm.x -= 1;
                break;
            case 3:
                rm.y -= 1;
                break;
        }

        // Make sure that the rm we want to jump to is valid
        if (0 <= rm.x && rm.x <= InRoom.MAX_RM_X) {
            if (0 <= rm.y && rm.y <= InRoom.MAX_RM_Y) {
                roomNum = rm;
                roomTransPos = InRoom.DOORS[ (doorNum + 2) % 4 ];
                posInRoom = roomTransPos;
                mode = eMode.roomTrans;
                roomTransDone = Time.time + roomTransDelay;
                lastSafeLoc = transform.position;
                lastSafeFacing = facing;
            }
        }
    }

    void OnCollisionEnter2D( Collision2D coll ) {
        if ( isControlled ) return;

        if (invincible) return; // Return if Dray can't be damaged
        DamageEffect dEf = coll.gameObject.GetComponent<DamageEffect>();
        if (dEf == null) return; // If no DamageEffect, exit this method

        health -= dEf.damage; // Subtract the damage amount from health
        invincible = true;
        invincibleDone = Time.time + invincibleDuration;

        if (dEf.knockback) { // Knockback Dray
            // Determine the direction of knockback from relative position
            Vector2 delta = transform.position - coll.transform.position;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) {
                // Knockback should be horizontal
                delta.x = (delta.x > 0) ? 1 : -1;
                delta.y = 0;
            } else {
                // Knockback should be vertical
                delta.x = 0;
                delta.y = (delta.y > 0) ? 1 : -1;
            }

            // Apply knockback speed to the Rigidbody
            knockbackVel = delta * knockbackSpeed;
            rigid.linearVelocity = knockbackVel;

            // If not in gadget mode OR if GadgetCancel is successful
            if ( mode != eMode.gadget || currentGadget.GadgetCancel() ) {
                // Set mode to knockback and set time to stop knockback
                mode = eMode.knockback;
                knockbackDone = Time.time + knockbackDuration;
            }
        }
    }

    void OnTriggerEnter2D( Collider2D colld ) {
        if ( isControlled ) return;

        PickUp pup = colld.GetComponent<PickUp>();
        if (pup == null) return;

        switch ( pup.itemType ) {
            case PickUp.eType.health:
                health = Mathf.Min( health + healthPickupAmount, maxHealth );
                break;
            case PickUp.eType.key:
                _numKeys++;
                break;
            case PickUp.eType.grappler:
                currentGadget = grappler;
                break;
            case PickUp.eType.flag:
                if (GameManager.S != null) {
                    GameManager.S.LoadNextMap();
                } else {
                    Debug.LogWarning("GameManager is missing!");
                }
                break;
            default:
                Debug.LogError("No case for Pickup type: " + pup.itemType);
                break;
        }

        Destroy( pup.gameObject );
    }

    public void ResetInRoom( int healthLoss = 0 ) {
        transform.position = lastSafeLoc;
        facing = lastSafeFacing;
        health -= healthLoss;

        invincible = true; // Make Dray invincible
        invincibleDone = Time.time + invincibleDuration;
    }

    static public int HEALTH { get { return S._health; } }
    static public int NUM_KEYS { get { return S._numKeys; } }

    // -------------- Implementation of IFacingMover --------------
    public int GetFacing() { return facing; }

    public float GetSpeed() { return speed; }

    public bool moving { get { return (mode == eMode.move); } }

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

    // -------------- Implementation of IKeyMaster --------------
    public int keyCount {
        get { return _numKeys; }
        set { _numKeys = value; }
    }

    public Vector2 pos {
        get { return (Vector2) transform.position; }
    }

#region IGadget_Affordances
    // --------------- IGadget Affordances ---------------
    public IGadget currentGadget { get; private set; }

    /// <summary>
    /// Called by an IGadget when it is done. Sets mode to eMode.idle.
    /// Matches the System.Func<IGadget, bool> delegate type required by the
    /// tDoneCallback parameter of IGadget.GadgetUse().
    /// </summary>
    /// <param name="gadget">The IGadget calling this method</param>
    /// <returns>true if successful, false if not</returns>
    public bool GadgetIsDone( IGadget gadget ) {
        if ( gadget != currentGadget ) {
            Debug.LogError( "A non-current Gadget called GadgetDone"
                            + "\ncurrentCadget: " + currentGadget.name
                            + "\tcalled by: " + gadget.name );
        }
        controlledBy = null;
        physicsEnabled = true;
        mode = eMode.idle;
        return true;
    }

    public IGadget controlledBy { get; set; }
    public bool isControlled {
        get { return (controlledBy != null); }
    }

    [SerializeField]
    private bool _physicsEnabled = true;
    public bool physicsEnabled {
        get { return _physicsEnabled; }
        set {
            if ( _physicsEnabled != value ) {
                _physicsEnabled = value;
                colld.enabled = _physicsEnabled;
                rigid.simulated = _physicsEnabled;
            }
        }
    }
#endregion
}
