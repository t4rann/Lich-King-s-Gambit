using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("UI References")]
    public TextMeshPro titleText;      
    public TextMeshPro descriptionText; 
    public TextMeshPro statsText;       

    private Dictionary<Type, string> descriptions = new Dictionary<Type, string>()
    {
        { typeof(Pawn),
            "Moves 1 tile orthogonally.\n" +
            "Attacks enemies diagonally in melee."
        },

        { typeof(Ranger),
            "Moves 1 tile orthogonally.\n" +
            "Ranged attacks in straight lines.\n" +
            "Requires clear line of sight."
        },

        { typeof(Knight),
            "Moves 1 tile in any direction.\n" +
            "Can attack enemies only diagonally."
        },

        { typeof(Paladin),
            "Moves 1 tile orthogonally.\n" +
            "Can attack adjacent enemies in ANY direction."
        },

        { typeof(Rogue),
            "Moves in an L-shape.\n" +
            "Jumps over units.\n" +
            "Melee attacker."
        },

        { typeof(Mage),
            "Moves 1 tile orthogonally.\n" +
            "Fires a piercing magic beam in straight lines.\n" +
            "Beam damages ALL enemies in its path.\n" +
            "Requires line of sight."
        },

        { typeof(Cleric),
            "Moves diagonally.\n" +
            "Ranged ability along diagonals.\n" +
            "Heals allies instead of damaging them."
        },
        
        { typeof(Captain),
            "Moves 1 tile in any direction.\n" +
            "Attacks enemies within 2 tiles in any direction.\n" +
            "Can hit multiple enemies in one attack."
        },
    };

    private Dictionary<Type, string> titles = new Dictionary<Type, string>()
    {
        { typeof(Pawn),   "Pawn" },
        { typeof(Ranger), "Archer" },
        { typeof(Knight), "Knight" },
        { typeof(Paladin),"Paladin" },
        { typeof(Rogue),  "Rogue" },
        { typeof(Mage),   "Mage" },
        { typeof(Cleric), "Cleric" },
        { typeof(Captain), "Captain" }
    };

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowDescription(Piece piece)
    {
        if (piece == null) return;

        Type type = piece.GetType();

        if (titleText != null && titles.ContainsKey(type))
            titleText.text = titles[type];

        string abilityText = descriptions.ContainsKey(type)
            ? descriptions[type]
            : "";

        if (descriptionText != null)
            descriptionText.text = abilityText;

        if (statsText != null)
            statsText.text = piece.GetStatsDescription();
    }

    public void ClearDescription()
    {
        if (titleText != null) titleText.text = "- - -";
        if (descriptionText != null) descriptionText.text = "";
        if (statsText != null) statsText.text = "";
    }
}
