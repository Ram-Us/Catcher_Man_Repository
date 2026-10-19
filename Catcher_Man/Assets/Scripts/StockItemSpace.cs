using System.Diagnostics.Contracts;
using UnityEngine;

public class StockItemSpace : MonoBehaviour
{
    
    [SerializeField] private int stockID,limitStockNumber;
    public int StockID => stockID;
    public int LimitStockNumber => limitStockNumber;
    // Update is called once per frame
    void FixedUpdate()
    {
        if (limitStockNumber < 1)
        {
            Destroy(this.gameObject);
        }
    }
    public void SubLimitStockNumber()
    {
        limitStockNumber--;
    }

}
