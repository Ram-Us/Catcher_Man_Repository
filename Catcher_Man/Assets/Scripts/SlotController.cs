using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Security.Cryptography.X509Certificates;

public class SlotController : MonoBehaviour
{
    [SerializeField] private List<Image> slotItem = new ();
    SpriteRenderer sr;

    

    public bool TryAdd(){
        foreach (Image sItem in slotItem){
            if (sItem.sprite == null){
                Debug.Log("空きが見つかったのでtrueだぞ");
                return true;
                }
        }
        Debug.Log("全スロットを調べたが空きがなかった");
        return false;
    }
    public void RefreshUI(GameObject getItem)
    {
        int i = 0;
        foreach(Image sItem in slotItem)
        {
            Image SItem = sItem.GetComponent<Image>();
            if(SItem.sprite == null)
            {
                sr = getItem.GetComponent<SpriteRenderer>();
                SItem.sprite = sr.sprite;
                //Destroy(getItem);
                getItem.SetActive(false);
                Debug.Log(i+"番目に入れたぞ");
                break;
            
        }
        i++;
    }
    
    }
    public void DeleteUI(int r)
    {
        slotItem[r].sprite = null;
    }
}
