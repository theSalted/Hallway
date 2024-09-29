using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    // Start is called before the first frame update
    Portal[] portals;
    void Awake()
    {
        portals = FindObjectsOfType<Portal>();
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < portals.Length; i++)
        {
            portals[i].Render();
        }

        //  for (int i = 0; i < portals.Length; i++) {
        //     portals[i].PostPortalRender();
        // }
    }
}
