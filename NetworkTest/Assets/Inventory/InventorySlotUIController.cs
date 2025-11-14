using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// InventoryManager의 필터 상태에 따라 슬롯 UI의 바인딩과 활성화/비활성화를 관리합니다.
/// 이 스크립트는 필터링된 아이템을 UI 슬롯에 0번부터 순서대로 재배치하는 역할을 합니다.
/// </summary>
public class InventorySlotUIController : MonoBehaviour
{
    [Header("인벤토리의 모든 슬롯 UI")]
    [SerializeField] private List<Slot_UI> allInventorySlots;

    void Start()
    {
        // 1. 이벤트 구독: 인벤토리의 내용 또는 필터가 변경될 때마다 UpdateUI를 호출합니다.
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged += UpdateSlotUIBindings;
        }

        // 2. 초기화
        UpdateSlotUIBindings();
    }

    void OnDestroy()
    {
        // 스크립트가 파괴될 때 이벤트 구독 해제
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged -= UpdateSlotUIBindings;
        }
    }

    private void UpdateSlotUIBindings()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[InventorySlotUIController] InventoryManager가 없습니다.");
            return;
        }

        // 1. InventoryManager에서 필터링된 InventorySlot 리스트를 가져옵니다.
        List<InventorySlot> filteredSlots = InventoryManager.Instance.GetFilteredInventory();

        // 2. 'allInventorySlots' 리스트의 'i'번째 슬롯을 가져옵니다.
        // (이 슬롯의 slotIndex는 Inspector에서 설정한 0~59 고유값)
        for (int i = 0; i < allInventorySlots.Count; i++)
        {
            Slot_UI slotUI = allInventorySlots[i];
            if (slotUI == null) continue; // (안전 장치)

            // 3. 'filteredSlots' 리스트의 'i'번째 아이템을 가져옵니다.
            // (필터링된 리스트이므로 0, 1, 2... 순서)
            InventorySlot slotData;
            if (i < filteredSlots.Count)
            {
                slotData = filteredSlots[i];
            }
            else
            {
                // (안전 장치) 필터링된 아이템 수보다 UI 슬롯이 더 많으면
                // 뒤쪽 슬롯은 빈 슬롯으로 채웁니다.
                slotData = new InventorySlot(); // 빈 슬롯 데이터
            }

            // 4. ★★★ (핵심 수정) ★★★
            // slotUI.SetBoundItem(slotData, i); // (이전 코드, 에러 발생 지점)
            slotUI.SetBoundItem(slotData); // (수정된 코드)

            slotUI.gameObject.SetActive(true);
        }
    }
}