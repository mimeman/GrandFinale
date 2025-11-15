using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraSwitcher : MonoBehaviour
{

    public CharacterMove characterMove;
    public EventsCenter eventsCenter;

    [SerializeField] private CinemachineVirtualCamera fpvCamera;
    [SerializeField] private CinemachineVirtualCamera fpv_aimCamera;
    [SerializeField] private CinemachineVirtualCamera tpvCamera;
    [SerializeField] private CinemachineVirtualCamera tpv_aimCamera;

    // --- 상태 변수 ---
    private bool isGrounded;
    private bool isWeaponChange;
    private bool isFirstpersonView = false;
    private bool isAiming = false;

    bool CanAimCheck()
    {
        // [추가] 인벤토리 열려있으면 조준 불가
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsFocused)
            return false;

        var canAim = (isGrounded && !characterMove.moveState.isSprint && !isWeaponChange);
        return canAim;
    }

    void Awake()
    {
        StopAiming();
    }

    private void OnEnable()
    {
        characterMove.OnGroundedValueChange += ApplyIsGround;

        eventsCenter.OnWeaponChange += ApplyIsWeaponChange;
    }

    private void OnDisable()
    {
        characterMove.OnGroundedValueChange -= ApplyIsGround;

        eventsCenter.OnWeaponChange -= ApplyIsWeaponChange;
    }

    void ApplyIsGround(bool value) => isGrounded = value;
    void ApplyIsWeaponChange(bool value) => isWeaponChange = value;

    // 롱클릭을 위한 TPV 조준 시작 함수
    public void StartTpvAim()
    {
        if (isFirstpersonView)
        {
            ToggleFpvAim();
            return;
        }
        if (!CanAimCheck()) return;
        isAiming = true;
        characterMove.moveState.walk = true;
        tpv_aimCamera.Priority = 2; // TPV 조준 카메라 활성화
    }

    // 숏클릭을 위한 FPV 조준 토글 함수
    public void ToggleFpvAim()
    {
        // 현재 조준 중이 아닐 때, 조준 불가능 상태면 아무것도 안 함
        if (!isAiming && !CanAimCheck()) return;

        isAiming = !isAiming; // 조준 상태를 반전 (토글)
        characterMove.moveState.walk = isAiming;

        if (isAiming)
        {
            // FPV 조준 카메라 활성화
            fpv_aimCamera.Priority = 2;
        }
        else
        {
            // FPV 조준 카메라 비활성화
            fpv_aimCamera.Priority = 0;
        }
    }

    public void StopAiming()
    {
        if (!isAiming) return;
        isAiming = false;
        characterMove.moveState.walk = false;

        tpv_aimCamera.Priority = 0;
        fpv_aimCamera.Priority = 0;
    }

    public void AimViewChange(bool tps)
    {
        if (CanAimCheck())
        {
            if (tps)
                tpv_aimCamera.Priority = tpv_aimCamera.Priority == 0 ? 1 : 0;
            else
                fpv_aimCamera.Priority = fpv_aimCamera.Priority == 0 ? 2 : 0;
            isAiming = !isAiming;
            characterMove.moveState.walk = isAiming;
        }
    }
    public void ViewChange()
    {
        if (isAiming) return;

        isFirstpersonView = !isFirstpersonView;
        tpvCamera.Priority = isFirstpersonView ? 0 : 1;
        fpvCamera.Priority = isFirstpersonView ? 1 : 0;
    }

    private void Update()
    {
        if (isAiming && !CanAimCheck())
        {
            StopAiming();
        }
    }
}