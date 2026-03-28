using UnityEngine;

[System.Serializable]
public class RewardPiece
{
    public GameObject prefab;
    public Rarity rarity = Rarity.Common; 

    public int Price
    {
        get
        {
            switch (rarity)
            {
                case Rarity.Common: return 2;
                case Rarity.Rare:   return 4;
                case Rarity.Epic:   return 6;
                default: return 1;
            }
        }
    }
}

public enum Rarity
{
    Common,
    Rare,
    Epic
}
