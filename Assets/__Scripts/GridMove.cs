using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridMove : MonoBehaviour
{
    private IFacingMover mover;

    void Awake() {
        mover = GetComponent<IFacingMover>();
        if ( mover == null ) {
            Debug.LogError("Cannot find IFacingMover on " + gameObject.name );
        }
    }

    void FixedUpdate() {
        if ( !mover.moving ) return; // If not moving, nothing to do here
        int facing = mover.GetFacing();

        // If we are moving in a direction, align to the grid
        // First, get the grid location
        Vector2 posIR = mover.posInRoom;
        Vector2 posIRGrid = mover.GetGridPosInRoom();
        // This relies on IFacingMover (which uses InRoom) to choose grid spacing

        // Then move towards the grid line
        float delta = 0;
        if (facing == 0 || facing == 2) {
            // If the movement is horizontal, align to posIRGrid.y
            delta = posIRGrid.y - posIR.y;
        } else {
            // If the movement is vertical, align to posIRGrid.x
            delta = posIRGrid.x - posIR.x;
        }
        if (delta == 0) return; // Already aligned to the grid

        float gridAlignSpeed = mover.GetSpeed() * Time.fixedDeltaTime;
        gridAlignSpeed = Mathf.Min( gridAlignSpeed, Mathf.Abs(delta) );
        if (delta < 0) gridAlignSpeed = -gridAlignSpeed;

        if (facing == 0 || facing == 2) {
            posIR.y += gridAlignSpeed;
        } else {
            posIR.x += gridAlignSpeed;
        }

        mover.posInRoom = posIR;
    }
}
