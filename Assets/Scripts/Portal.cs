using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public Portal linkedPortal;
    public MeshRenderer screen;
    public Camera playerCamera;
    public Camera portalCamera;
    RenderTexture viewTexture;
    
    List<PortalTraveller> trackedTravellers;

    // Start is called before the first frame update
    void Awake()
    {
        playerCamera = Camera.main;
        portalCamera = GetComponentInChildren<Camera>();
        portalCamera.enabled = false;
        trackedTravellers = new List<PortalTraveller> ();
    }
    
    void LateUpdate()
    {
        HandleTravellers();
    }

    void HandleTravellers () {

        for (int i = 0; i < trackedTravellers.Count; i++) {
            PortalTraveller traveller = trackedTravellers[i];
            Transform travellerT = traveller.transform;
            var m = linkedPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * travellerT.localToWorldMatrix;

            Vector3 offsetFromPortal = travellerT.position - transform.position;
            int portalSide = System.Math.Sign (Vector3.Dot (offsetFromPortal, transform.forward));
            int portalSideOld = System.Math.Sign (Vector3.Dot (traveller.previousOffsetFromPortal, transform.forward));
            // Teleport the traveller if it has crossed from one side of the portal to the other
            if (portalSide != portalSideOld) {
                traveller.Teleport (transform, linkedPortal.transform, m.GetColumn (3), m.rotation);
                trackedTravellers.RemoveAt (i);
                i--;

            } else {
                //UpdateSliceParams (traveller);
                traveller.previousOffsetFromPortal = offsetFromPortal;
            }
        }
    }

    // Create the RenderTexture for portal view
    void CreateViewTexture()
    {
        if (viewTexture == null || viewTexture.width != Screen.width || viewTexture.height != Screen.height) {
            if (viewTexture != null) {
                viewTexture.Release ();
            }
            viewTexture = new RenderTexture (Screen.width, Screen.height, 0);
            // Render the view from the portal camera to the view texture
            portalCamera.targetTexture = viewTexture;
            // Display the view texture on the screen of the linked portal
            linkedPortal.screen.material.SetTexture ("_MainTex", viewTexture);
        }
    }

    static bool VisibleFromCamera(Renderer renderer, Camera cam)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
    }

    float ProtectScreenFromClipping (Vector3 viewPoint) {
        float halfHeight = playerCamera.nearClipPlane * Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * playerCamera.aspect;
        float dstToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, playerCamera.nearClipPlane).magnitude;
        float screenThickness = dstToNearClipPlaneCorner;

        Transform screenT = screen.transform;
        bool camFacingSameDirAsPortal = Vector3.Dot(transform.forward, transform.position - viewPoint) > 0;
        screenT.localScale = new Vector3(screenT.localScale.x, screenT.localScale.y, screenThickness);
        screenT.localPosition = Vector3.forward * screenThickness * ((camFacingSameDirAsPortal) ? 0.5f : -0.5f);
        return screenThickness;
    }

    // Update the portal camera's position and render its view
    public void Render()
    {
        if (!VisibleFromCamera(linkedPortal.screen, playerCamera)) {
            return;
        }
        // Disable screen renderer to prevent recursion effect during render
        screen.enabled = false;

        CreateViewTexture();

        // Set the portal camera to match the player's view but transformed into the linked portal's space
        var matrix = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix * playerCamera.transform.localToWorldMatrix;
        portalCamera.transform.SetPositionAndRotation(matrix.GetColumn(3), matrix.rotation);

        // Render the portal camera's view
        portalCamera.Render();

        // Re-enable screen rendering after the render is done
        screen.enabled = true;
    }

    public void PostPortalRender () {
        ProtectScreenFromClipping (playerCamera.transform.position);
    }

    void OnTravellerEnter(PortalTraveller traveller)
    {
        if (!trackedTravellers.Contains(traveller)) {
            traveller.EnterPortalThreshold();
            traveller.previousOffsetFromPortal = traveller.transform.position - transform.position;
            trackedTravellers.Add(traveller);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter");
        var traveller = other.GetComponent<PortalTraveller>();
        if (traveller) {
            OnTravellerEnter(traveller);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("OnTriggerExit");
        var traveller = other.GetComponent<PortalTraveller>();
        if (traveller && trackedTravellers.Contains(traveller)) {
            traveller.ExitPortalThreshold();
            trackedTravellers.Remove(traveller);
        }
    }
}