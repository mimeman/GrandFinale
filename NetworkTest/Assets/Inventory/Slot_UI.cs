using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class Slot_UI : MonoBehaviour, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public int slotIndex;
    public Image slotIcon;
    public bool dropSuccessful = false;

    // 1. ItemData -> RelicData로 변경
    public RelicData currentItem { get; private set; } // currentItem을 public으로 변경

    // (Start, OnDestroy는 동일)
    void Start()
    {
        InventoryManager.OnInventoryChanged += UpdateSlotVisuals;
        UpdateSlotVisuals();
    }
    void OnDestroy()
    {
        InventoryManager.OnInventoryChanged -= UpdateSlotVisuals;
    }

    // 2. (핵심 수정!) iconPath를 Resources.Load로 불러오는 로직
    void UpdateSlotVisuals()
    {
        // InventoryManager에서 RelicData를 가져옴
        currentItem = InventoryManager.Instance.inventorySlots[slotIndex];

        if (currentItem != null && !string.IsNullOrEmpty(currentItem.iconPath))
        {
            // 3. "Icons/Relics/AuxHeart" 같은 경로에서 스프라이트를 로드
            Sprite icon = Resources.Load<Sprite>(currentItem.iconPath);

            if (icon != null)
            {
                slotIcon.sprite = icon;
                slotIcon.enabled = true;
            }
            else
            {
                // 경로에 파일이 없을 경우 (에러 처리)
                Debug.LogWarning($"아이콘을 로드할 수 없습니다: {currentItem.iconPath}");
                slotIcon.enabled = false;
            }
        }
        else
        {
            // 아이템이 없으면(null) 아이콘을 숨김
            slotIcon.sprite = null;
            slotIcon.enabled = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null) return; // 아이템이 없으면 아무것도 안 함

        // 1. 좌클릭 (한 번 클릭) -> 상세 정보 표시
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 1)
        {
            InventoryUIManager.Instance.UpdateDetails(currentItem);
        }

        // 2. 좌 더블클릭 (두 번 클릭) -> 장착 시도
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
        {
            Debug.Log("더블 클릭으로 장착 시도...");
            AttemptEquip();
        }

        // 3. (미래 기능) 우클릭
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // TODO: 여기에 우클릭 메뉴(버리기, 착용하기) 띄우는 로직
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null)
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
        // 2. 드롭 대상(Target)은 나 자신 (Slot_UI)
        Slot_UI targetSlot = this;

        // 3. 드래그 시작(Source)이 인벤토리 슬롯(Slot_UI)이었나?
        Slot_UI sourceInventorySlot = eventData.pointerDrag.GetComponent<Slot_UI>();
        if (sourceInventorySlot != null)
        {
            // [인벤토리 -> 인벤토리] 아이템 교체
            if (sourceInventorySlot != targetSlot)
            {
                InventoryManager.Instance.SwapItems(sourceInventorySlot.slotIndex, targetSlot.slotIndex);
                sourceInventorySlot.dropSuccessful = true;
            }
            return; // 작업 끝
        }

        // 4. 드래그 시작(Source)이 장비 슬롯(EquipmentSlot_UI)이었나?
        EquipmentSlot_UI sourceEquipSlot = eventData.pointerDrag.GetComponent<EquipmentSlot_UI>();
        if (sourceEquipSlot != null && sourceEquipSlot.currentItem != null)
        {
            bool success = EquipmentManager.Instance.UnequipItem(sourceEquipSlot.currentItem, sourceEquipSlot.equipmentSlotIndex);

            if (success)
            {
                // 성공했음을 Source 슬롯에 알려서 OnEndDrag가 복구하지 않도록 막습니다.
                sourceEquipSlot.MarkDropSuccessful();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        InventoryUIManager.Instance.EndDrag(); // 유령 아이콘은 무조건 끔

        // 1. 드롭이 성공했거나 (dropSuccessful == true)
        // 2. UI 밖으로 버렸다면 (eventData.pointerEnter == null)
        if (dropSuccessful || eventData.pointerEnter == null)
        {
            // InventoryManager.DropItem이 OnInventoryChanged를 호출하여 갱신할 때까지 대기.
            // 아이템 버리기는 DropItem에서 처리되므로 별도 로직 불필요.
        }
        else
        {
            // 3. UI 안의 유효하지 않은 곳에 놓았을 경우
            // 원본 슬롯 아이콘을 즉시 복구합니다.
            slotIcon.enabled = true;
        }

        dropSuccessful = false; // 플래그 리셋
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentItem != null)
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
        // 1. 이 아이템이 장착 가능한 타입인지 확인
        // (주의: "Relic" 문자열은 EquipmentSlot_UI의 requiredType과 일치해야 함)
        string requiredType = "Relic";

        if (currentItem.itemType == requiredType)
        {
            // 2. EquipmentManager에게 장착 요청
            // (EquipItem 함수가 알아서 빈 장비 슬롯을 찾고, 가방에서 이 아이템을 제거함)
            bool success = EquipmentManager.Instance.EquipItem(currentItem, this.slotIndex);

            if (success)
            {
                // 장착 성공
            }
            else
            {
                Debug.Log("장비 슬롯이 꽉 찼습니다.");
            }
        }
        else
        {
            Debug.Log($"이 아이템({currentItem.itemName})은 장착할 수 없습니다.");
        }
    }
}