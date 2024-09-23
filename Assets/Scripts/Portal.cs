using UnityEngine;
using UnityEngine.Rendering;

public class Portal : MonoBehaviour
{
    public Portal linkedPortal;
    public MeshRenderer screen;
    public Camera playerCamera;
    public Camera portalCamera;
    RenderTexture viewTexture;

    // Start is called before the first frame update
    void Awake()
    {
        playerCamera = Camera.main;
        portalCamera = GetComponentInChildren<Camera>();
        portalCamera.enabled = false;
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

    // Update the portal camera's position and render its view
    public void Render()
    {
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
}