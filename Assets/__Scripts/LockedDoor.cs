using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class LockedDoor : MonoBehaviour, ISwappable
{
    static private LockedDoor[,] _LOCKED_DOORS;
    static private Dictionary<int, DoorInfo> _DOOR_INFO_DICT;

    // These consts are based on the default DelverTiles image.
    // If you rearrange DelverTiles, you may need to change it!
    //-------------- LockedDoor tileNums --------------
    const int LOCKED_R = 73;
    const int LOCKED_UR = 57;
    const int LOCKED_UL = 56;
    const int LOCKED_L = 72;
    const int LOCKED_DL = 88;
    const int LOCKED_DR = 89;

    public Vector2Int mapLoc; // Coords on MapInfo.MAP where this tile is

    /// <summary>
    /// This public DoorInfo class is defined *inside* of the LockedDoor class.
    /// <summary>
    public class DoorInfo {
        public int tileNum;
        public Vector2Int otherHalf;

        /// <summary>
        /// This constructor for the DoorInfo class constructs a new DoorInfo
        /// AND adds it to LockedDoor._DOOR_INFO_DICT
        /// </summary>
        /// <param name="tN">tileNum of the door Sprite</param>
        /// <param name="oH">relative location of the otherHalf door</param>
        public DoorInfo( int tN, Vector2Int oH ) {
            tileNum = tN;
            otherHalf = oH;
            if ( _DOOR_INFO_DICT != null ) {
                _DOOR_INFO_DICT.Add( tileNum, this );
            }
        }
    }

    void Awake()
    {
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _LOCKED_DOORS = null;
        _DOOR_INFO_DICT = null;

        if ( _LOCKED_DOORS == null ) {
            BoundsInt mapBounds = MapInfo.GET_MAP_BOUNDS();
            _LOCKED_DOORS = new LockedDoor[mapBounds.size.x, mapBounds.size.y];
            InitDoorInfoDict();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from the sceneLoaded event to avoid memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        if ( _LOCKED_DOORS == null ) {
            BoundsInt mapBounds = MapInfo.GET_MAP_BOUNDS();
            _LOCKED_DOORS = new LockedDoor[mapBounds.size.x, mapBounds.size.y];
            InitDoorInfoDict();
        }
        mapLoc = Vector2Int.FloorToInt( transform.position );
        _LOCKED_DOORS[mapLoc.x, mapLoc.y] = this;
    }

    /// <summary>
    /// Initialize the _DOOR_INFO_DICT Dictionary that holds information about
    /// the facing required to open each door and the relative location of the
    /// otherHalf door, if any (e.g., lockedUL and lockedUR are otherHalves)
    /// </summary
    void InitDoorInfoDict() {
        _DOOR_INFO_DICT = new Dictionary<int, DoorInfo>();

        new DoorInfo( LOCKED_R, Vector2Int.zero );
        new DoorInfo( LOCKED_UR, Vector2Int.left );
        new DoorInfo( LOCKED_UL, Vector2Int.right );
        new DoorInfo( LOCKED_L, Vector2Int.zero );
        new DoorInfo( LOCKED_DL, Vector2Int.right );
        new DoorInfo( LOCKED_DR, Vector2Int.left );
    }

    /// <summary>
    /// Every Physics update that this GameObject is in contact with Dray, check
    /// to see if Dray has a key and is facing the right direction. If so, this
    /// LockedDoor and its otherHalf door are destroyed.
    /// </summary
    /// <param name="coll">Collision2D information</param>
    void OnCollisionStay2D( Collision2D coll ) {
        // Return if this LockedDoor's location in _LOCKED_DOORS is already null
        if ( GET_LOCKED_DOOR( mapLoc ) == null ) return;

        IKeyMaster iKeyM = coll.gameObject.GetComponent<IKeyMaster>();
        if ( iKeyM == null ) return;

        if ( !_DOOR_INFO_DICT.ContainsKey( tileNum ) ) {
            Debug.LogError("_DOOR_INFO_DICT has no key " + tileNum);
            return;
        }

        DoorInfo myDoor = _DOOR_INFO_DICT[tileNum];
        int reqFacing = GetRequiredFacingToOpenDoor( iKeyM );
        if ( iKeyM.keyCount > 0 && iKeyM.GetFacing() == reqFacing ) {
            // Decrement the number of keys held by this IKeyMaster
            iKeyM.keyCount--;
            // Destroy this Door
            Destroy(gameObject);
            // Clear this mapLoc in _LOCKED_DOORS
            _LOCKED_DOORS[mapLoc.x, mapLoc.y] = null;

            // Destroy otherHalf
            if ( myDoor.otherHalf == Vector2Int.zero ) return;
            Vector2Int otherHalfLoc = mapLoc + myDoor.otherHalf;
            LockedDoor otherLD = GET_LOCKED_DOOR( otherHalfLoc );
            if ( otherLD != null ) {
                Destroy( otherLD.gameObject );
                _LOCKED_DOORS[otherHalfLoc.x, otherHalfLoc.y] = null;
            }
        }
    }

    /// <summary>
    /// Uses relative positions of iKeyM and this LockedDoor to determine which
    /// direction iKeyM must face to be looking at the door. A LockedDoor will
    /// not open unless the iKeyM is facing it.
    /// </summary
    /// <param name="iKeyM">The iKeyM that has collided with this door</param>
    /// <returns>An int [0..3] representing the facing required</returns>
    int GetRequiredFacingToOpenDoor( IKeyMaster iKeyM ) {
        // Because A - B looks at A, relPos is the direction iKeyM needs to face
        Vector2 relPos = (Vector2) transform.position - iKeyM.pos;
        // It is impossible for relPos.magnitude to be much less than 1
        if ( Mathf.Abs( relPos.x ) > Mathf.Abs( relPos.y ) ) {
            return ( relPos.x > 0 ) ? 0 : 2; // 0 = right, 2 = left
        } else {
            return ( relPos.y > 0 ) ? 1 : 3; // 1 = up, 3 = down
        }
    }

    /// <summary>
    /// Gets the LockedDoor at a specific location in the _LOCKED_DOORS 2D array
    /// or null if there is nothing in the array at that location.
    /// </summary
    /// <param name="mLoc">The 2D location to look for the LockedDoor</param>
    /// <returns>The LockedDoor at that location or null</returns>
    LockedDoor GET_LOCKED_DOOR( Vector2Int mLoc ) {
        if ( _LOCKED_DOORS == null ) return null;
        if ( mLoc.x < 0 || mLoc.x >= _LOCKED_DOORS.GetLength( 0 ) ) return null; 
        if ( mLoc.y < 0 || mLoc.y >= _LOCKED_DOORS.GetLength( 1 ) ) return null; 
        // This is a valid location
        return _LOCKED_DOORS[mLoc.x, mLoc.y];
    }

    // ------------------- Implementation of ISwappable -------------------
    public GameObject guaranteedDrop { get; set; }

    public int tileNum { get; private set; }

    public void Init( int fromTileNum, int tileX, int tileY ) {
        tileNum = fromTileNum;

        TilemapManager tilemapManager = FindAnyObjectByType<TilemapManager>(); // Find the instance of TilemapManager
        if (tilemapManager == null)
        {
            Debug.LogError("TilemapManager instance not found in the scene.");
            return;
        }

        // Get the Sprite for this SpriteRenderer
        SpriteRenderer sRend = GetComponent<SpriteRenderer>();
        sRend.sprite = TilemapManager.DELVER_TILES[fromTileNum].sprite;

        // Position this GameObject correctly
        transform.position = new Vector3( tileX, tileY, 0 ) + MapInfo.OFFSET;
    }
}
