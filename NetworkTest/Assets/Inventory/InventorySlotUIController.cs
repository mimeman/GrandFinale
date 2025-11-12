using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// InventoryManager의 필터 상태에 따라 슬롯 UI의 바인딩과 활성화/비활성화를 관리합니다.
/// 이 스크립트는 필터링된 아이템을 UI 슬롯에 0번부터 순서대로 재배치하는 역할을 합니다.
/// </summary>
public class InventorySlotUIController : MonoBehaviour
{
    // ★ Inspector에서 인벤토리의 모든 Slot_UI 컴포넌트를 이 리스트에 연결해야 합니다. ★
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
        //    (InventoryManager.GetFilteredInventory() 함수가 null 슬롯으로 채워진 리스트를 반환한다고 가정)
        List<RelicData> filteredItems = InventoryManager.Instance.GetFilteredInventory();

        // 2. UI 슬롯과 필터링된 아이템을 1:1 매칭합니다.
        for (int i = 0; i < allInventorySlots.Count; i++)
        {
            Slot_UI slotUI = allInventorySlots[i];

            // UI 슬롯 인덱스가 필터링된 아이템 리스트의 범위를 초과하는 경우: 
            // 예를 들어, 필터링 후 아이템이 5개만 남았는데 8번째 슬롯을 처리하는 경우
            if (i >= filteredItems.Count)
            {
                slotUI.gameObject.SetActive(false);
                continue;
            }

            RelicData item = filteredItems[i];

            // 3. 필터링된 아이템이 null이 아닌 경우 (즉, 아이템이 있는 경우)
            if (item != null)
            {
                // 해당 슬롯 UI를 활성화하고, 아이템 데이터를 바인딩합니다.
                // (Slot_UI.UpdateSlotVisuals()가 slotIndex 대신 이 바인딩된 데이터를 사용하도록 변경 필요)
                slotUI.SetBoundItem(item, i); // 바인딩 후 내부적으로 UpdateSlotVisuals 호출
                slotUI.gameObject.SetActive(true);
            }
            // 4. 필터링된 아이템이 null인 경우 (즉, 아이템이 비어있는 경우)
            else
            {
                // 인벤토리 슬롯 자체는 활성화하되, 내부 아이콘은 비워줍니다.
                // (현재 인벤토리의 총 크기를 표시하기 위해 빈 슬롯도 활성화)
                slotUI.SetBoundItem(null, i);
                slotUI.gameObject.SetActive(true);
            }
        }
    }
}