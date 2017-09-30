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
using System.Collections;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour {
    public static BoardManager instance;
    public List<Sprite> characters = new List<Sprite>();
    public GameObject tileBackground;
    public GameObject tile;
    public int xSize, ySize;

    private enum CellType { Invalid = -1, Valid = -2, Spawn = -3, Random = -4 };

    private GameObject[,] tiles;
    private int[,] board;

    public bool IsShifting { get; set; }
    public float tileMoveSpeed = .3f;
    public float tilePlungeSpeed = .1f;

    void Start() {
        instance = GetComponent<BoardManager>();

        board = new int[,] {    { -1, -3, -3, -1, -3, -3, -1 },
                                { -3, -4, -4, -3, -4, -4, -3 },
                                { -4, -4, -4, -4, -4, -4, -4 },
                                { -4, -4, -4, -4, -4, -4, -4 },
                                { -4, -4, -4, -4, -4, -4, -4 },
                                { -1, -4, -4, -1, -4, -4, -1 },
                                { -4, -4, -4, -4, -4, -4, -4 },
                                { -4, -4, -4, -4, -4, -4, -4 },
                                { -4, -4, -4, -4, -4, -4, -4 },
                                { -1, 2, 2, 2, -4, -4, -1 },
        };

		Vector2 offset = tile.GetComponent<SpriteRenderer>().bounds.size;
        CreateBoard(offset.x, offset.y);
    }

    public void SpawnTile(Vector3 position)
    {
        Debug.Log("Spawn Tile em: " + position);
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.up);
        if (hit.collider != null)
        {
            hit.collider.gameObject.GetComponent<Tile>().Plunge();
        }
    }

    private void CreateBoard (float xOffset, float yOffset)
    {        
        Vector2 startPos = new Vector2()
        {
            x = transform.position.x - ((board.GetLength(1) / 2.0f) * xOffset) + (xOffset / 2.0f),
            y = board.GetLength(0) / 2.0f * yOffset + transform.position.y
        };
        Vector2 currPos = startPos;
        int slot;        
        for (int x = 0; x < board.GetLength(0); x++)
        {
            currPos.x = startPos.x;
            for (int y = 0; y < board.GetLength(1); y++)
            {
                slot = board[x,y];
                if (slot != (int)CellType.Invalid)
                {
                    GameObject newTile = Instantiate(tile, new Vector3(currPos.x, currPos.y), tile.transform.rotation);
                    if (slot == (int)CellType.Random)
                    {
                        newTile.GetComponent<SpriteRenderer>().sprite = characters[Random.Range(0, characters.Count)];
                    }
                    else if (slot >= 0)
                    {
                        newTile.GetComponent<SpriteRenderer>().sprite = characters[slot];
                    }
                }
                currPos.x += xOffset;
            }
            currPos.y -= yOffset;
        }
    }
    
    private void CreateBoard_old (float xOffset, float yOffset) {
		tiles = new GameObject[xSize, ySize];
        Debug.Log("Largura padrao: " + xOffset);
        Sprite[] previousLeft = new Sprite[ySize];
        Sprite previousBelow = null;

        float startX = transform.position.x;
		float startY = transform.position.y;

		for (int x = 0; x < xSize; x++) {
			for (int y = 0; y < ySize; y++) {
				GameObject newTile = Instantiate(tile, new Vector3(startX + (xOffset * x), startY + (yOffset * y), 0), tile.transform.rotation);
				tiles[x, y] = newTile;

                List<Sprite> possibleCharacters = new List<Sprite>();
                possibleCharacters.AddRange(characters);
                possibleCharacters.Remove(previousLeft[y]);
                possibleCharacters.Remove(previousBelow);

                newTile.transform.parent = transform;
                Sprite newSprite = possibleCharacters[Random.Range(0, possibleCharacters.Count)];
                newTile.GetComponent<SpriteRenderer>().sprite = newSprite;
                Debug.Log("Largura: " + newSprite.bounds.size.x);
                previousBelow = newSprite;
                previousLeft[y] = newSprite;
			}
        }
    }

    public IEnumerator FindNullTiles()
    {
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                if (tiles[x, y].GetComponent<SpriteRenderer>().sprite == null)
                {
                    yield return StartCoroutine(ShiftTilesDown(x, y));
                    break;
                }
            }
        }

        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                tiles[x, y].GetComponent<Tile>().ClearAllMatches();
            }
        }
    }

    public void PlungeTiles()
    {

    }

    private IEnumerator ShiftTilesDown(int x, int yStart, float shiftDelay = .03f)
    {
        IsShifting = true;
        List<SpriteRenderer> renders = new List<SpriteRenderer>();
        int nullCount = 0;

        for (int y = yStart; y < ySize; y++)
        {
            SpriteRenderer render = tiles[x, y].GetComponent<SpriteRenderer>();
            if (render.sprite == null)
            {
                nullCount++;
            }
            renders.Add(render);
        }

        for (int i = 0; i < nullCount; i++)
        {
            GUIManager.instance.Scrore += 50;
            yield return new WaitForSeconds(shiftDelay);
            for (int k = 0; k < renders.Count - 1; k++)
            {
                renders[k].sprite = renders[k + 1].sprite;
                renders[k + 1].sprite = GetNewSprite(x, ySize - 1);
            }
        }

        IsShifting = false;
    }

    private Sprite GetNewSprite(int x, int y)
    {
        List<Sprite> possibleCharacters = new List<Sprite>();
        possibleCharacters.AddRange(characters);

        if (x > 0)
        {
            possibleCharacters.Remove(tiles[x - 1, y].GetComponent<SpriteRenderer>().sprite);
        }
        if (x < xSize - 1)
        {
            possibleCharacters.Remove(tiles[x + 1, y].GetComponent<SpriteRenderer>().sprite);
        }
        if (y > 0)
        {
            possibleCharacters.Remove(tiles[x, y - 1].GetComponent<SpriteRenderer>().sprite);
        }

        return possibleCharacters[Random.Range(0, possibleCharacters.Count)];
    }
}
