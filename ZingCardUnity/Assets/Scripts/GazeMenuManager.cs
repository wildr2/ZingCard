using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GazeMenuManager : MonoBehaviour
{
    private GazeButton selected;
    public GraphicRaycaster gr;
    public Canvas[] canvases;
    public Transform cursor;


    private void Update()
    {
        Vector3 eye = Camera.main.transform.position;
        Vector3 look = Camera.main.transform.forward;

        bool hit = false;

        foreach (Canvas canvas in canvases)
        {
            RectTransform rt = canvas.GetComponent<RectTransform>();
            Bounds b = new Bounds(rt.transform.position, Vector3.Scale(rt.localScale, rt.rect.size));

            float dist;
            Quaternion inv = Quaternion.Inverse(rt.transform.rotation);
            Ray ray = new Ray(RotatePointAroundPivot(eye, b.center, inv.eulerAngles), inv * look);
            
            if (b.IntersectRay(ray, out dist))
            {
                // Find 2D gaze point on canvas
                Vector3 point = RotatePointAroundPivot(ray.GetPoint(dist), b.center, rt.transform.rotation.eulerAngles);
                Vector2 point2d = ray.GetPoint(dist) - b.center;
                point2d = Vector2.Scale((point2d), rt.localScale);

                cursor.transform.position = point;

                // Raycast 
                //List<RaycastResult> results = new List<RaycastResult>();
                //PointerEventData pointer = new PointerEventData(EventSystem.current);
                //pointer.position = Vector2.zero;
                //EventSystem.current.RaycastAll(pointer, Event);
                //List<RaycastResult> results = m_RaycastResultCache;
                //EventSystem.current.RaycastAll(pointer, results);

                break;
            }
        }


        //RaycastHit[] hits = Physics.RaycastAll(eye, look);
        //GazeButton btn = null;
        //foreach (RaycastHit hit in hits)
        //{
        //    Tools.Log(hit.collider.name);
        //    btn = hit.collider.GetComponent<GazeButton>();
        //    if (btn != null) break;
        //}
        //if (btn != null)
        //{
        //    if (btn != selected) btn.Highlight();
        //    else
        //    {
        //        if (selected != null) selected.Highlight(false);
        //        selected = btn;
        //        selected.Highlight();
        //    }
        //}
        //else if (selected != null)
        //{
        //    selected.Highlight(false);
        //    selected = null;
        //}
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        Vector3 dir = point - pivot; // get point direction relative to pivot
        dir = Quaternion.Euler(angles) * dir; // rotate it
        point = dir + pivot; // calculate rotated point
        return point; // return it
    }
}
