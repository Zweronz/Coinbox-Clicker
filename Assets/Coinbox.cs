using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

public class Coinbox : MonoBehaviour
{
    public Coin[] coins;

    public MultiClickerUpgrade[] multiClickerUpgrades;

    public CoinTypesUpgrade[] coinTypesUpgrades;

    public AutoClickerUpgrade[] autoClickerUpgrades;

    public AudioClip click, coin;

    public Sprite soundOn, soundOff;

    public Sprite[] numberSprites;

    public UnityEngine.UI.Image[] numbers;

    public CoinPercentage[] currentPercentages, nextPercentages;

    public UnityEngine.UI.Image soundToggle;

    public GameObject store, storePopup, percentages, buyButton;

    public RectTransform coinBox;

    public TextMeshProUGUI popupText, popupPrice;

    public GameObject resetPopup, creditsPopup;

    public GameObject templateCoin, coinStackBackground, coinStackForeground;
    
    private AudioSource source;

    private Save save;

    private WeightedList<int> currentCoins = new WeightedList<int>();

    private int buyState;

    private bool playingBoxAnimation;

    private float builtUpCps;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        string savePath = Path.Combine(Application.persistentDataPath, "game.save");
        
        if (!File.Exists(savePath))
        {
            File.WriteAllText(savePath,  JsonConvert.SerializeObject(save = new Save()));
        }
        else
        {
            save = JsonConvert.DeserializeObject<Save>(File.ReadAllText(savePath));
        }

        RefreshCoinList();
        RefreshCounter();

        soundToggle.sprite = save.sound ? soundOn : soundOff;

