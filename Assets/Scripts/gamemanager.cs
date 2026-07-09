using UnityEngine;
using TMPro;

public class gamemanager : MonoBehaviour
{
    public static gamemanager instance;                                      // allows other scripts to access the manager

    [SerializeField] TMP_Text killCountText;
    [SerializeField] GameObject menuLose;

    public bool isGameOver;
    public GameObject player;
    public playerHealth playerScript;

    int killCount;

    void Awake()
    {
        instance = this;                                                     // sets this object as the active manager before start

        player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            playerScript = player.GetComponent<playerHealth>();              // grabs the player health script for later use
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Time.timeScale = 1;
        isGameOver = false;
        updateKillUI();

        if (menuLose != null)
        {
            menuLose.SetActive(false);
        }
    }

    public void addKill()
    {
        killCount++;
        updateKillUI();                                                      // refreshes the kill text after the value changes
    }

    void updateKillUI()
    {
        if (killCountText != null)
        {
            killCountText.text = "Kills: " + killCount.ToString();
        }
    }

    public void youLose()
    {
        isGameOver = true;
        Time.timeScale = 0;                                                  // pauses the game when the player dies

        if (menuLose != null)
        {
            menuLose.SetActive(true);
        }
    }
}