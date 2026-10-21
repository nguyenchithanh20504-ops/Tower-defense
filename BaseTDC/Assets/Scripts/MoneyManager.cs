
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoneyManager : MonoBehaviour
{
    [SerializeField] private Text playerMoneyTxt;
    [SerializeField] private Text playerScoreTxt;   //For player score on GO screen

    public static MoneyManager main;

    private static int currPlayerMoney;
    public int startMoney;

    private void Start()
    {
        if (main == null) main = this;
        currPlayerMoney = startMoney;
    }

    public int GetCurrMoney()
    {
        return currPlayerMoney;
    }

    //Modified to change and print out the players wood when added to
    //Modified to show player score on game over screen
    public void addMoney(int amount)
    {
        currPlayerMoney += amount;
        playerMoneyTxt.text = $"Wood: {currPlayerMoney}";
        playerScoreTxt.text = $"Score: {currPlayerMoney}";
    }

    //Modified to change and print out the players wood when taken away
    //Modified to show player score on game over screen
    public void removeMoney(int amount)
    {
        currPlayerMoney -= amount;
        playerMoneyTxt.text = $"Wood: {currPlayerMoney}";
        playerScoreTxt.text = $"Score: {currPlayerMoney}";
    }
}
