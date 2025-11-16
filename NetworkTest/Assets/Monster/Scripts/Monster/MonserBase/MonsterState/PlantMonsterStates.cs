using UnityEngine;

namespace PlantMonsterStates
{

    public static class PlantAnimHashes
    {
        public static readonly int goAlive = Animator.StringToHash("goAlive");
        public static readonly int goPlant = Animator.StringToHash("goPlant");
        public static readonly int castStart = Animator.StringToHash("castStart");
        public static readonly int castEnd = Animator.StringToHash("castEnd");
    }

    // --- 1. 은신 상태 (FSM의 IdleState 슬롯) ---
    public class Plant_HidingState : ZombieBaseState<MonsterAIController>
    {
        private PlantMonsterConfig plantConfig;

        // (참고) HidingState에 있던 불필요한 RangedAttack 변수들을 제거했습니다.

        public override void EnterState(MonsterAIController monster)
        {
            if (plantConfig == null)
                plantConfig = monster.config as PlantMonsterConfig;

            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f); // Locomotion 0

            // ★ (수정) EnterState에서는 goPlant를 호출하지 않습니다.
            // monster.SetAnimTrigger(PlantAnimHashes.goPlant); 
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (plantConfig == null) return this;

            if (monster.GetDistanceToPlayer() <= plantConfig.activationRange)
            {
                // ★ (수정) 전투 상태로 전환 "직전"에 goAlive 애니메이션을 트리거합니다.
                monster.SetAnimTrigger(PlantAnimHashes.goAlive);
                return monster.fsm.TraceState; // -> Plant_AliveState
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }
    // --- 2. 전투 대기 상태 (FSM의 TraceState 슬롯) ---
    public class Plant_AliveState : ZombieBaseState<MonsterAIController>
    {
        private PlantMonsterConfig plantConfig;
        private PlantMonsterFSM plantFSM;
        private float checkTimer = 0f;
        private float persistenceTimer = 0f;

        public override void EnterState(MonsterAIController monster)
        {
            // ★ (오류 수정) Config 및 FSM 캐스팅
            if (plantConfig == null)
                plantConfig = monster.config as PlantMonsterConfig;
            if (plantFSM == null)
                plantFSM = monster.fsm as PlantMonsterFSM;

            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f); // Locomotion 0
            persistenceTimer = 0f;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (plantConfig == null || plantFSM == null) return this;

            checkTimer += Time.deltaTime;

            if (monster.player != null)
                monster.LookAt(monster.player.transform.position);

            // 플레이어 감지 체크
            if (monster.sensor.CanSeePlayer)
            {
                persistenceTimer = 0f;
            }
            else
            {
                persistenceTimer += Time.deltaTime;
                if (persistenceTimer >= monster.config.persistenceTime)
                {
                    monster.SetAnimTrigger(PlantAnimHashes.goPlant);
                    return monster.fsm.IdleState; // -> Plant_HidingState
                }
            }

            // (새 로직) 1초마다 공격 패턴 결정
            if (checkTimer >= 1.0f)
            {
                checkTimer = 0f;
                float distance = monster.GetDistanceToPlayer();

                // (요청) 우선순위 1: 원거리 공격 (쿨다운 아닐 때)
                if (distance <= plantConfig.rangedAttackRange && !plantFSM.IsRangedAttackOnCooldown)
                {
                    return monster.fsm.LookAroundState; // -> Plant_RangedAttackState
                }

                // (요청) 우선순위 2: 근접 공격
                if (distance <= monster.config.attackRange)
                {
                    return monster.fsm.AttackState; // -> Plant_MeleeAttackState
                }

                // (요청) 우선순위 3: 점프 돌진
                if (distance <= plantConfig.jumpRange)
                {
                    return monster.fsm.PatrolState; // -> Plant_LeapState
                }

                // (참고) 4순위: 모든 범위 밖이면 걷기 (locomotion 1)
                // (이 로직은 FSM 슬롯이 부족하여 현재는 대기 상태로 유지됨)
            }

            return this; // 전투 대기 상태 유지
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 3. 점프 돌진 상태 (FSM의 PatrolState 슬롯) ---
    public class Plant_LeapState : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            // (AnimConfig의 'tauntTrigger'에 'jump'를 연결)
            monster.SetAnimTrigger(monster.hashTaunt);
            // (점프 애니메이션이 Root Motion으로 이동시킨다고 가정)
            monster.SetAnimFloat(monster.hashMoveSpeed, 1f);
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            // (Root Motion이 없다면 NavMesh로 이동)
            if (monster.player != null)
                monster.MoveTo(monster.player.transform.position);

            // (요청) "Jump -> Attack" 로직
            // 근접 공격 범위까지 도착하면 '전투 대기'가 아닌 '근접 공격' 상태로 바로 전환
            if (monster.GetDistanceToPlayer() <= monster.config.attackRange)
            {
                return monster.fsm.AttackState; // -> Plant_MeleeAttackState
            }

            // (참고) 만약 애니메이션이 끝나면 대기 상태로 돌아가게 하려면
            // 애니메이션 종료를 감지하는 로직이 필요함 (지금은 생략)

