using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Hand : MonoBehaviour
{
    private GameManager gm;
    private Player player;
    public MeshRenderer mesh;
    public Transform hand_wall;

    private Vector3 prev_pos;
    private Vector3 velocity;

    private TrailRenderer trail;
    private Vector3 trail_end_pos;
    private bool trail_ended = false;
    private bool has_control = false;

    private float hand_wall_dist = 0.45f;
    private bool hand_wall_on = false; 
    public System.Action<Player> event_hand_wall_tripped;


    public void Initialize(Player parent, GameManager gm)
    {
        this.gm = gm;
        player = parent;
        //mesh = GetComponent<MeshRenderer>();
        //mesh.material.color = player.player_color;

        trail = GetComponentInChildren<TrailRenderer>();
        trail.gameObject.SetActive(false);
        trail.material.color = player.player_color;

        EnableHandWall(false);
        StartCoroutine(UpdateHandWall());
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
    public void StartTrail()
    {
        StartCoroutine(UpdateTrail());
    }
    public void EnableControl(bool enable)
    {
        has_control = enable;

        Color c = mesh.material.color;
        if (has_control)
        {
            c.a = 1f;
        }
        else
        {
            c.a = 0.2f;
        }
        mesh.material.color = c;
    }
    public void EnableHandWall(bool enable=true)
    {
        hand_wall.gameObject.SetActive(enable);
        hand_wall_on = enable;
    }

    private void OnTriggerEnter(Collider collider)
    {
        GameCard card = collider.GetComponentInParent<GameCard>();
        if (!card) return;
    }
    private IEnumerator UpdateHandWall()
    {
        MeshRenderer mr = hand_wall.GetComponentInChildren<MeshRenderer>();
        float alpha_1 = mr.material.color.a;
        Vector3 local_scale = hand_wall.localScale;

        bool behind_hand_wall_last = false;

        while (true)
        {
            if (hand_wall_on)
            {
                // Position
                Vector3 pos = transform.position;
                pos.z = player.player_id == 0 ? hand_wall_dist : -hand_wall_dist;
                hand_wall.transform.position = pos;
                hand_wall.rotation = Quaternion.LookRotation(transform.position - pos) * Quaternion.Euler(90, 0, 0);

                // Distance to handwall
                float dist = Mathf.Abs(transform.position.z - hand_wall.transform.position.z);

                // Alpha
                //float a = dif < 0 ? 0 : Mathf.Lerp(1, 0, dif / 0.3f);
                //mr.material.color = Tools.SetColorAlpha(mr.material.color, alpha_1*a);

                // Scale
                hand_wall.localScale = local_scale * Mathf.Lerp(1.5f, 0, dist / 0.3f);

                // Trip
                if (IsBehindHandWall())
                {
                    behind_hand_wall_last = true;
                }
                else
                {
                    if (behind_hand_wall_last)
                    {
                        if (event_hand_wall_tripped != null) event_hand_wall_tripped(player);
                    }
                    behind_hand_wall_last = false;
                }
            }

            yield return null;
        }
    }
    private IEnumerator UpdateTrail()
    {
        trail_ended = false;
        trail.Clear();
        trail.transform.localPosition = Vector3.zero;
        trail.material.color = Tools.SetColorAlpha(player.player_color, 0f);
        trail.time = 20;
        trail.gameObject.SetActive(true);

        float start_time = Time.time;


        // Wait for post play
        while (gm.GetGameState() != GameState.PostPlay) yield return null;
        yield return new WaitForSeconds(2);

        // Fade in
        for (float i = 0; i < 1; i += Time.deltaTime / 0.5f)
        {
            trail.material.color = Tools.SetColorAlpha(player.player_color, i);
            yield return null;
        }

        // Outro
        while (gm.GetGameState() != GameState.Reset && gm.GetGameState() != GameState.PostGame) yield return null;
        float t = Time.time - start_time;
        trail.time = t;
        //for (float i = 0; i < 1; i += Time.deltaTime / 2f)
        //{
        //    trail.time = Mathf.Lerp(t, 0, i);
        //    yield return null;
        //}
        //trail.time = 0;
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
    public bool IsBehindHandWall()
    {
        return player.player_id == 0 ? 
            transform.position.z >= hand_wall_dist :
            transform.position.z <= -hand_wall_dist;
    }
}
