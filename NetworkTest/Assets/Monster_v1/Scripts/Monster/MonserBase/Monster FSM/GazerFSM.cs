using GazerStates;
using System;
using UnityEngine;

public class GazerFSM : MonsterFSM
{
    // --- Äð´Ù¿î Å¸ÀÌ¸Ó ---
    private float beamCooldownTimer = 0f;
    private float strafeCooldownTimer = 0f;

    public bool IsBeamOnCooldown => beamCooldownTimer > 0;
    public bool IsStrafeOnCooldown => strafeCooldownTimer > 0;

    public void StartBeamCooldown(float duration) { beamCooldownTimer = duration; }
    public void StartStrafeCooldown(float duration) { strafeCooldownTimer = duration; }

    private void Update()
    {
        if (beamCooldownTimer > 0) beamCooldownTimer -= Time.deltaTime;
        if (strafeCooldownTimer > 0) strafeCooldownTimer -= Time.deltaTime;
    }

    // --- »óÅÂ Á¤ÀÇ ---
    private readonly Idle _idleState = new Idle();
    private readonly Patrol _patrolState = new Patrol();
    private readonly Trace _traceState = new Trace();
    private readonly MeleeAttack _attackState = new MeleeAttack();
    private readonly BeamAttack _lookAroundState = new BeamAttack(); // (LookAround ½½·Ô -> ºö)
    private readonly Hit _hitState = new Hit();
    private readonly Die _dieState = new Die();
    private readonly Evasion _tauntState = new Evasion(); // (Taunt ½½·Ô -> È¸ÇÇ)

    // --- ½½·Ô ¿¬°á ---
    public override ZombieBaseState<MonsterAIController> IdleState => _idleState;
    public override ZombieBaseState<MonsterAIController> PatrolState => _patrolState;
    public override ZombieBaseState<MonsterAIController> TraceState => _traceState;
    public override ZombieBaseState<MonsterAIController> AttackState => _attackState;
    public override ZombieBaseState<MonsterAIController> LookAroundState => _lookAroundState;
    public override ZombieBaseState<MonsterAIController> HitState => _hitState;
    public override ZombieBaseState<MonsterAIController> DieState => _dieState;
    public override ZombieBaseState<MonsterAIController> TauntState => _tauntState;
    public override ZombieBaseState<MonsterAIController> BlockState => null;
}