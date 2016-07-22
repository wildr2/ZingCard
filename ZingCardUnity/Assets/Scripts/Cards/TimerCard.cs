using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TimerCard : Card
{
    private GameManager gm;
    public Image timer_img;
    private int id;

    public void Initialize(GameManager gm, Vector3 board_pos, Quaternion board_rot, int id)
    {
        this.gm = gm;
        this.id = id;

        this.board_pos = board_pos;
        this.board_rot = board_rot;

        // Clear face
        timer_img.gameObject.SetActive(false);
        SetText("");

        StartCoroutine(TimerRoutine());
    }

    private IEnumerator TimerRoutine()
    {
        while (gm.GetGameState() != GameState.Memorize) yield return null;

        // Show timer
        timer_img.gameObject.SetActive(true);
        
        // Spin
        while (gm.GetGameState() == GameState.Memorize)
        {
            transform.Rotate(Vector3.up, (360f / gm.GetMemorizeDuration()) * Time.deltaTime, Space.Self);
            yield return null;
        }

        // Time up - drop card
        timer_img.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.1f * id);
        Stack(gm.court.main_stack_pos, id, 1);
    }
}
