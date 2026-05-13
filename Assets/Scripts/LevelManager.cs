using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Pixelplacement;
using System.Collections;
using GameAnalyticsSDK;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public BoardManager boardManager;
    public int currentLevelIndex = 0;

    [Header("UI")]
    public TextMeshPro levelText;
    public TextMeshPro rewardText;
    public int totalLevels = 10;

    [Header("Level Layouts")]
    public LevelLayoutSO[] levels;

    [Header("Reward Board Layouts")]
    public LevelLayoutSO[] rewardBoardLayouts; 

    [Header("Reward Pieces")]
    public RewardPiece[] rewardPieces; 

    [Header("Victory")]
    public GameObject victoryPanel;

    [Header("Reward UI")]
    public GameObject rewardPriceUIPrefab; 

    [Header("Next Level Button")]
    public GameObject nextLevelButton; 
    public Collider2D nextLevelButtonCollider;

    [Header("Reroll Rewards")]
    public GameObject rerollButton;
    public Collider2D rerollButtonCollider;
    public int rerollCost = 1;

    [Header("Merge System")]
    public bool enableMergeSystem = true;

    bool waitingForPlacement;
    bool waitingForNextLevel = false; 

    private Piece pendingMovePiece;  
    private Tile originalMoveTile;  

    GameObject pendingReward;
    int pendingRewardPrice = 0;

    private List<Piece> rewardOptions = new List<Piece>();
    private List<RewardPriceUI> rewardPriceUIs = new List<RewardPriceUI>();
    private bool isTransitioning = false;

    [System.Serializable]
    public class PlayerPieceData
    {
        public GameObject prefabReference;
        public int x;
        public int y;
        
        public int mergeLevel = 1;
    }

    public List<PlayerPieceData> playerPiecesData = new List<PlayerPieceData>();

    void Awake()
    {
        Instance = this;
        
        if (nextLevelButton != null)
        {
            nextLevelButton.SetActive(false);
            
            if (nextLevelButtonCollider == null)
            {
                nextLevelButtonCollider = nextLevelButton.GetComponent<Collider2D>();
                if (nextLevelButtonCollider == null)
                {
                    nextLevelButtonCollider = nextLevelButton.AddComponent<BoxCollider2D>();
                }
            }
        }
    }

    void Start()
    {
        boardManager.InitBoard();
        StartLevel(currentLevelIndex);

        if (rewardText != null)
            rewardText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            RestartLevel();

        if (waitingForPlacement && rewardText != null && rewardText.gameObject.activeSelf)
        {
            float pulse = Mathf.Sin(Time.time * 2f) * 0.1f + 1f;
            rewardText.transform.localScale = Vector3.one * pulse;
        }

        if (waitingForNextLevel && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null && hit.collider == nextLevelButtonCollider)
            {
                OnNextLevelButtonClick();
            }
        }
        
        if (waitingForPlacement && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider == rerollButtonCollider)
            {
                RerollRewards();
                return;
            }

            if (waitingForNextLevel && hit.collider == nextLevelButtonCollider)
            {
                OnNextLevelButtonClick();
                return;
            }
        }
    }

    public void ShowRewardButtons()
    {
        ShowButton(nextLevelButton);
        ShowButton(rerollButton);
    }


    public void HideRewardButtons()
    {
        HideButton(nextLevelButton);
        HideButton(rerollButton);
    }

    private void ShowButton(GameObject button)
    {
        if (button == null) return;

        button.SetActive(true);
        button.transform.localScale = Vector3.zero;

        Tween.LocalScale(
            button.transform,
            Vector3.one,
            0.5f,
            0f,
            boardManager.boardAppearCurve
        );
    }

    private void HideButton(GameObject button)
    {
        if (button == null || !button.activeSelf) return;

        Tween.LocalScale(
            button.transform,
            Vector3.zero,
            0.3f,
            0f,
            completeCallback: () => button.SetActive(false)
        );
    }

    public void OnNextLevelButtonClick()
    {
        if (isTransitioning || !waitingForNextLevel) return;

        SoundManager.Instance.PlayApply();
        waitingForNextLevel = false;
        HideRewardButtons();
        CompleteRewardSelection();
    }

    public void RestartLevel()
    {
        if (isTransitioning) return;

        RemoveAllRemainingCurrency();

        Debug.Log("Restarting level...");
        StartLevel(currentLevelIndex);
    }

    // ================= UI =================
    void UpdateLevelText()
    {
        if (levelText != null)
            levelText.text = $"Room {currentLevelIndex + 1} / {totalLevels}";
    }

    void ShowRewardText(string message)
    {
        if (rewardText != null)
        {
            rewardText.text = message;
            rewardText.gameObject.SetActive(true);
            rewardText.transform.localScale = Vector3.zero;
            Tween.LocalScale(rewardText.transform, Vector3.one, 0.5f, 0f,
                boardManager.boardAppearCurve);
        }
    }

    void HideRewardText()
    {
        if (rewardText != null)
        {
            Tween.LocalScale(rewardText.transform, Vector3.zero, 0.3f, 0f,
                completeCallback: () => rewardText.gameObject.SetActive(false));
        }
    }

    // ================= STATE =================
    public bool IsWaitingForRewardPlacement() => waitingForPlacement;
    public bool IsWaitingForNextLevel() => waitingForNextLevel;
    public bool RewardOptionsContains(Piece piece) => rewardOptions.Contains(piece);

    public void SelectReward(Piece piece)
    {
        if (!waitingForPlacement) return;

        if (pendingReward != null)
        {
            Piece oldPiece = pendingReward.GetComponent<Piece>();
            if (oldPiece != null)
                oldPiece.SetSelected(false);
        }

        RewardPiece rewardPiece = null;
        foreach (var rp in rewardPieces)
        {
            if (rp.prefab == piece.prefabReference)
            {
                rewardPiece = rp;
                break;
            }
        }

        if (rewardPiece == null)
        {
            Debug.LogError("RewardPiece not found for this prefab!");
            return;
        }

        pendingRewardPrice = rewardPiece.Price;

        if (PlayerCurrency.Instance.totalCurrency < pendingRewardPrice)
        {
            ShowRewardText($"Need {pendingRewardPrice} coins");
            SoundManager.Instance.PlayFail();

            piece.SetSelected(false);

            pendingReward = null;
            pendingRewardPrice = 0;
            MergeManager.Instance.ClearMergeHighlights();
            return;
        }

        pendingReward = piece.gameObject;

        piece.SetSelected(true);

        MergeManager.Instance.CheckMergePossibility(piece.prefabReference, piece);

        ShowRewardText("Select a cell to place");
    }

