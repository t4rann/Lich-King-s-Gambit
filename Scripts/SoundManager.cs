using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("UI")]
    public AudioClip click;
    public AudioClip select;
    public AudioClip apply;
    public AudioClip merge;
    public AudioClip fail;

    [Header("Gameplay")]
    public AudioClip pierce;
    public AudioClip spell;
    public AudioClip laser;
    public AudioClip arrow;
    public AudioClip attack;
    public AudioClip killEnemy;
    public AudioClip hitEnemy;
    public AudioClip hitPlayer;
    public AudioClip move;
    public AudioClip pieceSpawn;

    [Header("End Game")]
    public AudioClip victory;
    public AudioClip defeat;

    [Header("Music")]
    public AudioClip backgroundMusic;
    public AudioClip shopMusic;
    public bool musicEnabled = true;

    public enum MusicType
    {
        None,
        Background,
        Shop
    }

    private MusicType currentMusic = MusicType.None;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Фильтрация частых звуков")]
    [SerializeField] private bool filterFrequentSounds = true;
    [SerializeField] private float minSpawnInterval = 0.1f;
    [SerializeField] private float minMoveInterval = 0.08f;
    [SerializeField] private float minApplyInterval = 0.1f;
    [SerializeField] private int maxSpawnsPerFrame = 3;
    [SerializeField] private int maxMovesPerFrame = 4;
    [SerializeField] private int maxApplyPerFrame = 2;

    private float lastSpawnSoundTime = 0f;
    private float lastMoveSoundTime = 0f;
    private float lastApplySoundTime = 0f;

    private int spawnSoundsThisFrame = 0;
    private int moveSoundsThisFrame = 0;
    private int applySoundsThisFrame = 0;

    const string MUSIC_PREF = "MusicEnabled";
    const string FILTER_PREF = "FilterFrequentSounds";

    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitSources();
        LoadSettings();

        PlayMusic(MusicType.Background);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            ToggleMusic();
    }

    void LateUpdate()
    {
        spawnSoundsThisFrame = 0;
        moveSoundsThisFrame = 0;
        applySoundsThisFrame = 0;
    }

    void InitSources()
    {
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.volume = 0f;

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.volume = sfxVolume;
    }

    // ===================== MUSIC =====================

    public void PlayMusic(MusicType type)
    {
        if (!musicEnabled) return;

        AudioClip targetClip = null;

        switch (type)
        {
            case MusicType.Background:
                targetClip = backgroundMusic;
                break;

            case MusicType.Shop:
                targetClip = shopMusic;
                break;
        }

        if (targetClip == null) return;

        if (currentMusic == type && musicSource.isPlaying)
            return;

        currentMusic = type;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(SwitchMusicRoutine(targetClip));
    }

    public void PlayShopMusic()
    {
        PlayMusic(MusicType.Shop);
    }

    private IEnumerator SwitchMusicRoutine(AudioClip newClip)
    {
        float startVolume = musicSource.volume;
        float t = 0f;

        // Fade OUT
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / 0.5f);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade IN
        t = 0f;

        while (t < 1.5f)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t / 1.5f);
            yield return null;
        }

        musicSource.volume = musicVolume;
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }

    public void ToggleMusic()
    {
        musicEnabled = !musicEnabled;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (musicEnabled)
            PlayMusic(currentMusic == MusicType.None ? MusicType.Background : currentMusic);
        else
            fadeCoroutine = StartCoroutine(FadeOutMusic(1.5f));

        PlayerPrefs.SetInt(MUSIC_PREF, musicEnabled ? 1 : 0);
    }

    void LoadSettings()
    {
        musicEnabled = PlayerPrefs.GetInt(MUSIC_PREF, 1) == 1;
        filterFrequentSounds = PlayerPrefs.GetInt(FILTER_PREF, 1) == 1;
    }

    // ===================== UI =====================

    public void PlayClick() => PlaySFX(click);
    public void PlaySelect() => PlaySFX(select);
    public void PlayFail() => PlaySFX(fail);
    public void PlayMerge() => PlaySFX(merge);

    public void PlayApply()
    {
        if (!filterFrequentSounds)
        {
            PlaySFX(apply);
            return;
        }

        float time = Time.time;

        if (time - lastApplySoundTime < minApplyInterval) return;
        if (applySoundsThisFrame >= maxApplyPerFrame) return;

        PlaySFX(apply);
        lastApplySoundTime = time;
        applySoundsThisFrame++;
    }

    // ===================== GAMEPLAY =====================
    public void PlayPierce() => PlaySFX(pierce);
    public void PlaySpell() => PlaySFX(spell);
    public void PlayLaser() => PlaySFX(laser);
    public void PlayArrow() => PlaySFX(arrow);
    public void PlayAttack() => PlaySFX(attack);
    public void PlayHitEnemy() => PlaySFX(hitEnemy);
    public void PlayHitPlayer() => PlaySFX(hitPlayer);

    public void PlayKillEnemy()
    {
        if (!killEnemy) return;

        sfxSource.pitch = Random.Range(0.75f, 0.85f);
        sfxSource.PlayOneShot(killEnemy, sfxVolume * 1.15f);
        StartCoroutine(PlayKillFollowUp());
    }

    private IEnumerator PlayKillFollowUp()
    {
        yield return new WaitForSeconds(0.03f);
        sfxSource.pitch = Random.Range(0.95f, 1.05f);
        sfxSource.PlayOneShot(killEnemy, sfxVolume * 0.45f);
    }

    public void PlayDeathPlayer() => PlaySFX(hitPlayer);
    public void PlayDefeat() => PlaySFX(defeat);

    public void PlayVictory(AudioClip clip, System.Action onComplete = null)
    {
        musicSource.Pause();

        sfxSource.PlayOneShot(clip);

        StartCoroutine(VictoryRoutine(clip.length, onComplete));
    }

    private IEnumerator VictoryRoutine(float duration, System.Action onComplete)
    {
        yield return new WaitForSeconds(duration);

        onComplete?.Invoke();
    }

    // ===================== FILTERED =====================

    public void PlayPieceSpawn()
    {
        if (!filterFrequentSounds)
        {
            PlaySFX(pieceSpawn);
            return;
        }

        float time = Time.time;

        if (time - lastSpawnSoundTime < minSpawnInterval) return;
        if (spawnSoundsThisFrame >= maxSpawnsPerFrame) return;

        PlaySFX(pieceSpawn);
        lastSpawnSoundTime = time;
        spawnSoundsThisFrame++;
    }

    public void PlayMove()
    {
        if (!filterFrequentSounds)
        {
            PlaySFX(move);
            return;
        }

        float time = Time.time;

        if (time - lastMoveSoundTime < minMoveInterval) return;
        if (moveSoundsThisFrame >= maxMovesPerFrame) return;

        PlaySFX(move);
        lastMoveSoundTime = time;
        moveSoundsThisFrame++;
    }

    // ===================== CORE =====================

    public void PlaySFX(AudioClip clip)
    {
        if (!clip) return;

        sfxSource.pitch = Random.Range(0.95f, 1.05f);
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlaySFXAtPoint(AudioClip clip, Vector3 position)
    {
        if (!clip) return;
        AudioSource.PlayClipAtPoint(clip, position, sfxVolume);
    }

    // ===================== SETTINGS =====================

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }

    public void SetFilterFrequentSounds(bool enabled)
    {
        filterFrequentSounds = enabled;
        PlayerPrefs.SetInt(FILTER_PREF, enabled ? 1 : 0);
    }

    public void ToggleFilterFrequentSounds()
    {
        SetFilterFrequentSounds(!filterFrequentSounds);
    }
}
