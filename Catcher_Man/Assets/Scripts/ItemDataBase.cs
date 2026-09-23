using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// アイテムの種類を表す列挙型
public enum ItemType
{
    Object,   // ボール
    Enemy,    // 回復

    Bullet,   //弾
    Ladder,    //はしご
    IronBall  //鉄球
}

[System.Serializable]
public class Item
{
    [SerializeField] private int id;             // アイテムのID
    public int Id => id;

    [SerializeField] private Sprite icon;        // アイテムのアイコン画像
    public Sprite Icon => icon;
    [SerializeField] private ItemType itemType;  // アイテムの種類
    public ItemType ItemType => itemType;
    [SerializeField] private string itemName;    // アイテムの名前
    public string ItemName => itemName;
    [SerializeField] private int cost;    // 効果値
    public int Cost => cost;
    [SerializeField] private Vector3 position = new Vector3(0f,0f,0f);  //アイテムのサイズ
    public Vector3 Position => position;
    [SerializeField] private Vector3 size = new Vector3(1f,1f,1f);  //アイテムのサイズ
    public Vector3 Size => size;
    [SerializeField] private float speed; //投げ時のスピード
    public float Speed => speed;
    [SerializeField] private int attack; //振り時のダメージ量
    public int Attack => attack;

    [SerializeField] private Vector3 colliderSize; //オブジェクトのコライダーのサイズ
    public Vector3 ColliderSize => colliderSize;
    [SerializeField] private Vector3 colliderCenterSize; //オブジェクトのコライダーのサイズ
    public Vector3 ColliderCenterSize => colliderCenterSize;

    [SerializeField] private GameObject instance;  //取得したアイテムのインスタンス

    [SerializeField] private bool rotated = true;  //回転するか否か
    public bool Rotated => rotated;

    [SerializeField] private Vector3 equippedItemScale;
    public Vector3 EquippedItemScale => equippedItemScale;

    [SerializeField] private int[] rect;
    public int[] Rect => rect;

    


    public GameObject Instance => instance;

     


}

[CreateAssetMenu(fileName = "ItemDataBase", menuName = "CreateItemDataBase")]
public class ItemDataBase : ScriptableObject
{
    [SerializeField] private Item[] items;

    
    public Item GetItemById(int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item;
            }
        }
        return null;
    }

    public GameObject GetIntanceById(int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item.Instance;
            }
        }
        return null;
    }
    public Sprite GetSpriteById(int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item.Icon;
            }
        }
        return null;
    }
    public int GetCostById(int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item.Cost;
            }
        }
        
        return 0;
    }
    public ItemType GetItemTypeById(int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item.ItemType;
            }
        }
        
        return 0;
    }
    public string GetItemNameById(int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item.ItemName;
            }
        }
        
        return null;
    }
    public float GetSpeedById(int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item.Speed;
            }
        }
        
        return 0f;
    }
    public int GetAttackById(int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item.Attack;
            }
        }
        
        return 0;
    }
    public Vector3 GetEquippedItemScaleById(int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item.EquippedItemScale;
            }
        }
        return new Vector3(0f,0f,0f);
    }
    public int GetWidthById(int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item.Rect[0];
            }
        }
        
        return 0;
    }
    public int GetHeightById(int id)
    {
        foreach (var item in items)
        {
            if (item.Id == id)
            {
                return item.Rect[1];
            }
        }
        
        return 0;
    }

    
    public int GetId(GameObject gameObject)
    {
        ItemGimmick itemData = gameObject.GetComponent<ItemGimmick>();
        return itemData.Id;
    }

    
}

