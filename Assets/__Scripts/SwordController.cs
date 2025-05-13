using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordController : MonoBehaviour
{
    private GameObject sword;
    private Dray dray;

    void Start()
    {
        // Find the Sword child of SwordController
        Transform swordT = transform.Find( "Sword" );
        if ( swordT == null ) {
            Debug.LogError("Could not find Sword child of SwordController.");
            return;
        }
        sword = swordT.gameObject;

        // Find the Dray component on the parent of SwordController
        dray = GetComponentInParent<Dray>();
        if ( dray == null ) {
            Debug.LogError("Could not find parent component Dray.");
            return;
        }

        // Deactivate the sword
        sword.SetActive(false);
    }

    void Update()
    {
        transform.rotation = Quaternion.Euler( 0, 0, 90*dray.facing );
        sword.SetActive(dray.mode == Dray.eMode.attack);
    }
}
