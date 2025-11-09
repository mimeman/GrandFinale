using UnityEngine;

public class Projectile_Arc : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float arcHeight;
    private float travelTime; // 총 이동 시간
    private float timer = 0f; // 경과 시간

    /// <summary>
    /// FireShot 함수가 이 함수를 호출하여 발사 정보를 설정합니다.
    /// </summary>
    public void Initialize(Vector3 target, float height, float speed)
    {
        startPos = transform.position;
        targetPos = target;
        arcHeight = height;

        // 거리를 속도로 나누어 총 이동 시간 계산
        float distance = Vector3.Distance(startPos, targetPos);
        if (speed <= 0) speed = 15f; // 0으로 나누기 방지
        travelTime = distance / speed;

        // 이동 시간이 지나면 자동으로 파괴
        Destroy(gameObject, travelTime + 0.1f);
    }

    void Update()
    {
        if (travelTime <= 0) return; // 아직 Initialize 안됨

        timer += Time.deltaTime;

        // 0.0 ~ 1.0 사이의 진행률 (t) 계산
        float t = timer / travelTime;
        if (t > 1f) t = 1f;

        // 1. 시작점 -> 목표점까지의 직선 경로상 현재 위치 계산
        Vector3 pos = Vector3.Lerp(startPos, targetPos, t);

        // 2. 포물선 높이 계산 (y = 4 * h * x * (1-x) 공식)
        // t가 0.5(중간)일 때 가장 높은 1.0 * arcHeight가 됨
        float arc = 4 * arcHeight * t * (1 - t);

        // 3. 직선 경로 높이에 포물선 높이를 더함
        pos.y += arc;

        // 4. 발사체 위치 업데이트
        transform.position = pos;
    }

    /// <summary>
    /// 이 발사체가 다른 Collider(Is Trigger)와 부딪혔을 때 호출됩니다.
    /// </summary>
/*    void OnTriggerEnter(Collider other)
    {
        // 1. 플레이어와 부딪혔는지 확인
        if (other.CompareTag("Player"))
        {
            // (가정) 플레이어에게 PlayerHealth 스크립트가 있다고 가정
            if (other.TryGetComponent<PlayerHealth>(out var playerHealth))
            {
                // (가정) 10의 데미지를 줌
                //playerHealth.TakeDamage(10f);
            }

            // 플레이어에게 닿았으므로 즉시 파괴
            Destroy(gameObject);
        }
        // 2. 플레이어가 아닌 '땅'이나 '벽'에 닿았을 때
        else if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            // (선택 사항) 여기에 바닥에 웅덩이가 생기는 이펙트(VFX) 생성
            // Instantiate(puddleEffectPrefab, transform.position, Quaternion.identity);

            // 땅에 닿았으므로 즉시 파괴
            Destroy(gameObject);
        }
    }*/
}