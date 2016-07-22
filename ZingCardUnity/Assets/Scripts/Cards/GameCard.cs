using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class GameCard : Card
{
    private GameManager gm;
    public CardMarker card_marker_prefab;

    private Color color_initial;

    private bool is_hit = false;
    private Hand touching_hand;
    public Action<int> event_hit;

    private Player winner;



    // PUBLIC MODIFIERS

    public void Initialize(string word, Vector3 board_pos, Quaternion board_rot, GameManager gm)
    {
        this.gm = gm;

        this.board_pos = board_pos;
        this.board_rot = board_rot;

        SetText(word);
        ShowText(false);

        color_initial = mesh.material.color;
    }
    public void SetWinner(Player winner)
    {
        SetColor(winner.player_color);
        this.winner = winner;
        CreateCardMarker(winner);
    }
    public void KnockAwayFrom(Player player)
    {
        rb.AddForce(Vector3.forward * (player.player_id == 0 ? -1 : 1) * 0.5f,
            ForceMode.Impulse);
    }
    public override void SetOnBoard(float seconds=1)
    {
        base.SetOnBoard(seconds);
        is_hit = false;
    }


    // PRIVATE MODIFIERS

    private void SetColor(Color color)
    {
        mesh.material.color = color;
    }
    private void CreateCardMarker(Player winner)
    {
        CardMarker marker = Instantiate(card_marker_prefab);
        marker.transform.position = board_pos;
        marker.transform.rotation = board_rot;
        marker.Initialize(gm, winner.player_color);
    }

    private void OnHit(Player hitter)
    {
        is_hit = true;
        if (event_hit != null) event_hit(hitter.player_id);
    }
    private void OnTriggerEnter(Collider collider)
    {
        Hand hand = collider.GetComponentInParent<Hand>();
        if (hand != null && touching_hand == null && hand.HasControl())
        {
            touching_hand = hand;
            Player hitter = hand.GetPlayer();
            if (!is_hit) OnHit(hitter);
        }
    }
    private void OnTriggerExit(Collider collider)
    {
        Hand hand = collider.GetComponentInParent<Hand>();
        if (hand == touching_hand)
        {
            touching_hand = null;
        }
    }
    private void OnTriggerStay(Collider collider)
    {
        Hand hand = collider.GetComponentInParent<Hand>();
        if (hand == touching_hand)
        {
            if (!hand.HasControl())
            {
                touching_hand = null;
                return;
            }
            rb.AddForceAtPosition(hand.GetVelocity() * 8f, hand.transform.position, ForceMode.Force);
        }
    }


    // PUBLIC ACCESSORS

    public Player GetWinner()
    {
        return winner;
    }
    public bool IsHit()
    {
        return is_hit;
    }
    public bool IsWon()
    {
        return winner != null;
    }
    public Rigidbody GetRB()
    {
        return rb;
    }
}
