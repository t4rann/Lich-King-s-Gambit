using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Pixelplacement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI")]
    public Transform panel;
    public TextMeshPro text;

    [Header("Animation")]
    public float scaleDuration = 0.3f;
    public float showDelay = 1.5f;        
    public float displayDuration = 20f;  

    [Header("Highlight Objects")]
    public GameObject timerHighlight;
    public GameObject chargeHighlight;
    public GameObject hpHighlight;
    public GameObject pieceHighlight;

    private bool isShowing = false;
    private HashSet<string> shown = new HashSet<string>();

    private Queue<TutorialData> tutorialQueue = new Queue<TutorialData>();

    private void Awake()
    {
        Instance = this;

        panel.gameObject.SetActive(false);
        panel.localScale = Vector3.zero;
    }

    // ===================== QUEUE =====================
    public void EnqueueTutorial(string id, GameObject highlightObj, string message)
    {
        if (shown.Contains(id)) return;

        tutorialQueue.Enqueue(new TutorialData(id, highlightObj, message));
        ShowNext();
    }

    private void ShowNext()
    {
        if (isShowing) return;
        if (tutorialQueue.Count == 0) return;

        var data = tutorialQueue.Dequeue();
        isShowing = true;
        shown.Add(data.id);

        StartCoroutine(ShowTutorialWithTimer(data));
    }

    private IEnumerator ShowTutorialWithTimer(TutorialData data)
    {
        yield return new WaitForSecondsRealtime(showDelay);

        panel.gameObject.SetActive(true);
        panel.localScale = Vector3.zero;
        text.text = data.message;
        
        SoundManager.Instance.PlayFail();

        Tween.LocalScale(panel, Vector3.one, scaleDuration, 0f, Tween.EaseInOut);

        if (data.highlight != null)
            data.highlight.SetActive(true);

        if (TurnTimer.Instance != null)
            TurnTimer.Instance.StopTimer();

        yield return new WaitForSecondsRealtime(displayDuration);

        Close(data);
    }

    private void Close(TutorialData data)
    {
        Tween.LocalScale(panel, Vector3.zero, scaleDuration, 0f, Tween.EaseInOut,
            completeCallback: () =>
            {
                panel.gameObject.SetActive(false);
            });

        if (data.highlight != null)
            data.highlight.SetActive(false);

        if (TurnTimer.Instance != null)
            TurnTimer.Instance.ResumeTimer();

        isShowing = false;

        ShowNext();
    }

    // ===================== Tutorial triggers =====================
    public void TimerHalf()
    {
        EnqueueTutorial("timer_half", timerHighlight,
            "The timer limits the time to complete the level. When time runs out, each second will start dealing damage."); 
    }

    public void ChargeTutorial()
    {
        EnqueueTutorial("charge_tutorial", chargeHighlight,
            "You can accumulate charges by taking turns with different characters. Attacks with 3 charges deal double damage."); 
    }

    public void FirstDamage()
    {
        EnqueueTutorial("first_damage", hpHighlight,
            "The Lich's health is linked to the pieces. If pieces die, you lose HP, so protect them!"); 
    }

    public void OnePieceLeft()
    {
        EnqueueTutorial("one_piece", pieceHighlight,
            "Only one piece left! If it dies, the level will restart. Be careful!");
    }

    public void MergeTutorial()
    {
        EnqueueTutorial("merge_tutorial", pieceHighlight,
            "Merge two identical characters to upgrade them into a stronger unit!");
    }

    // ===================== Data Struct =====================
    private class TutorialData
    {
        public string id;
        public GameObject highlight;
        public string message;

        public TutorialData(string id, GameObject highlight, string message)
        {
            this.id = id;
            this.highlight = highlight;
            this.message = message;
        }
    }
}
