using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SlotValue
{
    Ticket,
    Coin,
    Hummer,
    Key,
    Health
}

public class Slot : MonoBehaviour
{
    private int randomValue;
    [HideInInspector] public float timeInterval;
    [HideInInspector] public SlotValue randItem;
    private float speed;
    public SlotValue stoppedSlot;
    private SlotMachine sm;

    // Exact symbol centers in local Y space
    private const float Y_TICKET = -1.61f;
    private const float Y_HEALTH = -0.93f;
    private const float Y_HUMMER = -0.28f;
    private const float Y_KEY    =  0.37f;
    private const float Y_COIN   =  1.08f;
    private const float CYCLE_LENGTH = 3.417f;

    private void Start()
    {
        sm = gameObject.GetComponentInParent<SlotMachine>();
    }

    public IEnumerator Spin()
    {
        timeInterval = sm.timeInterval;
        randomValue = Random.Range(0, 90);
        speed = 30f + randomValue;

        while (speed >= 10f)
        {
            speed = speed / 1.01f;
            transform.Translate(Vector2.up * Time.deltaTime * -speed);
            if (transform.localPosition.y <= -1.8f)
            {
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + CYCLE_LENGTH, transform.localPosition.z);
            }

            yield return new WaitForSeconds(timeInterval);
        }

        StartCoroutine("EndSpin");
        yield return null;
    }

    public IEnumerator CheatSpin()
    {
        timeInterval = sm.timeInterval;
        randomValue = Random.Range(0, 90);
        speed = 30f + randomValue;

        while (speed >= 15f)
        {
            speed = speed / 1.01f;
            transform.Translate(Vector2.up * Time.deltaTime * -speed);
            if (transform.localPosition.y <= -1.8f)
            {
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + CYCLE_LENGTH, transform.localPosition.z);
            }

            yield return new WaitForSeconds(timeInterval);
        }

        float targetY;
        if (randItem == SlotValue.Ticket) targetY = Y_TICKET;
        else if (randItem == SlotValue.Health) targetY = Y_HEALTH;
        else if (randItem == SlotValue.Hummer) targetY = Y_HUMMER;
        else if (randItem == SlotValue.Key) targetY = Y_KEY;
        else targetY = Y_COIN;

        stoppedSlot = randItem;

        Vector3 targetPos = new Vector3(transform.localPosition.x, targetY, transform.localPosition.z);
        while (Vector3.Distance(transform.localPosition, targetPos) > 0.01f)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, 15f * Time.deltaTime);
            yield return null;
        }
        transform.localPosition = targetPos;

        sm.WaitResults();
        yield return null;
    }

    private IEnumerator EndSpin()
    {
        float targetY;
        float currentY = transform.localPosition.y;

        // Wrap into standard cycle range if needed
        while (currentY > 1.6f) currentY -= CYCLE_LENGTH;
        while (currentY < -1.8f) currentY += CYCLE_LENGTH;
        transform.localPosition = new Vector3(transform.localPosition.x, currentY, transform.localPosition.z);

        if (currentY < -1.27f)
        {
            targetY = Y_TICKET;
            stoppedSlot = SlotValue.Ticket;
        }
        else if (currentY < -0.60f)
        {
            targetY = Y_HEALTH;
            stoppedSlot = SlotValue.Health;
        }
        else if (currentY < 0.05f)
        {
            targetY = Y_HUMMER;
            stoppedSlot = SlotValue.Hummer;
        }
        else if (currentY < 0.72f)
        {
            targetY = Y_KEY;
            stoppedSlot = SlotValue.Key;
        }
        else
        {
            targetY = Y_COIN;
            stoppedSlot = SlotValue.Coin;
        }

        Vector3 targetPos = new Vector3(transform.localPosition.x, targetY, transform.localPosition.z);
        while (Vector3.Distance(transform.localPosition, targetPos) > 0.01f)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, 15f * Time.deltaTime);
            yield return null;
        }
        transform.localPosition = targetPos;

        sm.WaitResults();
        yield return null;
    }
}
