// MonsterSensor.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// 몬스터의 시야 감지(FOV)를 전문적으로 처리하는 컴포넌트입니다.
/// </summary>
public class MonsterSensor : MonoBehaviour
{
    [Header("감지 설정")]
    public MonsterConfig config;
    public LayerMask targetMask;
    public LayerMask obstructionMask;

    [Tooltip("레이캐스트를 시작할 '눈' 높이입니다. (몬스터 발 위치 기준)")]
    public float eyeHeight = 1.5f;

    // 감지 결과를 저장하는 프로퍼티
    public bool CanSeePlayer { get; private set; }
    public Vector3 TargetLastPosition { get; private set; }

    private GameObject player;
    private WaitForSeconds checkDelay = new WaitForSeconds(0.2f);

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Start()
    {
        // 컴포넌트가 스스로 감지 루틴을 시작합니다.
        StartCoroutine(CheckFovRoutine());
    }

    private IEnumerator CheckFovRoutine()
    {
        while (true)
        {
            CheckFov();
            yield return checkDelay;
        }
    }
    // MonsterSensor.cs 파일의 CheckFov 함수를 아래 코드로 교체하세요.

    private void CheckFov()
    {
        if (player == null) return;

        bool playerDetected = false;

        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, config.fovRange, targetMask);

        if (rangeChecks.Length > 0)
        {
            // Debug.Log($"<color=green>1. 거리 감지 성공...</color>"); // (디버그 로그는 잠시 비활성화)

            Transform target = rangeChecks[0].transform;

            // --- 수정된 '눈' 위치 및 방향 계산 ---
            // 몬스터의 '눈' 위치를 계산합니다. (transform.up을 사용해 경사로에서도 작동)
            Vector3 eyePosition = transform.position + transform.up * eyeHeight;

            // '눈'에서 '타겟'으로 향하는 방향을 계산합니다.
            Vector3 directionToTarget = (target.position - eyePosition).normalized;
            // --- 수정 끝 ---

            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle < config.fovAngle / 2)
            {
                // Debug.Log($"<color=green>2. 시야각 감지 성공...</color>"); // (디버그 로그는 잠시 비활성화)

                // '눈'에서 '타겟'까지의 거리를 계산합니다.
                float distanceToTarget = Vector3.Distance(eyePosition, target.position);

                // --- 수정된 '눈' 위치에서 레이캐스트 ---
                // 'transform.position' 대신 'eyePosition'에서 레이캐스트를 쏩니다.
                if (Physics.Raycast(eyePosition, directionToTarget, out RaycastHit hit, distanceToTarget, obstructionMask))
                {
                    Debug.LogWarning($"<color=red>3. 장애물 감지:</color> 시야가 '{hit.collider.name}'에 막혔습니다!");
                }
                else
                {
                    playerDetected = true;
                    TargetLastPosition = target.position; // 마지막 위치 저장
                    Debug.Log("<color=cyan>★★★ 최종 감지 성공! ★★★</color>");
                }
            }
            //else
            //{
            //    Debug.LogWarning($"<color=orange>2. 시야각 감지 실패...</color>"); // (디버그 로그는 잠시 비활성화)
            //}
        }
        //else
        //{
        //    Debug.Log($"<color=red>1. 거리 감지 실패...</color>"); // (디버그 로그는 잠시 비활성화)
        //}

        CanSeePlayer = playerDetected;
        // Debug.Log($"[Sensor Report] 최종 감지 결과: CanSeePlayer = {CanSeePlayer}"); // (디버그 로그는 잠시 비활성화)
    }

    // 디버그용 기즈모 (AI 컨트롤러에서 옮겨옴)
    private void OnDrawGizmosSelected()
    {
        if (config == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, config.fovRange);
        Vector3 fovLine1 = Quaternion.AngleAxis(config.fovAngle / 2, transform.up) * transform.forward * config.fovRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-config.fovAngle / 2, transform.up) * transform.forward * config.fovRange;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);
    }
}