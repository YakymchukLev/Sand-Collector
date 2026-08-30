using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.LevelPlay;
using System;

public class LevelPlayAdsManager : MonoBehaviour
{
    // === СИНГЛТОН (щоб викликати з інших скриптів) ===
    public static LevelPlayAdsManager Instance;

    [Header("Ключі з панелі керування ironSource")]
    public string appKey = "YOUR_APP_KEY_HERE";
    public string interstitialAdUnitId = "YOUR_INTERSTITIAL_AD_UNIT_ID"; 
    public string rewardedAdUnitId = "YOUR_REWARDED_AD_UNIT_ID";
    public string bannerAdUnitId = "YOUR_BANNER_AD_UNIT_ID";

    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayBannerAd bannerAd;
    private Action onRewardedCallback;
    private Action onClosedCallback;
    private bool rewardEarned = false;

    private void Awake()
    {
        // Налаштування Синглтона
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Menu")
        {
            HideBanner();
        }
        else
        {
            ShowBanner();
        }
    }

    void Start()
    {
        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;

        LevelPlay.SetMetaData("is_test_suite", "enable");
        LevelPlay.Init(appKey);
    }

    private void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay SDK успішно ініціалізовано!");
        LevelPlay.LaunchTestSuite();
        LoadInterstitial();
        LoadRewarded();
        LoadBanner();
    }

    private void SdkInitializationFailedEvent(LevelPlayInitError error)
    {
        Debug.LogError($"Помилка ініціалізації LevelPlay: {error.ErrorMessage}");
    }

    // ================= МІЖСТОРІНКОВА РЕКЛАМА (INTERSTITIAL) =================
    public void LoadInterstitial()
    {
        interstitialAd = new LevelPlayInterstitialAd(interstitialAdUnitId);
        interstitialAd.OnAdLoaded += (adInfo) => Debug.Log("Interstitial завантажено");
        interstitialAd.OnAdLoadFailed += (error) => Debug.Log("Помилка завантаження Interstitial: " + error.ErrorMessage);
        
        interstitialAd.OnAdClosed += (adInfo) => {
            Debug.Log("Interstitial закрито");
            LevelPlay.LaunchTestSuite();
        LoadInterstitial(); 
        };

        interstitialAd.LoadAd();
    }

    public void ShowInterstitial()
    {
        if (interstitialAd != null && interstitialAd.IsAdReady())
        {
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.Log("Interstitial ще не готовий до показу");
        }
    }

    // ================= РЕКЛАМА З ВИНАГОРОДОЮ (REWARDED) =================
    public void LoadRewarded()
    {
        rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);
        rewardedAd.OnAdLoaded += (adInfo) => Debug.Log("Rewarded завантажено");
        rewardedAd.OnAdLoadFailed += (error) => Debug.Log("Помилка завантаження Rewarded: " + error.ErrorMessage);
        
        rewardedAd.OnAdRewarded += (adInfo, reward) => {
            Debug.Log($"Гравець отримав нагороду: {reward.Name} ({reward.Amount})");
            rewardEarned = true; // Зберігаємо факт отримання нагороди
        };
        
        rewardedAd.OnAdClosed += (adInfo) => {
            Debug.Log("Rewarded закрито");
            
            if (rewardEarned && onRewardedCallback != null)
            {
                onRewardedCallback.Invoke();
            }
            
            if (onClosedCallback != null)
            {
                onClosedCallback.Invoke();
            }

            onRewardedCallback = null;
            onClosedCallback = null;
            rewardEarned = false;
            LoadRewarded(); 
        };

        rewardedAd.LoadAd();
    }

    public bool IsRewardedReady()
    {
        return rewardedAd != null && rewardedAd.IsAdReady();
    }

    public void ShowRewardedAd(Action onSuccess = null, Action onClosed = null)
    {
        if (IsRewardedReady())
        {
            onRewardedCallback = onSuccess;
            onClosedCallback = onClosed;
            rewardEarned = false; // Скидаємо перед показом
            rewardedAd.ShowAd();
        }
        else
        {
            Debug.Log("Rewarded Ad ще не готовий до показу");
        }
    }

    // ================= БАНЕРНА РЕКЛАМА (BANNER) =================
    public void LoadBanner()
    {
        var configBuilder = new LevelPlayBannerAd.Config.Builder();
        configBuilder.SetSize(LevelPlayAdSize.BANNER);
        configBuilder.SetPosition(LevelPlayBannerPosition.BottomCenter);
        var bannerConfig = configBuilder.Build();
        
        bannerAd = new LevelPlayBannerAd(bannerAdUnitId, bannerConfig);

        bannerAd.OnAdLoaded += (adInfo) => {
            Debug.Log("Banner завантажено");
            
            // Перевіряємо, в якій ми зараз сцені
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                HideBanner();
            }
            else
            {
                ShowBanner();
            }
        };
        bannerAd.OnAdLoadFailed += (error) => Debug.Log("Помилка завантаження Banner: " + error.ErrorMessage);

        bannerAd.LoadAd();
    }

    public void ShowBanner()
    {
        if (bannerAd != null)
        {
            bannerAd.ShowAd();
        }
    }

    public void HideBanner()
    {
        if (bannerAd != null)
        {
            bannerAd.HideAd();
        }
    }

    public void DestroyBanner()
    {
        if (bannerAd != null)
        {
            bannerAd.DestroyAd();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        LevelPlay.OnInitSuccess -= SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed -= SdkInitializationFailedEvent;
        DestroyBanner();
    }
}


