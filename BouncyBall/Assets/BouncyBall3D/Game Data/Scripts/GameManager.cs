using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TTSDK.UNBridgeLib.LitJson;
using TTSDK;
using StarkSDKSpace;
using System.Collections.Generic;

public class GameManager : Singleton<GameManager>
{
    public int score = 0;
    public int bestScore = 0;
    public int star = 0;

    private int gameSpeed = 1;
    public float GameSpeed => ((gameSpeed - 1) * 0.2f) + 1;
    public GameState CurrentGameState => gameState;

    GameState gameState;
    Player player;
    float songProgress = 0;

    [Header("UI")]
    [SerializeField] Image levelProgress;
    [SerializeField] Text scoreText;
    [SerializeField] Animator scoreAnim;
    [SerializeField] Animator reviveAnim;
    [SerializeField] GameObject revivePanel, playButton;
    [Space]
    [SerializeField] TextMeshProUGUI songName;
    [SerializeField] Text levelScore;
    [SerializeField] Image[] stars = new Image[3];
    [SerializeField] Color activeStars, inactiveStars;
    public string clickid;
    private StarkAdManager starkAdManager;

    protected override void Awake()
    {
        base.Awake();

        player = FindObjectOfType<Player>();
        bestScore = PlayerPrefs.GetInt("bestScore", 0);

        if (ServicesManager.instance != null)
        {
            ServicesManager.instance.InitializeAdmob();
            ServicesManager.instance.InitializeUnityAds();
        }
    }

    private void Start()
    {
        scoreText.text = score.ToString();
        if (scoreAnim.isActiveAndEnabled)
            scoreAnim.SetTrigger("Up");
    }

    public void PlayerFailed()
    {
        if (ServicesManager.instance != null)
        {
            ServicesManager.instance.ShowInterstitialAdmob();
            ServicesManager.instance.ShowInterstitialUnityAds();
        }
        gameState = GameState.Lost;
        SoundManager.Instance.StopTrack();
        revivePanel.SetActive(true);
        UIManager.Instance.ShowHUD(false);

        if (LevelGenerator.Instance.currentSong.stars < star)
        {
            LevelGenerator.Instance.currentSong.stars = star;
            LevelGenerator.Instance.currentSong.SaveData();
        }

        if (score > bestScore)
        {
            PlayerPrefs.SetInt("bestScore", score);
        }
        ShowLevelProgress();

        PlayerPrefs.Save();
        ShowInterstitialAd("1lcaf5895d5l1293dc",
            () => {
                Debug.LogError("--插屏广告完成--");

            },
            (it, str) => {
                Debug.LogError("Error->" + str);
            });
    }

    void ShowLevelProgress()
    {
        songName.text = LevelGenerator.Instance.currentSong.name;
        levelScore.text = score.ToString();
        for (int i = 0; i < 3; i++)
        {
            if (i < star)
                stars[i].color = activeStars;
            else
                stars[i].color = inactiveStars;
        }
    }

    public void Revive()
    {
        ShowVideoAd("192if3b93qo6991ed0",
            (bol) => {
                if (bol)
                {
                    player.Revive();
                    revivePanel.SetActive(false);
                    playButton.SetActive(true);



                    clickid = "";
                    getClickid();
                    apiSend("game_addiction", clickid);
                    apiSend("lt_roi", clickid);


                }
                else
                {
                    StarkSDKSpace.AndroidUIManager.ShowToast("观看完整视频才能获取奖励哦！");
                }
            },
            (it, str) => {
                Debug.LogError("Error->" + str);
                //AndroidUIManager.ShowToast("广告加载异常，请重新看广告！");
            });
        
        //if (ServicesManager.instance != null)
        //{
        //    ServicesManager.instance.ShowRewardedVideoAdAdmob();
        //    ServicesManager.instance.ShowRewardedVideoUnityAds();
        //}
    }

