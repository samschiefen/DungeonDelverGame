using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent( typeof(LineRenderer) )]
public class Grappler : MonoBehaviour, IGadget
{
    public enum eMode { gIdle, gOut, gRetract, gPull }
    [Header("Inscribed")]
    [Tooltip("Speed at which Grappler extends (doubled in gRetract mode)")]
    public float grappleSpd = 10;
    [Tooltip("Maximum length that Grappler will reach")]
    public float maxLength = 7.25f;
    [Tooltip("Minimum distance of Grappler from Dray")]
    public float minLength = 0.375f;
    [Tooltip("Health deducted when Dray ends a grapple on an unsafe tile")]
    public int unsafeTileHealthPenalty = 2;

    [Header("Dynamic")] [SerializeField]
    private eMode _mode = eMode.gIdle;
    public eMode mode {
        get { return _mode; }
        private set { _mode = value; }
    }

    private LineRenderer line;
    private Rigidbody2D rigid;
    private Collider2D colld;

    private Vector3 p0, p1;
    private int facing;
    private Dray dray;
    private System.Func<IGadget,bool> gadgetDoneCallback;

    private Vector2[] directions = new Vector2[] {
        Vector2.right, Vector2.up, Vector2.left, Vector2.down };
    private Vector3[] dirV3s = new Vector3[] {
        Vector3.right, Vector3.up, Vector3.left, Vector3.down };
    
    void Awake() { // Get component references to use throughout the script
        line = GetComponent<LineRenderer>();
        rigid = GetComponent<Rigidbody2D>();
        colld = GetComponent<Collider2D>();
    }

    void Start()
    {
        gameObject.SetActive(false); // Initially disable this GameObject
    }

    void SetGrappleMode( eMode newMode ) {
        switch ( newMode ) {
            case eMode.gIdle:
                transform.DetachChildren(); // Release any child Transforms
                gameObject.SetActive( false );
                if ( dray != null && dray.controlledBy == this as IGadget ) {
                    dray.controlledBy = null;
                    dray.physicsEnabled = true;
                }
                break;
            case eMode.gOut:
                gameObject.SetActive( true );
                rigid.linearVelocity = directions[facing] * grappleSpd;
                break;
            case eMode.gRetract:
                rigid.linearVelocity = -directions[facing] * (grappleSpd * 2);
                break;
            case eMode.gPull:
                p1 = transform.position;
                rigid.linearVelocity = Vector2.zero;
                dray.controlledBy = this;
                dray.physicsEnabled = false;
                break;
        }

        mode = newMode;
    }

    void FixedUpdate() {
        p1 = transform.position;
        line.SetPosition(1,p1);

        switch (mode) {
            case eMode.gOut: // Grappler shooting out
                // See if the Grappler reached its limit without hitting anything
                if ( (p1 - p0).magnitude >= maxLength ) {
                    SetGrappleMode( eMode.gRetract );
                }
                break;
            case eMode.gRetract: // Grappler missed; return at double speed
                // Check to see if the Grappler is no longer in front of Dray
                if ( Vector3.Dot( (p1-p0), dirV3s[facing] ) < 0 ) GrappleDone();
                break;
            case eMode.gPull:
                if ( (p1 - p0).magnitude > minLength ) {
                    // Move Dray toward the Grappler hit point
                    p0 += dirV3s[facing] * grappleSpd * Time.fixedDeltaTime;
                    dray.transform.position = p0;
                    line.SetPosition(0,p0);
                    // Stop Grappler from moving with Dray
                    transform.position = p1;
                } else {
                    // Dray is close enough to stop grappling
                    p0 = p1 - ( dirV3s[facing] * minLength );
                    dray.transform.position = p0;
                    // Check whether Dray landed on an unsafe tile
                    Vector2 checkPos = (Vector2) p0 + new Vector2( 0, -0.25f );
                    if ( MapInfo.UNSAFE_TILE_AT_VECTOR2( checkPos ) ) {
                        // Dray landed on an unsafe tile
                        dray.ResetInRoom( unsafeTileHealthPenalty );
                    }
                    GrappleDone();
                }
                break;
        }
    }

    // Ensures that p1 is aligned with the Grappler head
    void LateUpdate() {
        p1 = transform.position;
        line.SetPosition( 1, p1 );
    }

    /// <summary>
    /// Called when the Grappler hits a Trigger or Collider in the GrapTiles,
    /// Items, or Enemies Physics Layers (Grappler's Collider is a Trigger).
    /// </summary>
    /// <param name="coll"></param>
    void OnTriggerEnter2D( Collider2D colld ) {
        // The Grappler has collided with something, but what?
        string otherLayer = LayerMask.LayerToName(colld.gameObject.layer);

        switch ( otherLayer ) { 
            case "Items": // We've possibly hit a PickUp
                PickUp pup = colld.GetComponent<PickUp>();
                if ( pup == null ) return;
                // If this IS a PickUp, make it a child of this Transform so it moves
                // with the Grappler head.
                pup.transform.SetParent(transform);
                pup.transform.localPosition = Vector3.zero;
                SetGrappleMode( eMode.gRetract );
                break;
            case "Enemies": // We've hit an Enemy
                // The Grappler should return when it hits an Enemy
                Enemy e = colld.GetComponent<Enemy>();
                if ( e != null ) SetGrappleMode( eMode.gRetract );
                // Damaging the Enemy is handled by the DamageEffect & Enemy scripts
                break;
            case "GrapTiles": // We've hit a GrapTile
                SetGrappleMode( eMode.gPull );
                break;
            default:
                SetGrappleMode( eMode.gRetract );
                break;
        }
    }

    void GrappleDone() {
        SetGrappleMode( eMode.gIdle );

        // Callback to Dray so they return to normal controls
        gadgetDoneCallback( this );
    }

#region IGadget_Implementation
    // ----------------- Implementation of IGadget -----------------

    // Called by Dray to use this IGadget
    public bool GadgetUse( Dray tDray, System.Func<IGadget,bool> tCallback) {
        if ( mode != eMode.gIdle ) return false;

        dray = tDray;
        gadgetDoneCallback = tCallback;
        transform.localPosition = Vector3.zero;

        facing = dray.GetFacing();
        p0 = dray.transform.position;
        p1 = p0 + ( dirV3s[facing] * minLength );
        gameObject.transform.position = p1;
        gameObject.transform.rotation = Quaternion.Euler(0,0,90 * facing);

        line.positionCount = 2;
        line.SetPosition(0,p0);
        line.SetPosition(1,p1);

        SetGrappleMode( eMode.gOut );

        return true;
    }

    // Called by Dray if they are hit while grappling and mode != eMode.inHit
    public bool GadgetCancel() {
        // If pulling Dray to a wall, ignore GadgetCancel
        if ( mode == eMode.gPull ) return false;
        SetGrappleMode( eMode.gIdle );
        gameObject.SetActive(false);
        return true;
    }
    // string name is already part of Grappler (inherited from Object)
#endregion
}
