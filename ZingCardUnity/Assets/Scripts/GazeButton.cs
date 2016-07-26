using UnityEngine;
using System.Collections;

public class GazeButton : MonoBehaviour
{
    public Transform highlight_obj;

    public void Highlight(bool enable=true)
    {
        highlight_obj.gameObject.SetActive(enable);
    }
}
