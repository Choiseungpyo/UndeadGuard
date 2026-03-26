/// <summary>
/// 그리드 칸 위에 배치된 고정 오브젝트의 종류를 정의하는 열거형.
/// 핵, 스폰 지점처럼 타일 위에 놓이는 구조물을 구분할 때 사용한다.
/// </summary>
public enum CellObjectType
{
    None,
    DefensePoint,
    SpawnPoint
}