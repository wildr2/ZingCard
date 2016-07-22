using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Hand : MonoBehaviour
{
    private GameManager gm;
    private Player player;
    public MeshRenderer mesh;

    private Vector3 prev_pos;
    private Vector3 velocity;

    private TrailRenderer trail;
    private Vector3 trail_end_pos;
    private bool trail_ended = false;
    private bool has_control = false;


    public void Initialize(Player parent, GameManager gm)
    {
        this.gm = gm;
        player = parent;
        //mesh = GetComponent<MeshRenderer>();
        //mesh.material.color = player.player_color;

        trail = GetComponentInChildren<TrailRenderer>();
        trail.gameObject.SetActive(false);
        trail.material.color = player.player_color;
    }
    public void SetPosition(Vector3 pos)
    {
        prev_pos = transform.position;
        transform.position = pos;
        velocity = (pos - prev_pos) / Time.deltaTime;
        if (trail_ended) trail.transform.position = trail_end_pos;
    }
    public void EndTrail()
    {
        trail_end_pos = trail.transform.position;
        trail_ended = true;
    }
    public void EnableControl(bool enable)
    {
        has_control = enable;

        Color c = mesh.material.color;
        if (has_control)
        {
            StartCoroutine(RevealTrail());
            c.a = 1f;
        }
        else
        {
            c.a = 0.2f;
        }
        mesh.material.color = c;
    }

    private void OnTriggerEnter(Collider collider)
    {
        GameCard card = collider.GetComponentInParent<GameCard>();
        if (!card) return;
    }
    
    private IEnumerator RevealTrail()
    {
        trail_ended = false;
        trail.Clear();
        trail.transform.localPosition = Vector3.zero;
        trail.material.color = Tools.SetColorAlpha(player.player_color, 0f);
        trail.gameObject.SetActive(true);

        // Wait for post play
        while (gm.GetGameState() != GameState.PostPlay) yield return null;
        yield return new WaitForSeconds(2);

        // Fade in
        for (float i = 0; i < 1; i += Time.deltaTime * 2f)
        {
            trail.material.color = Tools.SetColorAlpha(player.player_color, i);
            yield return null;
        }
    }

    public bool HasControl()
    {
        return has_control;
    }
    public Vector3 GetVelocity()
    {
        return velocity;
    }
    public Player GetPlayer()
    {
        return player;
    }
}
