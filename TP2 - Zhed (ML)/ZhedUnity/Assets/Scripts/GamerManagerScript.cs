﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GamerManagerScript : MonoBehaviour
{
    public GameObject titleScreen;
    public GameObject gameScreen;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RestartGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadLevel(String path) {
        titleScreen.SetActive(false);
        gameScreen.SetActive(true);

        foreach (GameObject obj in GetGameManagers()) {
            obj.GetComponent<GameManagerScript>().LoadLevel(path);
        }

    }

    public void ToggleGraphics() {
        foreach (GameObject obj in GetGameManagers()) {
            obj.GetComponent<GameManagerScript>().ToggleCoolGraphics();
        }
    }

    private GameObject[] GetGameManagers() {
        return GameObject.FindGameObjectsWithTag("GameManager");
    }
}
