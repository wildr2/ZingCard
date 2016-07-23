using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

public class CardManager : MonoBehaviour
{
    // Dev
    public bool dev = false;

    // General
    private GameManager gm;
    private Teller teller;

    // Cards
    public GameCard card_prefab;
    public TimerCard timer_card_prefab;
    private GameCard[] game_cards;
    private GameCard[][] game_card_grid;
    private TimerCard[] timer_cards;
    private Card[] cards_stack;
    private GameCard target_card;

    // Layout
    float mid_gap_x = 0.45f;
    private int rows = 6;
    private int cols = 8;
    private string[] layout = new string[]
    {
        "CCCC   CCCC",
        "CCC-   -CCC",
        "CC--   --CC",
        "CC--   --CC",
        "CCC-   -CCC",
        "CCCC   CCCC"
    };

    // Events
    public Action<int, GameCard> event_card_hit;
    public Action event_board_setup;



    // PUBLIC MODIFIERS

    public void Initialize(GameManager gm, Teller teller)
    {
        this.gm = gm;
        this.teller = teller;

        CreateCards(gm);

        // Events
        gm.event_new_state += (GameState s) =>
        {
            if (s == GameState.CardDraw) DrawCards();
            if (s == GameState.Play) SetTargetCard();
            if (s == GameState.Reset) ResetCards();
            if (s == GameState.PostGame) StartCoroutine(OnGameOver());
        };
    }

    public IEnumerator OnGameOver()
    {
        //Time.timeScale = 0.1f;
        //Time.fixedDeltaTime *= 0.1f;

        Player winner = gm.GetWinner();

        // Stack last won card (no reset happened)
        StackWonCard(target_card);

        // Drop remaining cards
        foreach (GameCard card in game_cards)
        {
            if (!card.IsWon()) card.EnablePhysics(true);
        }
        yield return new WaitForSeconds(1);

        // Stack remaining cards
        int stack_i = 2;
        foreach (GameCard card in game_cards)
        {
            if (!card.IsWon())
            {
                card.Stack(gm.court.main_stack_pos, stack_i, 1);
                ++stack_i;
            }
        }


        //Vector3 winner_stack = gm.court.player_stacks[winner.player_id].position;
        //if (dev)
        //{
        //    // stack game cards in winner stack
        //    for (int i = 0; i < 16; ++i)
        //    {
        //        game_cards[i].Stack(winner_stack, Quaternion.identity, i);
        //        game_cards[i].SetWinner(winner);
        //    }
        //}

        //foreach (GameCard card in game_cards)
        //{
        //    card.EnablePhysics(true);
        //}
        //foreach (GameCard card in game_cards)
        //{
        //    Vector3 center = gm.court.transform.position;

        //    if (card.GetWinner() == winner)
        //    {
        //        Vector3 force = (Vector3.up + UnityEngine.Random.onUnitSphere * 0.5f).normalized
        //            * UnityEngine.Random.Range(0.7f, 1.3f);
        //        Debug.DrawLine(card.transform.position, card.transform.position + force, Color.red, 5);
        //        card.GetRB().AddForce(force, ForceMode.Impulse);
        //        card.GetRB().AddTorque(UnityEngine.Random.onUnitSphere, ForceMode.Impulse); 
        //    }
        //    else
        //    {
        //        Vector3 force = UnityEngine.Random.onUnitSphere * 1f;
        //        force.y *= Mathf.Sign(force.y);
        //        card.GetRB().AddForce(force, ForceMode.Impulse);
        //        card.GetRB().AddTorque(UnityEngine.Random.onUnitSphere, ForceMode.Impulse);
        //    }
        //}

    }
    public void ResetCards()
    {
        foreach (GameCard card in game_cards)
        {
            if (!card.IsWon()) card.SetOnBoard(1);
            if (card == target_card) StackWonCard(card,
                () => { if (event_board_setup != null) event_board_setup(); });
        }
    }
    public void ShowCardsText(bool show=true)
    {
        foreach (GameCard card in game_cards)
        {
            card.ShowText(show);
        }
    }


    // PRIVATE MODIFIERS

