using UnityEngine;

public interface IEnemyProvider
{
    float DirectionToPlayer { get; }
    Vector3 CrossDirectionToPlayer{ get; }
}