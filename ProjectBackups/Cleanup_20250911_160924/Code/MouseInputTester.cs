using UnityEngine;

namespace WallChess
{
    public class MouseInputTester : MonoBehaviour
    {
        void Update()
        {
            // Test mouse input detection
            if (Input.GetMouseButtonDown(0))
            {
                Camera cam = Camera.main;
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                Debug.Log($"Mouse clicked at screen position: {Input.mousePosition}");
                Debug.Log($"Camera ray origin: {ray.origin}, direction: {ray.direction}");
                
                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log($"Hit object: {hit.collider.gameObject.name} at position: {hit.point}");
                    
                    // Check if it's a player object
                    if (hit.collider.gameObject.name == "Player")
                    {
                        Debug.Log("Player object detected by raycast!");
                        
                        // Try to trigger OnMouseDown manually
                        hit.collider.gameObject.SendMessage("OnMouseDown", SendMessageOptions.DontRequireReceiver);
                    }
                }
                else
                {
                    Debug.Log("No object hit by raycast");
                }
            }
        }
    }
}