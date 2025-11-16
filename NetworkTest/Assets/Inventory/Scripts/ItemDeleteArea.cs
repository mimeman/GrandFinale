using UnityEngine;
using UnityEngine.EventSystems; // IDropHandler를 사용하기 위해 필요


[RequireComponent(typeof(RectTransform))]
public class ItemDeleteArea : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Slot_UI sourceSlot = eventData.pointerDrag.GetComponent<Slot_UI>();

        if (sourceSlot != null &&
            sourceSlot.currentSlot != null &&
            sourceSlot.currentSlot.item != null &&
            sourceSlot.currentSlot.slotIndex != -1) // 인덱스가 유효한지 확인
        {
            Debug.Log($"[ItemDeleteArea] {sourceSlot.currentSlot.slotIndex}번 슬롯의 아이템 '{sourceSlot.currentSlot.item.itemName}' (수량: {sourceSlot.currentSlot.quantity})을(를) 삭제합니다.");

            bool success = InventoryManager.Instance.RemoveItem(sourceSlot.currentSlot.slotIndex);

            if (success)
            {
                sourceSlot.dropSuccessful = true;
            }
        }
    }
}