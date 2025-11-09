using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
[RequireComponent(typeof(MonsterAIController))]
// ★ 1. 클래스 이름 변경
public class FloatingMovement : MonoBehaviour, IMonsterMovement
{
    private NavMeshAgent agent;
    private Animator animator;
    private MonsterAIController controller;
    private float targetSpeed;

    [Header("--- 가속/감속 설정 ---")]
    [SerializeField] private float accelerationRate = 5f;
    [SerializeField] private float decelerationRate = 10f;

    [Header("--- 부유 설정 ---")]
    [Tooltip("지면(NavMesh)으로부터 얼마나 높이 뜰지 설정")]
    public float hoverHeight = 3.0f;

    private int animSpeedHash;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        controller = GetComponent<MonsterAIController>();
        agent.updateRotation = true;

        // ★ 3. (추가) NavMeshAgent의 기본 높이를 설정
        agent.baseOffset = hoverHeight;
    }

    private void Start()
    {
        // Golem과 동일하게 AnimConfig에서 해시를 가져옴
        if (controller.animConfig != null)
        {
            this.animSpeedHash = controller.hashMoveSpeed;
        }
        else
        {
            Debug.LogError("FloatingMovement: animConfig가 없습니다!");
        }
    }

    private void Update()
    {
        // (수정) Golem과 동일하게 Locomotion은 FSM이 제어하므로 주석 처리
        float currentSpeed = agent.speed;
        float rate = (currentSpeed < targetSpeed) ? accelerationRate : decelerationRate;
        agent.speed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.deltaTime * rate);

        // Gazer도 Locomotion(Float) 파라미터를 쓴다면, FSM이 제어해야 함
        // animator.SetFloat(this.animSpeedHash, agent.velocity.magnitude);
    }

    // (이하 Move, TurnTowards, Stop 함수는 수정할 필요 없음)
    public void Move(Vector3 destination, float speed)
    {
        this.targetSpeed = speed;
        agent.SetDestination(destination);
    }

    public void TurnTowards(Vector3 worldTargetPosition, float turnSpeed)
    {
        agent.SetDestination(transform.position);
        transform.LookAt(new Vector3(worldTargetPosition.x, transform.position.y, worldTargetPosition.z));
    }

    public void Stop()
    {
        if (agent.hasPath)
        {
            agent.ResetPath();
        }
        this.targetSpeed = 0f;
    }
}