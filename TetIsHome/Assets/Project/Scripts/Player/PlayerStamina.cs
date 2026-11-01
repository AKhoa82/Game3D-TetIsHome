using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float sprintCostPerSecond = 15f;
    [SerializeField] private float regenPerSecond = 10f;
    [SerializeField] private int minStaminaToRestart = 25;

    private float spendBuffer;
    private float regenBuffer;
    private bool exhausted;

    public bool TickSprint(bool wantsToSprint, bool isMoving, float deltaTime)
    {
        GameSession session = GameSession.Instance;

        // Het nang luong thi phai nghi
        if (session.Stamina <= 0)
            exhausted = true;

        // Hoi du nang luong thi cho chay lai
        if (exhausted &&
            session.Stamina >= minStaminaToRestart)
        {
            exhausted = false;
        }

        bool isRunning = wantsToSprint && isMoving && !exhausted && session.Stamina > 0;

        if (isRunning)
        {
            regenBuffer = 0f;

            spendBuffer += sprintCostPerSecond * deltaTime;

            int cost = Mathf.FloorToInt(spendBuffer);

            if (cost > 0)
            {
                session.UseStamina(cost);
                spendBuffer -= cost;
            }

            if (session.Stamina <= 0)
            {
                exhausted = true;
                isRunning = false;
            }
        }
        else
        {
            spendBuffer = 0f;

            if (session.Stamina >= session.MaxStamina)
            {
                regenBuffer = 0f;
            }
            else
            {
                regenBuffer += regenPerSecond * deltaTime;

                int amount = Mathf.FloorToInt(regenBuffer);

                if (amount > 0)
                {
                    session.RestoreStamina(amount);
                    regenBuffer -= amount;
                }
            }
        }
        return isRunning;
    }
}
