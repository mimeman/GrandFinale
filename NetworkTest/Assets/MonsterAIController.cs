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

    [Tooltip("투사체가 발사될 위치 (예: 몬스터의 입, 손) 현재는 식물 형태 몬스터만 사용")]
    public Transform firePoint;

    [Header("애니메이션 설정")]
    public MonsterAnimationConfig animConfig;

    public MonsterFSM fsm { get; private set; }

    public GameObject player { get; private set; }

    [Header("디버그 설정")]
    public bool alwaysShowGizmos = false;
    // --- 상태 머신 (FSM) ---
    public ZombieBaseState<MonsterAIController> CurrentState { get; private set; }


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
    public int hashMoveSpeed { get; private set; }
    public int hashIsWalking { get; private set; }
    public int hashIsRunning { get; private set; }
    public int hashAttack1 { get; private set; }
    public int hashAttack2 { get; private set; }
    public int hashAttack3 { get; private set; }
    public int hashAttack4 { get; private set; }
    public int hashBlockStart { get; private set; } 
    public int hashBlockEnd { get; private set; } 
    public int hashHit { get; private set; }
    public int hashDie { get; private set; }
    public int hashDie2 { get; private set; }
    public int hashLookAround { get; private set; }
    public int hashTaunt { get; private set; }
    public int hashIdleType { get; private set; }
    #endregion

    #region 초기화 및 루프
    void Awake()
    {
        movement = GetComponent<IMonsterMovement>();
        health = GetComponent<MonsterHealth>();
        sensor = GetComponent<MonsterSensor>();
        animator = GetComponentInChildren<Animator>();
        fsm = GetComponent<MonsterFSM>();
        player = GameObject.FindGameObjectWithTag("Player");


        if (animConfig == null)
        {
            Debug.LogError(gameObject.name + "에 MonsterAnimationConfig 파일이 할당 안됨");
            return;
        }

        TryGetComponent<NavMeshAgent>(out agent);

        if (config == null)
        {
            Debug.LogError(gameObject.name + "에 MonsterConfig 파일이 할당 안됨");
            return;
        }

        if (fsm == null)
        {
            Debug.LogError(gameObject.name + "에 MonsterFSM ('게임팩') 컴포넌트가 없습니다! GolemFSM, GazerFSM 등을 추가해주세요.", this);
            return; // Start() 함수가 실행되지 않도록 중단
        }

        // 3. Health 컴포넌트에 Config 값을 넘겨 초기화시킵니다.
        InitializeAnimationHashes();
        health.Initialize(config);
    }

    void Start()
    {
        health.OnHit.AddListener(HandleHit);
        health.OnDeath.AddListener(HandleDeath);
        health.OnBlock.AddListener(HandleBlock);
        ChangeState(fsm.IdleState);
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

    private void InitializeAnimationHashes()
    {
        hashIdleType = Animator.StringToHash(animConfig.idleTypeInt);
        hashMoveSpeed = Animator.StringToHash(animConfig.moveSpeedFloat);
        hashIsWalking = Animator.StringToHash(animConfig.isWalkingBool);
        hashIsRunning = Animator.StringToHash(animConfig.isRunningBool);
        hashAttack1 = Animator.StringToHash(animConfig.attackTrigger1);
        hashAttack2 = Animator.StringToHash(animConfig.attackTrigger2);
        hashAttack3 = Animator.StringToHash(animConfig.attackTrigger3);
        hashAttack4 = Animator.StringToHash(animConfig.attackTrigger4);
        hashHit = Animator.StringToHash(animConfig.hitTrigger);
        hashDie = Animator.StringToHash(animConfig.dieTrigger);
        hashDie2 = Animator.StringToHash(animConfig.dieTrigger2);
        hashLookAround = Animator.StringToHash(animConfig.lookAroundTrigger);
        hashTaunt = Animator.StringToHash(animConfig.tauntTrigger);
        hashBlockStart = Animator.StringToHash(animConfig.blockStartTrigger);
        hashBlockEnd = Animator.StringToHash(animConfig.blockEndTrigger);
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
        float speed = (CurrentState == fsm.TraceState) ? config.runSpeed : config.walkSpeed;
        // 최종 목적지를 이동 시스템에 전달합니다.
        movement.Move(destination, speed);

        // NavMeshAgent가 없는 몬스터(거미)는 수동으로 회전시켜 줍니다.
        if (agent == null)
        {
            movement.TurnTowards(destination, config.turnSpeed);
        }
    }

    public void StopMoving() { movement.Stop();  }
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

    public float GetDistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;

        Vector3 monsterPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerPos = new Vector3(player.transform.position.x, 0, player.transform.position.z);
        return Vector3.Distance(monsterPos, playerPos);
    }

    public IEnumerator AttackRoutine() { WaitForSeconds attackCooldown = new WaitForSeconds(config.attackCooldown); while (GetDistanceToPlayer() <= config.attackRange) { Debug.Log("몬스터 공격!"); yield return attackCooldown; } }
    public void StopAttackRoutine() { if (attackRoutineCor != null) { StopCoroutine(attackRoutineCor); attackRoutineCor = null; } }
    // MonsterAIController.cs -> HandleHit (만약 1대만 맞아도 Block 하길 원한다면)

    private void HandleHit()
    {
        if (health.IsDead) return;

        // (요청) 조건 1: 플레이어를 감지하지 못했을 때 (예: 저격)
        if (!sensor.CanSeePlayer)
        {
            SetAnimTrigger(hashHit);
            ChangeState(fsm.HitState);
            return;
        }

        // --- (이하는 플레이어를 감지한 상황) ---

        // 1. GolemFSM인지 확인
        var golemFSM = fsm as GolemFSM;
        if (golemFSM != null)
        {
            // (골렘일 때)
            var blockState = CurrentState as GolemStates.Block;

            // 2. 만약 "Block" 상태라면
            if (blockState != null)
            {
                // (요청) "취약" 단계일 때만 피격당함
                if (blockState.CurrentPhase == GolemStates.Block.Phase.VulnerableCheck)
                {
                    SetAnimTrigger(hashHit);
                    ChangeState(fsm.HitState);
                }
                // "방어" 또는 "돌진" 중에는 피격 무시
                return;
            }
            // 3. (요청) Block 상태가 아닐 때 (예: Trace, Attack)
            //    Hit 대신 Block 상태로 전환 (쿨다운 확인)
            else
            {
                if (golemFSM.IsBlockOnCooldown)
                {
                    Debug.Log("방어 쿨다운 중... 피격!");
                    // (쿨다운 중일 땐 Hit 애니메이션 실행)
                    SetAnimTrigger(hashHit);
                    ChangeState(fsm.HitState);
                    return;
                }

                // ★ (수정) "Hit" 대신 "Block" 상태로 전환
                ChangeState(fsm.BlockState);
            }
        }
        // 4. Golem이 아닌 몬스터(좀비, 식물)
        else
        {
            SetAnimTrigger(hashHit);
            ChangeState(fsm.HitState);
        }
    }
    private void HandleBlock()
    {
        if (health.IsDead) return;

        // 1. GolemFSM인지 확인
        var golemFSM = fsm as GolemFSM;

        // 2. (수정) GolemFSM이 아니면 (예: 좀비, 슬라임) 방어/반격 안 함
        if (golemFSM == null)
        {
            return;
        }

        // --- (이하는 GolemFSM일 때만 실행) ---

        // 3. 이미 Block/Hit 중이면 무시
        if (CurrentState == fsm.BlockState || CurrentState == fsm.HitState)
        {
            return;
        }

        // 4. 10초 쿨다운이 돌고 있으면 무시
        if (golemFSM.IsBlockOnCooldown)
        {
            Debug.Log("방어 쿨다운 중... 무시!");
            return;
        }

        // 5. Golem이 맞고 쿨다운도 아니므로 Block 상태로 전환
        ChangeState(fsm.BlockState);
    }
    private void HandleDeath()
    {
        StopAllCoroutines();
        if (Random.value > 0.5f) SetAnimTrigger(hashDie); // "Death1"
        else SetAnimTrigger(hashDie2); // "Death2"
        ChangeState(fsm.DieState);
    }
    #endregion

    #region 애니메이션

    /// <summary>
    /// 애니메이터의 'Bool' 파라미터를 설정합니다.
    /// </summary>
    public void SetAnimBool(int animHash, bool value)
    {
        if (animator == null) return;
        animator.SetBool(animHash, value);
    }

    /// <summary>
    /// 애니메이터의 'Float' 파라미터를 설정합니다.
    /// </summary>
    public void SetAnimFloat(int animHash, float value)
    {
        if (animator == null) return;
        animator.SetFloat(animHash, value);
    }

    /// <summary>
    /// 애니메이터의 'Trigger' 파라미터를 발동시킵니다.
    /// (기존 SetTrigger 함수와 동일, 이름만 변경)
    /// </summary>
    public void SetAnimTrigger(int animHash)
    {
        if (animator != null)
        {
            animator.SetTrigger(animHash);
        }
    }

    public void SetAnimInt(int animHash, int value)
    {
        if (animator == null) return;
        animator.SetInteger(animHash, value);
    }

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
        if (Application.isPlaying && fsm != null && (CurrentState == fsm.PatrolState || CurrentState == fsm.TraceState))
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(currentDestination, 0.5f);
            Gizmos.DrawLine(transform.position, currentDestination);
        }
    }
#endif
}