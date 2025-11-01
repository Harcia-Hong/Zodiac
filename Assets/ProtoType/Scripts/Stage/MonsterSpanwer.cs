using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpanwer : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("현재 상태 (디버깅용)")]
    [SerializeField] private int currentWaveIndex = -1;
    [SerializeField] private bool isSpawning = false;
    [SerializeField] private int aliveEnemyCount = 0;

    // 현재 웨이브에서 살아있는 몬스터들 관리
    private List<Enemy> waveEnemies = new List<Enemy>();

    // 현재 사용중인 스테이지 데이터
    private StageData currentStageData;

    // 웨이브 완료 콜백 
    private System.Action<int> onWaveCompleted;

    public void Initialize(StageData stageData, System.Action<int> waveCompletedCallback)
    {
        currentStageData = stageData;
        onWaveCompleted = waveCompletedCallback;
        currentWaveIndex = -1;
        isSpawning = false;

        // 기존 몬스터들 정리
        ClearAllEnemies();

        Debug.Log($"[MonsterSpawner] 초기화 완료: {stageData.stageName}");
    }

    public void StartWave(int waveIndex)
    {
        if (currentStageData == null)
        {
            Debug.LogError("[MonsterSpawner] StageData가 설정되지 않았습니다!");
            return;
        }

        if (waveIndex >= currentStageData.waves.Length)
        {
            Debug.LogError($"[MonsterSpawner] 유효하지 않은 웨이브 인덱스: {waveIndex}");
            return;
        }

        if (isSpawning)
        {
            Debug.LogWarning("[MonsterSpawner] 이미 스폰 중입니다!");
            return;
        }

        currentWaveIndex = waveIndex;
        StartCoroutine(SpawnWaveCoroutine(currentStageData.waves[waveIndex]));
    }

    private IEnumerator SpawnWaveCoroutine(WaveData waveData)
    {
        isSpawning = true;

        Debug.Log($"[MonsterSpawner] 웨이브 {currentWaveIndex + 1} 시작 (몬스터 {waveData.enemyCount}마리)");

        // 스폰 딜레이 대기
        if (waveData.spawnDelay > 0)
        {
            yield return new WaitForSeconds(waveData.spawnDelay);
        }

        // 기존 웨이브 몬스터들 정리
        waveEnemies.Clear();

        // 지정된 수만큼 몬스터 스폰
        for (int i = 0; i < waveData.enemyCount; i++)
        {
            SpawnSingleEnemy(waveData);

            // 스폰 간격 (너무 빠르게 스폰되지 않도록)
            yield return new WaitForSeconds(0.1f);
        }

        isSpawning = false;
        aliveEnemyCount = waveEnemies.Count;

        Debug.Log($"[MonsterSpawner] 웨이브 {currentWaveIndex + 1} 스폰 완료 ({aliveEnemyCount}마리)");
    }
    private void SpawnSingleEnemy(WaveData waveData)
    {
        // 랜덤 스폰 포인트 선택
        Transform spawnPoint = GetRandomSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogError("[MonsterSpawner] 유효한 스폰 포인트가 없습니다!");
            return;
        }

        // 랜덤 몬스터 타입 선택
        GameObject enemyPrefab = GetRandomEnemyPrefab(waveData);
        if (enemyPrefab == null)
        {
            Debug.LogError("[MonsterSpawner] 유효한 몬스터 프리팹이 없습니다!");
            return;
        }

        // 몬스터 생성
        GameObject newEnemyObj = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        Enemy enemyComponent = newEnemyObj.GetComponent<Enemy>();

        if (enemyComponent != null)
        {
            // 죽음 콜백 등록 (콜백 방식으로 안정적 처리)
            enemyComponent.SetDeathCallback(() => OnEnemyDied(enemyComponent));

            // 현재 웨이브 몬스터 목록에 추가
            waveEnemies.Add(enemyComponent);

            Debug.Log($"[MonsterSpawner] 몬스터 스폰: {enemyPrefab.name} at {spawnPoint.name}");
        }
        else
        {
            Debug.LogError($"[MonsterSpawner] Enemy 컴포넌트가 없습니다: {enemyPrefab.name}");
            Destroy(newEnemyObj);
        }
    }
    private void OnEnemyDied(Enemy deadEnemy)
    {
        // 현재 웨이브 목록에서 제거
        if (waveEnemies.Remove(deadEnemy))
        {
            aliveEnemyCount = waveEnemies.Count;
            Debug.Log($"[MonsterSpawner] 몬스터 처치! 남은 수: {aliveEnemyCount}");

            // 웨이브의 모든 몬스터가 처치되었는지 확인
            if (aliveEnemyCount <= 0 && !isSpawning)
            {
                Debug.Log($"[MonsterSpawner] 웨이브 {currentWaveIndex + 1} 완료!");

                // StageManager에게 웨이브 완료 알림
                onWaveCompleted?.Invoke(currentWaveIndex);
            }
        }
    }
    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        int randomIndex = Random.Range(0, spawnPoints.Length);
        return spawnPoints[randomIndex];
    }
    private GameObject GetRandomEnemyPrefab(WaveData waveData)
    {
        if (currentStageData.availableEnemyPrefabs == null ||
            currentStageData.availableEnemyPrefabs.Length == 0)
            return null;

        // 가중치가 설정되어 있으면 가중치 기반 선택
        if (waveData.enemyTypeIndices != null && waveData.enemyTypeIndices.Length > 0)
        {
            return SelectEnemyByWeight(waveData);
        }

        // 기본: 랜덤 선택
        int randomIndex = Random.Range(0, currentStageData.availableEnemyPrefabs.Length);
        return currentStageData.availableEnemyPrefabs[randomIndex];
    }
    private GameObject SelectEnemyByWeight(WaveData waveData)
    {
        float totalWeight = 0f;

        // 총 가중치 계산
        for (int i = 0; i < waveData.spawnWeights.Length && i < waveData.enemyTypeIndices.Length; i++)
        {
            totalWeight += waveData.spawnWeights[i];
        }

        // 랜덤 값 선택
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        // 가중치에 따른 선택
        for (int i = 0; i < waveData.spawnWeights.Length && i < waveData.enemyTypeIndices.Length; i++)
        {
            currentWeight += waveData.spawnWeights[i];
            if (randomValue <= currentWeight)
            {
                int enemyIndex = waveData.enemyTypeIndices[i];
                if (enemyIndex >= 0 && enemyIndex < currentStageData.availableEnemyPrefabs.Length)
                {
                    return currentStageData.availableEnemyPrefabs[enemyIndex];
                }
            }
        }

        // 폴백: 첫 번째 몬스터 반환
        return currentStageData.availableEnemyPrefabs[0];
    }
    public void ClearAllEnemies()
    {
        foreach (Enemy enemy in waveEnemies)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        waveEnemies.Clear();
        aliveEnemyCount = 0;

        Debug.Log("[MonsterSpawner] 모든 몬스터 제거 완료");
    }
    public void StopSpawning()
    {
        StopAllCoroutines();
        isSpawning = false;
        Debug.Log("[MonsterSpawner] 스폰 중단");
    }
        
    public int GetAliveEnemyCount() => aliveEnemyCount;
    public int GetCurrentWaveIndex() => currentWaveIndex;

}
