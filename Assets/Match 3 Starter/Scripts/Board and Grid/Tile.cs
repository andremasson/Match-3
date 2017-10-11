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

public class Tile : MonoBehaviour
{
    private Vector2[] adjacentDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    public static Color selectedColor = new Color(.5f, .5f, .5f, 1.0f);
    public static Color deleteHighlightColor = Color.yellow;
    
    public SpriteRenderer render;
    public bool isSelected = false;
    public int points = 30;
    
    public bool isMoving;
    public struct IntVector2
    {
        public int x, y;
        public IntVector2(int newX, int newY)
        {
            x = newX;
            y = newY;
        }
    }

    public IntVector2 positionInArray;
    
    public Vector3 fixedPosition;
    
    void Awake() {
		render = GetComponent<SpriteRenderer>();
        isMoving = false;
    }

    public float GetAdjacentDistance(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, render.sprite.bounds.size.x, BoardManager.instance.maskTile);
        if (hit.collider != null)
        {
            return hit.distance;
        }
        return float.NaN;
    }

    public GameObject GetAdjacent(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, render.sprite.bounds.size.x, BoardManager.instance.maskTile);
        if (hit.collider != null)
        {
            return hit.collider.gameObject;
        }
        return null;
    }

    public Tile GetAdjacentTile(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, render.sprite.bounds.size.x, BoardManager.instance.maskTile);
        if (hit.collider != null)
        {
            return hit.collider.gameObject.GetComponent<Tile>();
        }
        return null;
    }

    public List<GameObject> GetAllAdjacentTiles()
    {
        List<GameObject> adjacentTiles = new List<GameObject>();
        for (int i = 0; i < adjacentDirections.Length; i++)
        {
            adjacentTiles.Add(GetAdjacent(adjacentDirections[i]));
        }
        return adjacentTiles;
    }

    public List<Tile> FindMatch(Vector2 castDir)
    {
        List<Tile> matchingTiles = new List<Tile>();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);
        while (hit.collider != null && hit.collider.gameObject.tag == "Tile" && hit.collider.GetComponent<SpriteRenderer>().sprite == render.sprite)
        {
            matchingTiles.Add(hit.collider.gameObject.GetComponent<Tile>());
            hit = Physics2D.Raycast(hit.collider.transform.position, castDir);
        }
        return matchingTiles;
    }

    public List<Tile> FindAllMatches()
    {
        List<Tile> matchingTiles = new List<Tile>();
        Vector2[] directions = new Vector2[4] { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        for (int i = 0; i < directions.Length; i++)
        {
            matchingTiles.AddRange(FindMatch(directions[i]));
        }
        return matchingTiles;
    }

    public void ToogleTileSelection()
    {
        isSelected = !isSelected;
        if (isSelected)
        {
            render.color = Tile.selectedColor;
            SFXManager.instance.PlaySFX(Clip.Select);
        }
        else
        {
            render.color = Color.white;
        }
    }

    public void DestroyTile()
    {
        BoardManager.instance.deletedCount++;
        StartCoroutine(AnimateDestroyTile());
    }

    private IEnumerator AnimateDestroyTile()
    {
        render.color = deleteHighlightColor;
        yield return new WaitForSeconds(BoardManager.instance.deleteHighlightTime);
        
        BoardManager.instance.tiles[positionInArray.x, positionInArray.y] = null;
        BoardManager.instance.Plunge();
        Destroy(gameObject);
    }

    public List<Tile> ClearMatch(Vector2[] paths)
    {
        List<Tile> matchingTiles = new List<Tile>();

        for (int i = 0; i < paths.Length; i++)
        {
            matchingTiles.AddRange(FindMatch(paths[i]));
        }

        if (matchingTiles.Count >= 2)
        {
            matchingTiles.Add(this);
        }
        else
        {
            matchingTiles.Clear();
        }
        return matchingTiles;
    }

    public IEnumerator ClearAllMatches()
    {
        yield return new WaitUntil(() => BoardManager.instance.movingCount == 0);

        List<Tile> matchingTiles = new List<Tile>();

        matchingTiles.AddRange(ClearMatch(new Vector2[2] { Vector2.left, Vector2.right }));
        matchingTiles.AddRange(ClearMatch(new Vector2[2] { Vector2.up, Vector2.down }));

        if (matchingTiles.Count > 0)
        {
            SFXManager.instance.PlaySFX(Clip.Clear);
            for (int i = 0; i < matchingTiles.Count; i++)
            {
                GUIManager.instance.Score += matchingTiles[i].points;
                matchingTiles[i].DestroyTile();
            }
            BoardManager.instance.ClearForPlunge();
        }
    }

    private IEnumerator AnimateMove( Vector3 endPosition)
    {
        Vector3 startPosition = transform.position;
        if (!isMoving)
        {
            isMoving = true;
            float delta = 0.0f;
            float rate = 1 / BoardManager.instance.tileMoveSpeed;
            while (delta < 1)
            {
                delta += Time.deltaTime * rate;
                transform.position = Vector3.Lerp(startPosition, endPosition, delta);
                yield return 0;
            }
            isMoving = false;
            BoardManager.instance.movingCount--;
        }

        yield return 0;
    }

    public void TransportTile(Vector3 endPosition)
    {
        BoardManager.instance.movingCount++;
        StartCoroutine(AnimateMove(endPosition));
    }

    public bool CanMoveTo(Vector2 direction)
    {
        // Dummy for now
        return true;
    }
}