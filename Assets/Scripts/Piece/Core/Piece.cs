using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Pixelplacement;
using Random = UnityEngine.Random;


    public abstract class Piece : MonoBehaviour
    {
        public bool isWhite;
        public int x;
        public int y;

        public GameObject prefabReference;
        public GameObject currencyPrefab;

        public abstract void PlayAttackAnimation(
            Piece target,
            BoardManager board,
            Action onComplete
        );

        [Header("Merge Settings")]
        public int mergeLevel = 1;
        public bool isMergingCandidate = false;
        
        private float mergeLevel1Scale = 1f;
        private float mergeLevel2Scale = 1.05f;
        private float mergeLevel3Scale = 1.1f;

        private Vector3 baseScale;

        [Header("Visual")]
        public GameObject outline;

        private bool isHighlighted = false;

        [HideInInspector] public bool isSelected = false;
        [HideInInspector] public SpriteRenderer outlineSR;
        [HideInInspector] public PieceAnimator animator;

        private SortingGroup sortingGroup;

        [Header("Floating Text")]
        public GameObject floatingDamageTextPrefab;


        [Header("Base Stats")]
        [SerializeField] private int baseMaxHP = 100;
        [SerializeField] private int baseAttack = 50;
        [SerializeField] private int baseDefense = 20;
        [SerializeField] private float baseCritChance = 0.1f;
        
        [Header("Current Stats")]
        public int maxHP = 100;
        public int currentHP;            
        public int attack = 20;
        public int defense = 10;
        [Range(0f,1f)]
        public float critChance = 0.2f;

        public int BaseMaxHP => baseMaxHP;
        public int BaseAttack => baseAttack;
        public int BaseDefense => baseDefense;
        public float BaseCritChance => baseCritChance;

        public virtual bool IsRangedAttack(Vector2Int target)
        {
            return false;
        }

        public abstract List<Vector2Int> GetMoves(Tile[,] board, int width, int height);

        void Awake()
        {
            animator = GetComponent<PieceAnimator>();

            sortingGroup = GetComponent<SortingGroup>() ?? gameObject.AddComponent<SortingGroup>();

            baseScale = transform.localScale;

            ApplyMergeScale();

            ApplyBaseStats();
            currentHP = maxHP;

            if (outline != null)
            {
                outlineSR = outline.GetComponent<SpriteRenderer>();
                outline.SetActive(false);
            }

        }

        void Start()
        {
            SoundManager.Instance?.PlayPieceSpawn();
        }

        private void ApplyBaseStats()
        {
            maxHP = baseMaxHP;
            attack = baseAttack;
            defense = baseDefense;
            critChance = baseCritChance;
        }
        
        public void SetBaseStats(int hp, int atk, int def, float crit)
        {
            baseMaxHP = hp;
            baseAttack = atk;
            baseDefense = def;
            baseCritChance = Mathf.Clamp01(crit);
            
            ApplyMergeStats(mergeLevel);
        }

        public void ApplyMergeStats(int mergeLevelValue)
        {
            mergeLevel = mergeLevelValue;

            float multiplier = mergeLevel switch
            {
                1 => 1f,
                2 => 1.2f,
                3 => 1.5f,
                _ => 1f
            };

            maxHP = Mathf.RoundToInt(baseMaxHP * multiplier);
            attack = Mathf.RoundToInt(baseAttack * multiplier);
            defense = Mathf.RoundToInt(baseDefense * multiplier);

            critChance = mergeLevel == 3
                ? Mathf.Min(baseCritChance + 0.1f, 0.5f)
                : baseCritChance;

            currentHP = maxHP;

            ApplyMergeScale();
        }
        
        public void ResetToBaseStats()
        {
            mergeLevel = 1;
            ApplyBaseStats();
            currentHP = maxHP;
        }
        
    private void ApplyMergeScale()
    {
        float scaleMultiplier = mergeLevel switch
        {
            1 => mergeLevel1Scale,
            2 => mergeLevel2Scale,
            3 => mergeLevel3Scale,
            _ => 1f
        };

        baseScale = Vector3.one * scaleMultiplier;
        animator?.UpdateBaseScale(baseScale);
    }

    public void TakeDamage(int damage, bool isCrit, Vector3 hitDirection)
    {
        int effectiveDamage = Mathf.Max(damage - defense, 0);
        currentHP -= effectiveDamage;

        if (currentHP <= 0)
        {
            Die(hitDirection);
        }
        else
        {
            if (isWhite)
            {
                SoundManager.Instance.PlayHitPlayer();
            }
            else
            {
                SoundManager.Instance.PlayHitEnemy();
            }

            animator?.PlayHit();
        }

        ShowFloatingDamage(effectiveDamage, isCrit);
    }

        private void ShowFloatingDamage(int damage, bool isCrit)
        {
            if (floatingDamageTextPrefab == null) return;

            Vector3 spawnPos = transform.position + Vector3.up * 0.3f;

            GameObject go = Instantiate(floatingDamageTextPrefab, spawnPos, Quaternion.identity);
            var floatingText = go.GetComponent<FloatingDamageText>();

            if (floatingText != null)
                floatingText.Init(damage, isCrit);
        }

        public void ShowFloatingHeal(int amount)
        {
            if (floatingDamageTextPrefab == null) return;

            Vector3 spawnPos = transform.position + Vector3.up * 0.3f;

            GameObject go = Instantiate(floatingDamageTextPrefab, spawnPos, Quaternion.identity);
            var floatingText = go.GetComponent<FloatingDamageText>();
            
            if (floatingText != null)
                floatingText.InitHeal(amount);
        }

    public int CalculateDamage(bool forceCrit = false)
    {
        int dmg = attack;

        if (TurnComboBar.Instance != null && TurnComboBar.Instance.IsAtMaxCharge() && isWhite)
            dmg *= 2;

        bool isCrit = forceCrit || Random.value < critChance;
        if (isCrit) dmg = Mathf.CeilToInt(dmg * 2f);

        return dmg;
    }

    public void DealDamage(Piece target)
    {
        if (target == null) return;

        int damage = CalculateDamage();
        bool isCrit = damage >= attack * 2; 
        Vector3 hitDir = (target.transform.position - transform.position).normalized;

        target.TakeDamage(damage, isCrit, hitDir);
    }

    public void HealTarget(Piece target, int amount)
    {
        if (target == null) return;

        target.currentHP = Mathf.Min(target.maxHP, target.currentHP + amount);
        target.ShowFloatingHeal(amount);
    }

    private void Die(Vector3 hitDirection)
    {
        if (isWhite)
        {
            PlayerHealth.Instance?.TakeDamage(5);
            SoundManager.Instance.PlayDeathPlayer();
        }
        else
        {
            SoundManager.Instance.PlayHitEnemy();
            SoundManager.Instance.PlayKillEnemy();
        }

        if (animator != null)
        {
            animator.PlayDeath(hitDirection, () =>
            {
                GameManager.Instance?.HandlePieceDeath(this);
            });
        }
        else
        {
            GameManager.Instance?.HandlePieceDeath(this);
        }
    }

    public void SpawnCurrencyAround()
    {
        if (currencyPrefab == null) return;

        if (GameManager.Instance == null || GameManager.Instance.board == null)
            return;

        Tile[,] tiles = GameManager.Instance.board.tiles;

        int dropsCount = Random.Range(1, 4);

        List<Tile> availableTiles = new List<Tile>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < GameManager.Instance.board.width &&
                    ny >= 0 && ny < GameManager.Instance.board.height)
                {
                    Tile t = tiles[nx, ny];
                    
                    if (t != null && !t.isWall && t.currentPiece == null && t.currency == null)
                    {
                        availableTiles.Add(t);
                    }
                }
            }
        }

        for (int i = 0; i < dropsCount && availableTiles.Count > 0; i++)
        {
            int index = Random.Range(0, availableTiles.Count);
            Tile t = availableTiles[index];
            availableTiles.RemoveAt(index);

            GameObject dropGO = Instantiate(currencyPrefab);
            dropGO.transform.position = GameManager.Instance.board.GetWorldPosition(t.x, t.y);
            var drop = dropGO.GetComponent<CurrencyDrop>();
            drop.tile = t;
            t.PlaceCurrency(drop);
        }
    }

    public void SetPosition(int newX, int newY, bool animate = true)
    {
        bool isMoving = x != newX || y != newY;

        x = newX;
        y = newY;

        Vector3 worldPos = GameManager.Instance.board.GetWorldPosition(x, y);

        if (isMoving && animate && animator != null)
        {
            animator.PlayMove(worldPos, UpdateSortingGroup);
        }
        else
        {
            transform.position = worldPos;
            UpdateSortingGroup();
        }
    }

        public void ForceSetPosition(int newX, int newY)
        {
            x = newX;
            y = newY;
            Vector3 worldPos = GameManager.Instance.board.GetWorldPosition(x, y);
            transform.position = worldPos;
            UpdateSortingGroup();
        }

        void UpdateSortingGroup()
        {
            if (sortingGroup != null)
                sortingGroup.sortingOrder = 100 - y;
        }

    void OnMouseDown()
    {
        SoundManager.Instance?.PlayClick();

        if (LevelManager.Instance != null && LevelManager.Instance.IsWaitingForRewardPlacement())
        {
            if (LevelManager.Instance.RewardOptionsContains(this))
            {
                LevelManager.Instance.SelectReward(this);
                animator?.ClickBounce();
                return;
            }

            Tile rewardTile = GameManager.Instance.board.tiles[x, y];
            if (LevelManager.Instance.HandleRewardClick(rewardTile))
            {
                animator?.ClickBounce();
                return;
            }
        }

        if (GameManager.Instance.selectedPiece != null &&
            GameManager.Instance.selectedPiece != this)
        {
            Tile tile = GameManager.Instance.board.tiles[x, y];
            GameManager.Instance.TryMove(tile);
            return;
        }

        GameManager.Instance.SelectPiece(this);

        animator?.ClickBounce();
    }


        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            if (outline != null)
            {
                outlineSR.color = Color.white;
                outline.SetActive(selected || isHighlighted);
            }
        }
        
    public void SetAttackHighlight(bool value)
    {
        if (outline == null || outlineSR == null) return;

        outlineSR.color = Color.red;
        outline.SetActive(value);
        isHighlighted = value;
    }

    public void SetHealHighlight(bool value)
    {
        if (outline == null || outlineSR == null) return;

        outlineSR.color = Color.green;
        outline.SetActive(value);
        isHighlighted = value;
    }

    public void ClearAllHighlights()
    {
        if (outline == null) return;
        
        outline.SetActive(false);
        isHighlighted = false;
        isSelected = false;
    }
        public void SetMergeHighlight(bool value)
        {
            if (outline == null || outlineSR == null) return;

            if (value)
            {
                outlineSR.color = Color.yellow;
                outline.SetActive(true);
                isMergingCandidate = true;
            }
            else
            {
                outline.SetActive(false);
                isMergingCandidate = false;
            }
        }

        void OnMouseEnter()
        {
            if (!isSelected && !isHighlighted && !isMergingCandidate)
            {
                outlineSR.color = Color.white;
                outline.SetActive(true);
            }

            TooltipManager.Instance?.ShowDescription(this);
            animator?.Hover(true);
        }

        void OnMouseExit()
        {
            if (!isSelected && !isHighlighted && !isMergingCandidate)
                outline.SetActive(false);

            TooltipManager.Instance?.ClearDescription();
            animator?.Hover(false);
        }
        
        public string GetStatsDescription()
        {
            return $"HP: {currentHP}/{maxHP}\n" +
                $"Attack: {attack}\n" +
                $"Defense: {defense}\n" +
                $"Crit: {(critChance * 100):0}%\n" +
                $"Merge Level: {mergeLevel}";
        }
        
        public BaseStats GetBaseStats()
        {
            return new BaseStats
            {
                maxHP = baseMaxHP,
                attack = baseAttack,
                defense = baseDefense,
                critChance = baseCritChance
            };
        }
        
        [System.Serializable]
        public struct BaseStats
        {
            public int maxHP;
            public int attack;
            public int defense;
            public float critChance;
        }
    }

    [System.Serializable]
    public class PieceData
    {
        public GameObject prefab;
        public int baseHP;
        public int baseAttack;
        public int baseDefense;
        public float baseCritChance;
        
        public void ApplyToPiece(Piece piece)
        {
            piece.SetBaseStats(baseHP, baseAttack, baseDefense, baseCritChance);
        }
    }