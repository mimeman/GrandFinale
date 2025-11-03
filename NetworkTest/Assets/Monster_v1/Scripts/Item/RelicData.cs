// Assets/Scripts/Data/RelicData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "REL_", menuName = "Data/Relic")]
public class RelicData : ScriptableObject
{
    [Header("시트 원본 정보")]
    public string itemID;
    public string itemName;
    public string itemType;
    public string grade;
    public int maxStack;

    [TextArea(3, 5)]
    public string description;

    [Header("리소스 경로")]
    public string iconPath; // Icons/Relics/AuxHeart

    [Header("★핵심 연동★")]
    public AbilityData grantedAbility;

    [Header("월드 드랍 설정")]
    [Tooltip("땅에 떨어질 때 생성될 3D 모델 프리팹 (ItemPickup.cs 포함)")]
    public GameObject dropPrefab;
}