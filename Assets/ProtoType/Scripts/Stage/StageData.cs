using UnityEditor.Overlays;
using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Scriptable Objects/StageData")]
public class StageData : ScriptableObject
{
    [Header("스테이지 기본 정보")]
    [Tooltip("스테이지 이름 (디버깅용)")]
    public string stageName = "Chapter 1 - Stage 1";

    [Tooltip("스테이지 설명")]
    [TextArea(2, 4)]
    public string stageDescription = "기본 스테이지입니다.";

    [Header("클리어 조건")]
    [Tooltip("클리어를 위해 처치해야 할 총 몬스터 수")]
    public int totalKillTarget = 5;

    [Header("웨이브 설정")]
    [Tooltip("각 웨이브별 상세 설정")]
    public WaveData[] waves;

    [Header("사용 가능한 몬스터")]
    [Tooltip("이 스테이지에서 사용할 몬스터 프리팹들")]
    public GameObject[] availableEnemyPrefabs;
}

[System.Serializable]
public class WaveData
{
    [Header("웨이브 기본 정보")]
    [Tooltip("이 웨이브에서 스폰할 몬스터 수")]
    public int enemyCount = 2;

    [Tooltip("웨이브 시작 전 대기 시간")]
    public float spawnDelay = 1f;

    [Header("몬스터 구성")]
    [Tooltip("사용할 몬스터 타입 인덱스들 (StageData.availableEnemyPrefabs 배열 기준)")]
    public int[] enemyTypeIndices;

    [Tooltip("몬스터별 스폰 확률 가중치")]
    public float[] spawnWeights;
}
