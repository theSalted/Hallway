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

    public float nearClipOffset = 0.05f;
    public float nearClipLimit = 0.2f;

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

   void HandleTravellers() {
        for (int i = 0; i < trackedTravellers.Count; i++) {
            PortalTraveller traveller = trackedTravellers[i];
            Transform travellerT = traveller.transform;

            Vector3 offsetFromPortal = travellerT.position - transform.position;
            float dotProduct = Vector3.Dot(offsetFromPortal, transform.forward);
            int portalSide = System.Math.Sign(dotProduct);
            int portalSideOld = System.Math.Sign(Vector3.Dot(traveller.previousOffsetFromPortal, transform.forward));

            // Debugging output
            Debug.Log($"Traveller: {traveller.name}, DotProduct: {dotProduct}, PortalSide: {portalSide}, PreviousPortalSide: {portalSideOld}");

            // Teleport the traveller if it has crossed from one side of the portal to the other
            if (portalSide != portalSideOld && dotProduct != 0) {
                var m = linkedPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * travellerT.localToWorldMatrix;
                traveller.Teleport(transform, linkedPortal.transform, m.GetColumn(3), m.rotation);
                // Update previousOffsetFromPortal after teleportation
                traveller.previousOffsetFromPortal = traveller.transform.position - transform.position;
            } else {
                // Update the previous offset
                traveller.previousOffsetFromPortal = offsetFromPortal;
            }
        }
    }

    void SetNearClipPlane() {
         Transform clipPlane = transform;
        int dot = System.Math.Sign (Vector3.Dot (clipPlane.forward, transform.position - portalCamera.transform.position));

        Vector3 camSpacePos = portalCamera.worldToCameraMatrix.MultiplyPoint (clipPlane.position);
        Vector3 camSpaceNormal = portalCamera.worldToCameraMatrix.MultiplyVector (clipPlane.forward) * dot;
        float camSpaceDst = -Vector3.Dot (camSpacePos, camSpaceNormal) + nearClipOffset;

        // Don't use oblique clip plane if very close to portal as it seems this can cause some visual artifacts
        if (Mathf.Abs (camSpaceDst) > nearClipLimit) {
            Vector4 clipPlaneCameraSpace = new Vector4 (camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);

            // Update projection based on new clip plane
            // Calculate matrix with player cam so that player camera settings (fov, etc) are used
            portalCamera.projectionMatrix = portalCamera.CalculateObliqueMatrix (clipPlaneCameraSpace);
        } else {
            portalCamera.projectionMatrix = portalCamera.projectionMatrix;
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

        SetNearClipPlane();

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