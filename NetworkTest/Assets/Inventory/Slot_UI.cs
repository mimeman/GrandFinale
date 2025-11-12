using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class Slot_UI : MonoBehaviour, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public int slotIndex;
    public Image slotIcon;
    public bool dropSuccessful = false;

    private Coroutine hideTooltipCoroutine; // 툴팁 숨김 딜레이용 코루틴
    private Coroutine tooltipCoroutine;
    private const float TooltipDelay = 0.5f;

    public RelicData currentItem { get; private set; }

    private static readonly string[] EquippableTypes =
    {
    ItemType.Weapon.ToString().ToLower(),    // "weapon"
    ItemType.Artifact.ToString().ToLower(),  // "artifact"
};

    void Start()
    {
        //InventoryManager.OnInventoryChanged += UpdateSlotVisuals;
        //UpdateSlotVisuals();
    }
    void OnDestroy()
    {
        //InventoryManager.OnInventoryChanged -= UpdateSlotVisuals;
    }

    /// <summary>
    /// [NEW] InventorySlotUIController에서 필터링된 아이템을 직접 바인딩합니다.
    /// </summary>
    public void SetBoundItem(RelicData newItem, int newIndex)
    {
        // 새로운 아이템 데이터와 인덱스를 저장합니다.
        currentItem = newItem;
        slotIndex = newIndex; // 슬롯 인덱스는 여전히 드래그/드롭에 필요합니다.

        // 시각적 업데이트를 강제합니다.
        UpdateSlotVisuals();
    }

    // L45 부근의 UpdateSlotVisuals 함수를 다음과 같이 수정 (새 로직)
    void UpdateSlotVisuals()
    {
        if (currentItem != null && !string.IsNullOrEmpty(currentItem.iconPath))
        {
            Sprite icon = Resources.Load<Sprite>(currentItem.iconPath);

            if (icon != null)
            {
                slotIcon.sprite = icon;
                slotIcon.enabled = true;
                // 인벤토리 상세 정보 업데이트
                if (InventoryUIManager.Instance != null)
                    InventoryUIManager.Instance.UpdateDetails(currentItem);
            }
            else
            {
                slotIcon.enabled = false;
            }
        }
        else
        {
            slotIcon.enabled = false;
        }

    }

    private bool CheckFilterMatch(RelicData item)
    {
        // L1: 현재 필터 가져오기
        InventoryFilterType currentFilter = InventoryManager.Instance.currentFilter;

        // L2: '전체' 필터는 항상 통과
        if (currentFilter == InventoryFilterType.All)
        {
            return true;
        }

        // L3: 아이템이 없으면 필터 통과 실패
        if (item == null)
        {
            return false;
        }

        // NOTE: ItemData.cs에 ItemType Enum이 정의되어 있으므로 이를 사용합니다.
        ItemType itemType = item.itemTypeEnum; // RelicData.cs에 itemTypeEnum 필드가 있다고 가정

        return currentFilter switch
        {
            // 무기: RelicData의 타입이 ItemType.Weapon일 때
            InventoryFilterType.Weapon => itemType == ItemType.Weapon,

            // 유물: RelicData의 타입이 ItemType.Artifact일 때
            InventoryFilterType.Relic => itemType == ItemType.Artifact,

            // 장비: 무기 또는 유물일 때 (장착 가능 아이템으로 간주)
            InventoryFilterType.Equipment => itemType == ItemType.Weapon || itemType == ItemType.Artifact,

            // 장신구: ItemType이 Accessory인 경우 (ItemData.cs에 Accessory가 없다면 임시로 StatBoost 사용)
            InventoryFilterType.Accessory => itemType == ItemType.StatBoost, // 실제 Accessory 타입으로 변경 필요

            // 기타: 위에 해당되지 않는 모든 타입
            InventoryFilterType.Etc => itemType != ItemType.Weapon && itemType != ItemType.Artifact,

            _ => false,
        };
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null) return;

        // 1. 좌클릭 (상세 정보만 업데이트)
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 1)
        {
            if (InventoryUIManager.Instance != null)
                InventoryUIManager.Instance.UpdateDetails(currentItem);
        }

        // 2. 좌 더블클릭 (요청 기능: 장착 시도)
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
        {
            AttemptEquip();
        }

        // 3. 우클릭 (요청 기능: 장착 시도)
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            AttemptEquip();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null && InventoryUIManager.Instance != null)
        {
            dropSuccessful = false;

            Sprite icon = Resources.Load<Sprite>(currentItem.iconPath);
            if (icon != null)
            {
                InventoryUIManager.Instance.StartDrag(icon);
                slotIcon.enabled = false;
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        Slot_UI targetSlot = this;

        Slot_UI sourceInventorySlot = eventData.pointerDrag.GetComponent<Slot_UI>();
        if (sourceInventorySlot != null)
        {
            // [인벤토리 -> 인벤토리] 아이템 교체
            if (sourceInventorySlot != targetSlot)
            {
                InventoryManager.Instance.SwapItems(sourceInventorySlot.slotIndex, targetSlot.slotIndex);
                sourceInventorySlot.dropSuccessful = true;
            }
            return;
        }

        EquipmentSlot_UI sourceEquipSlot = eventData.pointerDrag.GetComponent<EquipmentSlot_UI>();
        if (sourceEquipSlot != null && sourceEquipSlot.currentItem != null)
        {
            // 장비 해제 시도 (인벤토리로 이동)
            bool success = EquipmentManager.Instance.UnequipItem(sourceEquipSlot.currentItem, sourceEquipSlot.equipmentSlotIndex);

            if (success)
            {
                sourceEquipSlot.MarkDropSuccessful();
                dropSuccessful = true; // 인벤토리 슬롯에도 성공 플래그 설정
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.EndDrag(); // 유령 아이콘은 무조건 끔

        // 1. UI 밖으로 드롭했는지 확인 (pointerEnter == null)
        if (eventData.pointerEnter == null)
        {
            if (currentItem != null)
            {
                // 인게임에 드랍
                InventoryManager.Instance.DropItem(this.slotIndex);
                dropSuccessful = true;
            }
        }

        if (!dropSuccessful)
        {
            // UI 안의 유효하지 않은 곳에 놓았을 경우: 원본 슬롯 아이콘을 즉시 복구합니다.
            slotIcon.enabled = true;
        }

        dropSuccessful = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentItem != null && InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
        }
    }

    public void MarkDropSuccessful()
    {
        dropSuccessful = true;
    }

    private void AttemptEquip()
    {
        if (currentItem == null) return;

        string currentItemTypeString = currentItem.itemTypeEnum.ToString().ToLower();

        // 1. 장착 가능한 타입 목록에 현재 아이템 타입이 포함되는지 확인
        bool isEquippable = false;
        foreach (var type in EquippableTypes)
        {
            if (currentItemTypeString == type) // 수정된 변수 사용
            {
                isEquippable = true;
                break;
            }
        }

        if (isEquippable)
        {
            // 장착 시도
            bool success = EquipmentManager.Instance.EquipItem(currentItem, this.slotIndex);

            if (!success)
            {
                Debug.Log("장비 슬롯이 꽉 찼습니다. (기존 장비를 되돌릴 공간 부족)");
            }
            else
            {
                Debug.Log($"{currentItem.itemName} 장착 성공!");
            }
        }
        else
        {
            // 현재 아이템 타입이 장착 가능 목록에 없는 경우
            Debug.Log($"이 아이템({currentItem.itemName})은 장착할 수 없습니다. (타입: {currentItem.itemTypeEnum})");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;
        }

        if (currentItem == null) return;
        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(currentItem));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스가 벗어나면 툴팁 표시 딜레이 코루틴을 중단합니다.
        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = null;

        // ★ 추가: 툴팁 숨김 코루틴이 이미 실행 중이면 중단하고 새로 시작 (클릭 등으로 인한 중복 방지)
        if (hideTooltipCoroutine != null) StopCoroutine(hideTooltipCoroutine);

        // ★ 0.1초 딜레이 후 툴팁을 숨깁니다.
        hideTooltipCoroutine = StartCoroutine(HideTooltipAfterDelay(0.1f));
    }

    private IEnumerator ShowTooltipAfterDelay(RelicData item)
    {
        yield return new WaitForSeconds(TooltipDelay);

        // 0.5초 후에도 여전히 마우스가 위에 있다면 툴팁 표시
        if (InventoryUIManager.Instance != null)
        {
            // 툴팁 표시 (현재 슬롯 위치 기준)
            InventoryUIManager.Instance.ShowTooltip(item, transform.position);
        }
    }

    private IEnumerator HideTooltipAfterDelay(float delay)
    {
        // 1. 딜레이 동안 기다립니다.
        yield return new WaitForSeconds(delay);

        // 2. 딜레이가 끝났다면 툴팁을 숨깁니다.
        if (InventoryUIManager.Instance != null)
        {
            // InventoryUIManager.HideTooltip()은 페이드 아웃을 시작하고, 
            // 페이드 아웃이 완료되면 툴팁 GameObject를 비활성화합니다.
            InventoryUIManager.Instance.HideTooltip();
        }

        // 3. 이 코루틴이 완료되었으므로 레퍼런스를 해제합니다.
        hideTooltipCoroutine = null; // <--- 이 위치가 맞습니다.
    }
}