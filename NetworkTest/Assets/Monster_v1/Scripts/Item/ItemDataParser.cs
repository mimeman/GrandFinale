// Assets/Scripts/Editor/ItemDataParser.cs
using UnityEngine;
using UnityEditor; // Editor 스크립트
using System.IO;   // 파일 읽기/쓰기
using System.Collections.Generic; // List

public class ItemDataParser
{
    // 데이터 에셋이 저장될 기본 경로
    private const string ABILITY_DATA_PATH = "Assets/Monster_v1/Data/Abilities";
    private const string RELIC_DATA_PATH = "Assets/Monster_v1/Data/Relics";

    // 1. AbilityData 임포트 메뉴
    [MenuItem("MyTools/Import Data/1. Import AbilityData (CSV)")]
    public static void ImportAbilityData()
    {
        string path = EditorUtility.OpenFilePanel("Import Ability CSV", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        // 대상 폴더가 없으면 생성
        Directory.CreateDirectory(ABILITY_DATA_PATH);

        string[] allLines = File.ReadAllLines(path);
        if (allLines.Length <= 1) return; // 헤더만 있으면 종료

        Debug.Log($"[AbilityParser] {allLines.Length - 1}개 데이터 임포트 시작...");

        // 첫 줄은 헤더이므로 1부터 시작
        for (int i = 1; i < allLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(allLines[i])) continue;

            string[] row = SplitCSVLine(allLines[i]);

            if (row.Length < 9)
            {
                Debug.LogWarning($"[AbilityParser] 줄 무시됨 (열 부족, 9개 미만): {allLines[i]}");
                continue;
            }

            string abilityID = row[0].Trim();
            if (string.IsNullOrEmpty(abilityID)) continue;

            // 1. 에셋 경로 지정 (예: ABIL_001.asset)
            string assetPath = $"{ABILITY_DATA_PATH}/{abilityID}.asset";

            // 2. 기존 에셋 로드 또는 신규 생성
            AbilityData ability = AssetDatabase.LoadAssetAtPath<AbilityData>(assetPath);
            if (ability == null)
            {
                // 파일이 없으면 새로 만듭니다.
                ability = ScriptableObject.CreateInstance<AbilityData>();
                AssetDatabase.CreateAsset(ability, assetPath);
            }

            // 3. CSV 데이터로 ScriptableObject 필드 채우기
            ability.abilityID = abilityID;
            ability.abilityName = row[1].Trim();
            ability.activationType = row[2].Trim();
            ability.abilityLogicID = row[3].Trim();
            ability.param_Key = row[4].Trim();
            ability.param_ValueA = row[5].Trim();
            ability.param_ValueB = row[6].Trim();
            ability.param_ValueC = row[7].Trim();
            ability.resourcePath = row[8].Trim();

            // 4. 변경 사항 저장
            EditorUtility.SetDirty(ability);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[AbilityParser] 임포트 완료!");
    }

    // 2. RelicData 임포트 메뉴
    [MenuItem("MyTools/Import Data/2. Import RelicData (CSV)")]
    public static void ImportRelicData()
    {
        string path = EditorUtility.OpenFilePanel("Import Relic CSV", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        Directory.CreateDirectory(RELIC_DATA_PATH);

        string[] allLines = File.ReadAllLines(path);
        if (allLines.Length <= 1) return;

        Debug.Log($"[RelicParser] {allLines.Length - 1}개 데이터 임포트 시작...");

        for (int i = 1; i < allLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(allLines[i])) continue;

            string[] row = SplitCSVLine(allLines[i]);
            if (row.Length < 7) continue;

            string itemID = row[0].Trim();
            if (string.IsNullOrEmpty(itemID)) continue;

            string assetPath = $"{RELIC_DATA_PATH}/{itemID}.asset";

            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(assetPath);
            if (relic == null)
            {
                relic = ScriptableObject.CreateInstance<RelicData>();
                AssetDatabase.CreateAsset(relic, assetPath);
            }

            // RelicData 필드 채우기
            relic.itemID = itemID;
            relic.itemName = row[1].Trim();
            relic.itemType = row[2].Trim();
            relic.grade = row[3].Trim();
            relic.description = row[4].Trim();
            relic.iconPath = row[5].Trim();

            // maxStack 파싱 (숫자 변환)
            int.TryParse(row[7].Trim(), out relic.maxStack);

            // ★★★ 핵심 연동 로직 ★★★
            string abilityIDString = row[6].Trim(); // 시트의 "ABIL_001"
            if (!string.IsNullOrEmpty(abilityIDString))
            {
                // "ABIL_001" ID를 기반으로 실제 AbilityData.asset 파일 경로 탐색
                string abilityAssetPath = $"{ABILITY_DATA_PATH}/{abilityIDString}.asset";

                // 해당 경로의 에셋 파일을 로드
                AbilityData abilityAsset = AssetDatabase.LoadAssetAtPath<AbilityData>(abilityAssetPath);

                if (abilityAsset != null)
                {
                    // RelicData의 'grantedAbility' 변수에 찾은 에셋을 연결
                    relic.grantedAbility = abilityAsset;
                }
                else
                {
                    // 에셋을 못찾으면 경고
                    Debug.LogWarning($"[RelicParser] Ability 에셋을 찾을 수 없습니다: '{abilityIDString}' (Relic: '{itemID}')");
                    relic.grantedAbility = null;
                }
            }
            else
            {
                relic.grantedAbility = null; // 연결된 능력이 없음
            }

            EditorUtility.SetDirty(relic);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[RelicParser] 임포트 완료!");
    }


    // 제공해주신 CSV 파싱 유틸리티 (따옴표(")로 묶인 콤마 무시)
    private static string[] SplitCSVLine(string line)
    {
        List<string> result = new();
        bool inQuotes = false;
        string current = "";

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Replace("\"", "").Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }
        result.Add(current.Replace("\"", "").Trim());
        return result.ToArray();
    }
}