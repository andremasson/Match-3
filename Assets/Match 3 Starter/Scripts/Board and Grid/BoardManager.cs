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

    public Text texto;

    // Private
    public GameObject[,] tiles;
    public Vector3[,] positions;
    public GameObject[] spawners;

    private Vector2 touchOrigin;
    private GameObject selectedTile;
    private LayerMask maskTile = 1 << 8;

    private Vector2 directionAxis = Vector2.zero;
    private Collider2D touchCollider;
    private Vector3 worldPosition;

    private int movingCount = 0;
    private int deletedCount = 0;

    private Vector2[] adjacentDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
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
                    ToogleTileSelection(selectedTile);
                }
                else
                {
                    GameObject newSelectedTile = touchCollider.transform.gameObject;
                    ToogleTileSelection(selectedTile);
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
                    ToogleTileSelection(selectedTile);
                }
            }
            else if (myTouch.phase == TouchPhase.Ended)
            {
                if (selectedTile != null)
                {
                    ToogleTileSelection(selectedTile);
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
                    
                    GameObject adjacentObject = GetAdjacentTile(selectedTile.transform, directionAxis);
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
        List<GameObject> adjacentTiles = GetAllAdjacentTiles(tileA.transform);
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
        
        movingCount += 2;
        StartCoroutine(AnimateMove(tileAObj, newPositionTileA));
        StartCoroutine(AnimateMove(tileBObj, newPositionTileB));
        if (autoMatch && clearAfterMove)
        {
            GUIManager.instance.MoveCounter--;
            StartCoroutine(ClearMatchesForSwitchTile(tileAObj, tileBObj));
        }
    }

    private void TransportTile(Tile tile, Vector3 endPosition)
    {
        movingCount++;
        StartCoroutine(AnimateMove(tile, endPosition));
    }

    private IEnumerator AnimateMove(Tile tile, Vector3 endPosition)
    {
        Vector3 startPosition = tile.transform.position;
        if (!tile.isMoving)
        {
            tile.isMoving = true;
            float delta = 0.0f;
            float rate = 1 / tileMoveSpeed;
            while (delta < 1)
            {
                delta += Time.deltaTime * rate;
                tile.transform.position = Vector3.Lerp(startPosition, endPosition, delta);
                yield return 0;
            }
            tile.isMoving = false;
            movingCount--;            
        }

        yield return 0;
    }

    private void ToogleTileSelection(GameObject tile)
    {
        Tile tileObj = tile.GetComponent<Tile>();
        tileObj.isSelected = !tileObj.isSelected;
        if (tileObj.isSelected)
        {
            tile.GetComponent<SpriteRenderer>().color = Tile.selectedColor;
            SFXManager.instance.PlaySFX(Clip.Select);
        }
        else
        {
            tile.GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    private bool CanMoveTo(GameObject tile, Vector2 direction)
    {
        return true;
    }
    
    private List<Tile> GetAllAdjacentTiles(Tile tile)
    {
        List<Tile> adjacentTiles = new List<Tile>();
        for (int i = 0; i < adjacentDirections.Length; i++)
        {
            adjacentTiles.Add(GetAdjacentTile(tile, adjacentDirections[i]));
        }
        return adjacentTiles;
    }

    private List<GameObject> GetAllAdjacentTiles(Transform transform)
    {
        List<GameObject> adjacentTiles = new List<GameObject>();
        for (int i = 0; i < adjacentDirections.Length; i++)
        {
            adjacentTiles.Add(GetAdjacentTile(transform, adjacentDirections[i]));
        }
        return adjacentTiles;
    }

    private GameObject GetAdjacentTile(Transform transform, Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, tiles[0, 0].GetComponent<SpriteRenderer>().sprite.bounds.size.x, maskTile);
        if (hit.collider != null)
        {
            return hit.collider.gameObject;
        }
        return null;
    }

    private Tile GetAdjacentTile(Tile tile, Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(tile.transform.position, castDir, tiles[0, 0].GetComponent<SpriteRenderer>().sprite.bounds.size.x, maskTile);
        if (hit.collider != null)
        {
            return hit.collider.gameObject.GetComponent<Tile>();
        }
        return null;
    }

    private IEnumerator ClearMatchesForSwitchTile(Tile tileA, Tile tileB)
    {
        yield return new WaitUntil(() => movingCount == 0);

        List<Tile> matchingTiles = new List<Tile>();

        matchingTiles.AddRange(ClearMatch(tileA, new Vector2[2] { Vector2.left, Vector2.right }));
        matchingTiles.AddRange(ClearMatch(tileA, new Vector2[2] { Vector2.up, Vector2.down }));
        matchingTiles.AddRange(ClearMatch(tileB, new Vector2[2] { Vector2.left, Vector2.right }));
        matchingTiles.AddRange(ClearMatch(tileB, new Vector2[2] { Vector2.up, Vector2.down }));

        if (matchingTiles.Count > 0)
        {
            tiles[tileA.positionInArray.x, tileA.positionInArray.y] = tileA.gameObject;
            tiles[tileB.positionInArray.x, tileB.positionInArray.y] = tileB.gameObject;
            SFXManager.instance.PlaySFX(Clip.Clear);
            for (int i = 0; i < matchingTiles.Count; i++)
            {
                GUIManager.instance.Score += matchingTiles[i].points;
                DestroyTile(matchingTiles[i]);
            }
            ClearForPlunge();
        }
        else
        {
            StartCoroutine(UndoSwitch(tileA.gameObject, tileB.gameObject));
        }
    }

    private IEnumerator ClearAllMatches(Tile tile)
    {
        yield return new WaitUntil(() => movingCount == 0);

        List<Tile> matchingTiles = new List<Tile>();

        matchingTiles.AddRange(ClearMatch(tile, new Vector2[2] { Vector2.left, Vector2.right }));
        matchingTiles.AddRange(ClearMatch(tile, new Vector2[2] { Vector2.up, Vector2.down }));

        if (matchingTiles.Count > 0)
        {
            tiles[tile.positionInArray.x, tile.positionInArray.y] = tile.gameObject;
            SFXManager.instance.PlaySFX(Clip.Clear);
            for (int i = 0; i < matchingTiles.Count; i++)
            {
                GUIManager.instance.Score += matchingTiles[i].points;
                DestroyTile(matchingTiles[i]);
            }
            ClearForPlunge();
        }
    }

    private List<Tile> ClearMatch(Tile tile, Vector2[] paths)
    {
        List<Tile> matchingTiles = new List<Tile>();

        for (int i = 0; i < paths.Length; i++)
        {
            matchingTiles.AddRange(FindMatch(tile, paths[i]));
        }

        if (matchingTiles.Count >= 2)
        {   
            matchingTiles.Add(tile);
        }
        else
        {
            matchingTiles.Clear();
        }
        return matchingTiles;
    }
    
    private List<Tile> FindMatch(Tile tile, Vector2 castDir)
    {
        List<Tile> matchingTiles = new List<Tile>();
        if (tile != null)
        {
            RaycastHit2D hit = Physics2D.Raycast(tile.transform.position, castDir, tiles[0, 0].GetComponent<SpriteRenderer>().sprite.bounds.size.x, maskTile);
            while (hit && hit.collider.gameObject.GetComponent<SpriteRenderer>().sprite == tile.render.sprite)
            {
                matchingTiles.Add(hit.collider.gameObject.GetComponent<Tile>());
                hit = Physics2D.Raycast(hit.collider.transform.position, castDir, tiles[0, 0].GetComponent<SpriteRenderer>().sprite.bounds.size.x, maskTile);
            }
        }
        return matchingTiles;
    }

    private List<Tile> FindAllMatches(Tile tile)
    {
        List<Tile> matchingTiles = new List<Tile>();
        for(int i = 0; i < adjacentDirections.Length; i++)
        {
            matchingTiles.AddRange(FindMatch(tile, adjacentDirections[i]));
        }
        return matchingTiles;
    }

    private IEnumerator UndoSwitch(GameObject tileA, GameObject tileB)
    {
        ToogleTileSelection(tileA);
        ToogleTileSelection(tileB);
        yield return new WaitForSeconds(0.3f);
        SFXManager.instance.PlaySFX(Clip.Swap);
        ToogleTileSelection(tileA);
        ToogleTileSelection(tileB);
        SwitchTiles(tileA.gameObject, tileB.gameObject, false);
    }

    private void DestroyTile(Tile tile)
    {
        if (tile && tile.gameObject)
        {
            deletedCount++;
            StartCoroutine(AnimateDestroyTile(tile));
        }
    }

    private IEnumerator AnimateDestroyTile(Tile tile)
    {
        tile.GetComponent<SpriteRenderer>().color = Tile.deleteHighlightColor;
        yield return new WaitForSeconds(deleteHighlightTime);
        if (tile)
        {
            tiles[tile.positionInArray.x, tile.positionInArray.y] = null;
            Destroy(tile.gameObject);
        }
        StartCoroutine(WaitForPlunge());
    }

    private void ClearForPlunge()
    {
        deletedCount = 0;
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
            Plunge();
        }
    }

    private void Plunge()
    {
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

                TransportTile(newTileObj, endPosition);
                movedTiles.Add(newTile);
            }
        }

        if (autoMatch)
        {
            for(int i = 0; i < movedTiles.Count; i++)
            {
                StartCoroutine(ClearAllMatches(movedTiles[i].GetComponent<Tile>()));
            }
        }
    }
}
