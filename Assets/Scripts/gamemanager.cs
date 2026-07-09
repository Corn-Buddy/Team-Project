using UnityEngine;
using TMPro;

public class gamemanager : MonoBehaviour
{
    public static gamemanager instance;                                      // allows other scripts to access the manager

    [SerializeField] TMP_Text killCountText;
    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuLose;

    public bool isPaused;
    public bool isGameOver;
    public GameObject player;
    public playerHealth playerScript;

    float timeScaleOrig;
    int killCount;

    void Awake()
    {
        instance = this;                                                     // sets this object as the active manager before start

        timeScaleOrig = Time.timeScale;
        player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            playerScript = player.GetComponent<playerHealth>();              // grabs the player health script for later use
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Time.timeScale = timeScaleOrig;
        isPaused = false;
        isGameOver = false;
        updateKillUI();

        if (menuPause != null)
        {
            menuPause.SetActive(false);
        }

        if (menuLose != null)
        {
            menuLose.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Cancel") && !isGameOver)
        {
            if (menuActive == null)
            {
                statePause();
            }
            else if (menuActive == menuPause)
            {
                stateUnpause();
            }
        }
    }

    public void statePause()
    {
        isPaused = true;
        Time.timeScale = 0;                                                  // freezes gameplay while paused

        menuActive = menuPause;

        if (menuActive != null)
        {
            menuActive.SetActive(true);
        }
    }

    public void stateUnpause()
    {
        isPaused = false;
        Time.timeScale = timeScaleOrig;

        if (menuActive != null)
        {
            menuActive.SetActive(false);
        }

        menuActive = null;
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
        isPaused = true;
        Time.timeScale = 0;                                                  // pauses the game when the player dies

        if (menuActive != null)
        {
            menuActive.SetActive(false);
        }

        menuActive = menuLose;

        if (menuActive != null)
        {
            menuActive.SetActive(true);
        }
    }
}