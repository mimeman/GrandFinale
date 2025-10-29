using UnityEngine;
using System.Collections.Generic;

// [System.Serializable]은 이 클래스를 인스pector에 보여달라는 뜻.
[System.Serializable]
public class LootEntry
{
    public ItemData item;
    [Range(0f, 100f)]
    public float dropChance; // 0% ~ 100%
}

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Item/Loot Table")]
public class LootTable : ScriptableObject
{
    [Header("드랍될 아이템 목록")]
    public List<LootEntry> items;
}