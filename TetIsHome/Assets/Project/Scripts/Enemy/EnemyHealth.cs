using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 50;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int> Damaged;
    public event Action Died;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        Damaged?.Invoke(amount);

        if (IsDead)
            Died?.Invoke();
    }

    // Allows the death state to be demonstrated until Player Combat is connected.
    [ContextMenu("Debug/Take 25 Damage")]
    private void DebugTakeDamage()
    {
        if (Application.isPlaying)
            TakeDamage(25);
    }
}
