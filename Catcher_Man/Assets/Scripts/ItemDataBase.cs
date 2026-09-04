using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// アイテムの種類を表す列挙型
public enum ItemType
{
    Ball,   // ボール
    Heal    // 回復
}

[System.Serializable]
public class Item
{
    public int id;             // アイテムのID
    public Sprite icon;        // アイテムのアイコン画像
    public ItemType itemType;  // アイテムの種類
    public string itemName;    // アイテムの名前
    public int cost;    // 効果値
    public float speed; //投げ時のスピード
    public int attack; //振り時のダメージ量

     


}

[CreateAssetMenu(fileName = "ItemDataBase", menuName = "CreateItemDataBase")]
public class ItemDataBase : ScriptableObject
{
    [SerializeField] private Item[] items;

    
    public Item GetItemById(int id)
    {
        foreach (var item in items)
        {
            if (item.id == id)
            {
                return item;
            }
        }
        Debug.LogWarning($"Item with ID {id} not found.");
        return null;
    }

}

