using UnityEngine;
using MinotaurStates; // <--- 이 스크립트만 Minotaur를 참조합니다!

// 1. 아까 만든 MonsterFSM 설계도를 상속받습니다.
public class MinotaurFSM : MonsterFSM
{
    // 2. MonsterAIController가 하던 것처럼, 모든 상태를 '미리' 생성해 둡니다.
    // (GC(가비지 컬렉션) 방지를 위해 재사용)
    private readonly Idle _idleState = new Idle();
    private readonly Patrol _patrolState = new Patrol();
    private readonly Trace _traceState = new Trace();
    private readonly Attack _attackState = new Attack();
    private readonly LookAround _lookAroundState = new LookAround();
    private readonly Hit _hitState = new Hit();
    private readonly Die _dieState = new Die();

    // 3. MonsterFSM 설계도의 '추상 프로퍼티'들을 '구현'합니다.
    // (MonsterAIController가 fsm.IdleState를 호출하면, 이 FSM은 _idleState를 반환합니다)
    public override ZombieBaseState<MonsterAIController> IdleState => _idleState;
    public override ZombieBaseState<MonsterAIController> PatrolState => _patrolState;
    public override ZombieBaseState<MonsterAIController> TraceState => _traceState;
    public override ZombieBaseState<MonsterAIController> AttackState => _attackState;
    public override ZombieBaseState<MonsterAIController> LookAroundState => _lookAroundState;
    public override ZombieBaseState<MonsterAIController> HitState => _hitState;
    public override ZombieBaseState<MonsterAIController> DieState => _dieState;
    public override ZombieBaseState<MonsterAIController> BlockState => null;
    public override ZombieBaseState<MonsterAIController> TauntState => null;
}