using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FishCollectionManager : MonoBehaviour
{
    [Header("Blur")]
    public Image blur_0;
    public Image blur_1;
    public Image blur_2;
    public Image blur_3;
    public Image blur_4;
    public Image blur_5;
    public Image blur_6;
    public Image blur_7;

    private Image[] blurImages;
    [Header("References")]
    public MultiBoardGenerator boardGenerator;
    public CollectionBar collectionBar;
    public GameResultManager gameResultManager;

    [Header("Animation Settings")]
    public float moveAnimationDuration = 0.5f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Board Intro Animation")]
    public float introDuration = 0.5f;                     
    public float introDelayBetweenBoards = 0.1f;         
    public AnimationCurve introCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Board Order (Largest to Smallest)")]
    public string[] boardOrder = { "Board_1", "Board_2", "Board_3", "Board_4", "Board_5", "Board_6", "Board_7", "Board_8", "Board_9" };

    private Dictionary<string, int> boardFishCount = new Dictionary<string, int>();
    private string currentActiveBoard = "Board_9"; 
    private bool isAnimating = false;

    void Start()
    {
        blurImages = new Image[] { blur_0, blur_1, blur_2, blur_3, blur_4, blur_5, blur_6, blur_7 };

        InitializeBoardCounts();
        StartCoroutine(AnimateBoardEntrance());

        if (gameResultManager == null)
            gameResultManager = FindObjectOfType<GameResultManager>();
        foreach (var blur in blurImages)
        {
            if (blur != null)
                blur.gameObject.SetActive(false);
        }

    }

    void InitializeBoardCounts()
    {
        foreach (string boardName in boardOrder)
        {
            BoardConfig config = boardGenerator.GetBoardConfig(boardName);
            if (config != null)
            {
                boardFishCount[boardName] = config.width * config.height;
            }
        }
    }

    IEnumerator AnimateBoardEntrance()
    {
        foreach (string boardName in boardOrder)
        {
            var config = boardGenerator.GetBoardConfig(boardName);
            if (config?.boardTransform != null)
            {
                boardGenerator.SetBoardActive(boardName, true);
                config.boardTransform.localScale = Vector3.zero;
            }
        }
        foreach (string boardName in boardOrder)
        {
            var config = boardGenerator.GetBoardConfig(boardName);
            if (config?.boardTransform == null) continue;

            Transform board = config.boardTransform;
            Vector3 targetScale = Vector3.one;

            float elapsed = 0f;
            while (elapsed < introDuration)
            {
                elapsed += Time.deltaTime;
                float t = introCurve.Evaluate(elapsed / introDuration);
                board.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
                yield return null;
            }

            board.localScale = targetScale;
            yield return new WaitForSeconds(introDelayBetweenBoards);
        }
        // Vì board_9 là nhỏ nhất nên blur cuối cùng (index 7) sẽ che phần còn lại
        if (blurImages.Length > 0 && blurImages[blurImages.Length - 1] != null)
        {
            blurImages[blurImages.Length - 1].gameObject.SetActive(true);
        }

    }

    public void OnFishClicked(FishController fish, string boardName)
    {
        if (boardName != currentActiveBoard || isAnimating || collectionBar.IsAnimating())
        {
            return;
        }

        if (gameResultManager != null && gameResultManager.IsGameEnded())
        {
            return;
        }

        if (collectionBar.CanAddFish())
        {
            StartCoroutine(MoveFishToCollection(fish, boardName));
        }
    }

    System.Collections.IEnumerator MoveFishToCollection(FishController fish, string boardName)
    {
        isAnimating = true;

        int fx = fish.x;
        int fy = fish.y;
        if (boardGenerator != null)
        {
            boardGenerator.SetFish(boardName, fx, fy, null);
        }

        FishClickHandler click = fish.GetComponent<FishClickHandler>();
        if (click != null)
            click.enabled = false;

        fish.SetBoardName("Collected");

        Transform targetSlot = collectionBar.collectionSlots[collectionBar.GetCollectedCount()];

        Vector3 startWorldPos = fish.transform.position;
        Vector3 targetWorldPos = targetSlot.position;

        float elapsed = 0f;
        while (elapsed < moveAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(elapsed / moveAnimationDuration);

            fish.transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);
            yield return null;
        }

        fish.transform.position = targetWorldPos;

        collectionBar.AddFish(fish, targetSlot);

        boardFishCount[boardName]--;

        while (collectionBar.IsAnimating())
        {
            yield return null;
        }

        if (boardFishCount[boardName] <= 0)
            yield return StartCoroutine(HandleBoardComplete(boardName));

        isAnimating = false;
    }

    System.Collections.IEnumerator HandleBoardComplete(string completedBoard)
    {
        boardGenerator.SetBoardActive(completedBoard, false);
        yield return new WaitForSeconds(0.3f);

        int currentIndex = System.Array.IndexOf(boardOrder, completedBoard);

        // Vì boardOrder là từ lớn đến nhỏ nên cần đảo hướng blur tương ứng
        int blurIndex = blurImages.Length - 1 - currentIndex;

        // Ẩn blur trước (vừa che board cũ)
        if (blurIndex >= 0 && blurIndex < blurImages.Length && blurImages[blurIndex] != null)
            blurImages[blurIndex].gameObject.SetActive(false);

        // Hiển thị blur kế tiếp (che các board còn lại phía trên)
        if (blurIndex - 1 >= 0 && blurIndex - 1 < blurImages.Length && blurImages[blurIndex - 1] != null)
            blurImages[blurIndex - 1].gameObject.SetActive(true);

        string nextBoard = GetNextBoard(completedBoard);
        if (nextBoard != null)
        {
            currentActiveBoard = nextBoard;
            foreach (string boardName in boardOrder)
            {
                bool shouldShow = (boardName == currentActiveBoard);
                boardGenerator.SetBoardActive(boardName, shouldShow);
            }
        }
        else
        {
            OnGameComplete();
        }
    }



string GetNextBoard(string currentBoard)
    {
        int currentIndex = System.Array.IndexOf(boardOrder, currentBoard);
        if (currentIndex >= 0 && currentIndex < boardOrder.Length - 1)
        {
            return boardOrder[currentIndex + 1];
        }
        return null;
    }

    void OnGameComplete()
    {
        
    }

    public string GetCurrentActiveBoard()
    {
        return currentActiveBoard;
    }

    public int GetRemainingFishCount(string boardName)
    {
        return boardFishCount.ContainsKey(boardName) ? boardFishCount[boardName] : 0;
    }

    public bool IsCollectionInProgress()
    {
        return isAnimating || collectionBar.IsAnimating();
    }
}