    private void CreateCards(GameManager gm)
    {
        int game_cards_n = 36;

        // Determine card dimensions
        GameCard temp_card = Instantiate(card_prefab);
        Bounds card_bounds = temp_card.GetComponent<Collider>().bounds;
        float card_w = card_bounds.size.z; // THIS NEEDS FIXING IN MODEL
        float card_h = card_bounds.size.x;  
        Destroy(temp_card.gameObject);

        // Other dimensions
        float w = gm.court.GetTopRightBoard().x - gm.court.GetBotLeftBoard().x;
        float h = gm.court.GetTopRightBoard().y - gm.court.GetBotLeftBoard().y;
        float x_step = (w - mid_gap_x) / (cols);
        float y_step = h / (rows);

        Vector3 board_center = gm.court.board.transform.position;

        int i = 0; // card number
        timer_cards = new TimerCard[2];
        game_cards = new GameCard[game_cards_n];


        if (gm.GetMemorizeDuration() > 0)
        {
            // Timer Cards
            for (i = 0; i < 2; ++i)
            {
                Vector3 pos = board_center + Vector3.up * 0.625f
                    + Vector3.forward * (i == 0 ? 0.01f : -0.01f);
                Quaternion rot = Quaternion.Euler(-90, 180 + i * 180, 0);

                // Create card
                TimerCard card = Instantiate(timer_card_prefab);
                card.transform.SetParent(transform, false);
                card.Initialize(gm, pos, rot, i);

                // Add to game_cards array
                timer_cards[i] = card;
            }
        }

        // Game Cards
        for (i = 0; i < layout.Length; ++i) layout[i] = layout[i].Replace(" ", string.Empty);
        game_card_grid = new GameCard[cols][];

        i = 0;
        for (int x = 0; x < cols; ++x)
        {
            game_card_grid[x] = new GameCard[rows];
            for (int y = 0; y < rows; ++y) // Column
            {
                if (layout[y][x] != 'C') continue;

                float offset = x >= cols / 2 ? mid_gap_x : 0;
                Vector3 board_pos = board_center + new Vector3(
                    gm.court.GetBotLeftBoard().x + x_step/2f + x * x_step + offset,
                    gm.court.GetBotLeftBoard().y + y_step/2f + y * y_step, 0);

                // Create card
                GameCard card = Instantiate(card_prefab);
                card.transform.SetParent(transform, false);
                card.Initialize(teller.GetTargetWord(i), board_pos, Quaternion.Euler(-90, 0, 0), gm);
                card.event_hit += (int id) => { if (event_card_hit != null) event_card_hit(id, card); };

                // Add to game_cards array
                game_cards[i] = card;
                game_card_grid[x][y] = card;
                ++i;
            }
        }

        // Stack
        StackAllCards();
    }
    private void StackAllCards()
    {
        bool use_timer_cards = gm.GetMemorizeDuration() > 0;

        cards_stack = new Card[game_cards.Length + (use_timer_cards ? timer_cards.Length : 0)];
        int i = 0;

        if (use_timer_cards)
        {
            // Timer Cards
            foreach (TimerCard card in timer_cards)
            {
                card.Stack(gm.court.main_stack_pos.transform.position, Quaternion.Euler(0, 90, 0), i);
                cards_stack[i] = card;
                ++i;
            }
        }

        // Game Cards
        for (int x = cols / 2 - 1; x > -1; --x)
        {
            for (int y = 0; y < rows; ++y) // Column
            {
                Card card = game_card_grid[x][y];
                if (card == null) continue;
                card.Stack(gm.court.main_stack_pos.transform.position, Quaternion.Euler(0, 90, 0), i);
                cards_stack[i] = card;
                ++i;
            }
        }
        for (int x = cols / 2; x < cols; ++x)
        {
            for (int y = 0; y < rows; ++y) // Column
            {
                Card card = game_card_grid[x][y];
                if (card == null) continue;
                card.Stack(gm.court.main_stack_pos.transform.position, Quaternion.Euler(0, 90, 0), i);
                cards_stack[i] = card;
                ++i;
            }
        }
    }
    private void DrawCards()
    {
        if (dev) DrawCardsImmediate();
        else StartCoroutine(DrawCardsRoutine());
    }
    private IEnumerator DrawCardsRoutine()
    {
        // Move cards
        for (int i = cards_stack.Length - 1; i > -1; --i)
        {
            cards_stack[i].SetOnBoard();
            yield return new WaitForSeconds(0.15f);
        }

        // Wait for last card to finish moving
        bool flag = false;
        cards_stack[0].event_done_moving += () => flag=true;
        while (flag == false) yield return null;

        // Reveal game cards 
        for (int y = 0; y < rows; ++y)
        {
            for (int x = 0; x < cols / 2; ++x)
            {
                yield return new WaitForSeconds(0.01f);
                if (game_card_grid[x][y] != null)
                    game_card_grid[x][y].ShowText();
                if (game_card_grid[cols / 2 + x][y] != null)
                    game_card_grid[cols / 2 + x][y].ShowText();
            }
        }

        // Start memorization
        if (event_board_setup != null) event_board_setup(); 
    }
    private void DrawCardsImmediate()
    {
        // Move cards
        foreach (Card card in cards_stack)
        {
            card.SetOnBoard(0);
        }

        // Reveal game cards 
        foreach (GameCard card in game_cards)
        {
            card.ShowText();
        }

        // Start memorization
        if (event_board_setup != null) event_board_setup();
    }
    private void SetTargetCard()
    {
        target_card = game_cards[teller.GetTargetWordIndex()];
    }
    private void StackWonCard(GameCard card, Action on_move_done=null)
    {
        // Determine stack position / orientation
        int id = card.GetWinner().player_id;
        int stack_i = gm.GetPoints(id) - 1;
        Vector3 pos = Card.GetStackPos(gm.court.player_stacks[id].transform.position, stack_i);
        Quaternion rotation = Card.GetStackRotation(Quaternion.identity);

        // Move to player's stack
        card.EnablePhysics(false);
        //card.Move(card.transform.position + Vector3.Scale(Vector3.up, pos), card.transform.rotation);
        //card.event_done_moving = () =>
        //{
        card.Move(pos + Vector3.up * 0.7f, rotation);
        card.event_done_moving = () =>
        {
            card.Move(pos, rotation);
            card.event_done_moving = () =>
            {
                if (on_move_done != null) on_move_done();
            };
        };
        //};
    }


    // PUBLIC ACCESSORS

    public GameCard GetCard(int board_x, int board_y)
    {
        return game_cards[board_x * rows + board_y];
    }
    public GameCard GetTargetCard()
    {
        return target_card;
    }
    public int GetNumCards()
    {
        return game_cards.Length;
    }
    public int GetNumCardsLeft()
    {
        return teller.GetNumUntold();
    }
    
}
