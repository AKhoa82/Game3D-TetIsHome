using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(CharacterController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private WeaponData currentWeapon;

    [Header("Hit Detection")]
    [SerializeField] private LayerMask enemyLayers = ~0;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField] private float attackAngle = 120f;

    [Header("Dodge")]
    [SerializeField] private float dodgeSpeed = 8f;
    [SerializeField] private float dodgeDuration = 0.25f;
    [SerializeField] private float dodgeCooldown = 0.8f;
    [SerializeField] private int dodgeStaminaCost = 20;

    [Header("Block")]
    [SerializeField] private float blockDamageMultiplier = 0.3f;
    [SerializeField] private int blockStaminaCost = 8;
    [SerializeField] private float blockAngle = 120f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private PlayerMovement movement;
    private CharacterController controller;

    private float nextAttackTime;
    private float nextDodgeTime;
    private float dodgeEndTime;

    private bool pendingAttack;
    private float attackHitTime;
    private int pendingDamage;
    private float pendingRange;

    public bool IsDodging =>
        Time.time < dodgeEndTime;

    public bool IsBlocking { get; private set; }

    public Vector3 DodgeDirection { get; private set; }

    public float DodgeSpeed => dodgeSpeed;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        GameSession session = GameSession.Instance;

        if (session.Health <= 0 || !movement.enabled)
        {
            pendingAttack = false;
            dodgeEndTime = 0f;
            SetBlocking(false);
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            pendingAttack = false;
            dodgeEndTime = 0f;
            SetBlocking(false);
            return;
        }

        if (pendingAttack && Time.time >= attackHitTime)
        {
            pendingAttack = false;
            HitEnemy(pendingDamage, pendingRange);
        }

        if (IsDodging)
        {
            SetBlocking(false);
            return;
        }

        bool attackLocked = Time.time < nextAttackTime;

        SetBlocking(
            mouse != null &&
            mouse.rightButton.isPressed &&
            session.Stamina >= blockStaminaCost &&
            !attackLocked
        );

        if (keyboard != null &&
            keyboard.leftCtrlKey.wasPressedThisFrame &&
            !attackLocked)
        {
            Dodge();
            return;
        }

        if (IsBlocking || attackLocked)
            return;

        if (keyboard != null &&
            keyboard.qKey.wasPressedThisFrame)
        {
            HeavyAttack();
        }
        else if (mouse != null &&
                 mouse.leftButton.wasPressedThisFrame)
        {
            LightAttack();
        }
    }

    // ================= LIGHT ATTACK =================

    public void LightAttack()
    {
        StartAttack(
            currentWeapon != null
                ? currentWeapon.lightDamage : 10,

            currentWeapon != null
                ? currentWeapon.lightStaminaCost : 5,

            currentWeapon != null
                ? currentWeapon.attackRange : 1.8f,

            currentWeapon != null
                ? currentWeapon.lightCooldown : 0.45f,

            0.12f,
            "LightAttack"
        );
    }

    // ================= HEAVY ATTACK =================

    public void HeavyAttack()
    {
        StartAttack(
            currentWeapon != null
                ? currentWeapon.heavyDamage : 25,

            currentWeapon != null
                ? currentWeapon.heavyStaminaCost : 15,

            currentWeapon != null
                ? currentWeapon.attackRange : 1.8f,

            currentWeapon != null
                ? currentWeapon.heavyCooldown : 0.9f,

            0.3f,
            "HeavyAttack"
        );
    }

    private void StartAttack(
        int damage,
        int staminaCost,
        float range,
        float cooldown,
        float windup,
        string animationTrigger)
    {
        GameSession session = GameSession.Instance;

        if (!isActiveAndEnabled ||
            !movement.isActiveAndEnabled ||
            !controller.enabled ||
            session.Health <= 0 ||
            session.Stamina < staminaCost ||
            IsBlocking ||
            IsDodging ||
            Time.time < nextAttackTime ||
            !controller.isGrounded)
        {
            return;
        }

        session.UseStamina(staminaCost);

        nextAttackTime =
            Time.time + Mathf.Max(cooldown, windup);

        pendingDamage = damage;
        pendingRange = range;
        attackHitTime = Time.time + windup;
        pendingAttack = true;

        PlayTrigger(animationTrigger);

        Debug.Log(animationTrigger);
    }

    // ================= HIT DETECTION =================

    private void HitEnemy(int damage, float range)
    {
        Vector3 origin =
            transform.position + Vector3.up * 0.7f;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            range,
            enemyLayers,
            QueryTriggerInteraction.Ignore
        );

        EnemyHealth closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy =
                hit.GetComponentInParent<EnemyHealth>();

            if (enemy == null || enemy.IsDead)
                continue;

            Vector3 direction =
                enemy.transform.position - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f &&
                Vector3.Angle(transform.forward, direction)
                    > attackAngle / 2f)
            {
                continue;
            }

            Vector3 targetPoint = hit.bounds.center;

            if (HasBlockingObstacle(origin, targetPoint, enemy))
            {
                continue;
            }

            float distance = Vector3.Distance(
                origin,
                hit.ClosestPoint(origin)
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null)
        {
            closestEnemy.TakeDamage(damage);

            Debug.Log(
                "Enemy nhan " + damage + " sat thuong. HP: "
                + closestEnemy.CurrentHealth
                + "/" + closestEnemy.MaxHealth
            );
        }
        else
        {
            Debug.Log("Danh hut!");
        }
    }

    private bool HasBlockingObstacle(
        Vector3 origin,
        Vector3 targetPoint,
        EnemyHealth target)
    {
        Vector3 direction = targetPoint - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
            return false;

        RaycastHit[] obstacles = Physics.RaycastAll(
            origin,
            direction / distance,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit obstacle in obstacles)
        {
            Transform hitTransform = obstacle.collider.transform;
            EnemyHealth obstacleEnemy =
                obstacle.collider.GetComponentInParent<EnemyHealth>();

            if (hitTransform.IsChildOf(transform) ||
                obstacleEnemy == target ||
                (obstacleEnemy != null && obstacleEnemy.IsDead))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    // ================= DODGE =================

    private void Dodge()
    {
        GameSession session = GameSession.Instance;

        if (Time.time < nextDodgeTime ||
            session.Stamina < dodgeStaminaCost ||
            !controller.isGrounded)
        {
            return;
        }

        Vector3 direction = movement.GetMoveDirection();

        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        direction.y = 0f;

        DodgeDirection = direction.normalized;

        session.UseStamina(dodgeStaminaCost);

        dodgeEndTime = Time.time + dodgeDuration;
        nextDodgeTime = Time.time + dodgeCooldown;

        SetBlocking(false);
        PlayTrigger("Dodge");

        Debug.Log("Player ne don!");
    }

    // ================= BLOCK =================

    private void SetBlocking(bool value)
    {
        IsBlocking = value;

        if (HasParameter(
                "Block",
                AnimatorControllerParameterType.Bool))
        {
            animator.SetBool("Block", value);
        }
    }

    public void ReceiveDamage(
        int damage,
        Vector3 attackerPosition)
    {
        GameSession session = GameSession.Instance;

        if (damage <= 0 ||
            session.Health <= 0 ||
            !movement.enabled)
        {
            return;
        }

        if (IsDodging)
        {
            Debug.Log("Ne don thanh cong!");
            return;
        }

        Vector3 direction =
            attackerPosition - transform.position;

        direction.y = 0f;

        bool attackFromFront =
            direction.sqrMagnitude < 0.001f ||
            Vector3.Angle(transform.forward, direction)
                <= blockAngle / 2f;

        if (IsBlocking &&
            attackFromFront &&
            session.Stamina >= blockStaminaCost)
        {
            session.UseStamina(blockStaminaCost);

            damage = Mathf.CeilToInt(
                damage * blockDamageMultiplier
            );

            Debug.Log("Do don thanh cong!");
        }

        session.TakeDamage(damage);

        Debug.Log("Player nhan " + damage + " sat thuong");
    }

    // ================= WEAPON =================

    public void EquipWeapon(WeaponData weapon)
    {
        currentWeapon = weapon;
    }

    // ================= ANIMATION =================

    private bool HasParameter(
        string parameterName,
        AnimatorControllerParameterType type)
    {
        if (animator == null ||
            animator.runtimeAnimatorController == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter
                 in animator.parameters)
        {
            if (parameter.name == parameterName &&
                parameter.type == type)
            {
                return true;
            }
        }

        return false;
    }

    private void PlayTrigger(string triggerName)
    {
        if (HasParameter(
                triggerName,
                AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(triggerName);
        }
    }

    private void OnDisable()
    {
        pendingAttack = false;
        dodgeEndTime = 0f;
        SetBlocking(false);
    }
}
