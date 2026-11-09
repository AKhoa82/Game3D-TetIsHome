using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(EnemyHealth))]
public class WildDogAI : MonoBehaviour
{
    public enum DogState { Patrol, Chase, Attack, Search, Retreat, Dead }

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField, Min(0f)] private float patrolWait = 1f;
    [SerializeField, Min(0.1f)] private float patrolSpeed = 2f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 3.5f;

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float detectionRange = 12f;
    [SerializeField, Range(1f, 360f)] private float fieldOfView = 170f;
    [SerializeField, Min(0.1f)] private float loseRange = 18f;
    [SerializeField, Min(0f)] private float memoryTime = 3f;
    [SerializeField, Min(0f)] private float searchTime = 3f;
    [SerializeField, Min(0f)] private float eyeHeight = 0.8f;
    [SerializeField] private LayerMask sightMask = ~0;

    [Header("Combat")]
    [SerializeField, Min(0.1f)] private float attackRange = 1.6f;
    [SerializeField, Min(1)] private int attackDamage = 10;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.5f;
    [SerializeField, Min(0f)] private float attackWindup = 0.4f;
    [SerializeField, Min(0.1f)] private float attackDuration = 1.4f;
    [SerializeField, Min(0f)] private float retreatTime = 1.5f;
    [SerializeField, Min(0f)] private float retreatDistance = 4f;
    [SerializeField, Min(0f)] private float hitReactionTime = 0.45f;

    [Header("Visual")]
    [SerializeField] private Animator animator;

    [Header("Navigation")]
    [SerializeField, Min(0.1f)] private float navMeshSnapDistance = 3f;

    public DogState State { get; private set; }

    private NavMeshAgent agent;
    private EnemyHealth health;
    private PlayerMovement player;
    private Vector3 homePosition;
    private Vector3 lastSeenPosition;
    private int patrolIndex;
    private float waitUntil;
    private float lastSeenTime;
    private float nextAttackTime;
    private float retreatUntil;
    private float searchUntil;
    private float attackStartedAt;
    private float hitReactionUntil;
    private bool attackHitApplied;
    private string currentAnimation;
    private bool waitingAtPatrolPoint;
    private bool startedPatrol;
    private bool warnedAboutNavMesh;
    private float nextNavMeshRetry;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        homePosition = transform.position;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (health == null)
            return;

        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void Start()
    {
        if (health.IsDead)
        {
            OnDied();
            return;
        }
        TryStartPatrol();
    }

    private void Update()
    {
        if (health.IsDead)
            return;

        if (!agent.enabled || !agent.isOnNavMesh || !startedPatrol)
        {
            if (Time.time >= nextNavMeshRetry)
                TryStartPatrol();
            return;
        }

        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>();

        bool canSeePlayer = CanSeePlayer();
        if (canSeePlayer)
        {
            lastSeenPosition = player.transform.position;
            lastSeenTime = Time.time;
        }

        switch (State)
        {
            case DogState.Patrol:
                if (canSeePlayer)
                    ChangeState(DogState.Chase);
                else
                    TickPatrol();
                break;

            case DogState.Chase:
                if (CanAttackPlayer() && Time.time >= nextAttackTime)
                    ChangeState(DogState.Attack);
                else if (HasLostPlayer())
                    ChangeState(DogState.Search);
                else
                    SetDestination(lastSeenPosition);
                break;

            case DogState.Attack:
                TickAttack();
                break;

            case DogState.Search:
                if (canSeePlayer)
                    ChangeState(DogState.Chase);
                else if (Time.time >= searchUntil ||
                         (ReachedDestination() && Time.time - lastSeenTime >= memoryTime))
                    ChangeState(DogState.Patrol);
                break;

            case DogState.Retreat:
                if (Time.time >= retreatUntil || ReachedDestination())
                    ChangeState(canSeePlayer ? DogState.Chase : DogState.Search);
                break;
        }

        UpdateAnimation();
    }

    private void TryStartPatrol()
    {
        nextNavMeshRetry = Time.time + 1f;
        if (!agent.enabled)
            return;

        if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position,
                out NavMeshHit nearby, navMeshSnapDistance, agent.areaMask))
        {
            if (!agent.Warp(nearby.position))
            {
                agent.enabled = false;
                transform.position = nearby.position;
                agent.enabled = true;
            }
        }

        if (!agent.isOnNavMesh)
        {
            if (!warnedAboutNavMesh)
            {
                Debug.LogWarning("WildDogAI: no NavMesh near " + transform.position, this);
                warnedAboutNavMesh = true;
            }
            return;
        }

        warnedAboutNavMesh = false;
        homePosition = transform.position;
        startedPatrol = true;
        ChangeState(DogState.Patrol);
    }

    private bool PlayerIsAlive()
    {
        return player != null && player.isActiveAndEnabled &&
               GameSession.Instance.Health > 0;
    }

    private bool CanSeePlayer()
    {
        if (!PlayerIsAlive())
            return false;

        Vector3 from = transform.position + Vector3.up * eyeHeight;
        Vector3 to = player.transform.position + Vector3.up * 0.9f;
        Vector3 direction = to - from;
        if (direction.sqrMagnitude > detectionRange * detectionRange)
            return false;

        Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (flatDirection.sqrMagnitude > 0.01f &&
            Vector3.Angle(transform.forward, flatDirection) > fieldOfView * 0.5f)
            return false;

        return Physics.Raycast(from, direction.normalized, out RaycastHit hit,
                   direction.magnitude, sightMask, QueryTriggerInteraction.Ignore) &&
               hit.collider.GetComponentInParent<PlayerMovement>() == player;
    }

    private bool CanAttackPlayer()
    {
        return PlayerIsAlive() &&
               Vector3.Distance(transform.position, player.transform.position) <= attackRange &&
               CanSeePlayer();
    }

    private bool HasLostPlayer()
    {
        return !PlayerIsAlive() ||
               Vector3.Distance(transform.position, player.transform.position) > loseRange ||
               Time.time - lastSeenTime > memoryTime;
    }

    private void TickPatrol()
    {
        if (!ReachedDestination())
        {
            waitingAtPatrolPoint = false;
            return;
        }

        if (!waitingAtPatrolPoint)
        {
            waitingAtPatrolPoint = true;
            waitUntil = Time.time + patrolWait;
        }

        if (Time.time < waitUntil)
            return;

        waitingAtPatrolPoint = false;
        if (patrolPoints.Length > 0)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        SetPatrolDestination();
    }

    private void TickAttack()
    {
        agent.isStopped = true;
        if (PlayerIsAlive())
        {
            Vector3 look = player.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(look), 360f * Time.deltaTime);
        }

        float elapsed = Time.time - attackStartedAt;
        if (!attackHitApplied && elapsed >= attackWindup)
        {
            attackHitApplied = true;
            if (CanAttackPlayer())
                GameSession.Instance.TakeDamage(attackDamage);
        }

        if (elapsed >= attackDuration)
            ChangeState(HasLostPlayer() ? DogState.Search : DogState.Chase);
    }

    private void OnDamaged(int amount)
    {
        if (health.IsDead)
            return;

        hitReactionUntil = Time.time + hitReactionTime;
        PlayAnimation("Hit");
        if (!PlayerIsAlive())
            return;

        lastSeenPosition = player.transform.position;
        lastSeenTime = Time.time;
        if (!agent.enabled || !agent.isOnNavMesh)
            return;
        if (retreatTime <= 0f || retreatDistance <= 0f)
        {
            ChangeState(DogState.Chase);
            return;
        }

        Vector3 away = transform.position - player.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = -transform.forward;

        Vector3 goal = transform.position + away.normalized * retreatDistance;
        if (!NavMesh.SamplePosition(goal, out NavMeshHit hit, retreatDistance, agent.areaMask))
        {
            ChangeState(DogState.Chase);
            return;
        }

        retreatUntil = Time.time + retreatTime;
        ChangeState(DogState.Retreat);
        SetDestination(hit.position);
    }

    private void OnDied()
    {
        State = DogState.Dead;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        agent.enabled = false;
        PlayAnimation("Dead");
    }

    private void ChangeState(DogState next)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;
        State = next;
        agent.isStopped = next == DogState.Attack;

        if (next == DogState.Patrol)
        {
            waitingAtPatrolPoint = false;
            agent.speed = patrolSpeed;
            SetPatrolDestination();
        }
        else if (next == DogState.Chase)
        {
            agent.speed = chaseSpeed;
            SetDestination(lastSeenPosition);
        }
        else if (next == DogState.Search)
        {
            agent.speed = patrolSpeed;
            searchUntil = Time.time + searchTime;
            SetDestination(lastSeenPosition);
        }
        else if (next == DogState.Attack)
        {
            attackStartedAt = Time.time;
            attackHitApplied = false;
            nextAttackTime = Time.time + attackCooldown;
            PlayAnimation("Attack");
        }
        else if (next == DogState.Retreat)
            agent.speed = chaseSpeed;
    }

    private void SetPatrolDestination()
    {
        Transform point = patrolPoints.Length > 0 ? patrolPoints[patrolIndex] : null;
        SetDestination(point != null ? point.position : homePosition);
    }

    private void SetDestination(Vector3 position)
    {
        if (!agent.isOnNavMesh)
            return;

        if (!agent.isStopped)
            agent.SetDestination(position);
    }

    private bool ReachedDestination()
    {
        return agent.isOnNavMesh && !agent.pathPending &&
               (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.2f);
    }

    private void UpdateAnimation()
    {
        if (animator == null || State == DogState.Dead)
            return;
        if (Time.time < hitReactionUntil)
            return;
        if (State == DogState.Attack)
        {
            PlayAnimation("Attack");
            return;
        }
        bool moving = !agent.isStopped && agent.velocity.sqrMagnitude > 0.04f;
        PlayAnimation(!moving ? "Idle" :
            State == DogState.Chase || State == DogState.Retreat ? "Run" : "Walk");
    }

    private void PlayAnimation(string state)
    {
        if (animator == null || currentAnimation == state)
            return;
        currentAnimation = state;
        animator.CrossFadeInFixedTime(state, 0.12f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
