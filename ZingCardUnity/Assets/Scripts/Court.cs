using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Court : MonoBehaviour
{
    public RectTransform board;
    public Text tell_text;
    public Text score_text;
    public Text[] player_name_texts;
    public Transform main_stack_pos;
    public Transform[] player_stacks;

    public Vector3 GetBotLeftBoard()
    {
        return Vector3.Scale(board.localScale, board.rect.min);
    }
    public Vector3 GetTopRightBoard()
    {
        return Vector3.Scale(board.localScale, board.rect.max);
    }
    public Vector3 GetBoardCenter()
    {
        return Vector3.Scale(board.localScale, board.rect.position);
    }
}