public bool HandleRewardClick(Tile tile)
{
    if (pendingReward != null && tile.currentPiece != null && MergeManager.Instance.IsMergeCandidate(tile))
    {
        TryPlaceReward(tile);
        return true;
    }

    if (pendingReward != null && tile.currentPiece == null && tile.y <= 1)
    {
        TryPlaceReward(tile);
        return true;
    }

    if (waitingForPlacement &&
        tile.currentPiece != null &&
        tile.currentPiece.isWhite &&
        !rewardOptions.Contains(tile.currentPiece))
    {
        SelectOwnedPieceForMove(tile);
        return true;
    }

    if (pendingMovePiece != null &&
        tile.currentPiece == null &&
        tile.y <= 1)
    {
        MoveOwnedPiece(tile);
        return true;
    }

    GameManager.Instance.HighlightRewardPlacement();
    return false;
}

    void SelectOwnedPieceForMove(Tile tile)
    {
        if (pendingMovePiece != null)
            pendingMovePiece.SetSelected(false);

        pendingMovePiece = tile.currentPiece;
        originalMoveTile = tile;

        pendingMovePiece.SetSelected(true);

        ShowRewardText("Select a new cell");
        SoundManager.Instance.PlaySelect();

        GameManager.Instance.HighlightRewardPlacement();
    }

    void MoveOwnedPiece(Tile targetTile)
    {
        originalMoveTile.currentPiece = null;

        pendingMovePiece.SetPosition(targetTile.x, targetTile.y);
        targetTile.currentPiece = pendingMovePiece;

        foreach (var data in playerPiecesData)
        {
            if (data.x == originalMoveTile.x && data.y == originalMoveTile.y)
            {
                data.x = targetTile.x;
                data.y = targetTile.y;
                break;
            }
        }

        pendingMovePiece.SetSelected(false);
        pendingMovePiece = null;
        originalMoveTile = null;

        MergeManager.Instance.ClearMergeHighlights();
        GameManager.Instance.UpdateTileCollidersImmediately();
        GameManager.Instance.HighlightRewardPlacement();

        ShowRewardText("Piece moved");
        SoundManager.Instance.PlayApply();

        Invoke(nameof(HideRewardTextAfterDelay), 1.2f);
    }

    // ================= LEVEL FLOW =================
    public void StartLevel(int index)
    {
        if (isTransitioning) return;

        SoundManager.Instance.PlayMusic(SoundManager.MusicType.Background);

        isTransitioning = true;
        HideRewardText();
        HideRewardButtons();
        waitingForNextLevel = false;

        TurnTimer.Instance?.ResetTimer();
        TurnComboBar.Instance?.ResetCharge();

        if (boardManager.tiles != null)
        {
            boardManager.OnBoardDisappearComplete = null;
            boardManager.OnBoardAppearComplete = null;

            boardManager.OnBoardDisappearComplete += () => StartNewLevel(index);
            boardManager.ClearBoardAnimated();
        }
        else
        {
            StartNewLevel(index);
        }
    }

    void StartNewLevel(int index)
    {
        UpdateLevelText();

        if (levels != null && index < levels.Length)
        {
            levels[index].EnsureLayoutSize();
            boardManager.boardLayout = levels[index].layout;
            boardManager.width = levels[index].width;
            boardManager.height = levels[index].height;
        }
        else
        { 
            boardManager.EnsureLayoutSize();
        }

        boardManager.OnBoardDisappearComplete = null;
        boardManager.OnBoardAppearComplete = null;

        boardManager.OnBoardAppearComplete += () =>
        {
            boardManager.SpawnAllPiecesAnimated(playerPiecesData, () =>
            {
                SpawnEnemiesForLevel(index);
                GameManager.Instance?.UpdateTileCollidersImmediately();

                // ✅ Отправляем событие начала уровня
                GameAnalyticsManager.Instance.LevelStarted(index + 1);
            });
        };
        
        boardManager.GenerateBoardAnimated();
    }

    void SpawnEnemiesForLevel(int levelIndex)
    {
        GameManager.Instance.whiteTurn = true;
        ClearEnemies();

        if (levels == null || levelIndex >= levels.Length)
            return;

        LevelLayoutSO level = levels[levelIndex];
        if (level.enemies == null) return;

        foreach (var enemy in level.enemies)
        {
            if (enemy.prefab == null) continue;

            boardManager.SpawnPieceAnimated(
                enemy.prefab,
                false,
                enemy.x,
                enemy.y
            );
        }

        GameManager.Instance?.UpdateTileCollidersImmediately();
        isTransitioning = false;
    }

    void ClearEnemies()
    {
        if (boardManager.tiles == null) return;

        for (int x = 0; x < boardManager.width; x++)
        {
            for (int y = 0; y < boardManager.height; y++)
            {
                Tile t = boardManager.tiles[x, y];
                if (t != null && t.currentPiece != null && !t.currentPiece.isWhite)
                {
                    Destroy(t.currentPiece.gameObject);
                    t.currentPiece = null;
                }
            }
        }
    }

    // ================= CHECK =================
    private bool onePieceLeftTriggered = false; 

    public void OnPieceKilled(Piece piece)
    {
        if (!piece.isWhite) return;

        int aliveAllies = 0;
        Piece lastAllyPiece = null;

        foreach (var tile in boardManager.tiles)
        {
            if (tile?.currentPiece != null && tile.currentPiece.isWhite)
            {
                aliveAllies++;
                lastAllyPiece = tile.currentPiece;
            }
        }

        int aliveEnemies = 0;
        foreach (var tile in boardManager.tiles)
        {
            if (tile?.currentPiece != null && !tile.currentPiece.isWhite)
                aliveEnemies++;
        }

        if (aliveAllies == 1 && aliveEnemies > 0 && !onePieceLeftTriggered)
        {
            onePieceLeftTriggered = true;
            TutorialManager.Instance?.OnePieceLeft();
        }
        
        if (!piece.isWhite) return;

        if (AllPlayerPiecesDead())
        {
            SoundManager.Instance.PlayDefeat();

            GameAnalyticsManager.Instance.LevelFailed(currentLevelIndex + 1);

            Debug.Log("ALL PLAYER PIECES DEAD — restarting level");
            RestartLevel();
        }

        GameManager.Instance?.UpdateTileCollidersImmediately();
    }

    bool AllPlayerPiecesDead()
    {
        foreach (var tile in boardManager.tiles)
            if (tile?.currentPiece != null && tile.currentPiece.isWhite)
                return false;
        return true;
    }

    bool AllEnemiesDead()
    {
        foreach (var tile in boardManager.tiles)
            if (tile?.currentPiece != null && !tile.currentPiece.isWhite)
                return false;
        return true;
    }

    public void CheckLevelCompletion()
    {
        if (waitingForPlacement || waitingForNextLevel || isTransitioning) return;
        if (!AllEnemiesDead()) return;

        if (PlayerHealth.Instance == null || PlayerHealth.Instance.currentHP <= 0) return; //NEW

        SoundManager.Instance.PlayVictory(SoundManager.Instance.victory, () =>
        {
            SoundManager.Instance.PlayShopMusic();
        });

        CompleteLevel(true);
    }

    // ================= VICTORY / LEVEL END =================
    public void CompleteLevel(bool passed)
    {

        if (passed)
        {
            GameAnalyticsManager.Instance.LevelComplete(currentLevelIndex + 1);
        }

        isTransitioning = true;
        waitingForPlacement = false;
        waitingForNextLevel = false;

        TurnTimer.Instance?.StopTimer();

        if (passed) CollectAllRemainingCurrency();
        else RemoveAllRemainingCurrency();

        GameManager.Instance?.ClearHighlights();
        HideRewardButtons();
        boardManager.ClearBoardAnimated();

        if (passed && currentLevelIndex >= totalLevels - 1 && victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            isTransitioning = false;
            return;
        }
        
        isTransitioning = false;
        if (passed)
        {
            currentLevelIndex++;
            ShowRewardSelection();
        }
    }

    void CollectAllRemainingCurrency()
    {
        if (boardManager.tiles == null) return;

        for (int x = 0; x < boardManager.width; x++)
        {
            for (int y = 0; y < boardManager.height; y++)
            {
                Tile t = boardManager.tiles[x, y];
                if (t?.currency != null)
                {
                    t.currency.Collect();
                    t.currency = null;
                }
            }
        }
    }

    void RemoveAllRemainingCurrency()
    {
        if (boardManager.tiles == null) return;

        for (int x = 0; x < boardManager.width; x++)
        {
            for (int y = 0; y < boardManager.height; y++)
            {
                Tile t = boardManager.tiles[x, y];
                if (t?.currency != null)
                {
                    Destroy(t.currency.gameObject);
                    t.currency = null;
                }
            }
        }
    }

    // ================= REWARD =================
    void ShowRewardSelection()
    {
        if (isTransitioning) return;

        SoundManager.Instance.PlayMusic(SoundManager.MusicType.Shop);

        waitingForPlacement = true;
        rewardOptions.Clear();

        foreach (var ui in rewardPriceUIs)
            if (ui != null) Destroy(ui.gameObject);
        rewardPriceUIs.Clear();

        ClearEnemies();

        LevelLayoutSO selectedRewardBoard = null;

        if (rewardBoardLayouts != null && rewardBoardLayouts.Length > 0)
        {
            int index = currentLevelIndex / 3; 
            index = Mathf.Clamp(index, 0, rewardBoardLayouts.Length - 1);
            selectedRewardBoard = rewardBoardLayouts[index];
        }

        if (selectedRewardBoard != null)
        {
            selectedRewardBoard.EnsureLayoutSize();
            boardManager.boardLayout = selectedRewardBoard.layout;
            boardManager.width = selectedRewardBoard.width;
            boardManager.height = selectedRewardBoard.height;
        }
        else
        {
            boardManager.EnsureLayoutSize();
        }

        boardManager.OnBoardDisappearComplete = null;
        boardManager.OnBoardAppearComplete = null;

        boardManager.OnBoardDisappearComplete += () =>
        {
            boardManager.OnBoardAppearComplete = () =>
            {
                SpawnRewards(new List<PlayerPieceData>(playerPiecesData));
                ShowRewardText("Choose a reward");
            };
            boardManager.GenerateBoardAnimated();
        };

        boardManager.ClearBoardAnimated();
    }

    // ================= REWARD =================
    void SpawnRewards(List<PlayerPieceData> tempPlayerPieces)
    {
        rewardOptions.Clear();

        foreach (var data in tempPlayerPieces)
        {
            GameObject obj = Instantiate(data.prefabReference);
            Piece p = obj.GetComponent<Piece>();
            p.isWhite = true;
            p.prefabReference = data.prefabReference;
            p.mergeLevel = data.mergeLevel;
            p.ApplyMergeStats(data.mergeLevel);
            p.SetPosition(data.x, data.y);

            p.transform.localScale = Vector3.zero;
            Tween.LocalScale(p.transform, Vector3.one, 0.5f, ((data.x + data.y) * 0.1f),
                boardManager.boardAppearCurve);

            boardManager.tiles[data.x, data.y].currentPiece = p;
        }

        SpawnRewardAnimated(1, 3);
        SpawnRewardAnimated(3, 3);
        SpawnRewardAnimated(5, 3);

        foreach (var reward in rewardOptions)
        {
            RewardPiece rewardPiece = null;
            foreach (var rp in rewardPieces)
            {
                if (rp.prefab == reward.prefabReference)
                {
                    rewardPiece = rp;
                    break;
                }
            }

            if (rewardPiece == null) continue;

            GameObject uiObj = Instantiate(rewardPriceUIPrefab);
            RewardPriceUI ui = uiObj.GetComponent<RewardPriceUI>();
            ui.Init(reward.transform, rewardPiece.Price, reward);

            rewardPriceUIs.Add(ui);
        }

        ShowRewardButtons();
        waitingForNextLevel = true;

        GameManager.Instance?.HighlightRewardPlacement();
    }


    public void RerollRewards()
    {
        if (!waitingForPlacement) return;

        if (PlayerCurrency.Instance.totalCurrency < rerollCost)
        {
            ShowRewardText("Need 1 coin to reroll");
            SoundManager.Instance.PlayFail();
            return;
        }

        PlayerCurrency.Instance.SpendCurrency(rerollCost);

        foreach (var reward in rewardOptions)
            if (reward != null)
                Destroy(reward.gameObject);

        foreach (var ui in rewardPriceUIs)
            if (ui != null)
                Destroy(ui.gameObject);

        rewardOptions.Clear();
        rewardPriceUIs.Clear();
        pendingReward = null;
        pendingRewardPrice = 0;

        SpawnRewardAnimated(1, 3);
        SpawnRewardAnimated(3, 3);
        SpawnRewardAnimated(5, 3);

        ShowRewardText("Rewards rerolled!");
        SoundManager.Instance.PlaySelect();
    }

    // ================= SpawnRewardAnimated =================
    void SpawnRewardAnimated(int x, int y)
    {
        if (rewardPieces == null || rewardPieces.Length == 0) return;

        RewardPiece rewardPiece = GetRandomRewardPiece();
        if (rewardPiece == null) return;

        GameObject obj = Instantiate(rewardPiece.prefab);
        Piece p = obj.GetComponent<Piece>();
        p.isWhite = true;
        p.prefabReference = rewardPiece.prefab;
        p.SetPosition(x, y);

        p.transform.localScale = Vector3.zero;
        Tween.LocalScale(
            p.transform,
            Vector3.one,
            0.5f,
            ((x + y) * 0.1f),
            boardManager.boardAppearCurve
        );

        boardManager.tiles[x, y].currentPiece = p;
        rewardOptions.Add(p);

        GameObject uiObj = Instantiate(rewardPriceUIPrefab);
        RewardPriceUI ui = uiObj.GetComponent<RewardPriceUI>();
        ui.Init(p.transform, rewardPiece.Price, p);
        rewardPriceUIs.Add(ui);
    }

    // ================= GetRandomRewardPiece =================
    RewardPiece GetRandomRewardPiece()
    {
        if (rewardPieces == null || rewardPieces.Length == 0)
            return null;

        List<RewardPiece> weightedList = new List<RewardPiece>();
        foreach (var piece in rewardPieces)
        {
            int weight = piece.rarity switch
            {
                Rarity.Common => 70,
                Rarity.Rare => 20,
                Rarity.Epic => 10,
                _ => 70
            };

            for (int i = 0; i < weight; i++)
                weightedList.Add(piece);
        }

        int index = Random.Range(0, weightedList.Count);
        return weightedList[index];
    }

    // ================= REWARD PLACEMENT =================
    public void TryPlaceReward(Tile tile)
    {
        if (!waitingForPlacement || pendingReward == null || isTransitioning) return;
        if (tile.y > 1) return;

        Piece p = pendingReward.GetComponent<Piece>();

        if (PlayerCurrency.Instance.totalCurrency < pendingRewardPrice)
        {
            ShowRewardText($"Need {pendingRewardPrice} coins");
            SoundManager.Instance.PlayDeathPlayer();
            ClearPendingReward();
            return;
        }

        bool isMerging = false;

        if (tile.currentPiece != null && MergeManager.Instance.IsMergeCandidate(tile))
        {
            if (MergeManager.Instance.TryMerge(p, tile))
            {
                isMerging = true;
                PlayerCurrency.Instance.SpendCurrency(pendingRewardPrice);

                UpdatePlayerPieceDataAfterMerge(p, tile);

                RemovePendingRewardFromOptions(p);

                ShowRewardText("Merged successfully!");
                SoundManager.Instance.PlayMerge();

                p.SetSelected(false);
            }
            else
            {
                PlaceRewardNormally(p, tile);
            }
        }
        else if (tile.currentPiece == null)
        {
            PlaceRewardNormally(p, tile);
        }
        else
        {
            ShowRewardText("Cell is occupied");
            SoundManager.Instance.PlayFail();
            return;
        }

        MergeManager.Instance.ClearMergeHighlights();
        GameManager.Instance.HighlightRewardPlacement();

        UpdateUIAfterRewardPlacement(p, tile, isMerging);

        ClearPendingReward();

        waitingForPlacement = true;
        isTransitioning = false;
    }

    private void PlaceRewardNormally(Piece p, Tile tile)
    {
        PlayerCurrency.Instance.SpendCurrency(pendingRewardPrice);

        p.SetSelected(false);

        RemovePendingRewardFromOptions(p);

        bool exists = false;
        foreach (var data in playerPiecesData)
        {
            if (data.prefabReference == p.prefabReference &&
                data.x == tile.x && data.y == tile.y)
            {
                data.mergeLevel = p.mergeLevel;
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            playerPiecesData.Add(new PlayerPieceData
            {
                prefabReference = p.prefabReference,
                x = tile.x,
                y = tile.y,
                mergeLevel = p.mergeLevel
            });
        }

        p.SetPosition(tile.x, tile.y);
        boardManager.tiles[tile.x, tile.y].currentPiece = p;

        GameManager.Instance?.UpdateTileCollidersImmediately();
        GameManager.Instance.HighlightRewardPlacement();


        ShowRewardText("Reward purchased!");
        SoundManager.Instance.PlayApply();
    }


    private void ClearPendingReward()
    {
        if (pendingReward != null)
        {
            Piece oldPiece = pendingReward.GetComponent<Piece>();
            if (oldPiece != null && GameManager.Instance != null && 
                GameManager.Instance.selectedPiece != oldPiece)
            {
                oldPiece.SetSelected(false);
            }
        }
        pendingReward = null;
        pendingRewardPrice = 0;
    }

    private void RemovePendingRewardFromOptions(Piece p)
    {
        if (rewardOptions.Contains(p))
            rewardOptions.Remove(p);

        for (int i = rewardPriceUIs.Count - 1; i >= 0; i--)
        {
            var ui = rewardPriceUIs[i];
            if (ui != null && ui.targetPiece == p)
            {
                Destroy(ui.gameObject);
                rewardPriceUIs.RemoveAt(i);
            }
        }
    }

    private void UpdatePlayerPieceDataAfterMerge(Piece mergedPiece, Tile tile)
    {
        for (int i = 0; i < playerPiecesData.Count; i++)
        {
            if (playerPiecesData[i].x == tile.x && playerPiecesData[i].y == tile.y)
            {
                playerPiecesData[i].prefabReference = mergedPiece.prefabReference;
                playerPiecesData[i].mergeLevel = mergedPiece.mergeLevel;
                return;
            }
        }
        
        playerPiecesData.Add(new PlayerPieceData
        {
            prefabReference = mergedPiece.prefabReference,
            x = tile.x,
            y = tile.y,
            mergeLevel = mergedPiece.mergeLevel
        });
    }

    private void UpdateUIAfterRewardPlacement(Piece p, Tile tile, bool isMerging)
    {
        if (!isMerging)
        {
            for (int i = rewardPriceUIs.Count - 1; i >= 0; i--)
            {
                var ui = rewardPriceUIs[i];
                if (ui != null && ui.transform.parent == p.transform)
                {
                    Destroy(ui.gameObject);
                    rewardPriceUIs.RemoveAt(i);
                }
            }
            
            rewardOptions.Remove(p);
        }
        
        Invoke(nameof(HideRewardTextAfterDelay), 1.5f);
    }

    private void HideRewardTextAfterDelay()
    {
        HideRewardText();
    }

    void CompleteRewardSelection()
    {
        MergeManager.Instance.ClearMergeHighlights();
        
        foreach (var reward in rewardOptions)
            if (reward != null)
                Destroy(reward.gameObject);

        foreach (var ui in rewardPriceUIs)
            if (ui != null)
                Destroy(ui.gameObject);

        rewardOptions.Clear();
        rewardPriceUIs.Clear();
        pendingReward = null;
        pendingRewardPrice = 0;
        waitingForPlacement = false;

        isTransitioning = false;

        StartLevel(currentLevelIndex);
    }
}