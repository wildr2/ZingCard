using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    private GameManager gm;

    public int player_id = 0;
    public string player_name;
    public Color player_color;

    public Hand hand;
    public Transform head;
    private Vector3 hand_offset;

    public bool ai = false;
    public bool do_nothing = false;


    public void Initialize(GameManager gm)
    {
        this.gm = gm;
        hand.Initialize(this, gm);

        gm.event_new_state += (GameState s) =>
        {
            if (s == GameState.CardDraw) EnableControl(false);
            if (s == GameState.Play) EnableControl(true);
            if (s == GameState.Reset) EnableControl(false);
            if (s == GameState.PostGame) EnableControl(false);
        };
        gm.event_point += (Player p) => hand.EndTrail();

        if (!do_nothing)
        {
            if (!ai) StartCoroutine(HumanUpdate());
            else
            {
                hand_offset = hand.transform.position - head.transform.position;
                StartCoroutine(AIUpdate());
            }
        }
    }


    private IEnumerator HumanUpdate()
    {
        float reach = 0;

        Vector3 offset1 = new Vector3(0.15f, -0.4f, 0.5f);
        Vector3 offset2 = new Vector3(0, 0, 0.8f);

        while (true)
        {
            if (OVRInput.Get(OVRInput.Button.One))
            {
                reach = Mathf.Min(reach + Time.deltaTime * 5f, 1);
            }
            else
            {
                reach = Mathf.Max(reach - Time.deltaTime * 5f, 0);
            }

            head.transform.position = Camera.main.transform.position;
            head.transform.rotation = Camera.main.transform.rotation;


            Vector3 tip_target = head.transform.position + head.forward * 1f;
            Vector3 start = head.transform.position + head.right * 0.15f + head.up * -0.3f + head.forward * 0.5f;
            Vector3 target = start + (tip_target - start).normalized * 0.3f;
            target.z = Mathf.Max(target.z, -0.05f);

            hand.transform.rotation = Quaternion.LookRotation(tip_target - start, Vector3.up);
            hand.SetPosition(Vector3.Lerp(start, target, reach));

            //Vector3 start = head.transform.position + head.transform.rotation * offset1;
            //Vector3 target = head.transform.position + head.transform.rotation * offset2;
            //target.z = Mathf.Max(target.z, 0);
            //hand.SetPosition(Vector3.Lerp(start, target, reach));

            


            yield return null;
        }
    }
    private IEnumerator AIUpdate()
    {
        Vector3 hand_pos1 = hand.transform.position;

        while (true)
        {
            // Wait
            while (gm.GetGameState() != GameState.Play) yield return null;
            while (!gm.teller.HasShownTargetWord())
            {
                yield return null;
            }
            yield return new WaitForSeconds(Random.Range(0.25f, 2.5f));


            // Swing at card
            Vector3 target_pos = gm.card_manager.GetTargetCard().transform.position;
            Vector3 desired_dir = (target_pos - hand.transform.position).normalized;
            Vector3 dir = (desired_dir + Random.onUnitSphere).normalized;
            float speed = 4.5f;

            while (gm.GetGameState() == GameState.Play)
            {
                desired_dir = (target_pos - hand.transform.position).normalized;
                dir = Vector3.Lerp(dir, desired_dir, Time.deltaTime * speed * 1.5f);

                Vector3 p = hand.transform.position + dir * speed * Time.deltaTime;
                p.z = head.position.z > 0 ? Mathf.Max(p.z, 0) : Mathf.Min(p.z, 0);
                hand.SetPosition(p);

                AIUpdateHead(Vector3.Lerp(target_pos - head.transform.position,
                    hand.transform.position - head.transform.position, 0.2f));
                

                yield return null;
            }

            // Follow through
            while (gm.GetGameState() != GameState.Reset)
            {
                desired_dir = (hand_pos1 - hand.transform.position).normalized;
                dir = Vector3.Lerp(dir, desired_dir, Time.deltaTime * speed / 2f);
                speed = Mathf.Lerp(speed, 0, Time.deltaTime);

                Vector3 p = hand.transform.position + dir * speed * Time.deltaTime;
                p.z = head.position.z > 0 ? Mathf.Max(p.z, 0) : Mathf.Min(p.z, 0);
                hand.SetPosition(p);

                AIUpdateHead(Vector3.Lerp(target_pos - head.transform.position,
                    hand.transform.position - head.transform.position, 0.2f));

                yield return null;
            }

            // Return hand
            while (gm.GetGameState() == GameState.Reset)
            {
                hand.SetPosition(Vector3.Lerp(hand.transform.position, hand_pos1, Time.deltaTime * 2f));
                AIUpdateHead(transform.forward);

                yield return null;
            }

            yield return null;
        }
    }
    private void AIUpdateHead(Vector3 look_dir)
    {
        head.transform.rotation = Quaternion.Slerp(head.transform.rotation,
            Quaternion.LookRotation(look_dir, Vector3.up), Time.deltaTime * 4f);

        Vector3 head_target = hand.transform.position - hand_offset;
        head_target.y = Mathf.Lerp(transform.position.y, head_target.y, 0.2f);
        head.transform.position = Vector3.Lerp(head.transform.position, head_target, Time.deltaTime * 0.5f);
    }
    private void EnableControl(bool enable = true)
    {
        hand.EnableControl(enable);
    }


    public bool HasControl()
    {
        return hand.HasControl();
    }
}
