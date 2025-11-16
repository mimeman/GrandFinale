using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public RelicData item; // 이 슬롯에 담긴 아이템 데이터
    public int quantity;   // 이 슬롯에 담긴 아이템의 수량


    [System.NonSerialized]
    public int slotIndex;
    // 기본 생성자 (비어있는 슬롯)
    public InventorySlot()
    {
        item = null;
        quantity = 0;
    }

    // (호출되는 곳은 없지만, 안전을 위해 인덱스도 초기화)
    public InventorySlot(int index)
    {
        item = null;
        quantity = 0;
        slotIndex = index;
    }

    public void ClearSlot()
    {
        item = null;
        quantity = 0;
    }

    // (선택사항) 아이템을 추가하는 헬퍼 함수
    public void SetItem(RelicData newItem)
    {
        item = newItem;
        quantity = 1;
    }

    // (선택사항) 수량을 증가시키는 헬퍼 함수
    public void AddQuantity(int amount)
    {
        quantity += amount;
    }
}