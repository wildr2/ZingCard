using UnityEngine;
using System.Collections;

public class CardMarker : MonoBehaviour
{
    private GameManager gm;
    private Color color;

    public void Initialize(GameManager gm, Color color)
    {
        this.gm = gm;
        this.color = color;
        StartCoroutine(Routine());
    }
    private IEnumerator Routine()
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.enabled = false;
        yield return new WaitForSeconds(2);

        // Fade in
        lr.enabled = true;
        for (float i = 0; i < 1; i += Time.deltaTime * 2f)
        {
            lr.material.color = Tools.SetColorAlpha(color, i);
            yield return null;
        }

        while (gm.GetGameState() != GameState.Reset && gm.GetGameState() != GameState.PostGame) yield return null;

        // Fade out
        lr.enabled = true;
        for (float i = 1; i >= 0; i -= Time.deltaTime * 10f)
        {
            lr.material.color = Tools.SetColorAlpha(color, i);
            yield return null;
        }
        lr.enabled = false;


        // Post game
        while (gm.GetGameState() != GameState.PostGame) yield return null;
        yield return new WaitForSeconds(6);

        // Fade in
        lr.enabled = true;
        for (float i = 0; i < 1; i += Time.deltaTime * 2f)
        {
            lr.material.color = Tools.SetColorAlpha(color, i);
            yield return null;
        }
    }
}
