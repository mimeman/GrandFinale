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
    // 시트의 'grantedAbilityID' (예: "ABIL_001") 문자열을 기반으로
    // 파서가 'ABIL_001.asset' 파일을 찾아 이 변수에 연결해줍니다.
    public AbilityData grantedAbility;
}