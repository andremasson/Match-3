/*
 * Copyright (c) 2017 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour {
    // Public
    public static BoardManager instance;
    public List<Sprite> characters = new List<Sprite>();
    public GameObject tileBackground;
    public GameObject tile;
    public GameObject tileSpawner;
    public GameObject boundery;
    public int xSize, ySize;

    public bool IsShifting { get; set; }
    public float tileMoveSpeed = .3f;
    public float tilePlungeSpeed = .1f;
    public float deleteHighlightTime = 0.3f;
    public bool autoMatch = true;

    public int deletedCount = 0;
    public int movingCount = 0;
    public LayerMask maskTile = 1 << 8;

    public Text texto;

    // Private
    public GameObject[,] tiles;
    public Vector3[,] positions;
    public GameObject[] spawners;

    private Vector2 touchOrigin;
    private GameObject selectedTile;

    private Vector2 directionAxis = Vector2.zero;
    private Collider2D touchCollider;
    private Vector3 worldPosition;

    private int missCount = 0;

    private enum CellType { Invalid = -1, Valid = -2, Spawn = -3, Random = -4 };

    private void Awake()
    {
        instance = GetComponent<BoardManager>();
    }

    void Start()
    {
        Vector2 offset = tile.GetComponent<SpriteRenderer>().bounds.size;
        CreateBoard(offset.x, offset.y);
        selectedTile = null;
        missCount = 0;
    }

    private void CreateBoard(float xOffset, float yOffset)
    {
        //CreateCellGrid(xOffset, yOffset);
        CreateTiles(xOffset, yOffset);

        GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
        ShuffleTiles(tiles);
    }

    private void CreateTiles(float xOffset, float yOffset)
    {
        Vector3 startPos = new Vector3
        {
            x = transform.position.x - xSize / 2.0f * xOffset + xOffset / 2.0f,
            y = yOffset * ySize / 2.0f + transform.position.y
        };
        Vector3 currentPosition = startPos;
        positions = new Vector3[xSize, ySize];
        tiles = new GameObject[xSize, ySize];
        spawners = new GameObject[ySize];
        GameObject newTile;
        //GameObject newBorder;
        GameObject newSpawner;

        tiles = new GameObject[xSize, ySize];

        for (int x = 0; x < xSize; x++)
        {
            newSpawner = Instantiate(tileSpawner, new Vector3(currentPosition.x, currentPosition.y + yOffset, 0), tileSpawner.transform.rotation, transform);
            spawners[x] = newSpawner;

            for (int y = 0; y < ySize; y++)
            {
                newTile = Instantiate(tile, currentPosition, tile.transform.rotation, transform);
                newTile.GetComponent<Tile>().positionInArray = new Tile.IntVector2(x, y);
                newTile.GetComponent<Tile>().fixedPosition = currentPosition;
                tiles[x, y] = newTile;
                positions[x, y] = currentPosition;
                currentPosition.y -= yOffset;
            }

            //newBorder = Instantiate(boundery, currentPosition, tile.transform.rotation, transform);
            Instantiate(boundery, currentPosition, tile.transform.rotation, transform);
            currentPosition.x += xOffset;
            currentPosition.y = startPos.y;
        }
    }

    public Sprite GetNewSprite()
    {
        List<Sprite> possibleCharacters = new List<Sprite>();
        possibleCharacters.AddRange(characters);

        Sprite newSprite = possibleCharacters[Random.Range(0, possibleCharacters.Count)];
        return newSprite;
    }

    private void Update()
    {
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            if (movingCount == 0)
            {
                ProcessTouch();
            }
        }
        else
        {
            if (movingCount == 0)
            {
                ProcessMouseInput();
            }
        }
    }

    private void ProcessMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            touchOrigin = Input.mousePosition;
            worldPosition = Camera.main.ScreenToWorldPoint(touchOrigin);
            touchCollider = Physics2D.OverlapPoint(worldPosition, maskTile);
            if (touchCollider)
            {
                if (selectedTile == null)
                {
                    selectedTile = touchCollider.transform.gameObject;
                    int x = selectedTile.GetComponent<Tile>().positionInArray.x;
                    int y = selectedTile.GetComponent<Tile>().positionInArray.y;
                    Debug.Log("X: " + x + " - Y: " + y);
                    Debug.Log("Valor do Array: " + tiles[x, y]);
                    Debug.Log("Objeto igual ao array: " + (selectedTile == tiles[x, y]));
                    selectedTile.GetComponent<Tile>().ToogleTileSelection();
                }
                else
                {
                    GameObject newSelectedTile = touchCollider.transform.gameObject;
                    selectedTile.GetComponent<Tile>().ToogleTileSelection();
                    SwitchTiles(selectedTile, newSelectedTile);

                    selectedTile = null;
                }
            }
        }
    }

    private void ProcessTouch()
    {
        if (Input.touchCount > 0)
        {
            Touch myTouch = Input.touches[0];
            if (myTouch.phase == TouchPhase.Began)
            {
                touchOrigin = myTouch.position;
                worldPosition = Camera.main.ScreenToWorldPoint(touchOrigin);
                touchCollider = Physics2D.OverlapPoint(worldPosition, maskTile);
                if (touchCollider)
                {
                    selectedTile = touchCollider.transform.gameObject;
                    selectedTile.GetComponent<Tile>().ToogleTileSelection();
                }
            }
            else if (myTouch.phase == TouchPhase.Ended)
            {
                if (selectedTile != null)
                {
                    selectedTile.GetComponent<Tile>().ToogleTileSelection();
                    Vector2 touchEnd = myTouch.position;
                    float xAbs = Mathf.Abs(touchEnd.x - touchOrigin.x);
                    float yAbs = Mathf.Abs(touchEnd.y - touchOrigin.y);

                    directionAxis = Vector2.zero;
                    if (xAbs > yAbs)
                    {
                        directionAxis.x = touchEnd.x - touchOrigin.x;
                    }
                    else
                    {
                        directionAxis.y = touchEnd.y - touchOrigin.y;
                    }
                    
                    GameObject adjacentObject = selectedTile.GetComponent<Tile>().GetAdjacent(directionAxis);
                    if (adjacentObject)
                    {
                        SwitchTiles(selectedTile, adjacentObject);
                    }
                }
            }
        }
    }

    private void ProcessMove(GameObject selectedTile, Vector2 direction)
    {
        if (direction.x > 0.0f)
        {
            // Move para direita
            //selectedTile.GetComponent<Tile>().SwapTile(Vector2.right);
            texto.text = "DIREITA";
        }
        else if (direction.x < 0.0f)
        {
            // Move para esquerda
            //selectedTile.GetComponent<Tile>().SwapTile(Vector2.left);
            texto.text = "ESQUERDA";
        }
        else if (direction.y > 0.0f)
        {
            // Move para cima
            //selectedTile.GetComponent<Tile>().SwapTile(Vector2.up);
            texto.text = "CIMA";
        }
        else if (direction.y < 0.0f)
        {
            // Move para baixo
            //selectedTile.GetComponent<Tile>().SwapTile(Vector2.down);
            texto.text = "BAIXO";
        }
    }

    private void ShuffleTiles(GameObject[] tilesList)
    {
        foreach (GameObject tile in tilesList)
        {
            List<Sprite> possibleCharacters = new List<Sprite>();
            possibleCharacters.AddRange(characters);

            Sprite newSprite = possibleCharacters[Random.Range(0, possibleCharacters.Count)];
            tile.GetComponent<SpriteRenderer>().sprite = newSprite;
            while (tile.GetComponent<Tile>().FindAllMatches().Count >= 2)
            {
                possibleCharacters.Remove(newSprite);
                newSprite = possibleCharacters[Random.Range(0, possibleCharacters.Count)];
                tile.GetComponent<SpriteRenderer>().sprite = newSprite;
            }
        }
    }

    private void SwitchTiles(GameObject tileA, GameObject tileB, bool clearAfterMove = true)
    {
        List<GameObject> adjacentTiles = tileA.GetComponent<Tile>().GetAllAdjacentTiles();
        if (!adjacentTiles.Contains(tileB)) return;
        
        Tile tileAObj = tileA.GetComponent<Tile>();
        Tile tileBObj = tileB.GetComponent<Tile>();
        int[] newPositionInArrayTileA = new int[2];
        int[] newPositionInArrayTileB = new int[2];
        newPositionInArrayTileA[0] = tileBObj.positionInArray.x;
        newPositionInArrayTileA[1] = tileBObj.positionInArray.y;
        newPositionInArrayTileB[0] = tileAObj.positionInArray.x;
        newPositionInArrayTileB[1] = tileAObj.positionInArray.y;

        tileBObj.positionInArray = new Tile.IntVector2(newPositionInArrayTileB[0], newPositionInArrayTileB[1]);
        tileAObj.positionInArray = new Tile.IntVector2(newPositionInArrayTileA[0], newPositionInArrayTileA[1]);

        tiles[tileAObj.positionInArray.x, tileAObj.positionInArray.y] = tileA;
        tiles[tileBObj.positionInArray.x, tileBObj.positionInArray.y] = tileB;

        Vector3 newPositionTileA = tileBObj.fixedPosition;
        Vector3 newPositionTileB = tileAObj.fixedPosition;

        tileAObj.fixedPosition = newPositionTileA;
        tileBObj.fixedPosition = newPositionTileB;

        tileAObj.TransportTile(newPositionTileA);
        tileBObj.TransportTile(newPositionTileB);
        if (autoMatch && clearAfterMove)
        {
            StartCoroutine(ClearMatchesForSwitchTile(tileAObj, tileBObj));
        }
    }
        
    private IEnumerator ClearMatchesForSwitchTile(Tile tileA, Tile tileB)
    {
        yield return new WaitUntil(() => movingCount == 0);

        List<Tile> matchingTiles = new List<Tile>();

        matchingTiles.AddRange(tileA.ClearMatch(new Vector2[2] { Vector2.left, Vector2.right }));
        matchingTiles.AddRange(tileA.ClearMatch(new Vector2[2] { Vector2.up, Vector2.down }));
        matchingTiles.AddRange(tileB.ClearMatch(new Vector2[2] { Vector2.left, Vector2.right }));
        matchingTiles.AddRange(tileB.ClearMatch(new Vector2[2] { Vector2.up, Vector2.down }));

        if (matchingTiles.Count > 0)
        {
            tiles[tileA.positionInArray.x, tileA.positionInArray.y] = tileA.gameObject;
            tiles[tileB.positionInArray.x, tileB.positionInArray.y] = tileB.gameObject;
            SFXManager.instance.PlaySFX(Clip.Clear);
            for (int i = 0; i < matchingTiles.Count; i++)
            {
                GUIManager.instance.Score += matchingTiles[i].points;
                matchingTiles[i].DestroyTile();
            }
            GUIManager.instance.MoveCounter--;
            ClearForPlunge();
        }
        else
        {
            missCount++;
            StartCoroutine(UndoSwitch(tileA.gameObject, tileB.gameObject));
        }
    }
        
    private IEnumerator UndoSwitch(GameObject tileA, GameObject tileB)
    {
        tileA.GetComponent<Tile>().ToogleTileSelection();
        tileB.GetComponent<Tile>().ToogleTileSelection();
        yield return new WaitForSeconds(0.3f);
        SFXManager.instance.PlaySFX(Clip.Swap);
        tileA.GetComponent<Tile>().ToogleTileSelection();
        tileB.GetComponent<Tile>().ToogleTileSelection();
        SwitchTiles(tileA.gameObject, tileB.gameObject, false);
    }

    public void ClearForPlunge()
    {
        deletedCount = 0;
    }

    public void Plunge()
    {
        StartCoroutine(WaitForPlunge());
    }

    private IEnumerator WaitForPlunge()
    {
        if (deletedCount > 1)
        {
            yield return 0;
        }
        else
        {
            yield return new WaitUntil(() => deletedCount == 0);
            PlungeAll();
        }
    }

    private void PlungeAll()
    {
        Debug.Log("Plunge");
        int countEmpty = 0;
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                if (!tiles[x, y])
                {
                    countEmpty++;
                }
            }
            if (countEmpty > 0)
            {
                PlungeCol(x);
            }
            countEmpty = 0;
        }
    }

    private void PlungeCol(int x)
    {
        List<GameObject> movedTiles = new List<GameObject>();
        bool foundTile;
        Vector3 endPosition;
        GameObject newTile;
        Tile newTileObj;
        
        for (int y = ySize - 1; y >= 0; y--)
        {
            if (tiles[x, y] == null)
            {
                foundTile = false;
                endPosition = positions[x, y];
                newTile = null;

                for (int tmpY = y - 1; tmpY >= 0 && !foundTile; tmpY--)
                {
                    newTile = tiles[x, tmpY];
                    if (newTile != null)
                    {
                        foundTile = true;
                        tiles[x, tmpY] = null;
                    }
                }

                if (!foundTile)
                {
                    newTile = spawners[x].GetComponent<TileSpawner>().SpawnAndReturnTile();
                }

                newTileObj = newTile.GetComponent<Tile>();
                newTileObj.fixedPosition = endPosition;
                newTileObj.positionInArray = new Tile.IntVector2(x, y);
                tiles[x, y] = newTile;

                newTileObj.TransportTile(endPosition);
                movedTiles.Add(newTile);
            }
        }

        if (autoMatch)
        {
            for(int i = 0; i < movedTiles.Count; i++)
            {
                StartCoroutine(movedTiles[i].GetComponent<Tile>().ClearAllMatches());
            }
        }
    }
}
