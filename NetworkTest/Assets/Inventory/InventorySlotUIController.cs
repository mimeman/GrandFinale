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

    /// <summary>
    /// InventoryManager에서 필터링된 리스트를 가져와서 UI 슬롯에 바인딩하고 활성/비활성화합니다.
    /// </summary>
    private void UpdateSlotUIBindings()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[InventorySlotUIController] InventoryManager가 없습니다.");
            return;
        }

        // 1. InventoryManager에서 현재 필터에 맞는 아이템 리스트를 가져옵니다.
        List<RelicData> filteredItems = InventoryManager.Instance.GetFilteredInventory();

        // 2. UI 슬롯과 필터링된 아이템을 1:1 매칭합니다.
        for (int i = 0; i < allInventorySlots.Count; i++)
        {
            Slot_UI slotUI = allInventorySlots[i];

            if (i >= filteredItems.Count)
            {
                slotUI.gameObject.SetActive(false);
                continue;
            }

            RelicData item = filteredItems[i];
            if (item != null)
            {

                slotUI.SetBoundItem(item, i);
                slotUI.gameObject.SetActive(true);
            }
            // 4. 필터링된 아이템이 null인 경우 (즉, 아이템이 비어있는 경우)
            else
            {
                slotUI.SetBoundItem(null, i);
                slotUI.gameObject.SetActive(true);
            }
        }
    }
}