    public void ReviveSucceed(bool completed)
    {
        if (completed)
        {
            player.Revive();
            revivePanel.SetActive(false);
            playButton.SetActive(true);
        }
    }

    public void StartGame()
    {
        if (CurrentGameState == GameState.Menu)
        {
            star = 0;
            gameState = GameState.Gameplay;
            player.StartMoving();
            SoundManager.Instance.PlayMusicFromBeat(player.platformHitCount);
        }
        else if (CurrentGameState == GameState.Lost)
        {
            gameState = GameState.Gameplay;
            player.StartMoving();
            SoundManager.Instance.PlayMusicFromBeat(player.platformHitCount);
        }
        UIManager.Instance.ShowHUD(true);
    }

    public void IncreaseGameSpeed()
    {
        if (gameSpeed < 5)
            gameSpeed++;
    }

    public void AddScore(bool perfect)
    {
        if (perfect)
            score += 10;
        else
            score += 5;

        scoreText.text = score.ToString();
        scoreAnim.SetTrigger("Up");
    }

    public void UpdateSongProgress(float value)
    {
        songProgress = value;
        levelProgress.fillAmount = Mathf.Lerp(levelProgress.fillAmount, value, 0.1f);
    }

    public void NoThanks()
    {
        reviveAnim.SetTrigger("No");
        
        if (ServicesManager.instance != null)
        {
            ServicesManager.instance.ShowInterstitialAdmob();
            ServicesManager.instance.ShowInterstitialUnityAds();
        }
        
    }

    public void Menu()
    {
        SceneManager.LoadScene(0);
    }
    public void getClickid()
    {
        var launchOpt = StarkSDK.API.GetLaunchOptionsSync();
        if (launchOpt.Query != null)
        {
            foreach (KeyValuePair<string, string> kv in launchOpt.Query)
                if (kv.Value != null)
                {
                    Debug.Log(kv.Key + "<-参数-> " + kv.Value);
                    if (kv.Key.ToString() == "clickid")
                    {
                        clickid = kv.Value.ToString();
                    }
                }
                else
                {
                    Debug.Log(kv.Key + "<-参数-> " + "null ");
                }
        }
    }

    public void apiSend(string eventname, string clickid)
    {
        TTRequest.InnerOptions options = new TTRequest.InnerOptions();
        options.Header["content-type"] = "application/json";
        options.Method = "POST";

        JsonData data1 = new JsonData();

        data1["event_type"] = eventname;
        data1["context"] = new JsonData();
        data1["context"]["ad"] = new JsonData();
        data1["context"]["ad"]["callback"] = clickid;

        Debug.Log("<-data1-> " + data1.ToJson());

        options.Data = data1.ToJson();

        TT.Request("https://analytics.oceanengine.com/api/v2/conversion", options,
           response => { Debug.Log(response); },
           response => { Debug.Log(response); });
    }


    /// <summary>
    /// </summary>
    /// <param name="adId"></param>
    /// <param name="closeCallBack"></param>
    /// <param name="errorCallBack"></param>
    public void ShowVideoAd(string adId, System.Action<bool> closeCallBack, System.Action<int, string> errorCallBack)
    {
        starkAdManager = StarkSDK.API.GetStarkAdManager();
        if (starkAdManager != null)
        {
            starkAdManager.ShowVideoAdWithId(adId, closeCallBack, errorCallBack);
        }
    }
    /// <summary>
    /// 播放插屏广告
    /// </summary>
    /// <param name="adId"></param>
    /// <param name="errorCallBack"></param>
    /// <param name="closeCallBack"></param>
    public void ShowInterstitialAd(string adId, System.Action closeCallBack, System.Action<int, string> errorCallBack)
    {
        starkAdManager = StarkSDK.API.GetStarkAdManager();
        if (starkAdManager != null)
        {
            var mInterstitialAd = starkAdManager.CreateInterstitialAd(adId, errorCallBack, closeCallBack);
            mInterstitialAd.Load();
            mInterstitialAd.Show();
        }
    }
}
