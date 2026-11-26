using UnityEngine;

[CreateAssetMenu(
    fileName = "NewWeapon",
    menuName = "TetIsHome/Combat/Weapon"
)]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Tay khong";

    [Header("Damage")]
    public int lightDamage = 10;
    public int heavyDamage = 25;

    [Header("Attack Range")]
    public float attackRange = 1.8f;

    [Header("Stamina")]
    public int lightStaminaCost = 5;
    public int heavyStaminaCost = 15;

    [Header("Cooldown")]
    public float lightCooldown = 0.45f;
    public float heavyCooldown = 0.9f;
}