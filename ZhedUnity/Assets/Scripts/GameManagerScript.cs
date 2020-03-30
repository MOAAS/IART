﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

using UnityEngine;

using ZhedSolver;
using System;
using System.Threading.Tasks;

public class GameManagerScript : MonoBehaviour
{

    public GameObject floatingPreviewPrefab;
    public GameObject emptyTilePrefab;
    public GameObject valueTilePrefab;
    public GameObject usedTilePrefab;
    public GameObject finishTilePrefab;

    // UI
    public GameObject titleScreen;
    public GameObject gameScreen;
    public GameObject youWin;
    public GameObject youLose;

    private GameObject board;
    private ZhedBoard zhedBoard;

    private TileController selectedTile;
    private float selectTime;

    private bool gameOver;


    private const float SPAWN_DELAY_SECS = 0.1f;
    private const float SOLVER_DELAY_SECS = 0.8f;


    private Dictionary<String, GameObject> finishTiles;
    private Dictionary<String, GameObject> valueTiles;

    // Start is called before the first frame update
    void Start() {
    }

    public void RestartGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Hint() {
        if (this.zhedBoard.GetValueTiles().Count == 0)
            return;
        Solver solver = new Solver(this.zhedBoard);
        ZhedStep step = solver.GetHint();
        if (step == null)
            return;
        StartCoroutine(this.PlayZhedStep(step, 0));
    }

    public void Solve() {        
        Solver solver = new Solver(this.zhedBoard);

        var task = Task.Run(() => solver.Solve(SearchMethod.Greedy));
        if (task.Wait(TimeSpan.FromSeconds(10))) {
            List<ZhedStep> zhedSteps = solver.Solve(SearchMethod.Greedy);
            for (int i = 0; i < zhedSteps.Count; i++)
                StartCoroutine(this.PlayZhedStep(zhedSteps[i], i * SOLVER_DELAY_SECS));
        }
    }
    
    private IEnumerator PlayZhedStep(ZhedStep step, float delay) {
        yield return new WaitForSeconds(delay);
        TileController tile = this.valueTiles[step.coords.x + ":" + step.coords.y].GetComponent<TileController>();

        switch(step.operations) {
            case Operations.MoveUp: Play(tile, Coords.MoveUp); break;
            case Operations.MoveDown: Play(tile, Coords.MoveDown); break;
            case Operations.MoveLeft: Play(tile, Coords.MoveLeft); break;
            case Operations.MoveRight: Play(tile, Coords.MoveRight); break;
        }
    }

    public void LoadLevel(String path) {
        // Update UI
        gameOver = false;
        titleScreen.SetActive(false);
        gameScreen.SetActive(true);
        if (board != null)
            Destroy(board);        


        // Load file
        valueTiles = new Dictionary<String, GameObject>();
        finishTiles = new Dictionary<String, GameObject>();

        zhedBoard = new ZhedBoard(path);
        board = new GameObject("Board");


        for (int y = 0; y < zhedBoard.height; y++) {
            for (int x = 0; x < zhedBoard.width; x++) {
                MakeTile(new Coords(x, y), emptyTilePrefab, Color.white);
            }
        }        

        foreach (int[] tile in zhedBoard.GetValueTiles()) {
            Coords coords = new Coords(tile[0], tile[1]);
            GameObject valueTileObject = MakeTile(coords, valueTilePrefab, BoardTheme.idleColor);
            valueTileObject.GetComponent<TileController>().SetTileInfo(coords, tile[2]);
            valueTiles.Add(coords.x + ":" + coords.y, valueTileObject);
        }

        foreach (int[] tile in zhedBoard.GetFinishTiles()) {
            Coords coords = new Coords(tile[0], tile[1]);
            finishTiles.Add(coords.x + ":" + coords.y, MakeTile(coords, finishTilePrefab, Color.white));
        }

        // Update Camera
        GameObject.Find("Main Camera").transform.position = new Vector3(0, zhedBoard.height, -zhedBoard.height / 5.0f);
    }

    Vector3 TilePos(Coords coords) {
        return new Vector3(coords.x + 0.5f - zhedBoard.width / 2.0f, 0, zhedBoard.height / 2.0f - coords.y - 0.5f);
    }

    GameObject MakeTile(Coords coords, GameObject prefab, Color color) {
        GameObject gameObject = Instantiate(prefab, TilePos(coords), prefab.transform.rotation, board.transform);
        gameObject.GetComponentInChildren<Renderer>().material.color = color;
        return gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (this.selectedTile == null || Time.time < this.selectTime + 0.1f)
            return;

        if (Input.GetMouseButtonDown(0)) {
            Vector3 tilePos = Camera.main.WorldToScreenPoint(selectedTile.gameObject.transform.position);
            Vector3 mousePos = Input.mousePosition;

            Vector2 diffVec = new Vector2(mousePos.x - tilePos.x, mousePos.y - tilePos.y);

            float angle = Vector2.Angle(diffVec, Vector2.right);
            angle = Mathf.Sign(Vector3.Cross(diffVec, Vector2.right).z) > 0 ? (360 - angle) % 360 : angle;

            if (angle < 45 || angle >= 315)
                this.Play(this.selectedTile, Coords.MoveRight);
            else if (angle < 135)
                this.Play(this.selectedTile, Coords.MoveUp);
            else if (angle < 225)
                this.Play(this.selectedTile, Coords.MoveLeft);
            else this.Play(this.selectedTile, Coords.MoveDown);

            DestroyFloatingPreviews();
        }
    }


