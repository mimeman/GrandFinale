using NormalZombieStates;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 이제 모든 필수 컴포넌트를 명시합니다.
[RequireComponent(typeof(IMonsterMovement), typeof(MonsterHealth), typeof(MonsterSensor))]
public class MonsterAIController : MonoBehaviour
{
    #region 필드
    // --- 주요 컴포넌트 참조 ---
    private IMonsterMovement movement;
    private MonsterHealth health;
    public MonsterSensor sensor { get; private set; }

    private NavMeshAgent agent;

    private Animator animator;
    [Header("몬스터 설정")]
    public MonsterConfig config;

    // --- AI 상태 제어 변수 ---
    public GameObject player { get; private set; }

    [Header("디버그 설정")]
    public bool alwaysShowGizmos = false;
    // --- 상태 머신 (FSM) ---
    public ZombieBaseState<MonsterAIController> CurrentState { get; private set; }
    public Idle idleState = new Idle();
    public Patrol patrolState = new Patrol();
    public Trace traceState = new Trace();
    public Attack attackState = new Attack();
    public LookAround lookAroundState = new LookAround();
    public Hit hitState = new Hit();
    public Die dieState = new Die();


    // <<<< 2. 도착 여부 판정 로직 수정 >>>>
    // NavMeshAgent가 있으면 그 상태를 사용하고, 없으면(거미) 기존처럼 거리 기반으로 판단
    public bool arrivedAtDestination
    {
        get
        {
            if (agent != null) // 좀비의 경우
            {
                // 경로 계산이 끝나고, 남은 거리가 정지 거리보다 작으면 도착한 것으로 간주
                return !agent.pathPending && agent.remainingDistance <= config.stoppingDistance;
            }
            else // 거미의 경우
            {
                return Vector3.Distance(transform.position, currentDestination) < config.stoppingDistance;
            }
        }
    }

    public Vector3 currentDestination { get; private set; }

    // --- 코루틴 참조 ---
    public Coroutine attackRoutineCor { get; set; }

    // --- 애니메이션 해시 ---
    public readonly int hashPatrol = Animator.StringToHash("Patrol");
    public readonly int hashTrace = Animator.StringToHash("Trace");
    public readonly int hashAttack = Animator.StringToHash("Attack");
    public readonly int hashDoAttack = Animator.StringToHash("DoAttack");
    public readonly int hashLookAround = Animator.StringToHash("LookAround");
    public readonly int hashHit = Animator.StringToHash("Hit");
    public readonly int hashDie = Animator.StringToHash("Die");
    public readonly int hashJumpAttack = Animator.StringToHash("DoJumpAttack");
    #endregion

    #region 초기화 및 루프
    void Awake()
    {
        movement = GetComponent<IMonsterMovement>();
        health = GetComponent<MonsterHealth>();
        sensor = GetComponent<MonsterSensor>();
        animator = GetComponentInChildren<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");

        TryGetComponent<NavMeshAgent>(out agent);

        if (config == null)
        {
            Debug.LogError(gameObject.name + "에 MonsterConfig 파일이 할당 안됨");
            return;
        }

        // 3. Health 컴포넌트에 Config 값을 넘겨 초기화시킵니다.
        health.Initialize(config);
    }

