using System.Collections;
using UnityEngine;

namespace NormalZombieStates
{
    public class Idle : ZombieBaseState<MonsterAIController>
    {
        private float idleTime;
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimation(monster.hashPatrol, false);
            monster.SetAnimation(monster.hashTrace, false);
            idleTime = Random.Range(monster.config.idleTimeMin, monster.config.idleTimeMax);
            timer = 0f;
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.sensor.CanSeePlayer)
            {
                return monster.traceState;
            }
            timer += Time.deltaTime;
            if (timer >= idleTime)
            {
                return monster.patrolState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    public class Patrol : ZombieBaseState<MonsterAIController>
    {
        private Vector3 patrolDestination;

        private float entryTimer;

        public override void EnterState(MonsterAIController monster)
        {
            patrolDestination = monster.GetRandomPatrolDestination();
            monster.SetAnimation(monster.hashPatrol, true);

            entryTimer = 0f;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.sensor.CanSeePlayer)
            {
                return monster.traceState;
            }

            monster.MoveTo(patrolDestination);

            // 타이머를 매 프레임 증가시킵니다
            entryTimer += Time.deltaTime;

            // 태 진입 후 아주 약간의 시간이 지난 뒤에만 도착 여부를 체크합니다.
            // 이렇게 하면 NavMeshAgent가 거리를 계산할 시간을 벌 수 있습니다.
            if (entryTimer > 0.1f && monster.arrivedAtDestination)
            {
                return monster.idleState;
            }

            return this;
        }

        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimation(monster.hashPatrol, false);
        }
    }

    public class Trace : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.SetAnimation(monster.hashTrace, true);
            if (monster.player != null)
            {
                monster.MoveTo(monster.player.transform.position);
            }
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.GetDistanceToPlayer() <= monster.config.attackRange)
            {
                return monster.attackState;
            }
            Vector3 targetPosition = monster.sensor.CanSeePlayer ? monster.player.transform.position : monster.sensor.TargetLastPosition;
            monster.MoveTo(targetPosition);
            if (!monster.sensor.CanSeePlayer && monster.arrivedAtDestination)
            {
                return monster.lookAroundState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimation(monster.hashTrace, false);
        }
    }

    public class LookAround : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimation(monster.hashLookAround, true);

            timer = 0f;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            // 1. 플레이어를 감지하면 즉시 추적합니다.
            if (monster.sensor.CanSeePlayer)
            {
                return monster.traceState;
            }

            // 2. C# 타이머를 증가시킵니다.
            timer += Time.deltaTime;

            // 3. MonsterConfig에 설정된 시간이 다 되면 Patrol 상태로 갑니다.
            if (timer >= monster.config.lookAroundTime)
            {
                return monster.patrolState;
            }
            return this;
        }

        public override void ExitState(MonsterAIController monster)
        {
            monster.SetAnimation(monster.hashLookAround, false);
        }
    }

    public class Attack : ZombieBaseState<MonsterAIController>
    {
        // 1. Hit 상태처럼, 공격이 지속되는 시간을 잴 타이머를 추가합니다.
        private float timer;

        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimation(monster.hashAttack, true); // 공격 애니메이션 켜기

            if (monster.player != null)
            {
                monster.LookAt(monster.player.transform.position);
            }

            // 2. 타이머를 0으로 초기화합니다.
            timer = 0f;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            // 5. (유지) 공격 중에도 플레이어를 계속 바라봅니다.
            monster.StopMoving();
            if (monster.player != null)
            {
                monster.LookAt(monster.player.transform.position);
            }

            // 6. (추가) 타이머를 증가시킵니다.
            timer += Time.deltaTime;

            // 7. (추가) MonsterConfig에 설정된 '공격 쿨다운' 시간이
            //    (애니메이션 재생 시간 + 대기 시간)이므로,
            //    이 시간이 지나면 공격이 끝난 것으로 간주하고 Trace 상태로 돌아갑니다.
            if (timer >= monster.config.attackCooldown)
            {
                return monster.traceState;
            }

            // 8. 시간이 다 되기 전까지는 "Attack" 상태에 "잠겨" 있습니다.
            return this;
        }

        public override void ExitState(MonsterAIController monster)
        {
            // 9. (유지) 상태를 나갈 때 애니메이션을 끕니다.
            monster.StopAttackRoutine(); // (호출한 적 없지만, 안전을 위해 놔둠)
            monster.SetAnimation(monster.hashAttack, false);
        }
    }
    public class Hit : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.SetAnimation(monster.hashHit, true);
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            return monster.traceState;  
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.SetAnimation(monster.hashHit, false);
        }
    }

    public class Die : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.StopAllCoroutines();
            //monster.SetAnimation(monster.hashDie, true); // Die 애니메이션 재생 -> Any State에서 처리

            // 1. 콜라이더 끄기 (다른 몬스터나 총알에 안 맞게)
            if (monster.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }

            // 2. NavMeshAgent 끄기
            if (monster.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
            {
                agent.enabled = false;
            }

/*            // 3. Rigidbody 켜서 중력 받기
            if (monster.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false; 
                rb.useGravity = true; 
            }*/

            MonsterHealth health = monster.GetComponent<MonsterHealth>();

            if (health != null && monster.config.lootTable != null)
            {

                health.SpawnLoot(monster.config.lootTable);
            }

            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.RegisterMonsterDied();
            }

            Object.Destroy(monster.gameObject, monster.config.corpseDestroyDelay);
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }
}