            return this;
        }
        public override void ExitState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.SetAnimFloat(monster.hashMoveSpeed, 0f);
        }
    }

    // --- 4. 근접 공격 상태 (FSM의 AttackState 슬롯) ---
    public class Plant_MeleeAttackState : ZombieBaseState<MonsterAIController>
    {
        private float timer;
        private bool hasAppliedDamage;
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();

            // ★ (수정) attack 1~3 중 랜덤
            int attackIndex = Random.Range(0, 2);
            if (attackIndex == 0)
                monster.SetAnimTrigger(monster.hashAttack1);
            else if (attackIndex == 1)
                monster.SetAnimTrigger(monster.hashAttack2);

            if (monster.player != null)
                monster.LookAt(monster.player.transform.position);
            timer = 0f;
            hasAppliedDamage = false;
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            timer += Time.deltaTime;

            if (!hasAppliedDamage && timer >= monster.config.attackDelay)
            {
                hasAppliedDamage = true;
                monster.ApplyDamageToPlayer();
            }

            if (timer >= monster.config.attackCooldown)
            {
                return monster.fsm.TraceState; // -> Plant_AliveState
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 5. 원거리 공격 상태 (FSM의 LookAroundState 슬롯) ---
    public class Plant_RangedAttackState : ZombieBaseState<MonsterAIController>
    {
        private PlantMonsterConfig plantConfig;
        private PlantMonsterFSM plantFSM;
        private float timer;
        private int shotsFired;
        private int totalShots;
        private bool isCharging;

        public override void EnterState(MonsterAIController monster)
        {
            if (plantConfig == null)
                plantConfig = monster.config as PlantMonsterConfig;
            if (plantFSM == null)
                plantFSM = monster.fsm as PlantMonsterFSM;

            monster.StopMoving();
            monster.SetAnimTrigger(PlantAnimHashes.castStart);

            timer = 0f;
            shotsFired = 0;
            totalShots = Random.Range(3, 6);
            isCharging = true;
        }

        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            if (plantConfig == null || plantFSM == null) return this;

            timer += Time.deltaTime;

            if (isCharging)
            {
                if (timer >= plantConfig.castTime)
                {
                    isCharging = false;
                    FireShot(monster);
                }
            }
            else if (shotsFired < totalShots)
            {
                if (timer >= plantConfig.timeBetweenShots)
                {
                    FireShot(monster);
                }
            }
            else
            {
                plantFSM.StartRangedCooldown(30f);
                return monster.fsm.TraceState; // -> Plant_AliveState
            }
            return this;
        }

        private void FireShot(MonsterAIController monster)
        {
            monster.SetAnimTrigger(PlantAnimHashes.castEnd);

            if (plantConfig.projectilePrefab == null || monster.firePoint == null || monster.player == null)
            {
                Debug.LogError("Projectile Fire Failed: Config, FirePoint, or Player is missing!");
                return;
            }

            GameObject projectile = Object.Instantiate(
                plantConfig.projectilePrefab,
                monster.firePoint.position,
                Quaternion.identity
            );

            Vector3 targetPosition = monster.player.transform.position;
            Projectile_Arc arcScript = projectile.GetComponent<Projectile_Arc>();

            if (arcScript != null)
            {
                arcScript.Setup(plantConfig);

                arcScript.Initialize(
                    targetPosition,
                    plantConfig.projectileArcHeight,
                    plantConfig.projectileSpeed
                );
            }
            else
            {
                Debug.LogError("projectilePrefab에 Projectile_Arc.cs 스크립트가 없습니다!");
            }

            Debug.Log($"[{monster.name}] 원거리 {shotsFired + 1} / {totalShots} 번째 발사!");
            shotsFired++;
            timer = 0f;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 6. 피격 상태 (FSM의 HitState 슬롯) ---
    public class Plant_HitState : ZombieBaseState<MonsterAIController>
    {
        private float hitStunDuration = 0.5f;
        private float timer;
        public override void EnterState(MonsterAIController monster)
        {
            timer = 0f;
            if (monster.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                agent.speed = monster.config.runSpeed * 0.3f;
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController monster)
        {
            timer += Time.deltaTime;
            if (timer >= hitStunDuration)
            {
                return monster.fsm.TraceState; // (피격 시 무조건 전투 상태로)
            }
            return this;
        }
        public override void ExitState(MonsterAIController monster) { }
    }

    // --- 7. 사망 상태 (FSM의 DieState 슬롯) ---
    public class Plant_DieState : ZombieBaseState<MonsterAIController>
    {
        public override void EnterState(MonsterAIController monster)
        {
            monster.StopMoving();
            monster.StopAllCoroutines();
            if (monster.TryGetComponent<Collider>(out var c)) c.enabled = false;
            if (monster.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var a)) a.enabled = false;
            MonsterHealth health = monster.GetComponent<MonsterHealth>();
            if (health != null && monster.config.lootTable != null)
                health.SpawnLoot(monster.config.lootTable);
            if (MonsterManager.Instance != null)
                MonsterManager.Instance.RegisterMonsterDied();
            Object.Destroy(monster.gameObject, monster.config.corpseDestroyDelay);
        }
        public override ZombieBaseState<MonsterAIController> UpdateState(MonsterAIController m) { return this; }
        public override void ExitState(MonsterAIController m) { }
    }
}