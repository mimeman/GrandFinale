using System.Collections;
using UnityEngine;

namespace SpiderStates
{
    public class Idle : ZombieBaseState<MonsterAIController>
    {
        private float idleTime;
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            // 이동만 멈추고 애니메이션 제어는 하지 않습니다.
            monster.StopMoving();
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
        public override void EnterState(MonsterAIController monster)
        {
            patrolDestination = monster.GetRandomPatrolDestination();
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (monster.sensor.CanSeePlayer)
            {
                return monster.traceState;
            }
            monster.MoveTo(patrolDestination);
            if (monster.arrivedAtDestination)
            {
                return monster.idleState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
        }
    }

    public class Trace : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster) { }

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
                return monster.patrolState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
        }
    }

    public class Attack : ZombieBaseState<MonsterAIController>
    {
        private float attackTimer;
        private float attackAnimationLength = 1.5f;

        public override void EnterState(MonsterAIController monster)
        {
            attackTimer = 0f;
            monster.StopMoving();
            monster.SetProceduralMovement(false); // IK 비활성화

            if (Random.value > 0.5f) { monster.SetTrigger(monster.hashDoAttack); }
            

            if (monster.player != null) { monster.LookAt(monster.player.transform.position); }
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackAnimationLength)
            {
                return monster.traceState;
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.SetProceduralMovement(true); // IK 다시 활성화
        }
    }

    public class Die : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.StopAllCoroutines();
            monster.SetProceduralMovement(false);
            monster.SetTrigger(monster.hashDie);

            if (monster.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster) { return this; }
        public override void ExitState(MonsterAIController monster) { }
    }
}