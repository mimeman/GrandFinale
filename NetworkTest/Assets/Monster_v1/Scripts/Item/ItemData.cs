using UnityEngine;

// "CreateAssetMenu"는 나중에 자식 클래스에서만 쓸 겁니다.
public abstract class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;
    [TextArea]
    public string description;
    public Sprite icon;

    // 아이템 타입을 Enum으로 관리 (범용성)
    public enum ItemType
    {
        StatBoost,  // 스탯 상승 패시브
        Skill,      // 사용 스킬 (불, 얼음 등)
        Consumable, // 소모품 (예: 체력 물약)
        KeyItem     // 퀘스트용
    }

    public abstract ItemType GetItemType();

    // (선택) 인벤토리 구현 시 필요
    // public int maxStack = 1; 
}