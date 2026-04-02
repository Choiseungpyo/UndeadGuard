using UnityEngine;

// 모든 유닛이 공통으로 구현해야 하는 인터페이스
public interface IUnit
{
    // 유닛이 속한 진영
    TeamType Team { get; }

    // 현재 그리드 위치
    Vector2Int GridPosition { get; }

    // 이번 턴에 이동을 완료했는지 여부
    bool HasMoved { get; }

    // 이번 턴에 행동을 완료했는지 여부
    bool HasActed { get; }

    // 유닛의 수치 데이터
    UnitStats Stats { get; }

    // 턴 시작 시 상태를 초기화한다
    void ResetTurnState();
}