    void Start()
    {
        health.OnHit.AddListener(HandleHit);
        health.OnDeath.AddListener(HandleDeath);
        ChangeState(idleState);
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.OnHit.RemoveListener(HandleHit);
            health.OnDeath.RemoveListener(HandleDeath);
        }
    }

    void Update()
    {
        if (CurrentState == null || health.IsDead) return;
        ZombieBaseState<MonsterAIController> nextState = CurrentState.UpdateState(this);
        if (nextState != CurrentState) { ChangeState(nextState); }
    }
    #endregion

    #region 상태 관리
    public void ChangeState(ZombieBaseState<MonsterAIController> newState)
    {
        CurrentState?.ExitState(this);
        CurrentState = newState;
        CurrentState.EnterState(this);
    }
    #endregion

    #region 이동 제어
    public void MoveTo(Vector3 destination)
    {
        currentDestination = destination;
        float speed = (CurrentState is Trace) ? config.runSpeed : config.walkSpeed;

        // 최종 목적지를 이동 시스템에 전달합니다.
        movement.Move(destination, speed);

        // NavMeshAgent가 없는 몬스터(거미)는 수동으로 회전시켜 줍니다.
        if (agent == null)
        {
            movement.TurnTowards(destination, config.turnSpeed);
        }
    }

    public void StopMoving() { movement.Stop(); }
    public void LookAt(Vector3 target) { movement.TurnTowards(target, config.turnSpeed); }
    public Vector3 GetRandomPatrolDestination()
    {
        float distance = Random.Range(config.patrolRadiusMin, config.patrolRadiusMax);
        Vector3 randomDir = Random.onUnitSphere * distance;
        randomDir.y = 0;
        Vector3 destination = transform.position + randomDir;
        if (Physics.Raycast(destination + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f)) { return hit.point; }
        return destination;
    }
    #endregion

    #region 감지, 공격, 이벤트 핸들러

    public bool CanSeePlayer => sensor.CanSeePlayer;
    public Vector3 targetLastPos => sensor.TargetLastPosition;

    public float GetDistanceToPlayer() { if (player == null) return Mathf.Infinity; return Vector3.Distance(player.transform.position, transform.position); }
    public IEnumerator AttackRoutine() { WaitForSeconds attackCooldown = new WaitForSeconds(config.attackCooldown); while (GetDistanceToPlayer() <= config.attackRange) { Debug.Log("몬스터 공격!"); yield return attackCooldown; } }
    public void StopAttackRoutine() { if (attackRoutineCor != null) { StopCoroutine(attackRoutineCor); attackRoutineCor = null; } }
    private void HandleHit() { if (!health.IsDead) { ChangeState(hitState); } }
    private void HandleDeath() { StopAllCoroutines(); ChangeState(dieState); SetAnimation(hashDieBool, true); } // 죽었을 때 모든 코루틴 정지
    #endregion

    #region 애니메이션

    private readonly int hashDieBool = Animator.StringToHash("Die");

    public void SetAnimation(int animHash, bool value) { if (animator == null) return; animator.SetBool(animHash, value); }
    #endregion

    /// <summary>
    /// 몬스터의 절차적 움직임(IK)을 켜거나 끕니다. 거미에게만 해당됩니다.
    /// </summary>
    public void SetProceduralMovement(bool isActive)
    {
        // Spider 컴포넌트가 있는지 확인
        if (TryGetComponent<Spider>(out var spiderBody))
        {
            spiderBody.enabled = isActive;
        }
        // IKStepManager 컴포넌트가 있는지 확인
        if (TryGetComponent<IKStepManager>(out var spiderStepManager))
        {
            spiderStepManager.enabled = isActive;
        }
    }

    /// <summary>
    /// 애니메이터의 Trigger 파라미터를 발동시킵니다.
    /// </summary>
    public void SetTrigger(int animHash)
    {
        if (animator != null)
        {
            animator.SetTrigger(animHash);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // config 파일이 없으면 아무것도 그리지 않습니다.
        if (config == null) return;

        // 1. 공격 범위 (Attack Range)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, config.attackRange);

        // 2. 멈추는 거리 (Stopping Distance)
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, config.stoppingDistance);

        // 3. 소리 감지 범위 (Sound Range)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.soundRange);

        // 4. 현재 목적지 (실행 중에만 표시)
        if (Application.isPlaying && (CurrentState is Patrol || CurrentState is Trace))
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(currentDestination, 0.5f);
            Gizmos.DrawLine(transform.position, currentDestination);
        }
    }
#endif
}