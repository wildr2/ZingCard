using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;


public enum GameState {
    Unset,
    PreGame, CardDraw, Memorize, Intro, PrePlay,
    Play, PostPlay, Reset, PostGame }


public class GameManager : MonoBehaviour
{
    // Dev
    public bool dev = false;

    public Court court;
    public AudioSource beep;
    //public MatchAudio match_audio;
    public CardManager card_manager;
    public Teller teller;
    public Player[] players;

    // State and score
    private GameState _state = GameState.Unset;
    private float state_start_time;
    private int[] scores;
    private int points_to_win = 18;
    private int last_scorer_id = -1;

    // Hitting
    private float miss_delay = 2f; // miss if don't hit the target card this long after hitting an incorrect card
    private int miss_cards = 4; // miss if hit this number of wrong cards before hitting the target card
    private int[] player_hits = new int[2];
    private Player first_hitter = null;
    private float first_hit_time;

    // State Times
    private float pre_game_dur = 5f;
    private float memorize_dur = 20f;
    private float post_play_dur = 6f;
    

    // Events
    public System.Action<GameState> event_new_state;
    public System.Action<Player> event_miss;
    public System.Action<Player> event_false_start;
    public System.Action<Player> event_correct_hit;
    public System.Action<Player> event_point;
    public System.Action event_game_point;


    // PRIVATE MODIFIERS

    private void Start()
    {
        teller.Initialize(this, 36);
        card_manager.Initialize(this, teller);

        // players
        players[0].Initialize(this);
        players[1].Initialize(this);

        card_manager.event_card_hit += OnCardHit;
        players[0].hand.event_hand_wall_tripped += OnTripHandWall;
        players[1].hand.event_hand_wall_tripped += OnTripHandWall;

        // scores
        scores = new int[2];
        UpdateUIScore();

        if (dev)
        {
            pre_game_dur = 1;
            memorize_dur = 1;
        }

        SetState(GameState.PreGame);
    }
    private void Update()
    {
        // Dev
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            SceneManager.LoadScene(0);
            OVRManager.display.RecenterPose();
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            OnCardHit(0, card_manager.GetTargetCard());
        }

