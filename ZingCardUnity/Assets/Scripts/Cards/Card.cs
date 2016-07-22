using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;


public class Card : MonoBehaviour
{
    protected MeshRenderer mesh;
    protected Text[] texts;
    protected Rigidbody rb;

    private Coroutine move_routine;
    public Action event_done_moving;

    protected Vector3 board_pos;
    protected Quaternion board_rot;


    // PUBLIC MODIFIERS

    public void EnablePhysics(bool enable=true)
    {
        if (enable)
        {
            rb.isKinematic = false;
        }
        else
        {
            rb.isKinematic = true;
        }
    }
    public void ShowText(bool visible=true)
    {
        foreach (Text text in texts) text.gameObject.SetActive(visible);
    }
    public void SetText(string text)
    {
        foreach (Text t in texts)
        {
            t.text = text;
        }
    }
    public void Move(Vector3 pos, Quaternion rotation, float seconds=1)
    {
        if (move_routine != null) StopCoroutine(move_routine);
        if (seconds == 0)
        {
            transform.position = pos;
            transform.rotation = rotation;
            if (event_done_moving != null) event_done_moving();
        }
        else move_routine = StartCoroutine(MoveRoutine(pos, rotation, seconds));
    }
    public virtual void SetOnBoard(float seconds=1)
    {
        EnablePhysics(false);
        event_done_moving += () => EnablePhysics(true);
        Move(board_pos, board_rot, seconds);
    }
    public void Stack(Vector3 stack_base_pos, Quaternion rotation, int stack_i, float seconds = 0)
    {
        // Move card
        EnablePhysics(false);
        Move(GetStackPos(stack_base_pos, stack_i), GetStackRotation(rotation), seconds);
    }
    public void Stack(Transform stack_base, int stack_i, float seconds = 0)
    {
        Stack(stack_base.position, stack_base.rotation, stack_i, seconds);
    }


    // PRIVATE / PROTECTED MODIFIERS

    protected virtual void Awake()
    {
        //card_audio = GetComponentInChildren<CardAudio>();
        rb = GetComponent<Rigidbody>();
        mesh = GetComponentInChildren<MeshRenderer>();
        texts = GetComponentsInChildren<Text>();
    }
    private IEnumerator MoveRoutine(Vector3 target_pos, Quaternion target_rotation, float seconds)
    {
        Vector3 p1 = transform.position;
        Quaternion r1 = transform.rotation;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / seconds;

            transform.position = Vector3.Lerp(p1, target_pos, Mathf.SmoothStep(0, 1, t));
            transform.rotation = Quaternion.Slerp(r1, target_rotation, Mathf.SmoothStep(0, 1, t));

            yield return null;
        }

        if (event_done_moving != null) event_done_moving();
        move_routine = null;
    }


    // PUBLIC HELPERS

    public static Vector3 GetStackPos(Vector3 stack_base, int stack_i)
    {
        return stack_base + new Vector3(Tools.RandNeg() * 0.005f, stack_i * 0.01f, Tools.RandNeg() * 0.005f);
    }
    public static Quaternion GetStackRotation(Quaternion stack_rotation)
    {
        return stack_rotation * Quaternion.AngleAxis(Tools.RandNeg() * 5f, Vector3.up);
    }


    // PUBLIC ACCESSORS

    public bool IsMakingMovement()
    {
        return move_routine != null;
    }
}
