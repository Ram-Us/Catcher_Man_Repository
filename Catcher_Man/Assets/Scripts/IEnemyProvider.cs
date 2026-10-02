using UnityEngine;

public interface IEnemyProvider
{
    int CurrentStateCode { get; }
    float DirectionToPlayer { get; }
    Vector3 CrossDirectionToPlayer{ get; }
}