    void DestroyFloatingPreviews() {
        foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("FloatingPreview"))
            Destroy(gameObject);
    }

    public void OnPieceSelected(TileController tile) {
        DestroyFloatingPreviews();

        if (this.selectedTile != null)
            this.selectedTile.Select(false);

        if (this.selectedTile == tile) {      
            this.selectedTile = null;
        }
        else {
            this.selectedTile = tile;
            this.selectTime = Time.time;
            this.selectedTile.Select(true);

            List<Coords> coordsUp = GetSpreadInDir(tile.coords, Coords.MoveUp);
            List<Coords> coordsDown = GetSpreadInDir(tile.coords, Coords.MoveDown);
            List<Coords> coordsLeft = GetSpreadInDir(tile.coords, Coords.MoveLeft);
            List<Coords> coordsRight = GetSpreadInDir(tile.coords, Coords.MoveRight);
            for (int i = 0; i < coordsUp.Count; i++) StartCoroutine(MakePreviewTile(coordsUp[i], i * SPAWN_DELAY_SECS));
            for (int i = 0; i < coordsDown.Count; i++) StartCoroutine(MakePreviewTile(coordsDown[i], i * SPAWN_DELAY_SECS));
            for (int i = 0; i < coordsLeft.Count; i++) StartCoroutine(MakePreviewTile(coordsLeft[i], i * SPAWN_DELAY_SECS));
            for (int i = 0; i < coordsRight.Count; i++) StartCoroutine(MakePreviewTile(coordsRight[i], i * SPAWN_DELAY_SECS));
        }
    }


    private void Play(TileController tile, Func<Coords, Coords> moveFunction) {  
        if (gameOver)
            return;

        Destroy(tile.gameObject);        
        StartCoroutine(MakeUsedTile(tile.coords, 0));

        Coords coords = tile.coords;
        for (int tileValue = tile.tileValue, numUsedTiles = 1; tileValue > 0; tileValue--, numUsedTiles++) {
            coords = moveFunction(coords);
            if(!zhedBoard.inbounds(coords))
                break;
            switch (zhedBoard.TileValue(coords)) {
                case ZhedBoard.EMPTY_TILE: StartCoroutine(MakeUsedTile(coords, numUsedTiles * SPAWN_DELAY_SECS)); break;
                case ZhedBoard.FINISH_TILE: StartCoroutine(MakeWinnerTile(coords, numUsedTiles * SPAWN_DELAY_SECS)); break;
                default: tileValue++; numUsedTiles--; break;
            }
        }

        this.zhedBoard = ZhedBoard.SpreadTile(this.zhedBoard, tile.coords, moveFunction);


        if (this.zhedBoard.isOver) {
            youWin.SetActive(true);
            gameOver = true;
        }
        else if (this.zhedBoard.GetValueTiles().Count == 0) {
            youLose.SetActive(true);
            gameOver = true;
        }
    }

    private IEnumerator MakeUsedTile(Coords coords, float delay) {
        yield return new WaitForSeconds(delay);
        MakeTile(coords, usedTilePrefab, BoardTheme.idleColor);
    }

    private IEnumerator MakePreviewTile(Coords coords, float delay) {
        yield return new WaitForSeconds(delay);
        MakeTile(coords, floatingPreviewPrefab, BoardTheme.selectedColor);          
    }

    private IEnumerator MakeWinnerTile(Coords coords, float delay) {
        yield return new WaitForSeconds(delay);
        Destroy(finishTiles[coords.x + ":" + coords.y]);
        MakeTile(coords, finishTilePrefab, BoardTheme.selectedColor);          
    }

// ** Devia tar no zhedboard mas n queria tar  a por mais memoria  **// 

    public List<Coords> GetTotalSpread(Coords coords) {
        List<Coords> totalSpread = new List<Coords>();
        totalSpread.AddRange(GetSpreadInDir(coords, Coords.MoveUp));
        totalSpread.AddRange(GetSpreadInDir(coords, Coords.MoveDown));
        totalSpread.AddRange(GetSpreadInDir(coords, Coords.MoveLeft));
        totalSpread.AddRange(GetSpreadInDir(coords, Coords.MoveRight));
        return totalSpread;        
    }

    public List<Coords> GetSpreadInDir(Coords coords, Func<Coords, Coords> moveFunction) {
        int tileValue = zhedBoard.TileValue(coords);
        List<Coords> coordList = new List<Coords>();
        for (int i = 0; i < tileValue; i++) {
            coords = moveFunction(coords);
            if(!zhedBoard.inbounds(coords))
                break;
            switch (zhedBoard.TileValue(coords)) {
                case ZhedBoard.EMPTY_TILE: coordList.Add(coords); break;
                case ZhedBoard.FINISH_TILE: coordList.Add(coords); break;
                default: i--; break;
            }
        }
        return coordList;
    }
}