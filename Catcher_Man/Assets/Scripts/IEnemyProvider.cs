using UnityEngine;

public interface IEnemyProvider
{

    public int CurrentStateCode {get;}
    float DirectionToPlayer { get; }
    Vector3 CrossDirectionToPlayer{ get; }
}