        // State
        UpdateState();
    }

    private void SetState(GameState new_state)
    {
        if (_state == new_state) return;

        GameState last_state = _state;
        _state = new_state;
        state_start_time = Time.time;


        // Exit State
        switch (last_state)
        {
        }

        // Enter State
        switch (_state)
        {
            case GameState.PreGame:
                break;
            case GameState.CardDraw:
                card_manager.event_board_setup = () => { SetState(GameState.Memorize); };
                break;
            case GameState.Memorize:
                int mins = (int)(memorize_dur / 60);
                court.tell_text.text = mins + (mins == 1 ? " MINUTE" : " MINUTES") + " TO MEMORIZE";
                break;
            case GameState.Intro:
                teller.event_intro_done = () => SetState(GameState.Play);
                break;
            case GameState.Play:
                if (IsGamePoint())
                {
                    if (event_game_point != null) event_game_point();
                }
                break;
            case GameState.Reset:
                card_manager.event_board_setup = () =>
                {
                    first_hitter = null;
                    player_hits[0] = 0; player_hits[1] = 0;
                    SetState(GameState.Play);
                };
                break;
        }

        // State change during this function - only let the new instance of SetState continue
        if (_state != new_state) return; 

        // Events
        if (event_new_state != null) event_new_state(_state);
    }
    private void UpdateState()
    {
        switch (_state)
        {
            case GameState.PreGame:
                if (StateTimeUp(pre_game_dur)) SetState(GameState.CardDraw);
                break;
            case GameState.CardDraw:
                break;
            case GameState.Memorize:
                if (StateTimeUp(memorize_dur)) SetState(GameState.Intro);
                break;
            case GameState.Intro:
                break;
            case GameState.Play:
                break;
            case GameState.PostPlay:
                if (StateTimeUp(post_play_dur))
                {
                    if (GetWinner() != null) SetState(GameState.PostGame);
                    else SetState(GameState.Reset);
                }
                break;
            case GameState.Reset:
                break;
            case GameState.PostGame:
                break;
        }
    }

    private void OnTripHandWall(Player player)
    {
        if (_state == GameState.Play)
        {
            player.hand.EnableHandWall(false);
            GetOpponent(player).hand.EnableHandWall(false);
            StartCoroutine(CoroutineUtil.DoAfterDelay(
                () => OnMissDelayUp(player), miss_delay));

            players[0].hand.StartTrail();
            players[1].hand.StartTrail();
        }
    }
    private void OnMissDelayUp(Player player)
    {
        if (_state == GameState.Play)
        {
            OnMiss(player.player_id);
        }
    }
    private void OnCardHit(int hitter_id, GameCard card)
    {
        if (first_hitter == null)
        {
            first_hitter = players[hitter_id];
            first_hit_time = Time.time;
        }

        if (_state == GameState.Play)
        {
            ++player_hits[hitter_id];

            // False Start
            if (!teller.HasShownTargetWord())
            {
                OnFalseStart(hitter_id);
            }
            // Correct Hit
            else if (card == card_manager.GetTargetCard())
            {
                OnCorrectHit(hitter_id);
            }
            // Miss
            else if (player_hits[hitter_id] == miss_cards)
            {
                OnMiss(hitter_id);
            }
            //else if (!players[hitter_id].HasControl())
            //{
            //    OnMiss(hitter_id);
            //}
        }
    }
    private void OnFalseStart(int player_id)
    {
        //match_audio.PlayFault();
        beep.pitch = 1;
        beep.Play();

        GameCard targetcard = card_manager.GetTargetCard();
        targetcard.KnockAwayFrom(players[player_id]);
        targetcard.SetWinner(players[1 - player_id]);

        if (event_false_start != null) event_false_start(players[player_id]);
        GivePoint(1 - player_id);
    }
    private void OnMiss(int player_id)
    {
        //match_audio.PlayFault();
        beep.pitch = 1;
        beep.Play();

        GameCard targetcard = card_manager.GetTargetCard();
        targetcard.KnockAwayFrom(players[player_id]);
        targetcard.SetWinner(players[1 - player_id]);

        if (event_miss != null) event_miss(players[player_id]);
        GivePoint(1 - player_id);
    }
    private void OnCorrectHit(int player_id)
    {
        //match_audio.PlayTellCardHit();
        beep.pitch = 2;
        beep.Play();


        card_manager.GetTargetCard().SetWinner(players[player_id]);

        if (event_correct_hit != null) event_correct_hit(players[player_id]);
        GivePoint(player_id);
    }
    private void OnWinGame(int winner_id)
    {
        //match_audio.PlayGameOver();
        //SetState(GameState.PostGame);
    }

    private void GivePoint(int player_id)
    {
        scores[player_id]++;
        last_scorer_id = player_id;
        UpdateUIScore();

        SetState(GameState.PostPlay);
        if (event_point != null) event_point(players[player_id]);

        if (scores[player_id] >= points_to_win)
        {
            OnWinGame(player_id);
        }
    }
    private void UpdateUIScore()
    {
        court.score_text.text = scores[0] + "-" + scores[1];
    }


    // PRIVATE HELPERS

    private bool StateTimeUp(float state_duration)
    {
        return GetStateTime() >= state_duration;
    }
    private string FormatMinSecTimer(float seconds)
    {
        if (Mathf.Abs(seconds) < 0.1f) return "0:00";

        int min = (int)(seconds / 60);
        int sec = (int)(seconds % 60);

        return min + ":" + (sec < 10 ? "0" : "") + sec;
    }


    // PUBLIC ACCESSORS

    public GameState GetGameState()
    {
        return _state;
    }
    public float GetStateTime()
    {
        return Time.time - state_start_time;
    }
    public float GetMemorizeDuration()
    {
        return memorize_dur;
    }

    public bool PlayersReady()
    {
        return players[0].hand.IsBehindHandWall() &&
               players[1].hand.IsBehindHandWall();
    }
    
    public int GetPoints(int player_id)
    {
        return scores[player_id];
    }
    public int GetPointsToWin()
    {
        return points_to_win;
    }
    public bool IsGamePoint()
    {
        return scores[0] == points_to_win - 1 || scores[1] == points_to_win - 1;
    }

    public Player GetWinner()
    {
        return scores[0] >= points_to_win ? players[0] :
               scores[1] >= points_to_win ? players[1] : null;
    }
    public Player GetLastScorer()
    {
        return last_scorer_id > -1 ? players[last_scorer_id] : null;
    }
    public Player GetOpponent(Player player)
    {
        return players[1 - player.player_id];
    }

}



//public class StateManager<T>
//{
//    private float state_start_time;

//    private Dictionary<T, System.Action> on_update_actions;
//    private Dictionary<T, System.Action<T>> on_enter_actions;
//    private Dictionary<T, System.Action<T>> on_exit_actions;
//    private T state;
//    public T State
//    {
//        get { return state; }
//        set
//        {
//            if (value.Equals(state)) return;

//            System.Action<T> old_on_exit;
//            if (on_exit_actions.TryGetValue(state, out old_on_exit))
//                if (old_on_exit != null) old_on_exit(value);

//            System.Action<T> new_on_enter;
//            if (on_enter_actions.TryGetValue(value, out new_on_enter))
//                if (new_on_enter != null) new_on_enter(state);

//            state = value;
//            state_start_time = Time.time;
//        }
//    } 
//    public float Time
//    {
//        get { return UnityEngine.Time.time - state_start_time; }
//    }

//    public void SetOnUpdate(GameState state, System.Action action)
//    {
//    }
//    public void SetOnEnter(GameState state, System.Action<T> action)
//    {
//    }
//    public void SetOnExit(GameState state, System.Action<T> action)
//    {
//    }
    
//    public void Update()
//    {
        
//    }
//}