        StartCoroutine(SaveLoop());
    }

    private void Update()
    {
        if (autoClickerUpgrades[save.autoClickerLevel].coins > 0)
        {
            bool canCollect = builtUpCps >= 1f;

            while (builtUpCps >= 1f)
            {
                builtUpCps -= 1f;
                CollectCoin();
            }

            if (canCollect)
            {
                RefreshCounter();
            }

            builtUpCps += Time.deltaTime * autoClickerUpgrades[save.autoClickerLevel].coins;
        }
    }

    private void OnApplicationQuit() => Save();
    private void OnApplicationPause() => Save();
    private void OnApplicationFocus() => Save();

    private IEnumerator SaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f);
            Save();
        }
    }

    private void Save()
    {
        File.WriteAllText(Path.Combine(Application.persistentDataPath, "game.save"),  JsonConvert.SerializeObject(save));
    }

    private void RefreshCoinList()
    {
        currentCoins.Clear();

        foreach (CoinInstance instance in coinTypesUpgrades[save.coinTypeLevel].instances)
        {
            currentCoins.Add(instance.coin, instance.percentage);
        }
    }

    private void RefreshCounter()
    {
        string coinString = save.coins.ToString();

        foreach (UnityEngine.UI.Image number in numbers)
        {
            number.sprite = numberSprites[0];
        }

        int numberIndex = coinString.Length - 1;

        for (int i = 0; i < coinString.Length; i++)
        {
            numbers[numberIndex--].sprite = numberSprites[int.Parse(coinString[i] + "")];
        } 
    }

    private bool TryBuy(long price)
    {
        if (save.coins < price)
        {
            return false;
        }

        save.coins -= price;
        RefreshCounter();

        Save();

        return true;
    }

    private void StorePopup()
    {
        storePopup.SetActive(true);
        store.SetActive(false);

        switch (buyState)
        {
            //multi click
            case 0:
            {
                percentages.SetActive(false);

                if (save.multiClickLevel < multiClickerUpgrades.Length - 1)
                {
                    buyButton.SetActive(true);

                    popupText.text =
@$"Current level:
{multiClickerUpgrades[save.multiClickLevel].clicks} coins per click
Next level:
{multiClickerUpgrades[save.multiClickLevel + 1].clicks} coins per click
Upgrade cost:";

                    popupPrice.text = "$" + multiClickerUpgrades[save.multiClickLevel + 1].cost.ToString("N0");
                }
                else
                {
                    buyButton.SetActive(false);

                    popupText.text =
@$"Current level:
{multiClickerUpgrades[save.multiClickLevel].clicks} coins per click

Max level achieved!";

                    popupPrice.text = "";
                }

                break;
            }

            //coin type
            case 1:
            {
                percentages.SetActive(true);

                if (save.coinTypeLevel < coinTypesUpgrades.Length - 1)
                {
                    buyButton.SetActive(true);

                    for (int i = 0; i < currentPercentages.Length; i++)
                    {
                        if (i < coinTypesUpgrades[save.coinTypeLevel].instances.Length)
                        {
                            currentPercentages[i].coin.transform.parent.gameObject.SetActive(true);

                            currentPercentages[i].coin.texture = coins[coinTypesUpgrades[save.coinTypeLevel].instances[i].coin].sprite;
                            currentPercentages[i].text.text = "%" + coinTypesUpgrades[save.coinTypeLevel].instances[i].percentage;
                        }
                        else
                        {
                            currentPercentages[i].coin.transform.parent.gameObject.SetActive(false);
                        }
                    }

                    for (int i = 0; i < nextPercentages.Length; i++)
                    {
                        if (i < coinTypesUpgrades[save.coinTypeLevel + 1].instances.Length)
                        {
                            nextPercentages[i].coin.transform.parent.gameObject.SetActive(true);

                            nextPercentages[i].coin.texture = coins[coinTypesUpgrades[save.coinTypeLevel + 1].instances[i].coin].sprite;
                            nextPercentages[i].text.text = "%" + coinTypesUpgrades[save.coinTypeLevel + 1].instances[i].percentage;
                        }
                        else
                        {
                            nextPercentages[i].coin.transform.parent.gameObject.SetActive(false);
                        }
                    }

                    popupText.text =
@$"Current level:

Next level:

Upgrade cost:";

                    popupPrice.text = "$" + coinTypesUpgrades[save.coinTypeLevel + 1].cost.ToString("N0");
                }
                else
                {
                    buyButton.SetActive(false);

                    for (int i = 0; i < currentPercentages.Length; i++)
                    {
                        if (i < coinTypesUpgrades[save.coinTypeLevel].instances.Length)
                        {
                            currentPercentages[i].coin.transform.parent.gameObject.SetActive(true);

                            currentPercentages[i].coin.texture = coins[coinTypesUpgrades[save.coinTypeLevel].instances[i].coin].sprite;
                            currentPercentages[i].text.text = "%" + coinTypesUpgrades[save.coinTypeLevel].instances[i].percentage;
                        }
                        else
                        {
                            currentPercentages[i].coin.transform.parent.gameObject.SetActive(false);
                        }
                    }

                    for (int i = 0; i < nextPercentages.Length; i++)
                    {
                        nextPercentages[i].coin.transform.parent.gameObject.SetActive(false);
                    }

                    popupText.text =
@$"Current level:

Max level achieved!";

                    popupPrice.text = "";
                }

                break;
            }

            //auto clicker
            case 2:
            {
                percentages.SetActive(false);

                if (save.autoClickerLevel < autoClickerUpgrades.Length - 1)
                {
                    buyButton.SetActive(true);

                    popupText.text =
@$"Current level:
{autoClickerUpgrades[save.autoClickerLevel].coins} coins per sec
Next level:
{autoClickerUpgrades[save.autoClickerLevel + 1].coins} coins per sec
Upgrade cost:";

                    popupPrice.text = "$" + autoClickerUpgrades[save.autoClickerLevel + 1].cost.ToString("N0");
                }
                else
                {
                    buyButton.SetActive(false);

                    popupText.text =
@$"Current level:
{autoClickerUpgrades[save.autoClickerLevel].coins} coins per sec

Max level achieved!";

                    popupPrice.text = "";
                }

                break;
            }
        }
    }

    public void ClickCoinbox()
    {
        if (save.sound)
        {
            source.PlayOneShot(coin);
        }

        if (!playingBoxAnimation)
        {
            playingBoxAnimation = true;
            coinBox.localPosition += Vector3.up * (coinBox.rect.height / 32f);

            Invoke("ResetBoxPosition", 0.1f);
        }

        for (int i = 0; i < multiClickerUpgrades[save.multiClickLevel].clicks; i++)
        {
            CollectCoin();
        }

        RefreshCounter();
    }

    private void ResetBoxPosition()
    {
        coinBox.localPosition = Vector3.zero;
        playingBoxAnimation = false;
    }

    public void CollectCoin()
    {
        Coin coin = coins[currentCoins.Next()];
        save.coins += coin.value;

        GameObject newCoin = Instantiate(templateCoin);
        newCoin.GetComponent<UnityEngine.UI.RawImage>().texture = coin.sprite;

        RectTransform transform = newCoin.GetComponent<RectTransform>();
        transform.parent = UnityEngine.Random.Range(0, 2) == 1 ? coinStackForeground.transform : coinStackBackground.transform;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        
        newCoin.SetActive(true);
        newCoin.GetComponent<Rigidbody2D>().AddForce(new Vector2(UnityEngine.Random.Range(-20000f, 20000f), 35000f));

        Destroy(newCoin, 3f);
    }

    //button methods
    public void ButtonSound()
    {
        if (save.sound)
        {
            source.PlayOneShot(click);
        }
    }

    public void Store()
    {
        if (storePopup.activeInHierarchy || resetPopup.activeInHierarchy || creditsPopup.activeInHierarchy)
        {
            store.SetActive(false);
            
            storePopup.SetActive(false);
            resetPopup.SetActive(false);
            creditsPopup.SetActive(false);
        }
        else
        {
            store.SetActive(!store.activeInHierarchy);
        }
    }

    public void ToggleSound()
    {
        save.sound = !save.sound;
        soundToggle.sprite = save.sound ? soundOn : soundOff;
    }

    public void MultiClicker()
    {
        buyState = 0;
        StorePopup();
    }
    
    public void CoinTypes()
    {
        buyState = 1;
        StorePopup();
    }

    public void AutoClicker()
    {
        buyState = 2;
        StorePopup();
    }

    public void Credits()
    {
        store.SetActive(false);
        creditsPopup.SetActive(true);
    }

    public void ResetData()
    {
        store.SetActive(false);
        resetPopup.SetActive(true);
    }

    public void ResetYes()
    {
        save = new Save()
        {
            sound = save.sound
        };

        resetPopup.SetActive(false);
        store.SetActive(false);

        RefreshCounter();
        RefreshCoinList();
    }

    public void ResetNo()
    {
        store.SetActive(true);
        resetPopup.SetActive(false);
    }

    public void Back()
    {
        storePopup.SetActive(false);
        store.SetActive(true);
    }

    public void CreditsBack()
    {
        store.SetActive(true);
        creditsPopup.SetActive(false);
    }

    public void CreditsVideo()
    {
        Debug.LogError("sorry, chief. haven't really made that video yet.");
        CreditsBack();
    }

    public void DecompAndRecompButton()
    {
        Debug.LogError("haven't made coinbox clicker 2 yet either, pal.");
    }

    public void Buy()
    {
        switch (buyState)
        {
            case 0:
            {
                if (TryBuy(multiClickerUpgrades[save.multiClickLevel + 1].cost))
                {
                    save.multiClickLevel++;
                    ButtonSound();

                    StorePopup();
                }

                break;
            }

            case 1:
            {
                if (TryBuy(coinTypesUpgrades[save.coinTypeLevel + 1].cost))
                {
                    save.coinTypeLevel++;
                    ButtonSound();

                    StorePopup();
                    RefreshCoinList();
                }

                break;
            }

            case 2:
            {
                if (TryBuy(autoClickerUpgrades[save.autoClickerLevel + 1].cost))
                {
                    save.autoClickerLevel++;
                    ButtonSound();

                    StorePopup();
                }

                break;
            }
        }
    }
}

[Serializable]
public class Coin
{
    public Texture2D sprite;

    public long value;
}

[Serializable]
public class Upgrade
{
    public long cost;
}

[Serializable]
public class MultiClickerUpgrade : Upgrade
{
    public int clicks;
}

[Serializable]
public class CoinTypesUpgrade : Upgrade
{
    public CoinInstance[] instances;
}

[Serializable]
public class AutoClickerUpgrade : Upgrade
{
    public int coins;
}

[Serializable]
public class CoinInstance
{
    public int coin, percentage;
}

[Serializable]
public class CoinPercentage
{
    public UnityEngine.UI.RawImage coin;

    public TextMeshProUGUI text;
}

public class Save
{
    public Save()
    {
        sound = true;
    }

    public long coins;

    public int multiClickLevel, coinTypeLevel, autoClickerLevel;

    public bool sound;
}