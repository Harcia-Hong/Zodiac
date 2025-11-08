using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("스테이지 설정")]
    [SerializeField] private StageData currentStageData;
    [SerializeField] private MonsterSpanwer monsterSpanwer;

    [Header("현재 상태 (디버깅용)")]
    [SerializeField] private int currentKillCount = 0;
    [SerializeField] private int currentWaveIndex = -1;
    [SerializeField] private StageState currentStageState = StageState.Ready;


    // 스테이지 상태 열거형
    public enum StageState
    {
        Ready, // 준비 상태
        Playing, // 진행 중
        WaveClear, // 웨이브 클리어 처리
        ShowingRewards, // 보상 선택 화면
        Cleared, // 클리어
        Failed // 실패
    }

    private void Start()
    {
        StartStage();
    }

    public void StartStage()
    {
        if(currentStageData == null)
        {
            Debug.LogError("StageData가 설정되지 않았습니다...");
            return;
        }

        if(monsterSpanwer == null)
        {
            Debug.LogError("MonsterSpawner가 설정되지 안ㅇ핬어요");
            return;
        }

        // 초기화
        currentKillCount = 0;
        currentWaveIndex = -1;
        currentStageState = StageState.Playing;

        // 몬스터 스포너 시작
        monsterSpanwer.Initialize(currentStageData, OnWaveCompleted);

        Debug.Log($"[StageManager] 스테이지 시작: {currentStageData.stageName}");
        Debug.Log($"[StageManager] 목표: {currentStageData.totalKillTarget}마리 처치");
        Debug.Log($"[StageManager] 총 웨이브 수: {currentStageData.waves.Length}");

        // 첫 번째 웨이브 시작
        StartNextWave();
    }

    private void StartNextWave()
    {
        currentWaveIndex++;

        if (currentWaveIndex >= currentStageData.waves.Length)
        {
            Debug.LogError("[StageManager] 더 이상 진행할 웨이브가 없습니다!");
            return;
        }

        Debug.Log($"[StageManager] 웨이브 {currentWaveIndex + 1} 시작");
        monsterSpanwer.StartWave(currentWaveIndex);
    }

    private void OnWaveCompleted(int completedWaveIndex)
    {
        if (currentStageState != StageState.Playing)
            return;

        currentStageState = StageState.WaveClear;

        // 이번 웨이브에서 처치한 몬스터 수 추가
        WaveData completedWave = currentStageData.waves[completedWaveIndex];
        int killedInThisWave = completedWave.enemyCount;
        currentKillCount += killedInThisWave;

        Debug.Log($"[StageManager] 웨이브 {completedWaveIndex + 1} 완료!");
        Debug.Log($"[StageManager] 이번 웨이브 처치: {killedInThisWave}마리");
        Debug.Log($"[StageManager] 총 처치 수: {currentKillCount}/{currentStageData.totalKillTarget}");

        // 클리어 조건 확인
        if (currentKillCount >= currentStageData.totalKillTarget)
        {
            // 목표 달성 시 스테이지 클리어
            StartCoroutine(HandleStageCleared());
        }
        else
        {
            // 아직 목표 미달성 시 다음 웨이브 확인
            if (currentWaveIndex + 1 < currentStageData.waves.Length)
            {
                // 다음 웨이브가 있으면 계속 진행
                currentStageState = StageState.Playing;
                StartCoroutine(StartNextWaveWithDelay());
            }
            else
            {
                // 더 이상 웨이브가 없는데 목표 미달성 (설계 오류)
                Debug.LogError("[StageManager] 모든 웨이브 완료했지만 목표 미달성! 스테이지 데이터를 확인하세요.");
                StartCoroutine(HandleStageCleared()); // 강제 클리어
            }
        }
    }

    private IEnumerator StartNextWaveWithDelay() // 딜레이 후 다음 웨이브 시작
    {
        yield return new WaitForSeconds(1f); // 웨이브 간 잠시 대기
        StartNextWave();
    }

    private IEnumerator HandleStageCleared() // 스테이지 클리어 처리 - 보상 시스템으로 연동
    {
        currentStageState = StageState.Cleared;

        Debug.Log($"[StageManager] 스테이지 클리어! {currentStageData.stageName}");

        // 스폰 중지
        monsterSpanwer.StopSpawning();

        // 잠시 대기 (연출용)
        yield return new WaitForSeconds(0.5f);

        // 남은 몬스터들 정리
        monsterSpanwer.ClearAllEnemies();

        Debug.Log("[StageManager] 남은 몬스터 정리 완료");
    }

    IEnumerator NextStage() // 다음 스테이지 시작 ( 현재는 1-1만 있기에 재시작으로 구현 )
    {
        yield return new WaitForSeconds(1f);

        Debug.Log("[StageManager] 다음 스테이지 ㄱㄱㄱㄱ");

        RestartStage(); // 현재는 재시작으로 구현하기 때문
    }

    public void RestartStage()
    {
        Debug.Log("[StageManager] 스테이지 다시 시작");
        StartStage();
    }

    public void StartNewStage(StageData newStageData)
    {
        currentStageData = newStageData;
        StartStage();
    }

    // 공개 인터페이스 (UI 연동용)
    public StageState GetCurrentStageState() => currentStageState;
    public int GetCurrentKillCount() => currentKillCount;
    public int GetTargetKillCount() => currentStageData?.totalKillTarget ?? 0;
    public int GetCurrentWaveIndex() => currentWaveIndex + 1; // UI용 1-based
    public int GetTotalWaveCount() => currentStageData?.waves.Length ?? 0;

    /// <summary>
    /// 현재 진행 상황 로그 출력 (디버깅용)
    /// </summary>
    [ContextMenu("현재 상황 확인")]
    public void LogCurrentStatus()
    {
        Debug.Log("=== 스테이지 현황 ===");
        Debug.Log($"스테이지: {currentStageData?.stageName ?? "없음"}");
        Debug.Log($"상태: {currentStageState}");
        Debug.Log($"웨이브: {currentWaveIndex + 1}/{GetTotalWaveCount()}");
        Debug.Log($"처치 수: {currentKillCount}/{GetTargetKillCount()}");
    }
}


