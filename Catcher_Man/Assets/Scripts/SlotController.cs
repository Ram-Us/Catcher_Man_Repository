using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
public class SlotController : MonoBehaviour
{
    [SerializeField] private List<Image> slotItem = new ();
    private int[] stockCount={0,0,0,0};

    [SerializeField] private Sprite baseSprite;
    private SpriteRenderer sr;

    [SerializeField] private Image frameImage;
     private TextMeshProUGUI stock;

    
    public bool TryAdd(){
        foreach (Image sItem in slotItem){
            if (sItem.sprite == baseSprite){
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
            if(SItem.sprite == baseSprite)
            {
                sr = getItem.GetComponent<SpriteRenderer>();
                SItem.sprite = sr.sprite;
                Destroy(getItem);
                //getItem.SetActive(false);
                Debug.Log(i+"番目に入れたぞ");
                break;
            
        }
        i++;
    }
    
    }
    public void DeleteUI(int r)
    {
        slotItem[r].sprite = baseSprite;
    }
    public void MoveFrame(int n)
    {
        frameImage.rectTransform.position = slotItem[n].rectTransform.position + new Vector3(0f,0f,-0.2f);
    }
    public void SetStock(int n)
    {
        stockCount[n]++;
        stock = slotItem[n].GetComponentInChildren<TextMeshProUGUI>();
        stock.SetText(stockCount[n].ToString());
    }
}
